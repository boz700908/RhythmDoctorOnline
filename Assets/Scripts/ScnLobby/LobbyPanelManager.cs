using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 大厅面板管理器 - 管理主面板和创建房间面板的Tab切换
    /// </summary>
    public class LobbyPanelManager : MonoBehaviour
    {
        [Header("面板引用")]
        [Tooltip("主面板（房间列表）")]
        public CanvasGroup MainPanel;
        [Tooltip("创建房间面板")]
        public CanvasGroup CreateRoomPanel;

        [Header("Tab按钮引用")]
        [Tooltip("大厅Tab按钮")]
        public Button LobbyTabButton;
        [Tooltip("创建房间Tab按钮")]
        public Button CreateRoomTabButton;

        [Header("动画设置")]
        [Tooltip("面板切换动画时长")]
        public float FadeDuration = 0.3f;

        private void Start()
        {
            InitializePanels();
            BindTabButtons();
        }

        /// <summary>
        /// 绑定Tab按钮事件
        /// </summary>
        private void BindTabButtons()
        {
            // 大厅Tab按钮
            if (LobbyTabButton != null)
                LobbyTabButton.onClick.AddListener(ShowMainPanel);

            // 创建房间Tab按钮
            if (CreateRoomTabButton != null)
                CreateRoomTabButton.onClick.AddListener(ShowCreateRoomPanel);
        }

        /// <summary>
        /// 初始化面板状态
        /// </summary>
        private void InitializePanels()
        {
            // 主面板：默认显示
            if (MainPanel != null)
            {
                MainPanel.alpha = 1f;
                MainPanel.blocksRaycasts = true;
                MainPanel.gameObject.SetActive(true);
            }

            // 创建房间面板：默认隐藏
            if (CreateRoomPanel != null)
            {
                CreateRoomPanel.alpha = 0f;
                CreateRoomPanel.blocksRaycasts = false;
                CreateRoomPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 显示创建房间面板（淡入淡出切换）
        /// </summary>
        public void ShowCreateRoomPanel()
        {
            if (CreateRoomPanel == null || MainPanel == null) return;

            // 隐藏主面板
            MainPanel.DOFade(0f, FadeDuration).OnComplete(() =>
            {
                MainPanel.blocksRaycasts = false;
                MainPanel.gameObject.SetActive(false);
            });

            // 显示创建房间面板
            CreateRoomPanel.gameObject.SetActive(true);
            CreateRoomPanel.alpha = 0f;
            CreateRoomPanel.DOFade(1f, FadeDuration).OnStart(() =>
            {
                CreateRoomPanel.blocksRaycasts = true;
            });
        }

        /// <summary>
        /// 显示主面板（淡入淡出切换）
        /// </summary>
        public void ShowMainPanel()
        {
            if (MainPanel == null || CreateRoomPanel == null) return;

            // 隐藏创建房间面板
            CreateRoomPanel.DOFade(0f, FadeDuration).OnComplete(() =>
            {
                CreateRoomPanel.blocksRaycasts = false;
                CreateRoomPanel.gameObject.SetActive(false);
            });

            // 显示主面板
            MainPanel.gameObject.SetActive(true);
            MainPanel.alpha = 0f;
            MainPanel.DOFade(1f, FadeDuration).OnStart(() =>
            {
                MainPanel.blocksRaycasts = true;
            });
        }
    }
}
