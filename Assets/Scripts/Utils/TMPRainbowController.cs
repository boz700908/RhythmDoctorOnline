using System.Collections.Generic;
using RDOnline.Utils;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

/// <summary>
/// 运行时控制 TMP 彩虹渐变方向 & 动画。
/// 挂载到含 TextMeshProUGUI / TextMeshPro 的 GameObject 上。
/// by StArray & Claude Sonnet 4.6
/// 草泥马谁再用我东邪不标注我我直接把我名写他脸上
/// </summary>
#if UNITY_EDITOR
[RequireComponent(typeof(TMP_Text))]
#endif
public class TMPRainbowController : MonoBehaviour
{
    [FormerlySerializedAs("direct")] [Header("方向")]
    public Direction direction = Direction.UpToDown;

    [Header("模式")]
    public bool useRainbow = false;
    [ColorUsage(true, true)]
    public Color solidColor = Color.white;

    [Header("彩虹外观")]
    [Range(0f, 5f)]  public float tiling     = 0.5f;
    [Range(0f, 1f)]  public float saturation = 0.9f;
    [Range(0f, 1f)]  public float brightness = 1.0f;
    [Range(0f, 1f)]  public float hueOffset  = 0f;

    [Header("流动动画")]
    [Range(0f, 5f)]  public float flowSpeed = 2.5f;

    public Shader shader;

    static readonly int ID_UseRainbow = Shader.PropertyToID("_UseRainbow");
    static readonly int ID_SolidColor = Shader.PropertyToID("_SolidColor");
    static readonly int ID_Angle      = Shader.PropertyToID("_RainbowAngle");
    static readonly int ID_Speed      = Shader.PropertyToID("_RainbowSpeed");
    static readonly int ID_Saturation = Shader.PropertyToID("_RainbowSaturation");
    static readonly int ID_Brightness = Shader.PropertyToID("_RainbowBrightness");
    static readonly int ID_Tiling     = Shader.PropertyToID("_RainbowTiling");
    static readonly int ID_Offset     = Shader.PropertyToID("_RainbowOffset");

    TMP_Text _text;
    Material _mat;
    bool     _needRebuild;

    // 延迟销毁队列，避免在渲染途中 DestroyImmediate 崩溃
    readonly List<Material> _pendingDestroy = new List<Material>();

    // ── 生命周期 ──────────────────────────────────────────────

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Init();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnDestroy()
    {
        // 还原共享材质
        if (_text != null && _text.font != null)
            _text.fontSharedMaterial = _text.font.material;

        // 销毁我们创建的材质实例
        SafeDestroy(_mat);
        _mat = null;

        foreach (var m in _pendingDestroy)
            SafeDestroy(m);
        _pendingDestroy.Clear();
    }

    void Update()
    {
        // 先把上一帧积压的待销毁材质清掉（此时已离开渲染阶段，安全）
        FlushPendingDestroy();

        if (_mat == null)
        {
            Init();
            return;
        }

        // 图集重建标记：延迟到 Update 执行，避免在事件回调（渲染中途）直接销毁
        if (_needRebuild)
        {
            _needRebuild = false;
            RebuildMaterial();
            return;
        }

        ApplyProperties();
    }

    // ── 初始化 & 重建 ─────────────────────────────────────────

    void Init()
    {
        if (_text == null)
            _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        if (shader == null)
        {
            if (Application.isEditor)
                shader = Shader.Find("TextMeshPro/RainbowGradient");
            else
                shader = AssetBundleManager.instance.LoadAsset<Shader>("RainbowGradient");
        }

        if (_mat == null || _text.fontMaterial != _mat)
        {
            // 旧材质加入延迟销毁队列而非立即销毁
            QueueDestroy(_mat);

            _mat = new Material(_text.fontSharedMaterial);
            _mat.shader = shader;
            _text.fontMaterial = _mat;

            Debug.Log($"[TMPRainbow] 初始化材质，着色器: {_mat.shader?.name ?? "null"}");
        }

        ApplyProperties();
    }

    // 图集重建事件：只设标记，绝不在此处销毁任何对象
    void OnTextChanged(Object obj)
    {
        if (obj == _text)
            _needRebuild = true;
    }

    void RebuildMaterial()
    {
        if (_text == null) return;

        // 旧材质延迟销毁
        QueueDestroy(_mat);

        _mat = new Material(_text.fontSharedMaterial);
        _mat.shader = shader;
        _text.fontMaterial = _mat;

        ApplyProperties();
    }

    // ── 参数同步 ──────────────────────────────────────────────

    void ApplyProperties()
    {
        _mat.SetFloat(ID_Angle,      GetAngle(direction));
        _mat.SetFloat(ID_Speed,      flowSpeed);
        _mat.SetFloat(ID_Saturation, saturation);
        _mat.SetFloat(ID_Brightness, brightness);
        _mat.SetFloat(ID_Tiling,     tiling);
        _mat.SetFloat(ID_Offset,     hueOffset);
        _mat.SetFloat(ID_UseRainbow, useRainbow ? 1f : 0f);
        _mat.SetColor(ID_SolidColor, solidColor);
    }

    // ── 工具方法 ──────────────────────────────────────────────

    void QueueDestroy(Material mat)
    {
        if (mat != null)
            _pendingDestroy.Add(mat);
    }

    void FlushPendingDestroy()
    {
        if (_pendingDestroy.Count == 0) return;
        foreach (var m in _pendingDestroy)
            SafeDestroy(m);
        _pendingDestroy.Clear();
    }

    static void SafeDestroy(Material mat)
    {
        if (mat == null) return;

        if (Application.isEditor && !Application.isPlaying)
            Object.DestroyImmediate(mat);
        else
            Object.Destroy(mat);
    }

    static float GetAngle(Direction dir)
    {
        switch (dir)
        {
            case Direction.RightToLeft: return 90f;
            case Direction.DownToUp:    return 180f;
            case Direction.LeftToRight: return 270f;
            default:                    return 0f;
        }
    }

    public enum Direction
    {
        UpToDown,
        RightToLeft,
        DownToUp,
        LeftToRight,
    }
}