using UnityEngine;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 大厅内网络玩家表现 - 只负责接收位置与推断动画，不处理输入
    /// </summary>
    public class NetworkPlayerView : MonoBehaviour
    {
        [Header("动画")]
        [Tooltip("动画状态名: idle_left, idle_right, walk_left, walk_right")]
        public Animator Animator;
        [Tooltip("动画所在层级，默认 0")]
        public int AnimatorLayer = 0;

        [Header("头顶 UI")]
        [Tooltip("头像/头像框/名字，不填则自动 GetComponentInChildren")]
        public LobbyPlayerHeadUI HeadUI;

        [Header("位置插值")]
        [Tooltip("向目标位置平滑移动的速度（单位/秒），越大跟得越紧")]
        public float InterpolationSpeed = 12f;

        /// <summary> 网络玩家 userId </summary>
        public int UserId { get; private set; }

        private float _targetX;
        private float _targetY;
        private bool _faceLeft = true;
        private const float MoveThreshold = 0.001f;

        /// <summary>
        /// 初始化网络玩家（由 PlayerManager 调用）
        /// </summary>
        public void Init(int userId, float x, float y, string username = null, string avatarUrl = null, string avatarFrameUrl = null, string nameColor = null)
        {
            UserId = userId;
            _targetX = x;
            _targetY = y;
            transform.localPosition = new Vector3(x, y, 0f);

            var head = HeadUI != null ? HeadUI : GetComponentInChildren<LobbyPlayerHeadUI>(true);
            if (head != null)
                head.SetPlayerInfo(username ?? "", avatarUrl, avatarFrameUrl, nameColor);
        }

        /// <summary>
        /// 更新目标位置；实际位置在 Update 中平滑插值
        /// </summary>
        public void SetPosition(float x, float y)
        {
            _targetX = x;
            _targetY = y;
        }

        private void Update()
        {
            Vector3 current = transform.localPosition;
            Vector3 target = new Vector3(_targetX, _targetY, 0f);
            transform.localPosition = Vector3.MoveTowards(current, target, InterpolationSpeed * Time.deltaTime);

            float dx = _targetX - current.x;
            float dy = _targetY - current.y;
            float distSq = dx * dx + dy * dy;
            bool moving = distSq > (MoveThreshold * MoveThreshold);
            if (moving && Mathf.Abs(dx) > 0.0001f)
                _faceLeft = dx < 0f;

            SetAnimation(moving, _faceLeft);
        }

        private static readonly int IdleLeft = Animator.StringToHash("idle_left");
        private static readonly int IdleRight = Animator.StringToHash("idle_right");
        private static readonly int WalkLeft = Animator.StringToHash("walk_left");
        private static readonly int WalkRight = Animator.StringToHash("walk_right");

        /// <summary>
        /// 设置动画状态：行走/待机、朝左/朝右（状态名 idle_left, idle_right, walk_left, walk_right）
        /// </summary>
        public void SetAnimation(bool walking, bool faceLeft)
        {
            if (Animator == null) return;
            int stateName = walking ? (faceLeft ? WalkLeft : WalkRight) : (faceLeft ? IdleLeft : IdleRight);
            Animator.Play(stateName, AnimatorLayer);
        }
    }
}
