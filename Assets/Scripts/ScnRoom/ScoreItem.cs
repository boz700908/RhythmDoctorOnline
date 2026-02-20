using UnityEngine;
using TMPro;
using RDOnline.Utils;

namespace RDOnline.ScnRoom
{
    /// <summary>
    /// 分数项组件 - 显示单个玩家的分数信息
    /// </summary>
    public class ScoreItem : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("用户名文本")]
        public TMP_Text UsernameText;
        [Tooltip("分数文本")]
        public TMP_Text ScoreText;

        /// <summary>
        /// 设置分数数据
        /// </summary>
        public void SetData(string username, string score, string nameColor = null)
        {
            if (UsernameText != null)
            {
                UsernameText.text = username;
                NameColorHelper.ApplyNameColor(UsernameText, nameColor);
            }

            if (ScoreText != null)
            {
                ScoreText.text = score;
            }
        }
    }
}