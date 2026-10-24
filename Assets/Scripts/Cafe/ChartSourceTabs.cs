using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 谱面来源。每个来源是一个标签页，切换时列表整块重建。
    /// </summary>
    public enum ChartSourceKind
    {
        /// <summary>社区谱面（Rhythm Cafe / 自建后端），联网搜索</summary>
        Community,
        /// <summary>游戏自带官方关卡，只传关卡标识，各端本地加载</summary>
        Official,
        /// <summary>玩家关卡库（游戏自己的 Levels 目录），真打包上传</summary>
        Library
    }

    /// <summary>
    /// 谱面来源标签条。
    ///
    /// 放在谱面面板顶行「左侧标题」与「右侧搜索框」之间的空档里。
    /// 位置与宽度全部由这两个既有控件在运行时的实际矩形推导，
    /// 含有不写死任何屏幕坐标/尺寸，换分辨率、换宽高比都自适应。
    /// 标签外观则是代码新建的，字体从面板里已有的 TMP 文字借，
    /// 因为游戏没有可新建的字体资源。
    /// </summary>
    public class ChartSourceTabs : MonoBehaviour
    {
        private sealed class Tab
        {
            public ChartSourceKind Kind;
            public Button Button;
            public Image Background;
            public TMP_Text Text;
        }

        public static ChartSourceTabs Instance { get; private set; }

        private readonly List<Tab> _tabs = new List<Tab>();
        private ScrLevelList _list;
        private RectTransform _panel;
        private RectTransform _bar;
        private TMP_Text _fontTemplate;
        private Vector2 _lastPanelSize = Vector2.zero;

        /// <summary>当前选中的来源</summary>
        public ChartSourceKind Current { get; private set; } = ChartSourceKind.Community;

        // 选中/未选中只差透明度，不引入任何新配色，也不涉及紫色
        private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color ActiveColor = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color IdleTextColor = new Color(1f, 1f, 1f, 0.72f);
        private static readonly Color ActiveTextColor = new Color(1f, 1f, 1f, 1f);

        /// <summary>
        /// 由谱面列表在自己启动时挂载。面板就是列表所在的谱面面板。
        /// </summary>
        public static ChartSourceTabs Attach(ScrLevelList list, RectTransform panel)
        {
            if (Instance != null) return Instance;
            if (panel == null) return null;

            var go = new GameObject("ChartSourceTabs", typeof(RectTransform));
            go.transform.SetParent(panel, false);

            var tabs = go.AddComponent<ChartSourceTabs>();
            tabs._list = list;
            tabs._panel = panel;
            Instance = tabs;
            tabs.StartCoroutine(tabs.BuildWhenReady());
            return tabs;
        }

        private IEnumerator BuildWhenReady()
        {
            // 等一帧，让面板的布局先算出来，否则量到的是 0 尺寸
            yield return null;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            BuildTabs();
            ApplyRect();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // 分辨率/窗口变化时重新贴合，标签条不依赖初始尺寸
            if (_panel == null || _bar == null) return;
            Vector2 size = _panel.rect.size;
            if (size != _lastPanelSize && size.x > 1f && size.y > 1f)
                ApplyRect();
        }

        // ---------- 构建 ----------

        private void BuildTabs()
        {
            _fontTemplate = FindFontTemplate();
            if (_fontTemplate == null)
            {
                Debug.LogWarning("[ChartSourceTabs] 面板内没有找到可借用的 TMP 文字，标签条无法创建");
                return;
            }

            var barGo = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _bar = barGo.GetComponent<RectTransform>();
            _bar.SetParent(_panel, false);
            _bar.SetAsLastSibling(); // 放在最上层，保证标签一定可点，不被滚动区吃掉点击

            var layout = barGo.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 0, 0);

            AddTab(ChartSourceKind.Community);
            AddTab(ChartSourceKind.Official);
            AddTab(ChartSourceKind.Library);

            RefreshVisuals();
        }

        private void AddTab(ChartSourceKind kind)
        {
            var go = new GameObject("Tab_" + kind, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_bar, false);

            var bg = go.GetComponent<Image>();
            bg.color = IdleColor;
            bg.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            ChartSourceKind captured = kind;
            button.onClick.AddListener(() => Select(captured));

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);

            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = LabelOf(kind);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.enableWordWrapping = false;

            // 字体与材质只能从既有文字借，游戏没有暴露字体资源
            text.font = _fontTemplate.font;
            text.fontSharedMaterial = _fontTemplate.fontSharedMaterial;
            text.color = IdleTextColor;

            _tabs.Add(new Tab
            {
                Kind = kind,
                Button = button,
                Background = bg,
                Text = text
            });
        }

        /// <summary>界面文字跟随游戏语言，与主菜单入口的做法一致</summary>
        public static string LabelOf(ChartSourceKind kind)
        {
            bool zh = true;
            try
            {
                zh = RDString.isChinese;
            }
            catch
            {
                // RDString 还没初始化时按中文处理
            }

            switch (kind)
            {
                case ChartSourceKind.Official:
                    return zh ? "官方关卡" : "Official";
                case ChartSourceKind.Library:
                    return zh ? "我的关卡库" : "My levels";
                default:
                    return zh ? "社区谱面" : "Community";
            }
        }

        private TMP_Text FindFontTemplate()
        {
            // 优先用标题文字，它一定存在且是整条 UI 的字号基准
            Transform title = _panel.Find("CafeLogo/Text (TMP)");
            if (title != null)
            {
                var t = title.GetComponent<TMP_Text>();
                if (t != null && t.font != null) return t;
            }

            // 退路：面板里任意一个 TMP 文字
            var all = _panel.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].font != null) return all[i];
            }
            return null;
        }

        // ---------- 布局 ----------

        /// <summary>
        /// 把标签条贴合到「左侧标题右缘」与「右侧搜索框左缘」之间的空档。
        /// 全部由那两个控件的实际矩形换算，没有任何写死的坐标或尺寸。
        /// </summary>
        private void ApplyRect()
        {
            if (_panel == null || _bar == null) return;

            RectTransform left = FindChildRect("CafeLogo");
            RectTransform right = FindChildRect("RightUpArea");
            if (left == null || right == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            Rect panelRect = _panel.rect;
            if (panelRect.width < 1f || panelRect.height < 1f) return;
            _lastPanelSize = panelRect.size;

            // 用包围盒量既有控件的真实位置，锚点/轴心/缩放怎么设都不用管
            Bounds lb = RectTransformUtility.CalculateRelativeRectTransformBounds(_panel, left);
            Bounds rb = RectTransformUtility.CalculateRelativeRectTransformBounds(_panel, right);

            // 垂直取两者交集（它们在同一行），水平取中间空档
            float bottom = Mathf.Max(lb.min.y, rb.min.y);
            float top = Mathf.Min(lb.max.y, rb.max.y);
            if (top <= bottom)
            {
                bottom = lb.min.y;
                top = lb.max.y;
            }

            float rowHeight = top - bottom;
            float gap = rowHeight * 0.08f;

            float x0 = lb.max.x + gap;
            float x1 = rb.min.x - gap;

            if (x1 <= x0 || rowHeight <= 1f)
            {
                // 这个宽高比下中间确实没有空档，宁可隐藏也不压住别的控件
                _bar.gameObject.SetActive(false);
                return;
            }
            _bar.gameObject.SetActive(true);

            // 锚点以父矩形左下为基准做归一化（父级轴心可能不在 0，必须带上 panelRect.xMin/yMin）
            _bar.anchorMin = new Vector2(
                (x0 - panelRect.xMin) / panelRect.width,
                (bottom - panelRect.yMin) / panelRect.height);
            _bar.anchorMax = new Vector2(
                (x1 - panelRect.xMin) / panelRect.width,
                (top - panelRect.yMin) / panelRect.height);
            _bar.offsetMin = Vector2.zero;
            _bar.offsetMax = Vector2.zero;

            // 间距与字号都跟着行高走，不写死任何像素
            var layout = _bar.GetComponent<HorizontalLayoutGroup>();
            if (layout != null) layout.spacing = rowHeight * 0.04f;

            float fontSizeMax = Mathf.Max(8f, rowHeight * 0.42f);
            float fontSizeMin = Mathf.Max(6f, rowHeight * 0.16f);
            for (int i = 0; i < _tabs.Count; i++)
            {
                var text = _tabs[i].Text;
                if (text == null) continue;
                text.fontSizeMax = fontSizeMax;
                text.fontSizeMin = fontSizeMin;
                text.fontSize = fontSizeMax;
            }
        }

        private RectTransform FindChildRect(string childName)
        {
            if (_panel == null) return null;
            Transform t = _panel.Find(childName);
            return t as RectTransform;
        }

        // ---------- 交互 ----------

        /// <summary>选中某个来源，并让列表整块重建</summary>
        public void Select(ChartSourceKind kind)
        {
            if (kind == Current)
            {
                // 重复点同一个标签不重建，避免列表闪一下
                RefreshVisuals();
                return;
            }

            Current = kind;
            RefreshVisuals();

            if (_list != null)
                _list.SetSource(kind);
        }

        private void RefreshVisuals()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                bool active = tab.Kind == Current;
                if (tab.Background != null)
                    tab.Background.color = active ? ActiveColor : IdleColor;
                if (tab.Text != null)
                    tab.Text.color = active ? ActiveTextColor : IdleTextColor;
            }
        }

        /// <summary>按显示次序返回每个标签的按钮，供无障碍导航接管。</summary>
        public IEnumerable<KeyValuePair<ChartSourceKind, Button>> GetTabButtons()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].Button == null) continue;
                yield return new KeyValuePair<ChartSourceKind, Button>(_tabs[i].Kind, _tabs[i].Button);
            }
        }

        /// <summary>当前选中的来源是否允许联网搜索（只有社区谱面需要）</summary>
        public static bool NeedsNetwork(ChartSourceKind kind)
        {
            return kind == ChartSourceKind.Community;
        }
    }
}
