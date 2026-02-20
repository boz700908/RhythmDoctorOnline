using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using RDOnline.Audio;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 房间谱面预览 - 显示下载后的谱面信息
    /// </summary>
    public class RoomChartPreview : MonoBehaviour, IChartPreview
    {
        [Header("UI组件")]
        [Tooltip("封面图片")]
        public RawImage CoverImage;
        [Tooltip("谱面名称")]
        public TMP_Text SongNameText;
        [Tooltip("谱面作者")]
        public TMP_Text AuthorText;
        [Tooltip("播放/暂停按钮")]
        public Button PlayButton;
        [Tooltip("音频源")]
        public AudioSource AudioSource;

        [Header("设置")]
        [Tooltip("封面旋转速度（度/秒）")]
        public float RotationSpeed = 30f;

        private string _chartDirectory;
        private string _chartFilePath;
        private bool _isPlaying;

        /// <summary>
        /// IChartPreview 接口实现 - 是否正在播放
        /// </summary>
        public bool IsPlaying => _isPlaying;

        private void Start()
        {
            // 绑定按钮事件
            if (PlayButton != null)
                PlayButton.onClick.AddListener(TogglePlayPause);
        }

        private void Update()
        {
            // 封面旋转动画
            if (CoverImage != null && CoverImage.texture != null)
            {
                CoverImage.transform.Rotate(0, 0, -RotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 从下载目录加载谱面
        /// </summary>
        public void LoadChartFromDirectory()
        {
            // 获取谱面目录
            if (ChartDownloader.Instance == null)
            {
                Debug.LogError("[RoomChartPreview] ChartDownloader 实例不存在");
                return;
            }

            _chartDirectory = ChartDownloader.Instance.GetChartDirectory();

            // 获取谱面名称
            if (RoomData.Instance == null || string.IsNullOrEmpty(RoomData.Instance.ChartName))
            {
                Debug.LogError("[RoomChartPreview] RoomData 或谱面名称为空");
                return;
            }

            string chartName = RoomData.Instance.ChartName;
            _chartFilePath = Path.Combine(_chartDirectory, chartName + ".adofai");

            // 检查文件是否存在
            if (!File.Exists(_chartFilePath))
            {
                Debug.LogError($"[RoomChartPreview] 谱面文件不存在: {_chartFilePath}");
                return;
            }

            Debug.Log($"[RoomChartPreview] 开始加载谱面: {_chartFilePath}");

            // 读取并解析谱面文件
            string content = File.ReadAllText(_chartFilePath);
            ParseChartData(content);
        }

        /// <summary>
        /// 解析谱面数据
        /// </summary>
        private void ParseChartData(string content)
        {
            // 使用正则表达式提取字段
            string songName = ExtractField(content, "song");
            string author = ExtractField(content, "author");
            string previewImage = ExtractField(content, "previewImage");
            string songFilename = ExtractField(content, "songFilename");

            // 去除HTML标签
            songName = RemoveHtmlTags(songName);
            author = RemoveHtmlTags(author);

            Debug.Log($"[RoomChartPreview] 谱面信息 - 名称: {songName}, 作者: {author}");

            // 更新UI
            if (SongNameText != null)
                SongNameText.text = songName;
            if (AuthorText != null)
                AuthorText.text = author;

            // 加载封面图片
            if (!string.IsNullOrEmpty(previewImage))
            {
                string imagePath = Path.Combine(_chartDirectory, previewImage);
                StartCoroutine(LoadCoverImage(imagePath));
            }

            // 加载音乐文件
            if (!string.IsNullOrEmpty(songFilename))
            {
                string audioPath = Path.Combine(_chartDirectory, songFilename);
                StartCoroutine(LoadAudio(audioPath));
            }
        }

        /// <summary>
        /// 提取字段值
        /// </summary>
        private string ExtractField(string content, string fieldName)
        {
            string pattern = $"\"{fieldName}\"\\s*:\\s*\"([^\"]+)\"";
            Match match = Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }

        /// <summary>
        /// 去除HTML标签
        /// </summary>
        private string RemoveHtmlTags(string text)
        {
            return Regex.Replace(text, "<.*?>", "");
        }

        /// <summary>
        /// 加载封面图片
        /// </summary>
        private IEnumerator LoadCoverImage(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Debug.LogWarning($"[RoomChartPreview] 封面图片不存在: {imagePath}");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture("file:///" + imagePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (CoverImage != null)
                    {
                        CoverImage.texture = texture;
                        Debug.Log("[RoomChartPreview] 封面加载成功");
                    }
                }
                else
                {
                    Debug.LogError($"[RoomChartPreview] 封面加载失败: {request.error}");
                }
            }
        }

        /// <summary>
        /// 加载音频文件
        /// </summary>
        private IEnumerator LoadAudio(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                Debug.LogWarning($"[RoomChartPreview] 音频文件不存在: {audioPath}");
                yield break;
            }

            AudioType audioType = GetAudioType(audioPath);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file:///" + audioPath, audioType))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (AudioSource != null)
                    {
                        AudioSource.clip = clip;
                        // 不自动播放，等待用户点击播放按钮
                        // AudioSource.Play();
                        // _isPlaying = true;

                        Debug.Log("[RoomChartPreview] 音频加载成功");
                    }
                }
                else
                {
                    Debug.LogError($"[RoomChartPreview] 音频加载失败: {request.error}");
                }
            }
        }

        /// <summary>
        /// 获取音频类型
        /// </summary>
        private AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN
            };
        }

        /// <summary>
        /// 切换播放/暂停
        /// </summary>
        private void TogglePlayPause()
        {
            if (AudioSource == null || AudioSource.clip == null)
                return;

            if (_isPlaying)
            {
                AudioSource.Pause();
                _isPlaying = false;

                // 通知 AudioManager 预览停止
                if (RDOnline.Audio.AudioManager.Instance != null)
                {
                    RDOnline.Audio.AudioManager.Instance.OnPreviewStop(this);
                }
            }
            else
            {
                AudioSource.Play();
                _isPlaying = true;

                // 通知 AudioManager 预览开始
                if (RDOnline.Audio.AudioManager.Instance != null)
                {
                    RDOnline.Audio.AudioManager.Instance.OnPreviewStart(this);
                }
            }
        }

        /// <summary>
        /// IChartPreview 接口实现 - 停止预览
        /// </summary>
        public void Stop()
        {
            if (AudioSource != null && _isPlaying)
            {
                AudioSource.Stop();
                _isPlaying = false;

                Debug.Log("[RoomChartPreview] 预览已停止");
            }
        }
    }
}
