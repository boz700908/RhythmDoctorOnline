using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline.Auth;

namespace RDOnline.A11y
{
    /// <summary>
    /// 断线提示面板的无障碍。
    ///
    /// 这个面板挂在会常驻的 GameBootstrap 下，所以登录、大厅、房间、游戏里
    /// 它都可能盖到当前界面上来。因此它必须临时接管键盘，关掉时把键盘
    /// 还给当前场景的控制器。
    ///
    /// 面板本身只有一句固定文案和一个「重新连接」按钮，所以清单是：
    /// 提示（只念，不占焦点）+ 按钮（回车回登录界面重新连接）。
    /// 断线原因只有 WebSocket 关闭码，那串数字对用户没有意义，不念。
    /// </summary>
    public class DisconnectA11y : MonoBehaviour
    {
        private DisconnectUI _ui;
        private A11yNavigator _nav;
        private A11yNavItem _button;
        private bool _built;
        private bool _open;
        private bool _leaving;

        private static string RowInfoName => A11yPhrases.Text("提示", "Message");
        private static string RowButtonName => A11yPhrases.Text("按钮", "Buttons");

        private static DisconnectA11y _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
            UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // 这个面板只在 StartUp 场景建一次，之后跟着 GameBootstrap 常驻
            if (_instance != null) return;
            DisconnectUI ui = Object.FindObjectOfType<DisconnectUI>();
            if (ui == null) return;

            ui.gameObject.AddComponent<DisconnectA11y>();
        }

        private void OnEnable()
        {
            _instance = this;
        }

        private void OnDisable()
        {
            if (_instance == this) _instance = null;
            if (_nav != null) _nav.Deactivate();
        }

        private void Awake()
        {
            _ui = GetComponent<DisconnectUI>();
        }

        private void Update()
        {
            if (_ui == null) return;

            bool open = IsOpen();

            // 面板是淡入淡出的，所以在打开期间反复确认；进出各处理一次即可
            if (open && !_open)
            {
                _open = true;
                TakeOver();
            }
            else if (!open && _open)
            {
                _open = false;
                Release();
            }

            // 面板盖着的时候别的界面可能刚切进来抢走键盘（例如回登录场景），
            // 这里把键盘收回来：面板挡住的东西不该还能被操作。
            if (_open && !_leaving && _nav != null && A11yNavigator.Active != _nav)
            {
                _nav.Resume(false);
            }
        }

        /// <summary>
        /// 以遮罩是否吃点击为准：面板淡出只改透明度，遮罩关掉的瞬间才算收起。
        /// 透明度也一起看，是为了应对动画未生效时遮罩还开着的旧状态。
        /// </summary>
        private bool IsOpen()
        {
            if (_ui == null || _ui.CanvasGroup == null) return false;
            if (!_ui.CanvasGroup.blocksRaycasts) return false;
            return _ui.CanvasGroup.alpha > 0.01f || _ui.CanvasGroup.interactable;
        }

        private void TakeOver()
        {
            _leaving = false;

            if (_nav == null) _nav = gameObject.AddComponent<A11yNavigator>();

            _nav.ListName = A11yPhrases.Text("连接断开", "Disconnected");
            _nav.BackAction = null;

            if (!_built) Build();

            _nav.BeginRebuild();
            _nav.Clear();

            _nav.StartRow(RowInfoName);
            _nav.Add(A11yNavItem.Custom(gameObject, A11yItemKind.Label,
                A11yPhrases.Text("断线提示", "Disconnect notice"),
                NoticeSpeech, null));

            if (_button != null)
            {
                _nav.StartRow(RowButtonName);
                _nav.Add(_button);
            }

            // 焦点直接落在重连按钮上：这时用户能做的只有这一件事
            if (_button != null) _nav.FocusFirst(false);
            else _nav.EndRebuild(false);

            _nav.SayIntro(A11yPhrases.Text(
                "与服务器的连接断开了。上下光标换项，回车返回登录界面重新连接。",
                "The connection to the server was lost. Up and down switch items. Enter returns to the login screen to reconnect."));

            // 面板原文放在最后读，免得被操作提示盖掉
            A11yNavigator.SayInfo(NoticeSpeech(), NarrationCategory.Notification);
        }

        private void Release()
        {
            // 先看清键盘在谁手里：如果不是自己，说明场景已经换过了
            bool wasMine = A11yNavigator.Active != null && A11yNavigator.Active == _nav;

            if (_nav != null) _nav.Deactivate();

            // 刚按了按钮要走人：旧场景马上就没了，不必再把键盘还回去
            if (!wasMine || _leaving) return;

            // 面板跟着 GameBootstrap 常驻，把键盘还给当前场景的控制器
            if (RDOnline.ScnRoom.RoomA11y.Instance != null)
                RDOnline.ScnRoom.RoomA11y.Instance.Resume();
            else if (LobbyA11y.Instance != null)
                LobbyA11y.Instance.Resume();
        }

        private void Build()
        {
            _built = true;

            if (_ui.BtnBackToLogin == null) return;

            _button = new A11yNavItem(_ui.BtnBackToLogin,
                A11yPhrases.Text("重新连接", "Reconnect"),
                null,
                OnReconnect);
        }

        /// <summary>
        /// 面板上的固定提示写的是「您似乎已与网络断开连接」，按钮写的是「重新连接」。
        /// 这里读面板自己的文字，跟着它走。
        /// </summary>
        private string NoticeSpeech()
        {
            TMP_Text label = FindLabel();

            return label != null && !string.IsNullOrEmpty(label.text)
                ? label.text
                : A11yPhrases.Text("连接已断开", "Connection lost");
        }

        /// <summary>
        /// 面板上那块提示文字叫 Tips，按名字找，免得抓到按钮上的字。
        /// 找不到就退一步：取按钮之外的第一段文字。
        /// </summary>
        private TMP_Text FindLabel()
        {
            if (_ui == null || _ui.CanvasGroup == null) return null;

            TMP_Text[] all = _ui.CanvasGroup.GetComponentsInChildren<TMP_Text>(true);
            if (all.Length == 0) return null;

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == "Tips") return all[i];
            }

            Transform button = _ui.BtnBackToLogin != null ? _ui.BtnBackToLogin.transform : null;
            for (int i = 0; i < all.Length; i++)
            {
                if (button != null && all[i].transform.IsChildOf(button)) continue;
                if (!string.IsNullOrEmpty(all[i].text)) return all[i];
            }

            return all[0];
        }

        /// <summary>
        /// 面板上这个按钮叫「重新连接」，实际动作是回登录场景重新登录。
        /// 跳场景要一点时间，先读一句，免得用户以为按键没生效。
        /// </summary>
        private void OnReconnect()
        {
            if (_ui == null || _ui.BtnBackToLogin == null) return;
            if (!_ui.BtnBackToLogin.interactable) return;

            _leaving = true;

            A11yNavigator.SayInfo(A11yPhrases.Text("正在返回登录界面，请在登录界面重新登录",
                "Returning to the login screen. Please log in again."), NarrationCategory.Notification);

            _ui.BtnBackToLogin.onClick.Invoke();

            // 面板跟着游戏常驻，切场景不会带走它。不管它就会一直盖在登录界面上
            // 挡鼠标，键盘也被这里的一次性接管占着。回登录界面后本来就是要重新
            // 登录，所以这里直接把面板收起来；下次真断线游戏会再把它弹出来。
            HidePanel();
        }

        /// <summary>
        /// 收起面板。先杀掉淡入淡出的补间，免得它在下一帧把透明度又改回去。
        /// </summary>
        private void HidePanel()
        {
            if (_ui == null || _ui.CanvasGroup == null) return;

            DG.Tweening.DOTween.Kill(_ui.CanvasGroup);

            _ui.CanvasGroup.alpha = 0f;
            _ui.CanvasGroup.interactable = false;
            _ui.CanvasGroup.blocksRaycasts = false;
        }
    }
}
