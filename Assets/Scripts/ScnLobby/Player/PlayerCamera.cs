using System.Collections;
using UnityEngine;

/// <summary>
/// 2D游戏摄像机控制器
/// 实现平滑跟随和地图边界限制
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    #region Singleton
    public static PlayerCamera Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[PlayerCamera] 场景中存在多个PlayerCamera实例，销毁多余的实例");
            Destroy(gameObject);
        }
    }
    #endregion

    [Header("跟随设置")]
    [SerializeField] private Transform followTarget;  // 跟随目标（玩家）
    [SerializeField] private float smoothSpeed = 5f;  // 平滑跟随速度

    [Header("地图边界")]
    [SerializeField] private BoxCollider2D mapBounds;  // 地图边界（用BoxCollider2D定义）

    [Header("边界平滑过渡设置")]
    [Tooltip("边界切换过渡距离阈值（摄像机距离玩家多近时开始应用边界限制）")]
    [SerializeField] private float transitionDistanceThreshold = 0.5f;

    [Header("多设备适配")]
    [Tooltip("勾选后根据屏幕比例自动调整正交大小，使不同设备上可见范围一致")]
    [SerializeField] private bool useAdaptiveSize = true;
    [Tooltip("设计时的 Orthographic Size（在参考分辨率下相机半高，单位：世界单位）")]
    [SerializeField] private float referenceOrthographicSize = 5f;
    [Tooltip("设计参考分辨率宽（如 1920）")]
    [SerializeField] private float referenceWidth = 1920f;
    [Tooltip("设计参考分辨率高（如 1080）")]
    [SerializeField] private float referenceHeight = 1080f;
    [Tooltip("0=按宽度适配(不同屏幕看到相同世界宽度), 1=按高度适配(相同高度)")]
    [Range(0f, 1f)] [SerializeField] private float matchWidthOrHeight = 0f;

    private Camera cam;
    private float cameraHalfHeight;
    private float cameraHalfWidth;
    private bool isTransitioningBounds = false; // 是否正在切换边界
    private BoxCollider2D pendingBounds = null; // 待切换的边界

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("[PlayerCamera] 未找到Camera组件！请将此脚本挂载在摄像机上。");
            enabled = false;
            return;
        }

        // 多设备适配：按参考分辨率与当前宽高比统一可见范围
        if (useAdaptiveSize && cam.orthographic && referenceWidth > 0 && referenceHeight > 0)
        {
            float designAspect = referenceWidth / referenceHeight;
            float currentAspect = (float)Screen.width / Screen.height;
            float factor = Mathf.Pow(designAspect / currentAspect, 1f - matchWidthOrHeight);
            cam.orthographicSize = referenceOrthographicSize * factor;
        }

        // 计算摄像机的视野大小（用于边界计算）
        CalculateCameraSize();
    }

    void LateUpdate()
    {
        if (followTarget == null) return;

        // 跟随目标
        FollowTarget();
    }

    /// <summary>
    /// 计算摄像机视野大小
    /// </summary>
    private void CalculateCameraSize()
    {
        if (cam.orthographic)
        {
            // 正交摄像机
            cameraHalfHeight = cam.orthographicSize;
            cameraHalfWidth = cameraHalfHeight * cam.aspect;
        }
        else
        {
            Debug.LogWarning("[PlayerCamera] 建议使用正交摄像机（Orthographic）来制作2D游戏");
            // 透视摄像机的计算（不常用）
            cameraHalfHeight = Mathf.Abs(transform.position.z) * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cameraHalfWidth = cameraHalfHeight * cam.aspect;
        }
    }

    /// <summary>
    /// 跟随目标
    /// </summary>
    private void FollowTarget()
    {
        // 目标位置（保持当前Z轴）
        Vector3 targetPosition = new Vector3(
            followTarget.position.x,
            followTarget.position.y,
            transform.position.z
        );

        // 平滑插值
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );

        // 如果正在切换边界，先让摄像机平滑跟随玩家
        if (isTransitioningBounds && pendingBounds != null)
        {
            // 计算摄像机到玩家的距离
            float distanceToTarget = Vector3.Distance(smoothedPosition, targetPosition);

            // 如果摄像机已经接近玩家位置，应用新边界限制
            if (distanceToTarget <= transitionDistanceThreshold)
            {
                // 切换到新边界
                mapBounds = pendingBounds;
                pendingBounds = null;
                isTransitioningBounds = false;
                CalculateCameraSize();

                // 应用边界限制
                smoothedPosition = ClampToBounds(smoothedPosition);
            }
            // 否则，暂时不应用边界限制，让摄像机自由跟随玩家
        }
        else
        {
            // 正常情况：应用边界限制
            if (mapBounds != null)
            {
                smoothedPosition = ClampToBounds(smoothedPosition);
            }
        }

        // 应用到摄像机
        transform.position = smoothedPosition;
    }

    /// <summary>
    /// 限制摄像机位置在地图边界内
    /// </summary>
    private Vector3 ClampToBounds(Vector3 position)
    {
        // 获取地图边界
        Bounds bounds = mapBounds.bounds;

        // 计算摄像机可以移动的范围
        float minX = bounds.min.x + cameraHalfWidth;
        float maxX = bounds.max.x - cameraHalfWidth;
        float minY = bounds.min.y + cameraHalfHeight;
        float maxY = bounds.max.y - cameraHalfHeight;

        // 如果地图比摄像机视野还小，居中显示
        if (minX > maxX)
        {
            float centerX = (bounds.min.x + bounds.max.x) / 2f;
            position.x = centerX;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            float centerY = (bounds.min.y + bounds.max.y) / 2f;
            position.y = centerY;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    /// <summary>
    /// 设置地图边界（直接切换，无过渡）
    /// </summary>
    public void SetMapBounds(BoxCollider2D bounds)
    {
        if (mapBounds == bounds) return;

        // 停止任何正在进行的平滑过渡
        if (isTransitioningBounds)
        {
            isTransitioningBounds = false;
            pendingBounds = null;
        }

        // 立即切换边界
        mapBounds = bounds;
        CalculateCameraSize();
    }

    /// <summary>
    /// 平滑过渡地图边界（有过渡）
    /// 原理：先解除地图边界，保存新的边界，然后让摄像机自由跟随目标，
    /// 等到摄像机快接近目标，再应用新的边界
    /// </summary>
    public void SmoothTransitionBounds(BoxCollider2D newBounds)
    {
        if (mapBounds == newBounds && !isTransitioningBounds) return;

        // 先解除地图边界
        mapBounds = null;

        // 保存新的边界
        pendingBounds = newBounds;
        isTransitioningBounds = true;

        // 让摄像机自由跟随目标，在 FollowTarget() 中会处理边界应用
    }


    /// <summary>
    /// Gizmos显示边界
    /// </summary>
    void OnDrawGizmos()
    {
        if (mapBounds != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mapBounds.bounds.center, mapBounds.bounds.size);
        }

        // 显示摄像机视野范围
        if (cam != null || Application.isPlaying)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null)
            {
                float halfHeight = cam.orthographic ? cam.orthographicSize : cameraHalfHeight;
                float halfWidth = halfHeight * cam.aspect;

                Gizmos.color = Color.green;
                Vector3 center = transform.position;
                Vector3 size = new Vector3(halfWidth * 2, halfHeight * 2, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}

