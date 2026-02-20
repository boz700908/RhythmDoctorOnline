using UnityEngine;
using UnityEngine.Video;
using DG.Tweening;

namespace RDOnline.CutScene
{
    /// <summary>
    /// 春节 PV：仅本机首次进入时播放，静音视频 + 6.2s / 12s 时 Image 缩放演出，已播放过则 CanvasGroup 置为 0 透明。
    /// </summary>
    public class SpringPVController : MonoBehaviour
    {
        private const string PrefsKey = "SpringPV_Played";

        [Header("调试")]
        [Tooltip("勾选后每次进入场景都播放 PV，不写入已播放标记")]
        public bool DebugMode;

        [Header("引用")]
        [Tooltip("PV 根物体上的 CanvasGroup，已播放过时设 alpha=0")]
        public CanvasGroup PvCanvasGroup;
        [Tooltip("渲染到纹理的 VideoPlayer")]
        public VideoPlayer VideoPlayer;
        [Tooltip("演出用 Image 的 RectTransform（初始 scale 0，6.2s 缩放到峰值，12s 缩回 0）")]
        public RectTransform TipImageRect;

        [Header("时间与缩放")]
        [Tooltip("视频播放到该秒数时，Image 从 0 缩放到峰值")]
        public float ScaleUpTime = 6.2f;
        [Tooltip("视频播放到该秒数时，Image 从峰值缩回 0")]
        public float ScaleDownTime = 12f;
        [Tooltip("缩放动画时长（秒）")]
        public float ScaleDuration = 1f;
        [Tooltip("Image 峰值缩放（0→此值→0，你要求 100）")]
        public float PeakScale = 100f;
        [Tooltip("放大缓动")]
        public Ease ScaleUpEase = Ease.OutQuad;
        [Tooltip("缩小缓动")]
        public Ease ScaleDownEase = Ease.InQuad;

        [Header("结束")]
        [Tooltip("视频播放到该秒数时，PV 画布透明度平滑插值回 0")]
        public float EndPvTime = 30f;
        [Tooltip("画布淡出时长（秒）")]
        public float FadeOutDuration = 1f;
        [Tooltip("淡出缓动")]
        public Ease FadeOutEase = Ease.Linear;

        private bool _scaleUpDone;
        private bool _scaleDownDone;
        private bool _fadeOutDone;

        private void Start()
        {
            if (!DebugMode && HasPlayed())
            {
                if (PvCanvasGroup != null)
                {
                    PvCanvasGroup.alpha = 0f;
                    PvCanvasGroup.blocksRaycasts = false;
                    PvCanvasGroup.interactable = false;
                }
                return;
            }

            if (PvCanvasGroup != null)
                PvCanvasGroup.alpha = 1f;
            if (TipImageRect != null)
                TipImageRect.localScale = Vector3.zero;

            if (VideoPlayer != null)
            {
                VideoPlayer.SetDirectAudioMute(0, true);
                VideoPlayer.Play();
            }

            if (!DebugMode)
                MarkPlayed();
            _scaleUpDone = false;
            _scaleDownDone = false;
            _fadeOutDone = false;
        }

        private void Update()
        {
            if (VideoPlayer == null || !VideoPlayer.isPlaying || TipImageRect == null)
                return;

            double t = VideoPlayer.time;

            if (!_scaleUpDone && t >= ScaleUpTime)
            {
                _scaleUpDone = true;
                TipImageRect.DOScale(Vector3.one * PeakScale, ScaleDuration).SetEase(ScaleUpEase);
            }

            if (!_scaleDownDone && t >= ScaleDownTime)
            {
                _scaleDownDone = true;
                TipImageRect.DOScale(Vector3.zero, ScaleDuration).SetEase(ScaleDownEase);
            }

            if (!_fadeOutDone && t >= EndPvTime && PvCanvasGroup != null)
            {
                _fadeOutDone = true;
                PvCanvasGroup.blocksRaycasts = false;
                PvCanvasGroup.interactable = false;
                PvCanvasGroup.DOFade(0f, FadeOutDuration).SetEase(FadeOutEase);
            }
        }

        private static bool HasPlayed()
        {
            return PlayerPrefs.GetInt(PrefsKey, 0) != 0;
        }

        private static void MarkPlayed()
        {
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
        }
    }
}
