using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ScnLoading : MonoBehaviour
{
    private static ScnLoading instance;
    public static ScnLoading Instance
    {
        get { return instance; }
    }

    [Header("UI组件")]
    [Tooltip("画布组（控制透明度）")]
    public CanvasGroup canvasGroup;
    [Tooltip("画布进入退出动画图片")]
    public Image canvasEnterImage;
    [Tooltip("进度条图片")]
    public Image progressImage;
    [Tooltip("进度百分比文本")]
    public TMP_Text progressText;
    [Tooltip("提示文本")]
    public TMP_Text tipText;

    [Header("时间设置")]
    [Tooltip("最低显示时间（秒）")]
    public float minimumLoadTime = 3f;
    [Tooltip("画布进入动画持续时间（秒）")]
    public float canvasEnterDuration = 0.3f;
    [Tooltip("画布退出动画持续时间（秒）")]
    public float canvasExitDuration = 0.3f;
    [Tooltip("淡入淡出持续时间（秒）")]
    public float fadeDuration = 0.5f;
    [Tooltip("进度条平滑速度")]
    public float progressSmoothSpeed = 2f;

    [Header("音效设置")]
    [Tooltip("画布进入音效")]
    public AudioClip enterSound;
    [Tooltip("画布退出音效")]
    public AudioClip exitSound;

    // 加载状态
    private bool isLoading = false;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化时自动获取组件
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (progressImage == null)
            progressImage = GetComponentInChildren<Image>();
        if (tipText == null)
            tipText = GetComponentInChildren<TMP_Text>();

        // 确保初始状态为隐藏
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false; // 初始状态不拦截点击
        }
        if (canvasEnterImage != null)
        {
            Vector3 scale = canvasEnterImage.transform.localScale;
            scale.x = 0f;
            canvasEnterImage.transform.localScale = scale;
        }

        // 初始化进度条为0%
        UpdateLoadingUI(0f);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    /// <summary>
    /// 画布进入动画协程（完整流程）
    /// </summary>
    IEnumerator CanvasEnter()
    {
        // 启用射线检测，阻止点击穿透
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = true;

        // 1. 播放画布进入动画（Image X缩放 0→1）
        if (canvasEnterImage != null)
        {
            PlaySound(enterSound);
            yield return canvasEnterImage.transform.DOScaleX(1f, canvasEnterDuration).SetEase(Ease.OutCubic).WaitForCompletion();
        }

        // 2. 淡入显示CanvasGroup
        yield return StartCoroutine(FadeIn());
    }

    /// <summary>
    /// 画布退出动画协程（完整流程）
    /// </summary>
    IEnumerator CanvasExit()
    {
        // 1. 淡出隐藏CanvasGroup
        yield return StartCoroutine(FadeOut());

        // 2. 播放画布退出动画（Image X缩放 1→0）
        if (canvasEnterImage != null)
        {
            PlaySound(exitSound);
            yield return canvasEnterImage.transform.DOScaleX(0f, canvasExitDuration).SetEase(Ease.InCubic).WaitForCompletion();
        }

        // 禁用射线检测，允许点击穿透
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 淡入效果协程
    /// </summary>
    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 淡出效果协程
    /// </summary>
    IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 静态方法：加载指定场景
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    public static void LoadScenes(string sceneName)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.LoadingSequence(sceneName));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
        else
        {
            Debug.LogWarning("已经在加载中，请等待当前加载完成。");
        }
    }
    
    /// <summary>
    /// 静态方法：显示加载动画（不加载场景）
    /// </summary>
    /// <param name="tipMessage">可选的提示信息，为null则使用随机提示</param>
    public static void ShowLoading(string tipMessage = null)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.ShowLoadingAnimation(tipMessage));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
    }
    
    /// <summary>
    /// 静态方法：隐藏加载动画
    /// </summary>
    public static void HideLoading()
    {
        if (Instance != null && Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.HideLoadingAnimation());
        }
    }
    
    /// <summary>
    /// 静态方法：显示加载动画并执行操作
    /// </summary>
    /// <param name="operation">要执行的操作（协程）</param>
    /// <param name="tipMessage">可选的提示信息</param>
    public static void ShowLoadingWithOperation(IEnumerator operation, string tipMessage = null)
    {
        if (Instance != null && !Instance.isLoading)
        {
            Instance.StartCoroutine(Instance.LoadingWithOperationSequence(operation, tipMessage));
        }
        else if (Instance == null)
        {
            Debug.LogError("ScnLoading实例不存在！请确保场景中有ScnLoading组件。");
        }
    }
    
    /// <summary>
    /// 加载序列协程
    /// </summary>
    IEnumerator LoadingSequence(string sceneName)
    {
        isLoading = true;

        // 重置进度条为0%（在动画开始前）
        UpdateLoadingUI(0f);

        // 设置随机提示
        if (tipText != null)
        {
            tipText.text = ScrTips.GetFormattedTips();
        }

        // 等待一帧确保界面完全激活
        yield return null;

        // 画布进入动画（Image缩放 + CanvasGroup淡入）
        yield return StartCoroutine(CanvasEnter());

        // 开始异步加载目标场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        // 进度更新循环
        float startTime = Time.time;
        bool realLoadComplete = false;
        float displayProgress = 0f; // 显示进度（平滑插值）

        while (true)
        {
            float elapsedTime = Time.time - startTime;
            float realProgress = asyncLoad.progress;

            // 检查真实加载是否完成
            if (asyncLoad.progress >= 0.9f && !realLoadComplete)
            {
                realLoadComplete = true;
            }

            // 计算目标进度（真实加载完成后卡在99%，直到达到最低显示时间）
            float targetProgress = realProgress;
            if (realLoadComplete && elapsedTime < minimumLoadTime)
            {
                targetProgress = 0.99f;
            }

            // 平滑插值更新显示进度
            displayProgress = Mathf.Lerp(displayProgress, targetProgress, Time.deltaTime * progressSmoothSpeed);

            // 更新UI
            UpdateLoadingUI(displayProgress);
            
            // 检查是否可以完成加载
            if (realLoadComplete && elapsedTime >= minimumLoadTime)
            {
                UpdateLoadingUI(1f);
                asyncLoad.allowSceneActivation = true;
                break;
            }
            
            yield return null;
        }
        
        // 等待场景完全激活
        yield return new WaitUntil(() => asyncLoad.isDone);

        // 画布退出动画（CanvasGroup淡出 + Image缩放）
        yield return StartCoroutine(CanvasExit());

        isLoading = false;
    }
    /// <summary>
    /// 更新加载界面UI
    /// </summary>
    void UpdateLoadingUI(float progress)
    {
        if (progressImage != null)
        {
            Vector3 scale = progressImage.transform.localScale;
            scale.x = progress;
            progressImage.transform.localScale = scale;
        }

        if (progressText != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100);
            progressText.text = percentage + "%";
        }
    }
    
    /// <summary>
    /// 显示加载动画协程（不加载场景）
    /// </summary>
    IEnumerator ShowLoadingAnimation(string tipMessage)
    {
        isLoading = true;

        // 重置进度条为0%（在动画开始前）
        UpdateLoadingUI(0f);

        // 设置提示信息
        if (tipText != null)
        {
            tipText.text = string.IsNullOrEmpty(tipMessage) ? ScrTips.GetFormattedTips() : tipMessage;
        }

        // 等待一帧确保界面完全激活
        yield return null;

        // 画布进入动画（Image缩放 + CanvasGroup淡入）
        yield return StartCoroutine(CanvasEnter());

        // 模拟加载进度（循环显示）
        float progress = 0f;
        while (isLoading)
        {
            progress += Time.deltaTime * 0.3f; // 缓慢增加进度
            if (progress > 0.99f)
            {
                progress = 0.99f; // 保持在99%
            }
            UpdateLoadingUI(progress);
            yield return null;
        }
    }
    
    /// <summary>
    /// 隐藏加载动画协程
    /// </summary>
    IEnumerator HideLoadingAnimation()
    {
        // 显示100%完成
        UpdateLoadingUI(1f);

        // 画布退出动画（CanvasGroup淡出 + Image缩放）
        yield return StartCoroutine(CanvasExit());

        isLoading = false;
    }
    
    /// <summary>
    /// 显示加载动画并执行操作协程
    /// </summary>
    IEnumerator LoadingWithOperationSequence(IEnumerator operation, string tipMessage)
    {
        isLoading = true;

        // 重置进度条为0%（在动画开始前）
        UpdateLoadingUI(0f);

        // 设置提示信息
        if (tipText != null)
        {
            tipText.text = string.IsNullOrEmpty(tipMessage) ? ScrTips.GetFormattedTips() : tipMessage;
        }

        // 等待一帧确保界面完全激活
        yield return null;

        // 画布进入动画（Image缩放 + CanvasGroup淡入）
        yield return StartCoroutine(CanvasEnter());

        // 执行传入的操作
        yield return StartCoroutine(operation);

        // 显示100%完成
        UpdateLoadingUI(1f);

        // 画布退出动画（CanvasGroup淡出 + Image缩放）
        yield return StartCoroutine(CanvasExit());

        isLoading = false;
    }
}
