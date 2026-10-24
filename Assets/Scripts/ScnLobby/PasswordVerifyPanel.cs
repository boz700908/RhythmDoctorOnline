using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using RDOnline.Network;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 密码验证面板 - 用于验证房间密码（单例）
    /// </summary>
    public class PasswordVerifyPanel : MonoBehaviour
    {
        public static PasswordVerifyPanel Instance { get; private set; }

        [Header("UI组件")]
        [Tooltip("密码输入框")]
        public TMP_InputField PasswordInput;
        [Tooltip("确认按钮")]
        public Button ConfirmButton;
        [Tooltip("取消按钮")]
        public Button CancelButton;
        [Tooltip("面板容器（用于缩放动画）")]
        public Transform PanelContainer;

        [Header("动画设置")]
        [Tooltip("弹出动画时长")]
        public float ShowDuration = 0.3f;
        [Tooltip("隐藏动画时长")]
        public float HideDuration = 0.2f;

        // 当前验证的房间信息
        private string _currentRoomId;
        private string _currentRoomName;
        private int _currentMaxPlayers;
        private bool _currentHasPassword;
        private string _currentChartUrl;
        private string _currentChartName;
        private int _currentOwnerId;
        private string _currentStatus;
        private int _currentPlayerCount;

        private bool _isVerifying;

        /// <summary>无障碍导航：显示时抢键盘，关闭时交还大厅。</summary>
        private RDOnline.A11y.PasswordVerifyA11y _a11y;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 绑定按钮事件
            if (ConfirmButton != null)
                ConfirmButton.onClick.AddListener(OnConfirmClick);
            if (CancelButton != null)
                CancelButton.onClick.AddListener(OnCancelClick);

            // 无障碍导航，显示时才接管键盘
            _a11y = RDOnline.A11y.PasswordVerifyA11y.Attach(this);

            // 初始化隐藏
            gameObject.SetActive(false);
            if (PanelContainer != null)
                PanelContainer.localScale = Vector3.zero;
        }

        /// <summary>
        /// 显示密码验证面板
        /// </summary>
        public void Show(string roomId, string roomName, int maxPlayers, bool hasPassword,
                        string chartUrl, string chartName, int ownerId, string status, int playerCount)
        {
            // 保存房间信息
            _currentRoomId = roomId;
            _currentRoomName = roomName;
            _currentMaxPlayers = maxPlayers;
            _currentHasPassword = hasPassword;
            _currentChartUrl = chartUrl;
            _currentChartName = chartName;
            _currentOwnerId = ownerId;
            _currentStatus = status;
            _currentPlayerCount = playerCount;

            // 清空输入框
            if (PasswordInput != null)
                PasswordInput.text = "";

            // 显示面板
            gameObject.SetActive(true);

            // 播放弹出动画
            if (PanelContainer != null)
            {
                PanelContainer.localScale = Vector3.zero;
                PanelContainer.DOScale(Vector3.one, ShowDuration).SetEase(Ease.OutBack);
            }

            // 弹窗盖住大厅，必须接管键盘；等弹出动画走完控件才可见，否则焦点落空
            if (_a11y != null) StartCoroutine(TakeOverAfterShow());

            Debug.Log($"[PasswordVerifyPanel] 显示密码验证面板，房间: {roomName}");
        }

        /// <summary>
        /// 隐藏密码验证面板
        /// </summary>
        private void Hide()
        {
            // 键盘立刻还给大厅，用户不必等收起动画
            if (_a11y != null) _a11y.Release();

            if (PanelContainer != null)
            {
                PanelContainer.DOScale(Vector3.zero, HideDuration).SetEase(Ease.InBack).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>等弹出动画结束、控件真正可见后再聚焦，否则导航器会把焦点略过。</summary>
        private IEnumerator TakeOverAfterShow()
        {
            yield return new WaitForSeconds(ShowDuration);
            if (_a11y != null && gameObject.activeInHierarchy) _a11y.TakeOver();
        }

        /// <summary>
        /// 确认按钮点击事件
        /// </summary>
        private void OnConfirmClick()
        {
            if (_isVerifying)
            {
                ScrAlert.Show("正在验证中，请稍候", true);
                return;
            }

            // 获取输入的密码
            string password = PasswordInput != null ? PasswordInput.text.Trim() : "";

            if (string.IsNullOrEmpty(password))
            {
                ScrAlert.Show("请输入密码", true);
                return;
            }

            // 开始验证密码
            VerifyPassword(password);
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void OnCancelClick()
        {
            Debug.Log("[PasswordVerifyPanel] 取消验证密码");
            Hide();
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        private void VerifyPassword(string password)
        {
            _isVerifying = true;

            var data = new
            {
                roomId = _currentRoomId,
                password = password
            };

            Debug.Log($"[PasswordVerifyPanel] 开始验证密码，房间: {_currentRoomId}");

            WebSocketManager.Instance.Send("room/verifyPassword", data, (res) =>
            {
                _isVerifying = false;

                if (res.success)
                {
                    Debug.Log("[PasswordVerifyPanel] 密码验证成功");
                    OnPasswordVerified(password);
                }
                else
                {
                    Debug.LogWarning($"[PasswordVerifyPanel] 密码验证失败: {res.message}");
                    ScrAlert.Show($"密码验证失败: {res.message}", true);
                }
            });
        }

        /// <summary>
        /// 密码验证成功后的处理
        /// </summary>
        private void OnPasswordVerified(string password)
        {
            // 保存房间数据到 RoomData
            if (RoomData.Instance != null)
            {
                RoomData.Instance.SetCurrentRoom(
                    _currentRoomId,
                    _currentRoomName,
                    _currentMaxPlayers,
                    _currentHasPassword,
                    password,
                    _currentChartUrl,
                    _currentChartName,
                    _currentOwnerId,
                    _currentStatus,
                    _currentPlayerCount
                );

                Debug.Log($"[PasswordVerifyPanel] 房间数据已保存到 RoomData，房间ID: {_currentRoomId}");
            }

            // 显示成功提示
            ScrAlert.Show("密码验证成功！", true);

            // 隐藏面板
            Hide();

            // 跳转到房间场景
            ScnLoading.LoadScenes("ScnRoom");
        }
    }
}

