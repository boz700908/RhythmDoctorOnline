using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RDOnline.ScnLobby;

namespace RDOnline.A11y
{
    /// <summary>
    /// 密码验证弹窗的无障碍导航。
    ///
    /// 这个弹窗盖在大厅上面，而不是切换场景，所以它必须临时接管键盘，
    /// 关掉时再把键盘还给大厅。行的划分：密码（输入框独占一组）、
    /// 确认与取消（一组两个按钮）。
    ///
    /// 密码本身永远不朗读，只读占位符和「已填写／未填写」。
    /// </summary>
    public class PasswordVerifyA11y : MonoBehaviour
    {
        private PasswordVerifyPanel _panel;
        private A11yNavigator _nav;
        private readonly List<A11yNavItem> _passwordRow = new List<A11yNavItem>();
        private readonly List<A11yNavItem> _buttonRow = new List<A11yNavItem>();
        private bool _built;

        private static string RowPasswordName => A11yPhrases.Text("密码", "Password");
        private static string RowButtonName => A11yPhrases.Text("按钮", "Buttons");

        /// <summary>
        /// 挂在密码面板自己的节点上。面板脚本在 Awake 里建好字段，这里在它之后执行。
        /// </summary>
        public static PasswordVerifyA11y Attach(PasswordVerifyPanel panel)
        {
            if (panel == null) return null;
            var existing = panel.GetComponent<PasswordVerifyA11y>();
            if (existing != null) return existing;
            return panel.gameObject.AddComponent<PasswordVerifyA11y>();
        }

        private void Awake()
        {
            _panel = GetComponent<PasswordVerifyPanel>();
        }

        /// <summary>弹窗正在显示时由面板调用：抢键盘。</summary>
        public void TakeOver()
        {
            if (_nav == null) _nav = gameObject.AddComponent<A11yNavigator>();

            _nav.ListName = A11yPhrases.Text("密码验证", "Password check");
            _nav.BackAction = Cancel;

            if (!_built) Build();

            _nav.BeginRebuild();
            _nav.Clear();

            _nav.StartRow(RowPasswordName);
            for (int i = 0; i < _passwordRow.Count; i++) _nav.Add(_passwordRow[i]);

            _nav.StartRow(RowButtonName);
            for (int i = 0; i < _buttonRow.Count; i++) _nav.Add(_buttonRow[i]);

            // 焦点直接落在密码框上，回车即可输入
            _nav.FocusFirst(false);
            _nav.SayIntro(A11yPhrases.Text("需要房间密码。回车开始输入，Esc 取消。",
                "Room password required. Enter starts typing, Escape cancels."));
        }

        /// <summary>弹窗关闭时由面板调用：把键盘还给大厅。</summary>
        public void Release()
        {
            if (_nav != null) _nav.Deactivate();

            if (LobbyA11y.Instance != null) LobbyA11y.Instance.Resume();
        }

        private void Build()
        {
            _built = true;

            if (_panel.PasswordInput != null)
            {
                _passwordRow.Add(new A11yNavItem(_panel.PasswordInput,
                    A11yPhrases.Text("房间密码", "Room password")));
            }

            if (_panel.ConfirmButton != null)
            {
                _buttonRow.Add(new A11yNavItem(_panel.ConfirmButton,
                    A11yPhrases.Text("确认", "Confirm"),
                    () => SpeechOf(_panel.ConfirmButton,
                        A11yPhrases.Text("确认", "Confirm")),
                    () => Confirm()));
            }

            if (_panel.CancelButton != null)
            {
                _buttonRow.Add(new A11yNavItem(_panel.CancelButton,
                    A11yPhrases.Text("取消", "Cancel")));
            }
        }

        /// <summary>验证中把确认按钮读成不可用，避免用户重复按。</summary>
        private string SpeechOf(UnityEngine.UI.Button button, string name)
        {
            if (button != null && !button.interactable) return name + A11yPhrases.Unavailable;
            return name;
        }

        /// <summary>
        /// 确认。面板自己带「正在验证」守卫，再按一次只会重复提示，
        /// 所以这里读一句状态，让盲人用户知道已经按下去了。
        /// </summary>
        private void Confirm()
        {
            if (_panel.ConfirmButton == null || !_panel.ConfirmButton.interactable) return;

            A11yNavigator.SayInfo(A11yPhrases.Text("正在验证密码", "Checking password"),
                NarrationCategory.Notification);
            _panel.ConfirmButton.onClick.Invoke();
        }

        private void Cancel()
        {
            if (_panel.CancelButton == null) return;
            if (!_panel.CancelButton.interactable) return;
            _panel.CancelButton.onClick.Invoke();
        }
    }
}
