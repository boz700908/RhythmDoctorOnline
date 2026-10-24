using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RhythmCafe.Level;
using System;
using System.Collections;
using RDOnline.ScnLobby;

public class ScrLevelCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    public RawImage coverImage;
    public TMP_Text songNameText;
    public TMP_Text authorText;
    public TMP_Text bpmText;
    public Image difficultyImage;
    public TMP_Text difficultyText;
    public Image borderImage;

    [Header("Difficulty Colors")]
    public Color easyColor = new Color(0.4f, 0.8f, 0.4f);
    public Color mediumColor = new Color(0.9f, 0.8f, 0.2f);
    public Color hardColor = new Color(0.9f, 0.5f, 0.2f);
    public Color expertColor = new Color(0.9f, 0.2f, 0.2f);

    private LevelDocument levelData;
    private Coroutine loadImageCoroutine;

    private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Expert" };

    private Coroutine borderFadeCoroutine;
    private float borderFadeDuration = 0.15f;

    // ---- 非社区条目（官方关卡 / 玩家关卡库） ----
    private ChartSourceEntry entry;
    private Action<ChartSourceEntry> onEntryClick;

    void Start()
    {
        SetBorderAlpha(0f);
    }

    // ---------- 社区谱面 ----------

    public void SetData(LevelDocument data)
    {
        levelData = data;
        entry = null;
        onEntryClick = null;

        if (songNameText != null)
            songNameText.text = data.song ?? "Unknown";

        if (authorText != null)
            authorText.text = data.authors != null && data.authors.Count > 0
                ? string.Join(", ", data.authors)
                : "Unknown";

        if (bpmText != null)
            bpmText.text = $"{data.max_bpm} BPM";

        SetDifficulty(data.difficulty);

        if (string.IsNullOrEmpty(data.image) && coverImage != null)
            coverImage.texture = null;

        LoadCoverImage(data.image);
    }

    // ---------- 官方关卡 / 玩家关卡库 ----------

    /// <summary>
    /// 用一个谱面来源条目填充卡片。官方关卡没有 BPM/难度/封面，
    /// 那些字段留空而不是编造数据。
    /// </summary>
    public void SetEntry(ChartSourceEntry sourceEntry, Action<ChartSourceEntry> clickHandler)
    {
        entry = sourceEntry;
        levelData = null;
        onEntryClick = clickHandler;

        if (loadImageCoroutine != null)
        {
            StopCoroutine(loadImageCoroutine);
            loadImageCoroutine = null;
        }

        if (coverImage != null)
            coverImage.texture = null;

        // 显示名一律用 Display，绝不显示 Id（官方关卡的 Id 是内部枚举名）
        if (songNameText != null)
            songNameText.text = string.IsNullOrEmpty(sourceEntry.Display)
                ? sourceEntry.Id
                : sourceEntry.Display;

        if (authorText != null)
            authorText.text = sourceEntry.Detail ?? string.Empty;

        if (bpmText != null)
            bpmText.text = string.Empty;

        SetDifficultyVisualOnly(sourceEntry.IsOfficial ? 0 : 1);
    }

    private void SetDifficulty(int difficulty)
    {
        difficulty = Mathf.Clamp(difficulty, 0, 3);

        if (difficultyText != null)
            difficultyText.text = DifficultyNames[difficulty];

        if (difficultyImage != null)
        {
            difficultyImage.color = difficulty switch
            {
                0 => easyColor,
                1 => mediumColor,
                2 => hardColor,
                3 => expertColor,
                _ => easyColor
            };
        }
    }

    /// <summary>
    /// 只上色不写难度文字。官方关卡没有难度字段，写「Easy」是错的信息。
    /// </summary>
    private void SetDifficultyVisualOnly(int difficulty)
    {
        if (difficultyText != null)
            difficultyText.text = string.Empty;

        if (difficultyImage != null)
        {
            difficultyImage.color = difficulty switch
            {
                0 => easyColor,
                1 => mediumColor,
                2 => hardColor,
                3 => expertColor,
                _ => easyColor
            };
        }
    }

    private void LoadCoverImage(string url)
    {
        if (string.IsNullOrEmpty(url) || coverImage == null)
            return;

        if (loadImageCoroutine != null)
            StopCoroutine(loadImageCoroutine);

        loadImageCoroutine = StartCoroutine(LoadImageCoroutine(url));
    }

    private IEnumerator LoadImageCoroutine(string url)
    {
        var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
            coverImage.texture = texture;
        }

        request.Dispose();
    }

    /// <summary>这张卡片当前代表的条目；社区谱面返回 null</summary>
    public ChartSourceEntry Entry => entry;

    /// <summary>当前卡片代表的社区谱面；官方关卡/关卡库条目返回 null</summary>
    public LevelDocument CurrentLevel => levelData;

    /// <summary>
    /// 触发这张卡片，与鼠标点击走同一条路。
    /// 无障碍导航用回车调用它，保证鼠标和键盘行为完全一致。
    /// </summary>
    public void Activate()
    {
        if (onEntryClick != null && entry != null)
        {
            onEntryClick(entry);
            return;
        }

        if (levelData != null)
            SelectedLevel.Set(levelData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Activate();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopBorderFade();
        borderFadeCoroutine = StartCoroutine(FadeBorderAlpha(1f));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopBorderFade();
        borderFadeCoroutine = StartCoroutine(FadeBorderAlpha(0f));
    }

    private void StopBorderFade()
    {
        if (borderFadeCoroutine != null)
            StopCoroutine(borderFadeCoroutine);
    }

    private IEnumerator FadeBorderAlpha(float targetAlpha)
    {
        if (borderImage == null) yield break;

        var color = borderImage.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < borderFadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / borderFadeDuration);
            borderImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        borderImage.color = color;
    }

    private void SetBorderAlpha(float alpha)
    {
        if (borderImage != null)
        {
            var color = borderImage.color;
            color.a = alpha;
            borderImage.color = color;
        }
    }
}
