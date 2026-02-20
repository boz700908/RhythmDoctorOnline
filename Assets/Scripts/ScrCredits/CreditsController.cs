using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Credits
{
    /// <summary>
    /// Main runtime: audio-driven beat, scene manager, single full-screen TMP. Port of credits.py main loop.
    /// 支持新 Input System；TMP 字号按 24 行铺满屏幕自动缩放。
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private TextMeshProUGUI fullScreenText;
        [SerializeField] private AudioClip bgmClip;

        [Header("显示")]
        [Tooltip("勾选后用 IMGUI 绘制 80×24 网格，避免 TMP 排版问题；不勾选则用 TMP。")]
        [SerializeField] private bool useImgui = false;
        [Tooltip("字号相对屏幕高度的比例，调小则字变小。")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float fontSizeScale = 0.6f;
        [Tooltip("IMGUI 模式下使用的字体（可选，不填用默认）。")]
        [SerializeField] private Font imguiFont;

        [Header("结束后")]
        [Tooltip("Credits 播完后自动加载的场景名；需已加入 Build Settings。")]
        [SerializeField] private string sceneAfterCredits = "ScnLobby";

        [Header("Timing (match original)")]
        [SerializeField] private float bpm = 179f;
        [SerializeField] private int subdivision = 8;
        [SerializeField] private float offsetSeconds = 5.492f;

        private CreditsCanvas _canvas;
        private SceneManager _sceneManager;
        private float _delay;
        private int _beat;
        private float _skipBy;
        private bool _pausedThisFrame;
        private bool _ffThisFrame;
        private float _lastUpdateTime;
        private float _menuStartTime;
        private bool _playbackStarted;
        private bool _returnedToLobby;
        private List<(string, Color)>[] _imguiSegments;

        private const float MenuDuration = 2f;
        private const float InputPollInterval = 1f / 30f;

        private void Awake()
        {
            if (fullScreenText == null)
                fullScreenText = GetComponentInChildren<TextMeshProUGUI>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            _delay = 60f / bpm / subdivision;
            _beat = -60;
            _skipBy = 0f;

            _canvas = new CreditsCanvas(new CanvasColor?(CanvasColor.Black));
            Scene[] scenes = CreditsScenes.BuildAll(_canvas);
            CreditsEvent[] events = CreditsTimeline.Build();
            _sceneManager = new SceneManager(scenes, events);

            _imguiSegments = new List<(string, Color)>[CreditsCanvas.Height];
            for (int i = 0; i < _imguiSegments.Length; i++)
                _imguiSegments[i] = new List<(string, Color)>();
        }

        private void Start()
        {
            if (bgmClip != null)
                audioSource.clip = bgmClip;
            _menuStartTime = Time.time;
            if (useImgui && fullScreenText != null)
                fullScreenText.enabled = false;
            else if (fullScreenText != null)
            {
                fullScreenText.text = "";
                ScaleTmpToFillScreen();
            }
            // 直接开始播放，不显示 2 秒选段菜单
            _playbackStarted = true;
            StartPlayback();
        }

        /// <summary>
        /// 按 fontSizeScale 和 24 行计算字号并应用；检查器里改 fontSizeScale 会实时生效。
        /// </summary>
        private void ScaleTmpToFillScreen()
        {
            if (fullScreenText == null) return;
            const int rows = CreditsCanvas.Height;
            float fontSize = Mathf.Max(12f, Screen.height / (float)rows * fontSizeScale);
            fullScreenText.fontSize = Mathf.RoundToInt(fontSize);
            fullScreenText.rectTransform.anchorMin = Vector2.zero;
            fullScreenText.rectTransform.anchorMax = Vector2.one;
            fullScreenText.rectTransform.offsetMin = Vector2.zero;
            fullScreenText.rectTransform.offsetMax = Vector2.zero;
        }

        private void OnValidate()
        {
            if (fullScreenText == null) return;
            if (useImgui)
                fullScreenText.enabled = false;
            else
            {
                fullScreenText.enabled = true;
                ScaleTmpToFillScreen();
            }
        }

        private void StartPlayback()
        {
            if (audioSource.clip != null)
            {
                audioSource.time = _skipBy;
                audioSource.Play();
            }
            _lastUpdateTime = Time.time;
            if (useImgui)
                _canvas.BuildImguiSegments(_imguiSegments);
        }

        /// <summary>
        /// Call from UI or external to skip to a segment (1-6). Call before or during first 2 seconds.
        /// </summary>
        public void SkipTo(int segment)
        {
            int amount;
            switch (segment)
            {
                case 1: amount = 0; break;
                case 2: amount = 1000; break;
                case 3: amount = 1770; break;
                case 4: amount = 3040; break;
                case 5: amount = 3780; break;
                case 6: amount = 5420; break;
                default: amount = 0; break;
            }
            if (amount > 0)
            {
                _sceneManager.CurBeat += amount;
                _beat += amount;
                _skipBy = offsetSeconds + _delay * amount;
                if (segment == 3)
                {
                    _sceneManager.SetEventsAtBeat(1849, new[] { new CreditsEvent(1849, CreditsEvent.LayerScene("redraw_ui")) });
                    _sceneManager.SetEventsAtBeat(1860, new[] { new CreditsEvent(1860, CreditsEvent.RemoveScene("redraw_ui")) });
                }
            }
            StartPlayback();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
#endif
            if (!_playbackStarted)
            {
                if (Time.time - _menuStartTime >= MenuDuration)
                {
                    _playbackStarted = true;
                    StartPlayback();
                }
                else
                {
#if ENABLE_INPUT_SYSTEM
                    if (k != null)
                    {
                        if (k.digit1Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(1); StartPlayback(); return; }
                        if (k.digit2Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(2); StartPlayback(); return; }
                        if (k.digit3Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(3); StartPlayback(); return; }
                        if (k.digit4Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(4); StartPlayback(); return; }
                        if (k.digit5Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(5); StartPlayback(); return; }
                        if (k.digit6Key.wasPressedThisFrame) { _playbackStarted = true; SkipTo(6); StartPlayback(); return; }
                    }
#else
                    if (Input.GetKeyDown(KeyCode.Alpha1)) { _playbackStarted = true; SkipTo(1); StartPlayback(); return; }
                    if (Input.GetKeyDown(KeyCode.Alpha2)) { _playbackStarted = true; SkipTo(2); StartPlayback(); return; }
                    if (Input.GetKeyDown(KeyCode.Alpha3)) { _playbackStarted = true; SkipTo(3); StartPlayback(); return; }
                    if (Input.GetKeyDown(KeyCode.Alpha4)) { _playbackStarted = true; SkipTo(4); StartPlayback(); return; }
                    if (Input.GetKeyDown(KeyCode.Alpha5)) { _playbackStarted = true; SkipTo(5); StartPlayback(); return; }
                    if (Input.GetKeyDown(KeyCode.Alpha6)) { _playbackStarted = true; SkipTo(6); StartPlayback(); return; }
#endif
                }
                return;
            }

            if (audioSource == null)
                return;
            if (!audioSource.isPlaying)
            {
                // BGM 播完后自动返回大厅
                if (_playbackStarted && !_returnedToLobby && audioSource.clip != null
                    && audioSource.time >= Mathf.Max(0f, audioSource.clip.length - 0.2f))
                {
                    _returnedToLobby = true;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneAfterCredits);
                }
                return;
            }

            float audioTime = audioSource.time;
            bool nextBeat = (audioTime - offsetSeconds) > ((_beat - 1) * _delay);
            bool needInputUpdate = Time.time - InputPollInterval > _lastUpdateTime;

            if (nextBeat)
            {
                _sceneManager.RequestNext(true);
                if (useImgui)
                    _canvas.BuildImguiSegments(_imguiSegments);
                else
                    _canvas.FlushToTMP(fullScreenText);
                _beat++;
            }

            if (needInputUpdate)
            {
#if ENABLE_INPUT_SYSTEM
                if (k != null)
                {
                    if (k.pKey.wasPressedThisFrame)
                    {
                        if (!_pausedThisFrame)
                        {
                            if (audioSource.isPlaying) audioSource.Pause();
                            else audioSource.UnPause();
                            _pausedThisFrame = true;
                        }
                    }
                    else
                        _pausedThisFrame = false;

                    if (k.commaKey.isPressed)
                    {
                        if (!_ffThisFrame) _ffThisFrame = true;
                        else audioSource.time = audioSource.time + _delay * 3f;
                    }
                    if (k.periodKey.isPressed)
                    {
                        if (!_ffThisFrame) _ffThisFrame = true;
                        else audioSource.time = audioSource.time + _delay * 7f;
                    }
                    if (k.slashKey.isPressed)
                    {
                        if (!_ffThisFrame) _ffThisFrame = true;
                        else audioSource.time = audioSource.time + _delay * 15f;
                    }
                }
#else
                if (Input.GetKeyDown(KeyCode.P))
                {
                    if (!_pausedThisFrame)
                    {
                        if (audioSource.isPlaying) audioSource.Pause();
                        else audioSource.UnPause();
                        _pausedThisFrame = true;
                    }
                }
                else
                    _pausedThisFrame = false;

                if (Input.GetKey(KeyCode.Comma))
                {
                    if (!_ffThisFrame) _ffThisFrame = true;
                    else audioSource.time = audioSource.time + _delay * 3f;
                }
                if (Input.GetKey(KeyCode.Period))
                {
                    if (!_ffThisFrame) _ffThisFrame = true;
                    else audioSource.time = audioSource.time + _delay * 7f;
                }
                if (Input.GetKey(KeyCode.Slash))
                {
                    if (!_ffThisFrame) _ffThisFrame = true;
                    else audioSource.time = audioSource.time + _delay * 15f;
                }
#endif

                _lastUpdateTime = Time.time;
            }
        }

        private void OnGUI()
        {
            if (!useImgui) return;

            float cw = Screen.width / (float)CreditsCanvas.Width;
            float ch = Screen.height / (float)CreditsCanvas.Height * fontSizeScale;
            int fontSize = Mathf.Max(10, Mathf.RoundToInt(ch * 0.9f));

            var style = new GUIStyle(GUI.skin.label);
            style.font = imguiFont ?? style.font;
            style.fontSize = fontSize;
            style.padding = new RectOffset(0, 0, 0, 0);

            if (!_playbackStarted)
            {
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "skips\n\n1 | start\n2 | title\n3 | funding\n4 | loading\n5 | break\n6 | final", style);
                return;
            }

            if (_imguiSegments == null) return;

            float x = 0f;
            for (int row = 0; row < CreditsCanvas.Height; row++)
            {
                float y = row * ch;
                x = 0f;
                foreach (var seg in _imguiSegments[row])
                {
                    style.normal.textColor = seg.Item2;
                    float w = seg.Item1.Length * cw;
                    GUI.Label(new Rect(x, y, w, ch), seg.Item1, style);
                    x += w;
                }
            }
        }
    }
}
