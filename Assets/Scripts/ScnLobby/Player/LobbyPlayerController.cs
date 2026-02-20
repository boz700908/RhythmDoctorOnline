using UnityEngine;
using RDOnline;
using RDOnline.Network;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 大厅本地玩家控制器 - 使用 Rigidbody2D 物理移动、播放动画、上报 lobby/move
    /// 动画状态名: idle_left, idle_right, walk_left, walk_right
    /// </summary>
    public class LobbyPlayerController : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("移动速度（单位/秒）")]
        public float MoveSpeed = 5f;
        [Tooltip("Rigidbody2D，不填则 GetComponent")]
        public Rigidbody2D Rb;

        [Header("动画")]
        [Tooltip("动画状态名: idle_left, idle_right, walk_left, walk_right")]
        public Animator Animator;
        [Tooltip("动画所在层级，默认 0")]
        public int AnimatorLayer = 0;

        [Header("头顶 UI")]
        [Tooltip("头像/头像框/名字，不填则自动 GetComponentInChildren")]
        public LobbyPlayerHeadUI HeadUI;

        [Header("网络同步")]
        [Tooltip("位置变化超过此值才上报（避免微小抖动也发）")]
        public float SendPositionThreshold = 0.01f;

        /// <summary> 为 false 时玩家不可移动（如切到创建房间视角时）。 </summary>
        public static bool CanMove { get; set; } = true;

        private float _lastSentX, _lastSentY;
        private bool _lastFaceLeft = true;
        private Vector2 _moveInput;

        private static readonly int IdleLeft = Animator.StringToHash("idle_left");
        private static readonly int IdleRight = Animator.StringToHash("idle_right");
        private static readonly int WalkLeft = Animator.StringToHash("walk_left");
        private static readonly int WalkRight = Animator.StringToHash("walk_right");

        private void PlayLobbyAnimation(bool walking, bool faceLeft)
        {
            int stateName = walking ? (faceLeft ? WalkLeft : WalkRight) : (faceLeft ? IdleLeft : IdleRight);
            Animator.Play(stateName, AnimatorLayer);
        }

        private void Start()
        {
            CanMove = true;
            if (Rb == null)
                Rb = GetComponent<Rigidbody2D>();
            RefreshHeadUI();
        }

        private void FixedUpdate()
        {
            if (Rb == null) return;
            Vector2 vel = CanMove ? _moveInput * MoveSpeed : Vector2.zero;
#if UNITY_6000
            Rb.linearVelocity = vel;
#endif
            Rb.velocity = vel;
        }

        private void RefreshHeadUI()
        {
            var head = HeadUI != null ? HeadUI : GetComponentInChildren<LobbyPlayerHeadUI>(true);
            if (head != null && UserData.Instance != null)
                head.SetPlayerInfo(UserData.Instance.Username, UserData.Instance.Avatar, UserData.Instance.AvatarFrame, UserData.Instance.NameColor);
        }

        private void Update()
        {
            if (!CanMove)
            {
                _moveInput = Vector2.zero;
            }
            else
            {
                Vector2 joystick = VirtualJoystick.InputDirection;
                if (joystick.sqrMagnitude > 0.01f)
                {
                    _moveInput = joystick;
                    if (_moveInput.sqrMagnitude > 1f)
                        _moveInput.Normalize();
                }
                else
                {
                    float h = Input.GetAxisRaw("Horizontal");
                    float v = Input.GetAxisRaw("Vertical");
                    _moveInput = new Vector2(h, v);
                    if (_moveInput.sqrMagnitude > 1f)
                        _moveInput.Normalize();
                }
            }

            bool walking = _moveInput.sqrMagnitude > 0.01f;
            bool faceLeft = true;
            if (Mathf.Abs(_moveInput.x) > 0.01f)
                faceLeft = _moveInput.x < 0f;
            else
                faceLeft = _lastFaceLeft;

            _lastFaceLeft = faceLeft;
            if (Animator != null)
                PlayLobbyAnimation(walking, faceLeft);

            Vector3 pos = transform.localPosition;
            float x = pos.x, y = pos.y;
            if (Mathf.Abs(x - _lastSentX) > SendPositionThreshold || Mathf.Abs(y - _lastSentY) > SendPositionThreshold)
            {
                _lastSentX = x;
                _lastSentY = y;
                SendMove(x, y);
            }
        }

        private void SendMove(float x, float y)
        {
            if (WebSocketManager.Instance == null || !WebSocketManager.Instance.IsConnected)
                return;
            WebSocketManager.Instance.Send("lobby/move", new { x, y });
        }
    }
}
