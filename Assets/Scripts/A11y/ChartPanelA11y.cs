using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RhythmCafe.Level;
using RDOnline.ScnLobby;

namespace RDOnline.A11y
{
    /// <summary>
    /// 谱面选择面板的无障碍导航内容。
    ///
    /// 原本分三组：谱面来源、谱面列表、操作。谱面面板长在大厅的「创建房间」
    /// 面板里，而一个场景只允许有一个键盘所有者（A11yNavigator.Active 是单例），
    /// 所以大厅在场时这里只负责提供内容（ContributeTo），由 LobbyA11y 统一朗读；
    /// 大厅不在场时退回自己接管键盘的旧行为。
    /// </summary>
    public class ChartPanelA11y : MonoBehaviour
    {
        private ScrLevelList _list;
        private ChartSourceTabs _tabs;
        private RDOnline.Component.ChartPreview _preview;

        /// <summary>自己当主人时用的控制器。</summary>
        private A11yNavigator _nav;
        /// <summary>托管给大厅时用的控制器（不归本组件所有）。</summary>
        private A11yNavigator _host;

        private readonly List<A11yNavItem> _tabItems = new List<A11yNavItem>();
        private readonly List<A11yNavItem> _cardItems = new List<A11yNavItem>();
        private readonly List<GameObject> _knownCards = new List<GameObject>();

        // 重建靠组名找回原来的位置，所以组名必须固定且唯一
        private static string RowSourceName => A11yPhrases.Text("谱面来源", "Chart source");
        private static string RowListName => A11yPhrases.Text("谱面列表", "Chart list");
        private static string RowActionName => A11yPhrases.Text("操作", "Actions");

        /// <summary>内容变了就通知托管方重建（列表随时由网络或本地扫描填充）。</summary>
        public System.Action ContentChanged;

        /// <summary>是否已被某个外部控制器托管。</summary>
        public bool Hosted => _host != null;

        /// <summary>是否正被指定控制器托管（大厅反复巡检时用来判断要不要重复收编）。</summary>
        public bool HostedBy(A11yNavigator nav)
        {
            return nav != null && _host == nav;
        }

        public static ChartPanelA11y Attach(ScrLevelList list, ChartSourceTabs tabs,
            RDOnline.Component.ChartPreview preview)
        {
            if (list == null) return null;

            var existing = list.GetComponent<ChartPanelA11y>();
            if (existing != null) return existing;

            var a11y = list.gameObject.AddComponent<ChartPanelA11y>();
            a11y._list = list;
            a11y._tabs = tabs;
            a11y._preview = preview;
            a11y.Init();
            return a11y;
        }

        private void Init()
        {
            BuildTabs();

            // 大厅的控制器在自己的 Start 里才建好，这里先不动键盘，等它来 Adopt；
            // 大厅整个不存在时（单独调试谱面面板）才自己接管。
            if (LobbyA11y.Instance == null) Own();

            // 列表内容由列表脚本分帧填充，这里持续观察卡片变化并同步出去
            StartCoroutine(WatchCards());
        }

        private void Own()
        {
            if (_nav != null) return;

            _nav = gameObject.AddComponent<A11yNavigator>();
            _nav.ListName = A11yPhrases.Text("谱面选择", "Chart selection");
            RebuildAll(true);
        }

        /// <summary>大厅把自己的控制器交给本组件，之后内容都往那里加。</summary>
        public void Adopt(A11yNavigator host)
        {
            _host = host;

            // 之前若已自己接管过键盘，交出去，保证同一时刻只有一个键盘所有者
            if (_nav != null)
            {
                _nav.Deactivate();
                Destroy(_nav);
                _nav = null;
            }
        }

        // ---------- 标签组 ----------

        private void BuildTabs()
        {
            _tabItems.Clear();
            if (_tabs == null) return;

            foreach (var kv in _tabs.GetTabButtons())
            {
                ChartSourceKind kind = kv.Key;
                Button button = kv.Value;
                if (button == null) continue;

                _tabItems.Add(A11yNavItem.Custom(
                    button.gameObject,
                    A11yItemKind.Button,
                    ChartSourceTabs.LabelOf(kind),
                    () => TabSpeech(kind),
                    () => _tabs.Select(kind)));
            }
        }

        /// <summary>读当前是否为选中来源，不改变任何状态</summary>
        private string TabSpeech(ChartSourceKind kind)
        {
            string name = ChartSourceTabs.LabelOf(kind);
            if (_tabs == null || _tabs.Current != kind) return name;
            return name + A11yPhrases.Text("，当前", ", selected");
        }

        // ---------- 列表组 ----------

        private IEnumerator WatchCards()
        {
            while (true)
            {
                yield return null;

                bool changed = false;

                // 标签条要等一帧才建好，建好后必须补进清单，否则标签组一直是空的
                if (_tabItems.Count != CountTabButtons())
                {
                    BuildTabs();
                    changed = true;
                }

                List<GameObject> cards = CollectCards();
                if (!SameCards(cards))
                {
                    RebuildCards(cards);
                    changed = true;
                }

                if (!changed) continue;

                if (_host != null)
                {
                    // 托管时不能自己清清单，通知大厅整体重建
                    if (ContentChanged != null) ContentChanged();
                }
                else
                {
                    RebuildAll(false);
                }
            }
        }

        private int CountTabButtons()
        {
            if (_tabs == null) return 0;
            int count = 0;
            foreach (var kv in _tabs.GetTabButtons()) count++;
            return count;
        }

        private List<GameObject> CollectCards()
        {
            var result = new List<GameObject>();
            if (_list == null || _list.cardContainer == null) return result;

            for (int i = 0; i < _list.cardContainer.childCount; i++)
            {
                Transform child = _list.cardContainer.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                    result.Add(child.gameObject);
            }
            return result;
        }

        private bool SameCards(List<GameObject> cards)
        {
            if (cards.Count != _knownCards.Count) return false;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != _knownCards[i]) return false;
            }
            return true;
        }

        private void RebuildCards(List<GameObject> cards)
        {
            _knownCards.Clear();
            _knownCards.AddRange(cards);
            _cardItems.Clear();

            for (int i = 0; i < cards.Count; i++)
            {
                GameObject go = cards[i];
                var card = go.GetComponent<ScrLevelCard>();
                if (card == null) continue;

                _cardItems.Add(A11yNavItem.Custom(
                    go,
                    A11yItemKind.Button,
                    SpeechOf(card),
                    () => SpeechOf(card),
                    () => card.Activate()));
            }
        }

        /// <summary>
        /// 卡片的朗读文本。官方关卡读本地化名称，社区谱面读歌名与作者，
        /// 两者都不读内部标识。
        /// </summary>
        private string SpeechOf(ScrLevelCard card)
        {
            if (card == null) return A11yPhrases.Text("未知谱面", "Unknown chart");

            ChartSourceEntry entry = card.Entry;
            if (entry != null)
                return WithCardExtras(card, string.IsNullOrEmpty(entry.Display) ? entry.Id : entry.Display);

            LevelDocument doc = card.CurrentLevel;
            if (doc != null)
            {
                string song = string.IsNullOrEmpty(doc.song) ? doc.id : doc.song;
                string author = doc.authors != null && doc.authors.Count > 0
                    ? string.Join(", ", doc.authors) : string.Empty;
                string head = string.IsNullOrEmpty(author)
                    ? song
                    : song + A11yPhrases.ItemSeparator + author;
                return WithCardExtras(card, head);
            }

            return A11yPhrases.Text("未知谱面", "Unknown chart");
        }

        /// <summary>
        /// 难度与 BPM 单独成条接在主信息后面：卡片上这两项各有自己的文字，
        /// 读出来才知道是哪一档，不看屏幕也不用猜。
        /// </summary>
        private static string WithCardExtras(ScrLevelCard card, string head)
        {
            var parts = new List<string> { head };

            if (card.difficultyText != null && !string.IsNullOrEmpty(card.difficultyText.text))
                parts.Add(card.difficultyText.text);

            if (card.bpmText != null && !string.IsNullOrEmpty(card.bpmText.text))
                parts.Add(card.bpmText.text + " BPM");

            return string.Join(A11yPhrases.ItemSeparator, parts);
        }

        // ---------- 对外提供内容 ----------

        /// <summary>来源标签组。</summary>
        public void ContributeTabs(A11yNavigator nav)
        {
            if (nav == null) return;
            nav.StartRow(RowSourceName);
            for (int i = 0; i < _tabItems.Count; i++) nav.Add(_tabItems[i]);
        }

        /// <summary>当前来源的谱面列表组。</summary>
        public void ContributeList(A11yNavigator nav)
        {
            if (nav == null) return;
            nav.StartRow(RowListName);
            for (int i = 0; i < _cardItems.Count; i++) nav.Add(_cardItems[i]);
        }

        /// <summary>试听、浏览文件、上传。</summary>
        public void ContributeActions(A11yNavigator nav)
        {
            if (nav == null) return;
            nav.StartRow(RowActionName);
            AddActions(nav);
        }

        /// <summary>三组按顺序一起加（自己独占键盘时用）。</summary>
        public void ContributeTo(A11yNavigator nav)
        {
            if (nav == null) return;
            ContributeTabs(nav);
            ContributeList(nav);
            ContributeActions(nav);
        }

        /// <summary>按当前状态重建自己的三组清单，尽量保住用户原来停的位置。</summary>
        public void RebuildAll(bool focusFirst)
        {
            if (_nav == null) return;

            _nav.BeginRebuild();
            _nav.Clear();

            ContributeTo(_nav);

            if (focusFirst)
                _nav.FocusFirst();
            else
                _nav.EndRebuild(false);
        }

        private void AddActions(A11yNavigator nav)
        {
            if (_preview == null) return;

            if (_preview.PlayButton != null)
            {
                nav.Add(A11yNavItem.Custom(
                    _preview.PlayButton.gameObject,
                    A11yItemKind.Button,
                    A11yPhrases.Text("试听", "Preview"),
                    () => _preview.IsPlaying
                        ? A11yPhrases.Text("试听，正在播放", "Preview, playing")
                        : A11yPhrases.Text("试听", "Preview"),
                    () => _preview.PlayButton.onClick.Invoke()));
            }

            if (_preview.FileBrowseButton != null)
            {
                nav.Add(A11yNavItem.Custom(
                    _preview.FileBrowseButton.gameObject,
                    A11yItemKind.Button,
                    A11yPhrases.Text("浏览文件", "Browse files"),
                    null,
                    () => _preview.FileBrowseButton.onClick.Invoke()));
            }

            if (_preview.UploadButton != null)
            {
                nav.Add(A11yNavItem.Custom(
                    _preview.UploadButton.gameObject,
                    A11yItemKind.Button,
                    A11yPhrases.Text("上传", "Upload"),
                    () => _preview.UploadButton.interactable
                        ? A11yPhrases.Text("上传", "Upload")
                        : A11yPhrases.Text("上传", "Upload") + A11yPhrases.Unavailable,
                    () => _preview.UploadButton.onClick.Invoke()));
            }
        }
    }
}
