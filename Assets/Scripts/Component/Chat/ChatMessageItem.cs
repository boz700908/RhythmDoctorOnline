using UnityEngine;
using TMPro;

namespace RDOnline.Component
{
    /// <summary>
    /// 聊天消息项 - 显示单条聊天消息
    /// </summary>
    public class ChatMessageItem : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("消息文本")]
        public TMP_Text MessageText;

        /// <summary>
        /// 设置消息内容
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="message">消息内容</param>
        public void SetMessage(string username, string message)
        {
            if (MessageText == null)
            {
                Debug.LogError("[ChatMessageItem] MessageText 未设置");
                return;
            }

            // 使用富文本高亮用户名
            string formattedMessage = $"<color=#FFD700><b>{username}</b></color>: {message}";
            MessageText.text = formattedMessage;
        }
    }
}
