using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 2D灯光类型
    /// </summary>
    public enum Light2DType
    {
        Point,          // 点光源
        Spot,           // 聚光灯
        Directional     // 方向光
    }

    /// <summary>
    /// 2D灯光组件
    /// 使用MeshRenderer和自定义Shader实现2D光照效果
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Light2D : MonoBehaviour
    {
        [Header("灯光类型")]
        [SerializeField] private Light2DType lightType = Light2DType.Point;

        [Header("基础属性")]
        [SerializeField] private Color lightColor = Color.white;
        [SerializeField, Range(0f, 10f)] private float intensity = 1f;
        [SerializeField, Range(0.1f, 50f)] private float range = 5f;

        [Header("聚光灯属性")]
        [SerializeField, Range(0f, 360f)] private float spotAngle = 45f;
        [SerializeField] private Vector2 direction = Vector2.down;

        [Header("网格设置")]
        [SerializeField, Range(8, 64)] private int segments = 32;

        [Header("材质")]
        [SerializeField] private Material lightMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh lightMesh;

        private Light2DType previousLightType;
        private float previousRange;
        private float previousSpotAngle;
        private int previousSegments;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            // 设置渲染层级
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 100;

            // 如果没有指定材质,尝试加载默认材质
            if (lightMaterial == null)
            {
                // 这里可以加载默认的Light2D材质
                Debug.LogWarning("[Light2D] 未指定材质,请在Inspector中设置Light2D材质");
            }
            else
            {
                meshRenderer.material = lightMaterial;
            }

            GenerateMesh();
            UpdateMaterial();
        }

        private void Update()
        {
            // 检查是否需要重新生成网格
            if (NeedRegenerateMesh())
            {
                GenerateMesh();
            }

            // 更新材质属性
            UpdateMaterial();
        }

        /// <summary>
        /// 检查是否需要重新生成网格
        /// </summary>
        private bool NeedRegenerateMesh()
        {
            return lightType != previousLightType ||
                   Mathf.Abs(range - previousRange) > 0.01f ||
                   Mathf.Abs(spotAngle - previousSpotAngle) > 0.01f ||
                   segments != previousSegments;
        }

        /// <summary>
        /// 生成灯光网格
        /// </summary>
        private void GenerateMesh()
        {
            if (lightMesh != null)
            {
                DestroyImmediate(lightMesh);
            }

            lightMesh = new Mesh();
            lightMesh.name = $"Light2D_{lightType}";

            switch (lightType)
            {
                case Light2DType.Point:
                    GeneratePointLightMesh();
                    break;
                case Light2DType.Spot:
                    GenerateSpotLightMesh();
                    break;
                case Light2DType.Directional:
                    GenerateDirectionalLightMesh();
                    break;
            }

            meshFilter.mesh = lightMesh;

            // 保存当前参数
            previousLightType = lightType;
            previousRange = range;
            previousSpotAngle = spotAngle;
            previousSegments = segments;
        }

        /// <summary>
        /// 生成点光源网格(圆形)
        /// </summary>
        private void GeneratePointLightMesh()
        {
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            int[] triangles = new int[segments * 3];

            // 中心点
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            // 圆周顶点
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * range;
                float y = Mathf.Sin(angle) * range;

                vertices[i + 1] = new Vector3(x, y, 0);
                uvs[i + 1] = new Vector2(
                    0.5f + Mathf.Cos(angle) * 0.5f,
                    0.5f + Mathf.Sin(angle) * 0.5f
                );
            }

            // 三角形索引
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }

            lightMesh.vertices = vertices;
            lightMesh.uv = uvs;
            lightMesh.triangles = triangles;
            lightMesh.RecalculateBounds();
        }

        /// <summary>
        /// 生成聚光灯网格(扇形)
        /// </summary>
        private void GenerateSpotLightMesh()
        {
            int segmentCount = Mathf.Max(3, Mathf.CeilToInt(segments * (spotAngle / 360f)));
            Vector3[] vertices = new Vector3[segmentCount + 2];
            Vector2[] uvs = new Vector2[segmentCount + 2];
            int[] triangles = new int[segmentCount * 3];

            // 中心点
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            // 计算方向角度
            float dirAngle = Mathf.Atan2(direction.y, direction.x);
            float halfAngle = spotAngle * 0.5f * Mathf.Deg2Rad;

            // 扇形顶点
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                float angle = dirAngle - halfAngle + t * spotAngle * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * range;
                float y = Mathf.Sin(angle) * range;

                vertices[i + 1] = new Vector3(x, y, 0);
                uvs[i + 1] = new Vector2(
                    0.5f + Mathf.Cos(angle) * 0.5f,
                    0.5f + Mathf.Sin(angle) * 0.5f
                );
            }

            // 三角形索引
            for (int i = 0; i < segmentCount; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            lightMesh.vertices = vertices;
            lightMesh.uv = uvs;
            lightMesh.triangles = triangles;
            lightMesh.RecalculateBounds();
        }

        /// <summary>
        /// 生成方向光网格(矩形)
        /// </summary>
        private void GenerateDirectionalLightMesh()
        {
            Vector3[] vertices = new Vector3[4];
            Vector2[] uvs = new Vector2[4];
            int[] triangles = new int[6];

            // 计算方向的垂直向量
            Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized;

            // 矩形的四个顶点
            vertices[0] = (Vector3)(perpendicular * range);
            vertices[1] = (Vector3)(-perpendicular * range);
            vertices[2] = (Vector3)(-perpendicular * range) + (Vector3)(direction.normalized * range * 2f);
            vertices[3] = (Vector3)(perpendicular * range) + (Vector3)(direction.normalized * range * 2f);

            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);

            triangles[0] = 0;
            triangles[1] = 2;
            triangles[2] = 1;
            triangles[3] = 0;
            triangles[4] = 3;
            triangles[5] = 2;

            lightMesh.vertices = vertices;
            lightMesh.uv = uvs;
            lightMesh.triangles = triangles;
            lightMesh.RecalculateBounds();
        }

        /// <summary>
        /// 更新材质属性
        /// </summary>
        private void UpdateMaterial()
        {
            // 编辑器模式使用sharedMaterial,运行时使用material
            Material mat = Application.isPlaying ? meshRenderer.material : meshRenderer.sharedMaterial;

            if (mat != null)
            {
                mat.SetColor("_LightColor", lightColor * intensity);
                mat.SetFloat("_Range", range);
            }
        }

        private void OnDestroy()
        {
            if (lightMesh != null)
            {
                DestroyImmediate(lightMesh);
            }
        }

        /// <summary>
        /// Gizmos显示灯光范围
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = lightColor;

            switch (lightType)
            {
                case Light2DType.Point:
                    DrawPointLightGizmo();
                    break;
                case Light2DType.Spot:
                    DrawSpotLightGizmo();
                    break;
                case Light2DType.Directional:
                    DrawDirectionalLightGizmo();
                    break;
            }
        }

        private void DrawPointLightGizmo()
        {
            Gizmos.DrawWireSphere(transform.position, range);
        }

        private void DrawSpotLightGizmo()
        {
            float dirAngle = Mathf.Atan2(direction.y, direction.x);
            float halfAngle = spotAngle * 0.5f * Mathf.Deg2Rad;

            Vector3 leftDir = new Vector3(
                Mathf.Cos(dirAngle - halfAngle),
                Mathf.Sin(dirAngle - halfAngle),
                0
            ) * range;

            Vector3 rightDir = new Vector3(
                Mathf.Cos(dirAngle + halfAngle),
                Mathf.Sin(dirAngle + halfAngle),
                0
            ) * range;

            Gizmos.DrawLine(transform.position, transform.position + leftDir);
            Gizmos.DrawLine(transform.position, transform.position + rightDir);
            Gizmos.DrawLine(transform.position + leftDir, transform.position + rightDir);
        }

        private void DrawDirectionalLightGizmo()
        {
            Vector2 perpendicular = new Vector2(-direction.y, direction.x).normalized;
            Vector3 start1 = transform.position + (Vector3)(perpendicular * range);
            Vector3 start2 = transform.position - (Vector3)(perpendicular * range);
            Vector3 end1 = start1 + (Vector3)(direction.normalized * range * 2f);
            Vector3 end2 = start2 + (Vector3)(direction.normalized * range * 2f);

            Gizmos.DrawLine(start1, end1);
            Gizmos.DrawLine(start2, end2);
            Gizmos.DrawLine(start1, start2);
            Gizmos.DrawLine(end1, end2);
        }
    }
}