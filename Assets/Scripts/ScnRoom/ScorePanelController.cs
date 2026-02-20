using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 分数数据
    /// </summary>
    [System.Serializable]
    public class ScoreData
    {
        public int UserId;
        public string Username;
        public string NameColor;
        public string Score;
        public float Accuracy; // 用于排序的精准度
    }

    /// <summary>
    /// 分数面板控制器 - 显示所有玩家的分数排名
    /// </summary>
    public class ScorePanelController : MonoBehaviour
    {
        public static ScorePanelController Instance { get; private set; }

        [Header("UI组件")]
        [Tooltip("面板容器（用于缩放动画）")]
        public Transform PanelContainer;
        [Tooltip("分数列表容器（ScrollView的Content）")]
        public Transform ScoreListContent;
        [Tooltip("分数项预制体")]
        public GameObject ScoreItemPrefab;
        [Tooltip("关闭按钮")]
        public Button CloseButton;

        [Header("动画设置")]
        [Tooltip("打开动画时长")]
        public float OpenDuration = 0.3f;
        [Tooltip("关闭动画时长")]
        public float CloseDuration = 0.2f;

        private List<GameObject> _scoreItems = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 初始化面板为关闭状态
            if (PanelContainer != null)
            {
                PanelContainer.localScale = Vector3.zero;
            }

            // 绑定关闭按钮事件
            if (CloseButton != null)
            {
                CloseButton.onClick.AddListener(HidePanel);
            }
        }

        /// <summary>
        /// 显示分数面板
        /// </summary>
        public void ShowPanel(List<ScoreData> scores)
        {
            if (PanelContainer == null || ScoreListContent == null || ScoreItemPrefab == null)
            {
                Debug.LogError("[ScorePanelController] 组件引用未设置");
                return;
            }

            // 清空旧的分数项
            ClearScoreItems();

            // 按精准度排序（从高到低）
            var sortedScores = scores.OrderByDescending(s => s.Accuracy).ToList();

            // 创建分数项
            foreach (var scoreData in sortedScores)
            {
                CreateScoreItem(scoreData);
            }

            // 播放打开动画
            PanelContainer.DOScale(Vector3.one, OpenDuration).SetEase(Ease.OutBack);

            Debug.Log($"[ScorePanelController] 显示分数面板，共 {scores.Count} 个玩家");
        }

        /// <summary>
        /// 隐藏分数面板
        /// </summary>
        public void HidePanel()
        {
            if (PanelContainer == null) return;

            PanelContainer.DOScale(Vector3.zero, CloseDuration).SetEase(Ease.InBack);
            Debug.Log("[ScorePanelController] 隐藏分数面板");
        }

        /// <summary>
        /// 创建分数项
        /// </summary>
        private void CreateScoreItem(ScoreData scoreData)
        {
            GameObject itemObj = Instantiate(ScoreItemPrefab, ScoreListContent);
            ScoreItem scoreItem = itemObj.GetComponent<ScoreItem>();

            if (scoreItem != null)
            {
                scoreItem.SetData(scoreData.Username, scoreData.Score, scoreData.NameColor);
                _scoreItems.Add(itemObj);
            }
            else
            {
                Debug.LogError("[ScorePanelController] ScoreItemPrefab 上没有 ScoreItem 组件");
                Destroy(itemObj);
            }
        }

        /// <summary>
        /// 清空所有分数项
        /// </summary>
        private void ClearScoreItems()
        {
            foreach (var item in _scoreItems)
            {
                if (item != null)
                    Destroy(item);
            }
            _scoreItems.Clear();
        }

        /// <summary>
        /// 从分数字符串中解析精准度
        /// 格式: "... 100.03% 100%"，提取最后的 100
        /// </summary>
        public static float ParseAccuracy(string scoreString)
        {
            if (string.IsNullOrEmpty(scoreString))
                return 0f;

            try
            {
                // 找到最后一个 '%' 的位置
                int lastPercentIndex = scoreString.LastIndexOf('%');
                if (lastPercentIndex == -1)
                    return 0f;

                // 从最后一个 '%' 往前找数字的起始位置
                int startIndex = lastPercentIndex - 1;
                while (startIndex >= 0 && (char.IsDigit(scoreString[startIndex]) || scoreString[startIndex] == '.'))
                {
                    startIndex--;
                }
                startIndex++;

                // 提取数字字符串
                string accuracyStr = scoreString.Substring(startIndex, lastPercentIndex - startIndex);

                // 转换为 float
                if (float.TryParse(accuracyStr, out float accuracy))
                {
                    return accuracy;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScorePanelController] 解析精准度失败: {e.Message}");
            }

            return 0f;
        }
    }
}
