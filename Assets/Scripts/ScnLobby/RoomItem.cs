using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 房间项组件 - 显示单个房间的信息
    /// </summary>
    public class RoomItem : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("房间名称")]
        public TMP_Text RoomNameText;
        [Tooltip("房间人数")]
        public TMP_Text PlayerCountText;
        [Tooltip("房间状态")]
        public TMP_Text StatusText;
        [Tooltip("房间ID")]
        public TMP_Text RoomIdText;
        [Tooltip("密码图标")]
        public Image PasswordIcon;
        [Tooltip("加入按钮")]
        public Button JoinButton;

        // 房间完整信息
        private string _roomId;
        private string _roomName;
        private int _playerCount;
        private int _maxPlayers;
        private bool _hasPassword;
        private string _status;
        private string _chartUrl;
        private string _chartName;
        private int _ownerId;

        private void Awake()
        {
            // 自动获取组件
            if (JoinButton == null)
                JoinButton = GetComponentInChildren<Button>();

            // 禁用文本的射线检测
            if (RoomNameText != null)
                RoomNameText.raycastTarget = false;
            if (PlayerCountText != null)
                PlayerCountText.raycastTarget = false;
            if (StatusText != null)
                StatusText.raycastTarget = false;
            if (RoomIdText != null)
                RoomIdText.raycastTarget = false;

            // 绑定按钮点击事件
            if (JoinButton != null)
                JoinButton.onClick.AddListener(OnJoinButtonClick);
        }

        /// <summary>
        /// 设置完整房间数据
        /// </summary>
        public void SetRoomData(string roomId, string roomName, int playerCount, int maxPlayers,
            bool hasPassword, string status, string chartUrl, string chartName, int ownerId)
        {
            _roomId = roomId;
            _roomName = roomName;
            _playerCount = playerCount;
            _maxPlayers = maxPlayers;
            _hasPassword = hasPassword;
            _status = status;
            _chartUrl = chartUrl;
            _chartName = chartName;
            _ownerId = ownerId;

            // 更新UI显示
            if (RoomNameText != null)
                RoomNameText.text = roomName;

            if (PlayerCountText != null)
                PlayerCountText.text = $"房间人数：{playerCount}/{maxPlayers}";

            if (StatusText != null)
                StatusText.text = GetStatusText(status);

            if (RoomIdText != null)
                RoomIdText.text = $"ID {roomId}";

            if (PasswordIcon != null)
                PasswordIcon.gameObject.SetActive(hasPassword);

            // 更新加入按钮状态
            UpdateJoinButtonState();
        }

        /// <summary>
        /// 更新部分房间数据（用于 room/updated 事件）
        /// </summary>
        public void UpdateRoomData(string roomName = null, int? playerCount = null, int? maxPlayers = null,
            bool? hasPassword = null, string status = null, string chartUrl = null, string chartName = null, int? ownerId = null)
        {
            if (roomName != null)
            {
                _roomName = roomName;
                if (RoomNameText != null)
                    RoomNameText.text = roomName;
            }

            if (playerCount.HasValue)
                _playerCount = playerCount.Value;

            if (maxPlayers.HasValue)
                _maxPlayers = maxPlayers.Value;

            if (playerCount.HasValue && maxPlayers.HasValue && PlayerCountText != null)
                PlayerCountText.text = $"房间人数：{playerCount}/{maxPlayers}";

            if (hasPassword.HasValue)
            {
                _hasPassword = hasPassword.Value;
                if (PasswordIcon != null)
                    PasswordIcon.gameObject.SetActive(_hasPassword);
            }

            if (status != null)
            {
                _status = status;
                if (StatusText != null)
                    StatusText.text = GetStatusText(status);
            }

            if (chartUrl != null)
                _chartUrl = chartUrl;

            if (chartName != null)
                _chartName = chartName;

            if (ownerId.HasValue)
                _ownerId = ownerId.Value;

            // 更新加入按钮状态
            UpdateJoinButtonState();
        }

        /// <summary>
        /// 获取房间ID
        /// </summary>
        public string GetRoomId()
        {
            return _roomId;
        }

        /// <summary>
        /// 转换状态文本
        /// </summary>
        private string GetStatusText(string status)
        {
            return status switch
            {
                "waiting" => "等待中",
                "playing" => "游戏中",
                "finished" => "已结束",
                _ => status
            };
        }

        /// <summary>
        /// 加入按钮点击事件
        /// </summary>
        private void OnJoinButtonClick()
        {
            Debug.Log($"[RoomItem] 点击加入房间: {_roomId}");

            // 检查是否有密码
            if (_hasPassword)
            {
                // 有密码，弹出密码验证面板
                if (PasswordVerifyPanel.Instance != null)
                {
                    PasswordVerifyPanel.Instance.Show(_roomId, _roomName, _maxPlayers, _hasPassword,
                        _chartUrl, _chartName, _ownerId, _status, _playerCount);
                }
                else
                {
                    Debug.LogError("[RoomItem] PasswordVerifyPanel.Instance 为空");
                    ScrAlert.Show("密码验证面板未初始化", true);
                }
            }
            else
            {
                // 无密码，直接保存房间数据
                if (RoomData.Instance != null)
                {
                    RoomData.Instance.SetCurrentRoom(
                        _roomId, _roomName, _maxPlayers, _hasPassword, "",
                        _chartUrl, _chartName, _ownerId, _status, _playerCount
                    );
                    Debug.Log($"[RoomItem] 房间数据已保存到 RoomData，房间ID: {_roomId}");
                }

                ScrAlert.Show("准备加入房间", true);

                // 跳转到房间场景
                ScnLoading.LoadScenes("ScnRoom");
            }
        }

        /// <summary>
        /// 更新加入按钮状态
        /// </summary>
        private void UpdateJoinButtonState()
        {
            if (JoinButton == null) return;

            // 判断房间是否已满
            bool isFull = (_playerCount >= _maxPlayers);

            // 判断房间是否在游戏中
            bool isPlaying = (_status == "playing");

            // 如果房间满了或者正在游戏中，禁用按钮
            bool shouldDisable = isFull || isPlaying;

            JoinButton.interactable = !shouldDisable;

            Debug.Log($"[RoomItem] 更新按钮状态 - 房间ID: {_roomId}, 人数: {_playerCount}/{_maxPlayers}, 状态: {_status}, 按钮可用: {!shouldDisable}");
        }
    }
}
