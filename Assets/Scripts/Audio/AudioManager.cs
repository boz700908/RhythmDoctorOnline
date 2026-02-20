using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RDOnline.Audio
{
    /// <summary>
    /// 音频管理器 - 负责管理所有音频播放
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("音频源")]
        [Tooltip("背景音乐音频源")]
        public AudioSource BackgroundMusicSource;
        [Tooltip("音效音频源")]
        public AudioSource SoundEffectSource;

        [Header("音量设置")]
        [Tooltip("背景音乐默认音量")]
        [Range(0f, 1f)]
        public float BackgroundMusicVolume = 0.5f;
        [Tooltip("音效音量")]
        [Range(0f, 1f)]
        public float SoundEffectVolume = 1f;

        [Header("淡入淡出设置")]
        [Tooltip("淡出时长（秒）")]
        public float FadeOutDuration = 1f;
        [Tooltip("淡入时长（秒）")]
        public float FadeInDuration = 1f;

        // 当前播放的预览
        private IChartPreview _currentPreview = null;

        // 淡入淡出状态
        private bool _isFading = false;
        private Coroutine _fadeCoroutine = null;

        private void Awake()
        {
            // 单例模式
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 初始化音频源
            InitializeAudioSources();

            // 监听场景切换事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            // 取消监听场景切换事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 场景加载时的回调
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopBackgroundMusic();
        }

        /// <summary>
        /// 初始化音频源
        /// </summary>
        private void InitializeAudioSources()
        {
            // 如果没有设置背景音乐音频源，自动创建
            if (BackgroundMusicSource == null)
            {
                GameObject bgmObj = new GameObject("BackgroundMusicSource");
                bgmObj.transform.SetParent(transform);
                BackgroundMusicSource = bgmObj.AddComponent<AudioSource>();
                BackgroundMusicSource.loop = true;
                BackgroundMusicSource.playOnAwake = false;
                BackgroundMusicSource.volume = BackgroundMusicVolume;
            }

            // 如果没有设置音效音频源，自动创建
            if (SoundEffectSource == null)
            {
                GameObject sfxObj = new GameObject("SoundEffectSource");
                sfxObj.transform.SetParent(transform);
                SoundEffectSource = sfxObj.AddComponent<AudioSource>();
                SoundEffectSource.loop = false;
                SoundEffectSource.playOnAwake = false;
                SoundEffectSource.volume = SoundEffectVolume;
            }
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="loop">是否循环播放，默认为 true</param>
        public void PlayBackgroundMusic(AudioClip clip, bool loop = true)
        {
            if (BackgroundMusicSource == null || clip == null) return;

            BackgroundMusicSource.clip = clip;
            BackgroundMusicSource.loop = loop;
            BackgroundMusicSource.volume = BackgroundMusicVolume;
            BackgroundMusicSource.Play();
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (BackgroundMusicSource == null) return;

            BackgroundMusicSource.Stop();
        }

        /// <summary>
        /// 播放音效（可重复播放）
        /// </summary>
        public void PlaySoundEffect(AudioClip clip)
        {
            if (SoundEffectSource == null || clip == null) return;

            // 使用 PlayOneShot 允许重复播放
            SoundEffectSource.PlayOneShot(clip, SoundEffectVolume);
        }

        // ==================== 静态方法（方便调用） ====================

        /// <summary>
        /// 静态方法 - 播放音乐（只能播放一首，会停止之前的音乐）
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="loop">是否循环播放，默认为 true</param>
        public static void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (Instance == null)
            {
                Debug.LogError("[AudioManager] AudioManager 实例不存在，无法播放音乐");
                return;
            }

            Instance.PlayBackgroundMusic(clip, loop);
        }

        /// <summary>
        /// 静态方法 - 停止音乐
        /// </summary>
        public static void StopMusic()
        {
            if (Instance == null)
            {
                Debug.LogError("[AudioManager] AudioManager 实例不存在，无法停止音乐");
                return;
            }

            Instance.StopBackgroundMusic();
        }

        /// <summary>
        /// 静态方法 - 播放音效（可重叠播放）
        /// </summary>
        public static void PlaySound(AudioClip clip)
        {
            if (Instance == null)
            {
                Debug.LogError("[AudioManager] AudioManager 实例不存在，无法播放音效");
                return;
            }

            Instance.PlaySoundEffect(clip);
        }

        /// <summary>
        /// 淡出背景音乐
        /// </summary>
        public void FadeOutBackgroundMusic()
        {
            if (BackgroundMusicSource == null || !BackgroundMusicSource.isPlaying) return;

            // 停止之前的淡入淡出
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        /// <summary>
        /// 淡入背景音乐
        /// </summary>
        public void FadeInBackgroundMusic()
        {
            if (BackgroundMusicSource == null) return;

            // 如果没有播放，先播放
            if (!BackgroundMusicSource.isPlaying && BackgroundMusicSource.clip != null)
            {
                BackgroundMusicSource.Play();
            }

            // 停止之前的淡入淡出
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeInCoroutine());
        }

        /// <summary>
        /// 预览开始播放
        /// </summary>
        public void OnPreviewStart(IChartPreview preview)
        {
            if (preview == null) return;

            // 如果有其他预览正在播放，停止它
            if (_currentPreview != null && _currentPreview != preview)
            {
                _currentPreview.Stop();
            }

            // 记录当前预览
            _currentPreview = preview;

            // 淡出背景音乐
            FadeOutBackgroundMusic();
        }

        /// <summary>
        /// 预览停止播放
        /// </summary>
        public void OnPreviewStop(IChartPreview preview)
        {
            if (preview == null) return;

            // 只有当前预览停止时才淡入背景音乐
            if (_currentPreview == preview)
            {
                _currentPreview = null;

                // 淡入背景音乐
                FadeInBackgroundMusic();
            }
        }

        /// <summary>
        /// 淡出协程
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            _isFading = true;
            float startVolume = BackgroundMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                BackgroundMusicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / FadeOutDuration);
                yield return null;
            }

            BackgroundMusicSource.volume = 0f;
            _isFading = false;
            _fadeCoroutine = null;
        }

        /// <summary>
        /// 淡入协程
        /// </summary>
        private IEnumerator FadeInCoroutine()
        {
            _isFading = true;
            float startVolume = BackgroundMusicSource.volume;
            float elapsed = 0f;

            while (elapsed < FadeInDuration)
            {
                elapsed += Time.deltaTime;
                BackgroundMusicSource.volume = Mathf.Lerp(startVolume, BackgroundMusicVolume, elapsed / FadeInDuration);
                yield return null;
            }

            BackgroundMusicSource.volume = BackgroundMusicVolume;
            _isFading = false;
            _fadeCoroutine = null;
        }
    }
}
