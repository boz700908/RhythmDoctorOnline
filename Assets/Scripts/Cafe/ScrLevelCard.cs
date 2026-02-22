using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RhythmCafe.Level;
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

    void Start()
    {
        SetBorderAlpha(0f);
    }

    public void SetData(LevelDocument data)
    {
        levelData = data;

        if (songNameText != null)
            songNameText.text = data.song ?? "Unknown";

        if (authorText != null)
            authorText.text = data.authors != null && data.authors.Count > 0
                ? string.Join(", ", data.authors)
                : "Unknown";

        if (bpmText != null)
            bpmText.text = $"{data.max_bpm} BPM";

        SetDifficulty(data.difficulty);
        LoadCoverImage(data.image);
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (levelData != null)
            SelectedLevel.Set(levelData);
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
