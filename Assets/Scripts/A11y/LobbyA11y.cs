using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RhythmCafe.Level;
using RDOnline.ScnLobby;
using RDOnline.Auth;

namespace RDOnline.A11y
{
    /// <summary>
    /// 大厅（ScnLobby）的无障碍导航，全场景唯一的键盘所有者。
    ///
    /// 按键模型与其它界面一致：上下换组，左右组内换项，回车触发/开合编辑框，
    /// Esc 返回。组的划分：
    ///   大厅视图：「房间列表」（每个房间整条算一项）、「按钮」
    ///   创建视图：「创建房间」（房间名、人数、密码、创建）、
    ///             谱面面板的三组（来源、列表、操作）、「其他按钮」
    ///
    /// 房间列表每 5 秒由服务端 lobby/sync 整体替换，所以这里每帧比对新旧控件，
    /// 变了才重建，重建时靠组名保住用户原来停的位置。
    /// 不写死任何屏幕坐标和尺寸，界面文字一律读控件自身的文本。
    /// </summary>
    public class LobbyA11y : MonoBehaviour
    {
        public static LobbyA11y Instance { get; private set; }

        private A11yNavigator _nav;
        private LobbyPanelManager _panels;
        private RoomListUI _roomList;
        private RoomCreator _creator;
        private ScrLevelList _chartList;
        private ChartPanelA11y _chartPanel;
        private UserInfoUI _userInfo;

        private readonly List<A11yNavItem> _roomItems = new List<A11yNavItem>();
        private readonly List<A11yNavItem> _buttonItems = new List<A11yNavItem>();
        private readonly List<List<A11yNavItem>> _creatorRows = new List<List<A11yNavItem>>();
        private readonly List<A11yNavItem> _extraItems = new List<A11yNavItem>();
        private readonly List<A11yNavItem> _userInfoItems = new List<A11yNavItem>();

        private readonly List<GameObject> _knownRooms = new List<GameObject>();
        private readonly List<GameObject> _knownButtons = new List<GameObject>();
        private readonly List<GameObject> _knownExtras = new List<GameObject>();

        private bool _introSpoken;
        private Coroutine _deferredRebuild;

        /// <summary>上一条已朗读的在线人数，变了才再读。</summary>
        private string _lastOnline;

        private bool _userInfoBuilt;

        private static string RowRoomName => A11yPhrases.Text("房间列表", "Room list");
        private static string RowButtonName => A11yPhrases.Text("按钮", "Buttons");
        private static string RowCreateName => A11yPhrases.Text("创建房间", "Create room");
        private static string RowExtraName => A11yPhrases.Text("其他按钮", "Other buttons");
        private static string RowUserInfoName => A11yPhrases.Text("我的信息", "My info");

        /// <summary>
        /// 场景加载完成后自动挂载。不依赖场景里预先摆好组件，也不改任何场景文件。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 只关心带大厅面板的场景；其它场景有自己的无障碍脚本
            if (Object.FindObjectOfType<LobbyPanelManager>() == null) return;
            if (Instance != null) return;

            var go = new GameObject("LobbyA11y");
            go.AddComponent<LobbyA11y>();
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (_chartPanel != null) _chartPanel.ContentChanged = null;
            if (_nav != null) _nav.Deactivate();
        }

        private void Start()
        {
            _nav = gameObject.AddComponent<A11yNavigator>();
            _nav.ListName = A11yPhrases.Text("大厅", "Lobby");
            _nav.BackAction = OnBack;

            RefreshRefs();
            // 谱面面板可能已先挂好并自持键盘，先收编再出第一份清单，
            // 否则首帧会漏掉它的三组、焦点也可能落在别的控件上
            AdoptChartPanel();
            RebuildAll(true);

            StartCoroutine(Watch());
        }

        // ---------- 引用与初始化 ----------

        /// <summary>
        /// 取场景里的各管理组件。找不到就下次再试，这样不必知道组件在层级里
        /// 摆在哪一层，也不会因为加载顺序不同就漏掉。
        /// </summary>
        private void RefreshRefs()
        {
            if (_panels == null) _panels = Object.FindObjectOfType<LobbyPanelManager>();
            if (_roomList == null) _roomList = Object.FindObjectOfType<RoomListUI>();
            if (_creator == null) _creator = Object.FindObjectOfType<RoomCreator>();
            if (_chartList == null) _chartList = Object.FindObjectOfType<ScrLevelList>();
            if (_chartPanel == null && _chartList != null)
                _chartPanel = _chartList.GetComponent<ChartPanelA11y>();
            if (_userInfo == null) _userInfo = Object.FindObjectOfType<UserInfoUI>();
        }

        /// <summary>把谱面面板收进来托管，保证同一时刻只有一个控制器收键。</summary>
        private void AdoptChartPanel()
        {
            if (_chartPanel == null) return;
            if (_chartPanel.Hosted && _chartPanel.HostedBy(_nav)) return;
            if (!_chartPanel.Hosted) _chartPanel.Adopt(_nav);

            _chartPanel.ContentChanged = () => RebuildAll(false);
            _knownExtras.Clear();
        }

        // ---------- 每帧巡检 ----------

        /// <summary>
        /// 巡检：房间列表和面板可见性每帧看一次（便宜），全场景按钮扫描
        /// 限制在每半秒一次（贵），否则帧率会被拖下去。
        /// </summary>
        private IEnumerator Watch()
        {
            float nextDeepScan = 0f;
            bool wasMain = MainVisible;

            while (true)
            {
                yield return null;

                RefreshRefs();
                AdoptChartPanel();
                WatchOnline();

                bool changed = false;

                List<GameObject> rooms = CollectRooms();
                if (!Same(_knownRooms, rooms))
                {
                    BuildRooms(rooms);
                    changed = true;
                }

                List<GameObject> buttons = CollectMainButtons();
                if (!Same(_knownButtons, buttons))
                {
                    BuildButtons(buttons);
                    changed = true;
                }

                // 面板切换时立即重扫一次，保证新面板上的按钮马上可导航
                bool nowMain = MainVisible;
                bool viewChanged = nowMain != wasMain;
                if (viewChanged) wasMain = nowMain;

                if (viewChanged || Time.unscaledTime >= nextDeepScan)
                {
                    nextDeepScan = Time.unscaledTime + 0.5f;

                    List<GameObject> extras = CollectExtras();
                    if (!Same(_knownExtras, extras))
                    {
                        BuildExtras(extras);
                        changed = true;
                    }

                    // 创建房间面板的控件由场景摆放，第一次进面板时才收进来
                    if (_creatorRows.Count == 0) BuildCreator();

                    // 用户信息只建一次：三项都是只读文本，位置和数量不会变
                    if (!_userInfoBuilt) BuildUserInfo();
                }

                if (changed) RebuildAll(false);
            }
        }

        private static bool Same(List<GameObject> known, List<GameObject> now)
        {
            if (known.Count != now.Count) return false;
            for (int i = 0; i < now.Count; i++)
            {
                if (known[i] != now[i]) return false;
            }
            return true;
        }

        // ---------- 房间列表组 ----------

        private List<GameObject> CollectRooms()
        {
            var result = new List<GameObject>();
            if (_roomList == null || _roomList.RoomListContent == null) return result;

            Transform content = _roomList.RoomListContent;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (child == null || !child.gameObject.activeSelf) continue;
                if (child.GetComponent<RoomItem>() == null) continue;
                result.Add(child.gameObject);
            }
            return result;
        }

        private void BuildRooms(List<GameObject> rooms)
        {
            _knownRooms.Clear();
            _knownRooms.AddRange(rooms);
            _roomItems.Clear();

            for (int i = 0; i < rooms.Count; i++)
            {
                var item = rooms[i].GetComponent<RoomItem>();
                if (item == null) continue;

                _roomItems.Add(A11yNavItem.Custom(
                    item.gameObject,
                    A11yItemKind.Button,
                    SpeechOf(item),
                    () => SpeechOf(item),
                    () => Join(item)));
            }
        }

        /// <summary>整条房间读成一句：名称、人数、状态、编号、有无密码。不读内部标识以外的技术细节。</summary>
        private string SpeechOf(RoomItem item)
        {
            if (item == null) return A11yPhrases.Text("未知房间", "Unknown room");

            var parts = new List<string>();
            if (item.RoomNameText != null && !string.IsNullOrEmpty(item.RoomNameText.text))
                parts.Add(item.RoomNameText.text);
            if (item.PlayerCountText != null && !string.IsNullOrEmpty(item.PlayerCountText.text))
                parts.Add(item.PlayerCountText.text);
            if (item.StatusText != null && !string.IsNullOrEmpty(item.StatusText.text))
                parts.Add(item.StatusText.text);
            if (item.RoomIdText != null && !string.IsNullOrEmpty(item.RoomIdText.text))
                parts.Add(item.RoomIdText.text);
            if (item.PasswordIcon != null && item.PasswordIcon.gameObject.activeSelf)
                parts.Add(A11yPhrases.Text("有密码", "password required"));

            string text = parts.Count > 0
                ? string.Join(A11yPhrases.Separator, parts)
                : A11yPhrases.Text("未知房间", "Unknown room");

            bool canJoin = item.JoinButton == null || item.JoinButton.interactable;
            return canJoin ? text : text + A11yPhrases.Unavailable;
        }

        /// <summary>回车加入：走房间条目自己的加入按钮，有密码时它会弹出密码面板。</summary>
        private void Join(RoomItem item)
        {
            if (item == null || item.JoinButton == null) return;
            if (!item.JoinButton.interactable)
            {
                A11yNavigator.SayInfo(A11yPhrases.Text("该房间暂时无法加入", "This room cannot be joined now"),
                    NarrationCategory.Notification);
                return;
            }
            item.JoinButton.onClick.Invoke();
        }

        // ---------- 主面板按钮组 ----------

        /// <summary>
        /// 大厅视图的按钮：两个面板标签，加上主面板里除房间条目之外的所有按钮。
        /// 直接扫主面板子树，不依赖场景里哪个按钮叫什么名字。
        /// </summary>
        private List<GameObject> CollectMainButtons()
        {
            var result = new List<GameObject>();
            var seen = new HashSet<GameObject>();

            if (_panels != null)
            {
                AddButton(result, seen, _panels.LobbyTabButton);
                AddButton(result, seen, _panels.CreateRoomTabButton);
            }

            if (_panels != null && _panels.MainPanel != null)
            {
                Button[] buttons = _panels.MainPanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    // 房间条目里的加入按钮由房间组整条代表，不再单独出现
                    if (_roomList != null && _roomList.RoomListContent != null &&
                        buttons[i].transform.IsChildOf(_roomList.RoomListContent))
                        continue;
                    AddButton(result, seen, buttons[i]);
                }
            }

            return result;
        }

        private void BuildButtons(List<GameObject> buttons)
        {
            _knownButtons.Clear();
            _knownButtons.AddRange(buttons);
            _buttonItems.Clear();

            for (int i = 0; i < buttons.Count; i++)
            {
                AddButtonItem(_buttonItems, buttons[i]);
            }
        }

        // ---------- 用户信息组 ----------

        /// <summary>
        /// 大厅右上角的用户名、延迟、在线人数。三项只读，逐条列出来让人听。
        /// </summary>
        private void BuildUserInfo()
        {
            if (_userInfo == null) return;

            _userInfoItems.Clear();
            UserInfoSpeech.AddItems(_userInfoItems, _userInfo);

            // 三项都没建出来说明控件还没就绪，下次巡检再试
            if (_userInfoItems.Count > 0) _userInfoBuilt = true;
        }

        /// <summary>在线人数变化时直接读一句，不抢焦点。</summary>
        private void WatchOnline()
        {
            if (_userInfo == null) return;

            string speech = UserInfoSpeech.WatchOnline(_userInfo, ref _lastOnline);
            if (string.IsNullOrEmpty(speech)) return;

            A11yNavigator.SayInfo(speech, NarrationCategory.Notification);
        }

        // ---------- 创建房间组 ----------

        /// <summary>
        /// 创建房间面板。每个控件各自成组：组里只有一项且是数值控件时，
        /// 左右键直接改值，所以人数滑块必须独占一组。
        /// </summary>
        private void BuildCreator()
        {
            _creatorRows.Clear();
            if (_creator == null) return;

            if (_creator.RoomNameInput != null)
            {
                _creatorRows.Add(new List<A11yNavItem>
                {
                    new A11yNavItem(_creator.RoomNameInput,
                        A11yPhrases.Text("房间名", "Room name"))
                });
            }

            if (_creator.PlayerCountSlider != null)
            {
                _creatorRows.Add(new List<A11yNavItem>
                {
                    new A11yNavItem(_creator.PlayerCountSlider,
                        A11yPhrases.Text("人数", "Players"),
                        () => A11yPhrases.Text("人数", "Players") + A11yPhrases.Separator +
                              Mathf.RoundToInt(_creator.PlayerCountSlider.value))
                });
            }

            if (_creator.PasswordInput != null)
            {
                _creatorRows.Add(new List<A11yNavItem>
                {
                    new A11yNavItem(_creator.PasswordInput,
                        A11yPhrases.Text("房间密码", "Room password"))
                });
            }

            if (_creator.CreateButton != null)
            {
                var row = new List<A11yNavItem>();
                AddButtonItem(row, _creator.CreateButton.gameObject);
                if (row.Count > 0) _creatorRows.Add(row);
            }
        }

        // ---------- 其他按钮组 ----------

        /// <summary>
        /// 兜底组：场景里既没有被上面任何一组收录、也不属于房间条目与谱面面板
        /// 的按钮都收进来，保证界面上能点的东西一个不漏。
        /// </summary>
        private List<GameObject> CollectExtras()
        {
            var result = new List<GameObject>();
            if (_panels == null) return result;

            Button[] buttons = Object.FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                GameObject go = buttons[i].gameObject;
                if (go == null) continue;

                if (_knownButtons.Contains(go)) continue;
                if (IsUnderRoomContent(go)) continue;
                if (_chartList != null && go.transform.IsChildOf(_chartList.transform)) continue;
                if (IsUnderPasswordPanel(go)) continue;

                // 创建面板里的控件已单独成组，避免同一按钮出现两次
                if (_creator != null && go.transform.IsChildOf(_creator.transform)) continue;

                result.Add(go);
            }
            return result;
        }

        private bool IsUnderRoomContent(GameObject go)
        {
            return _roomList != null && _roomList.RoomListContent != null &&
                   go.transform.IsChildOf(_roomList.RoomListContent);
        }

        private bool IsUnderPasswordPanel(GameObject go)
        {
            var panel = PasswordVerifyPanel.Instance;
            return panel != null && go.transform.IsChildOf(panel.transform);
        }

        private void BuildExtras(List<GameObject> extras)
        {
            _knownExtras.Clear();
            _knownExtras.AddRange(extras);
            _extraItems.Clear();

            for (int i = 0; i < extras.Count; i++)
            {
                AddButtonItem(_extraItems, extras[i]);
            }
        }

        // ---------- 通用 ----------

        private static void AddButton(List<GameObject> list, HashSet<GameObject> seen, Button button)
        {
            if (button == null) return;
            GameObject go = button.gameObject;
            if (go == null) return;
            if (!seen.Add(go)) return;
            list.Add(go);
        }

        /// <summary>把按钮收成一项。名字与朗读都取控件自身文字，自动跟随游戏语言。</summary>
        private static void AddButtonItem(List<A11yNavItem> list, GameObject go)
        {
            if (go == null) return;

            Button button = go.GetComponent<Button>();
            var text = go.GetComponentInChildren<TMP_Text>(true);
            string name = text != null && !string.IsNullOrEmpty(text.text) ? text.text : go.name;

            if (button != null)
            {
                list.Add(new A11yNavItem(button, name));
                return;
            }

            list.Add(A11yNavItem.Custom(go, A11yItemKind.Button, name, null, null));
        }

        // ---------- 清单 ----------

        private bool MainVisible =>
            _panels == null || _panels.MainPanel == null || _panels.MainPanel.alpha > 0.5f;

        /// <summary>按当前可见视图重建清单。切换视图时把焦点放到新视图的第一组。</summary>
        public void RebuildAll()
        {
            RebuildAll(false);
        }

        public void RebuildAll(bool focusFirst)
        {
            if (_nav == null) return;

            _nav.BeginRebuild();
            _nav.Clear();

            if (MainVisible)
            {
                _nav.StartRow(RowRoomName);
                for (int i = 0; i < _roomItems.Count; i++) _nav.Add(_roomItems[i]);

                _nav.StartRow(RowButtonName);
                for (int i = 0; i < _buttonItems.Count; i++) _nav.Add(_buttonItems[i]);
            }
            else
            {
                for (int i = 0; i < _creatorRows.Count; i++)
                {
                    // 第一项带上组名，后面几个是同一面板内的兄弟控件
                    _nav.StartRow(i == 0 ? RowCreateName : string.Empty);
                    List<A11yNavItem> row = _creatorRows[i];
                    for (int j = 0; j < row.Count; j++) _nav.Add(row[j]);
                }

                // 谱面面板在创建视图里，三组紧跟创建控件之后
                if (_chartPanel != null) _chartPanel.ContributeTo(_nav);
            }

            _nav.StartRow(RowExtraName);
            for (int i = 0; i < _extraItems.Count; i++) _nav.Add(_extraItems[i]);

            // 用户名/延迟/在线人数放在最后，前面的操作项不受它影响
            if (_userInfoItems.Count > 0)
            {
                _nav.StartRow(RowUserInfoName);
                for (int i = 0; i < _userInfoItems.Count; i++) _nav.Add(_userInfoItems[i]);
            }

            // 焦点只在没有别的弹窗（密码框等）占着键盘时才动，
            // 否则重建清单会把键盘从弹窗手里抢回来，用户正在输入的框会失去焦点
            bool canFocus = focusFirst && _nav.CanTakeKeyboard;

            if (canFocus)
            {
                _nav.FocusFirst(!_introSpoken);
                if (!_introSpoken)
                {
                    _introSpoken = true;
                    _nav.SayIntro(A11yPhrases.Text(
                        "大厅。上下光标换组，左右光标换项，回车确认，Esc 返回。",
                        "Lobby. Up and down switch groups, left and right switch items, Enter confirms, Escape goes back."));
                }
            }
            else
            {
                // 清单已换，位置尽量保留；不发声，免得和弹窗的朗读撞在一起
                _nav.EndRebuild(false);
            }
        }

        // ---------- 返回 ----------

        /// <summary>
        /// Esc：创建视图先退回大厅视图；在大厅视图则回主菜单。
        /// </summary>
        private void OnBack()
        {
            if (!MainVisible && _panels != null)
            {
                _panels.ShowMainPanel();
                RebuildAfterFade(true);
                return;
            }

            var navigator = Object.FindObjectOfType<Auth.SceneNavigator>();
            if (navigator == null)
            {
                A11yNavigator.SayInfo(A11yPhrases.CannotGoBack, NarrationCategory.Notification);
                return;
            }

            A11yNavigator.SayInfo(A11yPhrases.Text("返回主菜单", "Back to menu"), NarrationCategory.Notification);
            navigator.DoNavigate();
        }

        /// <summary>面板淡入淡出期间旧控件还活着，等动画走完再重建，焦点才落在新视图上。</summary>
        private void RebuildAfterFade(bool focusFirst)
        {
            if (_deferredRebuild != null) StopCoroutine(_deferredRebuild);
            _deferredRebuild = StartCoroutine(FadeThenRebuild(focusFirst));
        }

        private IEnumerator FadeThenRebuild(bool focusFirst)
        {
            float wait = _panels != null ? _panels.FadeDuration : 0.3f;
            yield return new WaitForSeconds(wait + 0.05f);
            _deferredRebuild = null;
            RebuildAll(focusFirst);
        }

        /// <summary>临时弹窗（密码框）关闭后把键盘交回来，位置不变。</summary>
        public void Resume()
        {
            if (_nav == null) return;
            RefreshRefs();
            RebuildAll(false);
            _nav.Resume(true);
        }

        /// <summary>面板标签被点了也重建一次，键盘焦点跟着视图走。</summary>
        public void SwitchToMain()
        {
            if (_panels != null) _panels.ShowMainPanel();
            RebuildAfterFade(true);
        }

        public void SwitchToCreate()
        {
            if (_panels != null) _panels.ShowCreateRoomPanel();
            RebuildAfterFade(true);
        }
    }
}
