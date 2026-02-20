using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 虚拟摇杆：挂在本体（背景圆）上，拖拽子物体 Handle 时输出方向。
    /// 结构：父物体挂本脚本 + 子物体 Image 作为 Handle。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("引用")]
        [Tooltip("摇杆头（小圆），不填则取第一个子物体 RectTransform")]
        public RectTransform Handle;

        [Header("参数")]
        [Tooltip("摇杆头可移动半径（相对本 RectTransform 的本地像素）")]
        public float MovementRadius = 60f;

        /// <summary> 当前摇杆方向，范围约 [-1,1]，松手为 Vector2.zero。供 LobbyPlayerController 等读取。 </summary>
        public static Vector2 InputDirection { get; private set; }

        private RectTransform _rect;
        private Canvas _canvas;
        private Camera _cam;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (Handle == null)
                Handle = _rect.childCount > 0 ? _rect.GetChild(0) as RectTransform : null;
            _canvas = GetComponentInParent<Canvas>();
            _cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        }

        private void OnDisable()
        {
            InputDirection = Vector2.zero;
            if (Handle != null)
                Handle.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Handle == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera ?? _cam, out Vector2 localPos))
                return;
            Vector2 clamped = Vector2.ClampMagnitude(localPos, MovementRadius);
            Handle.anchoredPosition = clamped;
            InputDirection = MovementRadius > 0f ? clamped / MovementRadius : Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Handle != null)
                Handle.anchoredPosition = Vector2.zero;
            InputDirection = Vector2.zero;
        }
    }
}
