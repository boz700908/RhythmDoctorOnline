using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class CreditsOpen : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string targetScene = "OnlineCredits";

    [Header("按钮引用")]
    public UnityEngine.UI.Button triggerButton;

    [Header("小人动画")]
    [SerializeField] private RectTransform characterRect;
    [SerializeField] private float hiddenY = 30f;
    [SerializeField] private float revealedY = -35f;
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("消息面板")]
    [SerializeField] private CanvasGroup messageGroup;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageFadeDuration = 0.3f;

    [Header("提示内容")]
    //[SerializeField, TextArea]
    private string[] warningMessages =
    {
        "别点我！",
        "真的不要再点啦！",
        "……你还在点？",
        "你不会把这当成 QTE 了吧？",
        "求你了，我们是假彩蛋。",
        "最后警告：下一次就真的没回头路了。"
    };

    //[SerializeField, TextArea]
    private string finalMessage = "佩服，这毅力你值得彩蛋。欢迎来到真正的 OnlineCredits！";

    [Header("交互参数")]
    [SerializeField, Min(0f)] private float finalMessageDelay = 1.2f;
    [SerializeField, Min(0f)] private float inactivityTimeout = 3.5f;

    private int messageIndex;
    private bool hasRevealedHead;
    private bool isLoadingScene;

    private Tween moveTween;
    private Tween messageFadeTween;
    private Coroutine autoHideRoutine;
    private Coroutine loadRoutine;

    private void Awake()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnEasterEggButtonClicked);
            triggerButton.onClick.AddListener(OnEasterEggButtonClicked);
        }

        if (characterRect != null)
        {
            var anchoredPosition = characterRect.anchoredPosition;
            anchoredPosition.y = hiddenY;
            characterRect.anchoredPosition = anchoredPosition;
        }

        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
            messageGroup.interactable = false;
            messageGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnEasterEggButtonClicked);
        }
    }

    public void OnEasterEggButtonClicked()
    {
        if (isLoadingScene)
            return;

        if (!hasRevealedHead)
        {
            RevealCharacter();
            hasRevealedHead = true;
            RestartAutoHideTimer();
            return;
        }

        RestartAutoHideTimer();
        HandleMessageSequence();
    }

    private void RevealCharacter()
    {
        if (characterRect == null)
            return;

        moveTween?.Kill();
        moveTween = characterRect.DOAnchorPosY(revealedY, moveDuration).SetEase(showEase);
    }

    private void HideCharacter()
    {
        if (characterRect == null)
            return;

        moveTween?.Kill();
        moveTween = characterRect.DOAnchorPosY(hiddenY, moveDuration).SetEase(hideEase);
    }

    private void HandleMessageSequence()
    {
        string messageToShow;
        var reachedFinal = false;

        if (warningMessages != null && messageIndex < warningMessages.Length)
        {
            messageToShow = warningMessages[messageIndex];
            messageIndex++;
        }
        else
        {
            messageToShow = finalMessage;
            reachedFinal = true;
        }

        ShowMessage(messageToShow);

        if (!reachedFinal)
            return;

        isLoadingScene = true;
        StopAutoHideTimer();

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        if (finalMessageDelay <= 0f)
        {
            LoadTargetScene();
        }
        else
        {
            loadRoutine = StartCoroutine(LoadSceneWithDelay(finalMessageDelay));
        }
    }

    private void ShowMessage(string content)
    {
        if (messageGroup == null || messageText == null)
        {
            Debug.LogWarning("消息面板未绑定组件，无法展示提示。");
            return;
        }

        messageText.text = content;

        messageFadeTween?.Kill();
        messageGroup.DOKill();
        messageGroup.alpha = 0f;
        messageGroup.interactable = false;
        messageGroup.blocksRaycasts = false;

        messageFadeTween = messageGroup
            .DOFade(1f, messageFadeDuration)
            .SetEase(Ease.OutQuad);
    }

    private void HideMessage()
    {
        if (messageGroup == null)
            return;

        messageFadeTween?.Kill();
        messageFadeTween = messageGroup
            .DOFade(0f, messageFadeDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                messageGroup.interactable = false;
                messageGroup.blocksRaycasts = false;
                if (messageText != null)
                    messageText.text = string.Empty;
            });
    }

    private void RestartAutoHideTimer()
    {
        if (inactivityTimeout <= 0f || isLoadingScene)
            return;

        StopAutoHideTimer();
        autoHideRoutine = StartCoroutine(AutoHideCoroutine());
    }

    private void StopAutoHideTimer()
    {
        if (autoHideRoutine == null)
            return;

        StopCoroutine(autoHideRoutine);
        autoHideRoutine = null;
    }

    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(inactivityTimeout);
        HideMessage();
        HideCharacter();
        hasRevealedHead = false;
        messageIndex = 0;
        autoHideRoutine = null;
    }

    private IEnumerator LoadSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        SceneManager.LoadScene(targetScene);
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        messageFadeTween?.Kill();

        StopAutoHideTimer();

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
    }
}
