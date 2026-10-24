using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RDOnline.A11y
{
    /// <summary>导航项类型，决定朗读时套用的官方元素类型。</summary>
    public enum A11yItemKind
    {
        Button,
        Input,
        Toggle,
        Slider,
        Label
    }

    /// <summary>
    /// 一个可聚焦项：包住真实控件，负责取文本、取状态、执行动作。
    /// 朗读一律走游戏自带的 Narration，这里不生成任何语音。
    /// </summary>
    public class A11yNavItem
    {
        public A11yItemKind Kind;
        public string Name;

        private readonly TMP_InputField _input;
        private readonly Button _button;
        private readonly Toggle _toggle;
        private readonly TMP_Text _label;
        private readonly Slider _slider;
        private readonly System.Func<string> _dynamicText;
        private readonly System.Action _activate;
        private readonly GameObject _root;

        public A11yNavItem(TMP_InputField input, string name)
        {
            Kind = A11yItemKind.Input;
            _input = input;
            Name = name;
            _root = input != null ? input.gameObject : null;
        }

        public A11yNavItem(Button button, string name, System.Func<string> dynamicText = null, System.Action activate = null)
        {
            Kind = A11yItemKind.Button;
            _button = button;
            Name = name;
            _dynamicText = dynamicText;
            _activate = activate;
            _root = button != null ? button.gameObject : null;
        }

        public A11yNavItem(Toggle toggle, string name)
        {
            Kind = A11yItemKind.Toggle;
            _toggle = toggle;
            Name = name;
            _root = toggle != null ? toggle.gameObject : null;
        }

        /// <summary>数值控件。组里只有它时，左右光标直接改值。</summary>
        public A11yNavItem(Slider slider, string name, System.Func<string> dynamicText = null)
        {
            Kind = A11yItemKind.Slider;
            _slider = slider;
            Name = name;
            _dynamicText = dynamicText;
            _root = slider != null ? slider.gameObject : null;
        }

        private A11yNavItem(GameObject root, A11yItemKind kind, string name,
            System.Func<string> text, System.Action activate)
        {
            Kind = kind;
            Name = name;
            _root = root;
            _dynamicText = text;
            _activate = activate;
        }

        /// <summary>
        /// 没有 Button 组件的可点项（社区关卡卡片、房间条目自己接指针事件，
        /// 不是 Button）。用根对象 + 自定义文本 + 自定义动作描述它。
        /// </summary>
        public static A11yNavItem Custom(GameObject root, A11yItemKind kind, string name,
            System.Func<string> text, System.Action activate)
        {
            return new A11yNavItem(root, kind, name, text, activate);
        }

        public A11yNavItem(TMP_Text label, string name)
        {
            Kind = A11yItemKind.Label;
            _label = label;
            Name = name;
            _root = label != null ? label.gameObject : null;
        }

        /// <summary>控件是否还存在于可见、可交互的层级里。</summary>
        public bool IsAlive
        {
            get
            {
                if (_root == null) return false;
                if (!_root.activeInHierarchy) return false;
                CanvasGroup[] groups = _root.GetComponentsInParent<CanvasGroup>();
                for (int i = 0; i < groups.Length; i++)
                {
                    if (groups[i].alpha <= 0.01f || !groups[i].blocksRaycasts) return false;
                }
                return true;
            }
        }

        /// <summary>按钮不可点时仍要能读到，但朗读里要说明，所以这里只决定能否触发。</summary>
        public bool IsInteractable
        {
            get
            {
                if (Kind == A11yItemKind.Button) return _button == null || _button.interactable;
                if (Kind == A11yItemKind.Input) return _input == null || _input.interactable;
                if (Kind == A11yItemKind.Toggle) return _toggle == null || _toggle.interactable;
                if (Kind == A11yItemKind.Slider) return _slider == null || _slider.interactable;
                return true;
            }
        }

        /// <summary>朗读文本。输入框读占位符或已填内容，密码框绝不读内容。</summary>
        public string GetSpeechText()
        {
            switch (Kind)
            {
                case A11yItemKind.Input:
                    return GetInputText();
                case A11yItemKind.Toggle:
                    return GetToggleText();
                case A11yItemKind.Slider:
                    return GetSliderText();
                case A11yItemKind.Button:
                    return GetButtonText();
                default:
                    return _label != null ? _label.text : Name;
            }
        }

        private string GetInputText()
        {
            if (_input == null) return Name;
            string placeholder = _input.placeholder != null
                ? (_input.placeholder is TMP_Text tmp ? tmp.text : _input.placeholder.name)
                : string.Empty;

            if (_input.contentType == TMP_InputField.ContentType.Password)
            {
                return string.IsNullOrEmpty(_input.text)
                    ? placeholder
                    : placeholder + A11yPhrases.Filled;
            }

            return string.IsNullOrEmpty(_input.text) ? placeholder : placeholder + A11yPhrases.Separator + _input.text;
        }

        private string GetSliderText()
        {
            string label = _dynamicText != null ? _dynamicText() : null;
            if (string.IsNullOrEmpty(label)) label = Name;
            return IsInteractable ? label : label + A11yPhrases.Unavailable;
        }

        /// <summary>左右改值。步长按控件自身设置取，整数值走 1。</summary>
        public void Step(int direction)
        {
            if (_slider == null || !_slider.interactable) return;
            float step = _slider.wholeNumbers ? 1f : Mathf.Max((_slider.maxValue - _slider.minValue) / 10f, 0.01f);
            _slider.value = Mathf.Clamp(_slider.value + step * direction, _slider.minValue, _slider.maxValue);
        }

        private string GetToggleText()
        {
            if (_toggle == null) return Name;
            string label = Name;
            TMP_Text child = _toggle.GetComponentInChildren<TMP_Text>();
            if (child != null && !string.IsNullOrEmpty(child.text)) label = child.text;
            return label + (IsInteractable ? "" : A11yPhrases.Unavailable);
        }

        private string GetButtonText()
        {
            // 优先读控件自身的文字，这样自动跟随游戏已有的本地化
            string text = _dynamicText != null ? _dynamicText() : GetChildText();
            if (string.IsNullOrEmpty(text)) text = Name;
            return IsInteractable ? text : text + A11yPhrases.Unavailable;
        }

        private string GetChildText()
        {
            if (_button == null) return null;
            TMP_Text child = _button.GetComponentInChildren<TMP_Text>(true);
            return child != null ? child.text : null;
        }

        /// <summary>开关项被回车时翻转状态。</summary>
        public void Flip()
        {
            if (_toggle == null || !_toggle.interactable) return;
            _toggle.isOn = !_toggle.isOn;
        }

        public bool IsOn => _toggle != null && _toggle.isOn;

        /// <summary>回车触发按钮动作。输入框的打开/关闭编辑由控制器处理。</summary>
        public void Activate()
        {
            if (_activate != null)
            {
                _activate();
                return;
            }
            if (_button != null && _button.interactable) _button.onClick.Invoke();
        }

        public TMP_InputField InputField => _input;
        public GameObject Root => _root;
    }
}
