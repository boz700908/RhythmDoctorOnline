using UnityEngine;

namespace RDOnline.Utils
{
    /// <summary>
    /// 安全区域适配器 - 自动调整画布以适应屏幕安全区域
    /// 用于处理刘海屏、圆角屏幕等设备的显示适配
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaAdapter : MonoBehaviour
    {
        [Header("设置")]
        [Tooltip("是否在启动时自动适配")]
        public bool AdaptOnStart = true;

        [Tooltip("是否在屏幕方向改变时自动适配")]
        public bool AdaptOnOrientationChange = true;

        [Header("调试信息")]
        [Tooltip("是否显示调试日志")]
        public bool ShowDebugLog = false;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private ScreenOrientation _lastOrientation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (AdaptOnStart)
            {
                ApplySafeArea();
            }
        }

        private void Update()
        {
            if (AdaptOnOrientationChange)
            {
                // 检查屏幕方向是否改变
                if (Screen.orientation != _lastOrientation)
                {
                    _lastOrientation = Screen.orientation;
                    ApplySafeArea();
                }

                // 检查安全区域是否改变
                if (Screen.safeArea != _lastSafeArea)
                {
                    ApplySafeArea();
                }
            }
        }

        /// <summary>
        /// 应用安全区域
        /// </summary>
        public void ApplySafeArea()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = GetSafeArea();
            _lastSafeArea = safeArea;

            // 计算安全区域的锚点
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            // 转换为相对于屏幕的比例
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // 应用到 RectTransform
            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            if (ShowDebugLog)
            {
                Debug.Log($"[SafeAreaAdapter] 安全区域已应用");
                Debug.Log($"  屏幕尺寸: {Screen.width}x{Screen.height}");
                Debug.Log($"  安全区域: {safeArea}");
                Debug.Log($"  锚点Min: {anchorMin}");
                Debug.Log($"  锚点Max: {anchorMax}");
            }
        }

        /// <summary>
        /// 获取安全区域（支持编辑器模拟）
        /// </summary>
        private Rect GetSafeArea()
        {
            // 直接使用 Screen.safeArea，在编辑器和运行时都能正确获取
            // Unity 编辑器的 Game 视图会根据选择的设备自动提供正确的安全区域
            return Screen.safeArea;
        }
    }
}
