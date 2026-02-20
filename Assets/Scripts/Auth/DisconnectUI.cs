using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using RDOnline.Network;

namespace RDOnline.Auth
{
    /// <summary>
    /// 断线提示UI
    /// </summary>
    public class DisconnectUI : MonoBehaviour
    {
        [Header("画布组件")]
        public CanvasGroup CanvasGroup;

        [Header("返回登录按钮")]
        public Button BtnBackToLogin;

        [Header("登录场景名称")]
        public string LoginSceneName = "Login";

        [Header("动画设置")]
        public float FadeDuration = 0.3f;

        private void Start()
        {
            // 初始隐藏
            HidePanel();

            // 监听连接事件
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnConnected += OnConnected;
                WebSocketManager.Instance.OnDisconnected += OnDisconnected;
            }

            // 按钮点击事件
            if (BtnBackToLogin != null)
            {
                BtnBackToLogin.onClick.AddListener(OnBackToLoginClick);
            }
        }

        private void OnDestroy()
        {
            // 取消监听
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnConnected -= OnConnected;
                WebSocketManager.Instance.OnDisconnected -= OnDisconnected;
            }

            // 移除按钮监听
            if (BtnBackToLogin != null)
            {
                BtnBackToLogin.onClick.RemoveListener(OnBackToLoginClick);
            }
        }

        /// <summary>
        /// 连接成功回调
        /// </summary>
        private void OnConnected()
        {
            Debug.Log("[DisconnectUI] 检测到重新连接");
            HidePanelWithAnimation();
        }

        /// <summary>
        /// 断开连接回调
        /// </summary>
        private void OnDisconnected(string reason)
        {
            Debug.Log($"[DisconnectUI] 检测到断开连接: {reason}");
            ShowPanel();
        }

        /// <summary>
        /// 返回登录按钮点击
        /// </summary>
        private void OnBackToLoginClick()
        {
            Debug.Log("[DisconnectUI] 返回登录场景");
            ScnLoading.LoadScenes(LoginSceneName);
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        private void ShowPanel()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.DOFade(1, FadeDuration).SetEase(Ease.OutCubic);
            }
        }

        /// <summary>
        /// 隐藏面板（立即隐藏，无动画）
        /// </summary>
        private void HidePanel()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 0;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 隐藏面板（带淡出动画）
        /// </summary>
        private void HidePanelWithAnimation()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.DOFade(0, FadeDuration)
                    .SetEase(Ease.InCubic)
                    .OnComplete(() =>
                    {
                        CanvasGroup.interactable = false;
                        CanvasGroup.blocksRaycasts = false;
                    });
            }
        }
    }
}
