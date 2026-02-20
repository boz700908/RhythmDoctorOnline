using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RDOnline.Component
{
    /// <summary>
    /// 文件项 - 用于文件浏览器的文件列表项
    /// </summary>
    public class FileItem : MonoBehaviour
    {
        [Header("UI组件")]
        public Button Button;
        public TMP_Text NameText;

        private string _filePath;
        private Action<string> _onClickCallback;

        private void Awake()
        {
            // 自动获取组件
            if (Button == null)
                Button = GetComponent<Button>();
            if (NameText == null)
                NameText = GetComponentInChildren<TMP_Text>();

            // 禁用文本的射线检测，避免遮挡按钮
            if (NameText != null)
                NameText.raycastTarget = false;

            // 绑定按钮点击事件
            if (Button != null)
                Button.onClick.AddListener(OnButtonClick);
        }

        /// <summary>
        /// 初始化文件项
        /// </summary>
        public void Initialize(string fileName, string filePath, Action<string> onClickCallback)
        {
            _filePath = filePath;
            _onClickCallback = onClickCallback;

            if (NameText != null)
            {
                NameText.text = fileName;
            }
        }

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClick()
        {
            _onClickCallback?.Invoke(_filePath);
        }
    }
}
