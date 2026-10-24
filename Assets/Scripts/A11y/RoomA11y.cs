using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RDOnline.A11y;
using RDOnline.Auth;
using RDOnline.Component;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间（ScnRoom）的无障碍导航，全场景唯一的键盘所有者。
    ///
    /// 组的划分：房间信息、玩家列表（每人一组：整条信息＋房主操作）、
    /// 谱面（试听／下载状态／重试）、准备、房间设置、离开。
    /// 编辑面板和成绩面板打开时各自并成若干额外组。
    ///
    /// 房间里的玩家由 room/sync、room/playerJoin 等随时变化，所以这里
    /// 定期比对新旧控件，变了才重建，重建靠组名保住用户原来停的位置。
    /// 不写死任何屏幕坐标和尺寸，文字一律读控件自身的文本。
    /// </summary>
    public class RoomA11y : MonoBehaviour
    {
        public static RoomA11y Instance { get; private set; }

        private class Group
        {
            public string Name;
            public readonly List<A11yNavItem> Items = new List<A11yNavItem>();
        }

        private A11yNavigator _nav;
        private PlayerListUI _playerList;
        private ReadyManager _ready;
        private RoomManager _room;
        private RoomUpdater _updater;
        private RoomEditPanelController _editPanel;
        private ScorePanelController _scorePanel;
        private RoomChartPreview _chartPreview;
        private RoomLeaver _leaver;
        private ChatManager _chat;
        private UserInfoUI _userInfo;

        /// <summary>上一条已朗读的聊天消息，用来只报新到的那条。</summary>
        private GameObject _lastChatItem;

        /// <summary>上一条已朗读的在线人数，变了才再读。</summary>
        private string _lastOnline;

        private readonly List<Group> _groups = new List<Group>();
        private readonly List<GameObject> _knownRoots = new List<GameObject>();

        private bool _introSpoken;
        private bool _wasEditOpen;
        private bool _wasScoreOpen;

        /// <summary>场景加载完成后自动挂载，不写任何场景文件。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 只关心带玩家列表的场景；别的大厅、登录各有自己的控制器
            if (Object.FindObjectOfType<PlayerListUI>() == null) return;
            if (Instance != null) return;

            var go = new GameObject("RoomA11y");
            go.AddComponent<RoomA11y>();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (_nav != null) _nav.Deactivate();
        }

        private void Start()
        {
            _nav = gameObject.AddComponent<A11yNavigator>();
            _nav.ListName = A11yPhrases.Text("房间", "Room");
            _nav.BackAction = OnBack;

            RefreshRefs();
            Rebuild(true);

            if (_chat != null && _chat.MessageListContent != null && _chat.MessageListContent.childCount > 0)
            {
                Transform content = _chat.MessageListContent;
                _lastChatItem = content.GetChild(content.childCount - 1).gameObject;
            }
        }

        private void Update()
        {
            RefreshRefs();

            // 消息要尽快读出来，不受下面半秒扫描的节流
            WatchChat();
            WatchOnline();

            // 玩家、准备状态、面板开合都由网络与按钮改变，半秒足够跟上，
            // 同时避免每帧建对象
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.5f;

            bool editOpen = EditPanelOpen;
            bool scoreOpen = ScorePanelOpen;

            List<Group> groups = BuildGroups();
            if (editOpen != _wasEditOpen || scoreOpen != _wasScoreOpen || !SameRoots(groups))
            {
                _wasEditOpen = editOpen;
                _wasScoreOpen = scoreOpen;
                Apply(groups);

                // 有面板刚弹出来，焦点要跟过去；收起时回原位
                Rebuild(editOpen || scoreOpen);
            }
        }

        private float _nextScan;

        // ---------- 引用 ----------

        private void RefreshRefs()
        {
            if (_playerList == null) _playerList = Object.FindObjectOfType<PlayerListUI>();
            if (_ready == null) _ready = Object.FindObjectOfType<ReadyManager>();
            if (_room == null) _room = Object.FindObjectOfType<RoomManager>();
            if (_updater == null) _updater = Object.FindObjectOfType<RoomUpdater>();
            if (_editPanel == null) _editPanel = Object.FindObjectOfType<RoomEditPanelController>();
            if (_scorePanel == null) _scorePanel = ScorePanelController.Instance;
            if (_chartPreview == null) _chartPreview = Object.FindObjectOfType<RoomChartPreview>();
            if (_leaver == null) _leaver = Object.FindObjectOfType<RoomLeaver>();
            if (_chat == null) _chat = Object.FindObjectOfType<ChatManager>();
            if (_userInfo == null) _userInfo = Object.FindObjectOfType<UserInfoUI>();
        }

        // ---------- 组装 ----------

        private List<Group> BuildGroups()
        {
            var groups = new List<Group>();
            var used = new HashSet<GameObject>();

            AddInfoGroup(groups);
            AddUserInfoGroup(groups);
            AddPlayerGroups(groups, used);
            AddChartGroups(groups, used);
            AddActionGroups(groups, used);
            AddEditPanelGroups(groups, used);
            AddScoreGroups(groups, used);
            AddChatGroups(groups, used);
            AddExtras(groups, used);

            return groups;
        }

        /// <summary>房间信息整条读一句，不占焦点以外的位置。</summary>
        /// <summary>
        /// 用户自己那一行：用户名、延迟、在线人数。都是屏幕上看得见的字，
        /// 所以逐条列出来让人上下光标听，但它们不是按钮，不占其他位置。
        /// </summary>
        private void AddUserInfoGroup(List<Group> groups)
        {
            if (_userInfo == null) return;

            var group = new Group { Name = A11yPhrases.Text("我的信息", "My info") };
            UserInfoSpeech.AddItems(group.Items, _userInfo);

            if (group.Items.Count > 0) groups.Add(group);
        }

        /// <summary>在线人数变化时直接读一句，不抢焦点。</summary>
        private void WatchOnline()
        {
            if (_userInfo == null) return;

            string speech = UserInfoSpeech.WatchOnline(_userInfo, ref _lastOnline);
            if (string.IsNullOrEmpty(speech)) return;

            A11yNavigator.SayInfo(speech, NarrationCategory.Notification);
        }

        private void AddInfoGroup(List<Group> groups)
        {
            var group = new Group { Name = A11yPhrases.Text("房间信息", "Room info") };

            group.Items.Add(A11yNavItem.Custom(
                _room != null ? _room.gameObject : gameObject,
                A11yItemKind.Label,
                InfoSpeech(),
                () => InfoSpeech(),
                null));

            groups.Add(group);
        }

        private string InfoSpeech()
        {
            var parts = new List<string>();

            if (_room != null && _room.RoomNameText != null && !string.IsNullOrEmpty(_room.RoomNameText.text))
                parts.Add(_room.RoomNameText.text);

            int players = CountPlayers();
            if (players > 0)
                parts.Add(A11yPhrases.Text($"玩家 {players} 人", $"{players} players"));

            if (_chartPreview != null && _chartPreview.SongNameText != null &&
                !string.IsNullOrEmpty(_chartPreview.SongNameText.text))
            {
                string song = _chartPreview.SongNameText.text;
                if (_chartPreview.AuthorText != null && !string.IsNullOrEmpty(_chartPreview.AuthorText.text))
                    song += A11yPhrases.Separator + _chartPreview.AuthorText.text;
                parts.Add(A11yPhrases.Text("谱面", "Chart") + A11yPhrases.Separator + song);
            }

            return parts.Count > 0
                ? string.Join(A11yPhrases.Separator, parts)
                : A11yPhrases.Text("房间", "Room");
        }

        private int CountPlayers()
        {
            if (_playerList == null || _playerList.PlayerListContent == null) return 0;

            int n = 0;
            Transform content = _playerList.PlayerListContent;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child != null && child.gameObject.activeSelf &&
                    child.GetComponent<PlayerItem>() != null) n++;
            }
            return n;
        }

        /// <summary>
        /// 玩家列表：一个人一组。组内第一项是整条信息，房主看到的
        /// 「转让房主」「踢出」紧跟在后面，左右光标切换。
        /// </summary>
        private void AddPlayerGroups(List<Group> groups, HashSet<GameObject> used)
        {
            if (_playerList == null || _playerList.PlayerListContent == null) return;

            Transform content = _playerList.PlayerListContent;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child == null || !child.gameObject.activeSelf) continue;

                var item = child.GetComponent<PlayerItem>();
                if (item == null) continue;

                var group = new Group { Name = PlayerName(item) };

                group.Items.Add(A11yNavItem.Custom(
                    item.gameObject,
                    A11yItemKind.Label,
                    PlayerSpeech(item),
                    () => PlayerSpeech(item),
                    null));
                used.Add(item.gameObject);

                AddIfVisible(group, item.TransferOwnerButton, used,
                    A11yPhrases.Text("转让房主", "Transfer ownership"));
                AddIfVisible(group, item.KickButton, used,
                    A11yPhrases.Text("踢出该玩家", "Kick this player"));

                groups.Add(group);
            }
        }

        private static string PlayerName(PlayerItem item)
        {
            if (item != null && item.UsernameText != null && !string.IsNullOrEmpty(item.UsernameText.text))
                return item.UsernameText.text;
            return A11yPhrases.Text("玩家", "Player");
        }

        private string PlayerSpeech(PlayerItem item)
        {
            if (item == null) return A11yPhrases.Text("玩家", "Player");

            var parts = new List<string> { PlayerName(item) };

            if (item.OwnerIcon != null && item.OwnerIcon.gameObject.activeInHierarchy)
                parts.Add(A11yPhrases.Text("房主", "owner"));

            bool ready = item.ReadyIcon != null && item.ReadyIcon.gameObject.activeInHierarchy;
            parts.Add(ready ? A11yPhrases.Text("已准备", "ready") : A11yPhrases.Text("未准备", "not ready"));

            return string.Join(A11yPhrases.Separator, parts);
        }

        /// <summary>
        /// 谱面：试听按钮、下载状态、重试按钮。进度读成「下载中」加百分比，
        /// 不读出裸数字。
        /// </summary>
        private void AddChartGroups(List<Group> groups, HashSet<GameObject> used)
        {
            var group = new Group { Name = A11yPhrases.Text("谱面", "Chart") };

            if (_chartPreview != null && _chartPreview.PlayButton != null)
            {
                var play = _chartPreview.PlayButton;
                group.Items.Add(A11yNavItem.Custom(
                    play.gameObject,
                    A11yItemKind.Button,
                    A11yPhrases.Text("试听", "Preview"),
                    () => A11yPhrases.Text("试听", "Preview") +
                          (_chartPreview.IsPlaying
                              ? A11yPhrases.Text("，正在播放", ", playing")
                              : string.Empty),
                    () => play.onClick.Invoke()));
                used.Add(play.gameObject);
            }

            // 下载链的状态朗读：进度条只有 0..1，交给 ChartDownloader 自己报
            if (ChartDownloader.Instance != null)
            {
                var downloader = ChartDownloader.Instance;
                if (downloader.ProgressBar != null && downloader.ProgressBar.gameObject.activeInHierarchy)
                {
                    group.Items.Add(A11yNavItem.Custom(
                        downloader.ProgressBar.gameObject,
                        A11yItemKind.Label,
                        A11yPhrases.Text("谱面下载", "Chart download"),
                        () => DownloadSpeech(downloader),
                        null));
                    used.Add(downloader.ProgressBar.gameObject);
                }

                AddIfVisible(group, downloader.RetryButton, used,
                    A11yPhrases.Text("重试下载", "Retry download"));
            }

            if (group.Items.Count > 0) groups.Add(group);
        }

        private static string DownloadSpeech(ChartDownloader d)
        {
            if (d == null) return string.Empty;
            if (d.IsDownloading())
            {
                float progress = d.ProgressBar != null ? d.ProgressBar.fillAmount : 0f;
                return A11yPhrases.Text("谱面下载中", "Downloading chart") +
                       A11yPhrases.Separator + Mathf.RoundToInt(progress * 100f) + "%";
            }
            return A11yPhrases.Text("谱面已就绪", "Chart ready");
        }

        /// <summary>准备、房间设置、离开。</summary>
        private void AddActionGroups(List<Group> groups, HashSet<GameObject> used)
        {
            var ready = new Group { Name = A11yPhrases.Text("准备", "Ready") };
            if (_ready != null && _ready.ReadyButton != null)
            {
                Button button = _ready.ReadyButton;
                ready.Items.Add(A11yNavItem.Custom(
                    button.gameObject,
                    A11yItemKind.Button,
                    A11yPhrases.Text("准备", "Ready"),
                    () => ReadySpeech(button),
                    () => button.onClick.Invoke()));
                used.Add(button.gameObject);
            }
            if (_ready != null && _ready.WaitingPanel != null && _ready.WaitingPanel.activeInHierarchy)
            {
                ready.Items.Add(A11yNavItem.Custom(
                    _ready.WaitingPanel.gameObject,
                    A11yItemKind.Label,
                    A11yPhrases.Text("等待其他玩家", "Waiting for others"),
                    () => A11yPhrases.Text("等待其他玩家准备", "Waiting for other players"),
                    null));
                used.Add(_ready.WaitingPanel.gameObject);
            }
            if (ready.Items.Count > 0) groups.Add(ready);

            var settings = new Group { Name = A11yPhrases.Text("房间设置", "Room settings") };
            AddIfVisible(settings, _room != null ? _room.EditRoomButton : null, used,
                A11yPhrases.Text("编辑房间", "Edit room"));
            AddIfVisible(settings, _editPanel != null ? _editPanel.ToggleButton : null, used,
                A11yPhrases.Text("房间设置", "Room settings"));
            if (settings.Items.Count > 0) groups.Add(settings);

            var leave = new Group { Name = A11yPhrases.Text("离开", "Leave") };
            AddIfVisible(leave, _leaver != null ? _leaver.LeaveButton : null, used,
                A11yPhrases.Text("离开房间", "Leave room"));
            if (leave.Items.Count > 0) groups.Add(leave);
        }

        private string ReadySpeech(Button button)
        {
            bool ready = ReadyManager.IsWantToPlay;
            string text = ready ? A11yPhrases.Text("取消准备", "Cancel ready")
                                : A11yPhrases.Text("准备", "Ready");
            if (button != null && !button.interactable) text += A11yPhrases.Unavailable;
            return text;
        }

        /// <summary>编辑面板打开时：每个控件各自一组，人数滑块才调得动。</summary>
        private void AddEditPanelGroups(List<Group> groups, HashSet<GameObject> used)
        {
            if (!EditPanelOpen || _updater == null) return;

            var nameRow = new Group { Name = A11yPhrases.Text("编辑房间", "Edit room") };
            AddInput(nameRow, _updater.RoomNameInput, used, A11yPhrases.Text("房间名", "Room name"));
            if (nameRow.Items.Count > 0) groups.Add(nameRow);

            var countRow = new Group { Name = string.Empty };
            if (_updater.PlayerCountSlider != null)
            {
                Slider slider = _updater.PlayerCountSlider;
                countRow.Items.Add(new A11yNavItem(slider,
                    A11yPhrases.Text("人数", "Players"),
                    () => A11yPhrases.Text("人数", "Players") + A11yPhrases.Separator +
                          Mathf.RoundToInt(slider.value)));
                used.Add(slider.gameObject);
            }
            if (countRow.Items.Count > 0) groups.Add(countRow);

            var passwordRow = new Group { Name = string.Empty };
            AddInput(passwordRow, _updater.PasswordInput, used,
                A11yPhrases.Text("房间密码", "Room password"));
            if (passwordRow.Items.Count > 0) groups.Add(passwordRow);

            var updateRow = new Group { Name = string.Empty };
            AddIfVisible(updateRow, _updater.UpdateButton, used,
                A11yPhrases.Text("保存修改", "Save changes"));
            if (_editPanel != null) AddIfVisible(updateRow, _editPanel.ToggleButton, used,
                A11yPhrases.Text("关闭设置", "Close settings"));
            if (updateRow.Items.Count > 0) groups.Add(updateRow);
        }

        /// <summary>成绩面板：每行成绩一条，加关闭。</summary>
        private void AddScoreGroups(List<Group> groups, HashSet<GameObject> used)
        {
            if (!ScorePanelOpen || _scorePanel == null) return;

            var scores = new Group { Name = A11yPhrases.Text("成绩", "Scores") };
            if (_scorePanel.ScoreListContent != null)
            {
                Transform content = _scorePanel.ScoreListContent;
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child == null || !child.gameObject.activeSelf) continue;

                    var text = child.GetComponentInChildren<TMP_Text>(true);
                    if (text == null) continue;

                    scores.Items.Add(new A11yNavItem(text, A11yPhrases.Text("成绩", "Score")));
                    used.Add(child.gameObject);
                }
            }
            if (scores.Items.Count > 0) groups.Add(scores);

            var close = new Group { Name = string.Empty };
            AddIfVisible(close, _scorePanel.CloseButton, used, A11yPhrases.Text("关闭", "Close"));
            if (close.Items.Count > 0) groups.Add(close);
        }

        /// <summary>
        /// 聊天。房间里只有这一个聊天面板是活的（大厅那份在场景里是关着的），
        /// 所以消息列表、输入框、发送按钮都归到这里。
        /// 消息逐条作为一个项，可以用左右光标从头翻到尾。
        /// </summary>
        private void AddChatGroups(List<Group> groups, HashSet<GameObject> used)
        {
            if (_chat == null) return;
            if (_chat.MessageListContent == null && _chat.InputField == null) return;

            var history = new Group { Name = A11yPhrases.Text("聊天消息", "Chat messages") };
            if (_chat.MessageListContent != null)
            {
                Transform content = _chat.MessageListContent;
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child == null || !child.gameObject.activeSelf) continue;

                    var text = child.GetComponentInChildren<TMP_Text>(true);
                    if (text == null || string.IsNullOrEmpty(text.text)) continue;

                    history.Items.Add(new A11yNavItem(text, A11yPhrases.Text("消息", "Message")));
                    used.Add(child.gameObject);
                }
            }

            if (history.Items.Count > 0)
            {
                groups.Add(history);
            }
            else
            {
                // 还没有人说话，给一条占位说明，用户也能确认聊天区是空的
                var empty = new Group { Name = A11yPhrases.Text("聊天消息", "Chat messages") };
                GameObject anchorGo = _chat.MessageListContent != null
                    ? _chat.MessageListContent.gameObject
                    : _chat.gameObject;
                empty.Items.Add(A11yNavItem.Custom(anchorGo, A11yItemKind.Label,
                    A11yPhrases.Text("聊天消息", "Chat messages"),
                    () => A11yPhrases.Text("还没有消息", "No messages yet"), null));
                used.Add(anchorGo);
                groups.Add(empty);
            }

            var compose = new Group { Name = A11yPhrases.Text("发言", "Say something") };
            AddInput(compose, _chat.InputField, used, A11yPhrases.Text("聊天输入框", "Chat box"));
            AddIfVisible(compose, _chat.SendButton, used, A11yPhrases.Text("发送", "Send"));
            if (compose.Items.Count > 0) groups.Add(compose);
        }

        /// <summary>
        /// 盯住消息列表：新到的一条用提示类别直接读出来，不动焦点。
        /// 消息只往末尾追加，所以记住上次看到的那一项就够了。
        /// </summary>
        private void WatchChat()
        {
            if (_chat == null || _chat.MessageListContent == null) return;

            Transform content = _chat.MessageListContent;
            if (content.childCount == 0)
            {
                _lastChatItem = null;
                return;
            }

            GameObject newest = content.GetChild(content.childCount - 1).gameObject;
            if (newest == _lastChatItem) return;

            bool hadPrevious = _lastChatItem != null;
            _lastChatItem = newest;

            // 刚进房间时列表里已有的旧消息不念，免得一进门就被灌一耳朵
            if (!hadPrevious) return;

            var text = newest.GetComponentInChildren<TMP_Text>(true);
            string message = text != null ? PlainText(text.text) : null;
            if (string.IsNullOrEmpty(message)) return;

            A11yNavigator.SayInfo(message, NarrationCategory.Notification);
        }

        /// <summary>
        /// 消息里用户名用富文本高亮，朗读前要把标签去掉，
        /// 否则旁白会念出 color 之类的内容。
        /// </summary>
        private static string PlainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string result = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", string.Empty);
            return result.Trim();
        }

        /// <summary>
        /// 兜底：场景里还没有被任何一组收进来的按钮都收进来，
        /// 保证能点的东西一个不漏。
        /// </summary>
        private void AddExtras(List<Group> groups, HashSet<GameObject> used)
        {
            var extra = new Group { Name = A11yPhrases.Text("其他按钮", "Other buttons") };

            Button[] buttons = Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                GameObject go = buttons[i].gameObject;
                if (go == null || used.Contains(go)) continue;
                if (!ScaleVisible(go.transform)) continue;
                if (IsPlayerChild(go)) continue;

                AddButton(extra, go, used);
            }

            if (extra.Items.Count > 0) groups.Add(extra);
        }

        private bool IsPlayerChild(GameObject go)
        {
            return _playerList != null && _playerList.PlayerListContent != null &&
                   go.transform.IsChildOf(_playerList.PlayerListContent);
        }

        // ---------- 可见性 ----------

        private bool EditPanelOpen =>
            _editPanel != null && _editPanel.PanelContainer != null &&
            _editPanel.PanelContainer.localScale.x > 0.5f;

        private bool ScorePanelOpen =>
            _scorePanel != null && _scorePanel.PanelContainer != null &&
            _scorePanel.PanelContainer.localScale.x > 0.5f;

        /// <summary>
        /// 控件是否真的露在外面。收缩到零的面板（编辑面板、成绩面板用
        /// DOScale 开合）里的控件靠这个剔除，导航栈本身只看 activeInHierarchy
        /// 和 CanvasGroup 透明度，看不见缩放。
        /// </summary>
        private static bool ScaleVisible(Transform t)
        {
            while (t != null)
            {
                if (t.localScale.sqrMagnitude < 1e-6f) return false;
                if (t.GetComponent<Canvas>() != null) break;
                t = t.parent;
            }
            return true;
        }

        // ---------- 清单 ----------

        private void Apply(List<Group> groups)
        {
            _groups.Clear();
            _groups.AddRange(groups);

            _knownRoots.Clear();
            for (int i = 0; i < _groups.Count; i++)
            {
                for (int j = 0; j < _groups[i].Items.Count; j++)
                {
                    GameObject root = _groups[i].Items[j].Root;
                    if (root != null) _knownRoots.Add(root);
                }
            }
        }

        private bool SameRoots(List<Group> groups)
        {
            var now = new List<GameObject>();
            for (int i = 0; i < groups.Count; i++)
            {
                for (int j = 0; j < groups[i].Items.Count; j++)
                {
                    GameObject root = groups[i].Items[j].Root;
                    if (root != null) now.Add(root);
                }
            }

            if (now.Count != _knownRoots.Count) return false;
            for (int i = 0; i < now.Count; i++)
            {
                if (now[i] != _knownRoots[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 临时盖在上面的弹窗（断线提示等）关掉后，把键盘交回来，位置不变。
        /// </summary>
        public void Resume()
        {
            if (_nav == null) return;
            Rebuild(false);
            _nav.Resume(true);
        }

        /// <summary>按当前组重建整份清单。切格、换人都走这里。</summary>
        public void Rebuild(bool focusFirst)
        {
            if (_nav == null) return;

            _nav.BeginRebuild();
            _nav.Clear();

            for (int i = 0; i < _groups.Count; i++)
            {
                Group group = _groups[i];
                _nav.StartRow(group.Name);
                for (int j = 0; j < group.Items.Count; j++) _nav.Add(group.Items[j]);
            }

            // 别人（密码框之类）占着键盘时不去抢
            if (focusFirst && _nav.CanTakeKeyboard)
            {
                _nav.FocusFirst(!_introSpoken);
                if (!_introSpoken)
                {
                    _introSpoken = true;
                    _nav.SayIntro(A11yPhrases.Text(
                        "房间。上下光标换组，左右光标换项，回车确认。",
                        "Room. Up and down switch groups, left and right switch items, Enter confirms."));
                }
            }
            else
            {
                _nav.EndRebuild(false);
            }
        }

        // ---------- 返回 ----------

        /// <summary>
        /// Esc：有面板开着就先收起它；否则不擅自退房（那是把玩家从房间里
        /// 拿掉的动作），只提示用「离开房间」按钮。
        /// </summary>
        private void OnBack()
        {
            if (EditPanelOpen && _editPanel != null)
            {
                _editPanel.TogglePanel();
                A11yNavigator.SayInfo(A11yPhrases.Text("已收起房间设置", "Room settings closed"),
                    NarrationCategory.Notification);
                return;
            }

            if (ScorePanelOpen && _scorePanel != null)
            {
                _scorePanel.CloseButton.onClick.Invoke();
                return;
            }

            A11yNavigator.SayInfo(
                A11yPhrases.Text("要离开房间请用「离开房间」按钮", "Use the Leave room button to leave"),
                NarrationCategory.Notification);
        }

        // ---------- 小工具 ----------

        private static void AddButton(Group group, GameObject go, HashSet<GameObject> used)
        {
            if (go == null) return;

            Button button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<TMP_Text>(true);
            string name = text != null && !string.IsNullOrEmpty(text.text) ? text.text : go.name;

            if (button != null)
            {
                group.Items.Add(new A11yNavItem(button, name));
                used.Add(go);
                return;
            }

            group.Items.Add(A11yNavItem.Custom(go, A11yItemKind.Button, name, null, null));
            used.Add(go);
        }

        /// <summary>按钮露在外面才收，收进去也顺手记下，避免兜底组重复。</summary>
        private static void AddIfVisible(Group group, Button button, HashSet<GameObject> used, string name)
        {
            if (button == null) return;
            GameObject go = button.gameObject;
            if (go == null || used.Contains(go)) return;
            if (!go.activeInHierarchy || !ScaleVisible(go.transform)) return;

            group.Items.Add(new A11yNavItem(button, name));
            used.Add(go);
        }

        private static void AddInput(Group group, TMP_InputField input, HashSet<GameObject> used, string name)
        {
            if (input == null) return;
            GameObject go = input.gameObject;
            if (go == null || used.Contains(go)) return;
            if (!go.activeInHierarchy || !ScaleVisible(go.transform)) return;

            group.Items.Add(new A11yNavItem(input, name));
            used.Add(go);
        }
    }
}
