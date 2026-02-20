using UnityEngine;

namespace RDOnline.Audio
{
    /// <summary>
    /// 背景音乐播放器 - 挂载到场景中自动播放背景音乐
    /// </summary>
    public class BackgroundMusicPlayer : MonoBehaviour
    {
        [Header("背景音乐设置")]
        [Tooltip("背景音乐片段")]
        public AudioClip BackgroundMusicClip;

        [Tooltip("场景加载时自动播放")]
        public bool PlayOnStart = true;

        private void Start()
        {
            if (PlayOnStart && BackgroundMusicClip != null)
            {
                PlayBackgroundMusic();
            }
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        public void PlayBackgroundMusic()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[BackgroundMusicPlayer] AudioManager 实例不存在");
                return;
            }

            if (BackgroundMusicClip == null)
            {
                Debug.LogWarning("[BackgroundMusicPlayer] 背景音乐片段未设置");
                return;
            }

            AudioManager.Instance.PlayBackgroundMusic(BackgroundMusicClip);
            Debug.Log($"[BackgroundMusicPlayer] 开始播放背景音乐: {BackgroundMusicClip.name}");
        }
    }
}
