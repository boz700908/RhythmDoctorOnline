using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RDOnline.Component
{
    /// <summary>
    /// 文件浏览器 - 用于选择ADOFAI谱面文件
    /// </summary>
    public class FileBrowser : MonoBehaviour
    {
        [Header("滚动视图")]
        [Tooltip("左侧文件夹滚动视图的Content")]
        public Transform FolderContent;
        [Tooltip("右侧文件滚动视图的Content")]
        public Transform FileContent;

        [Header("预制体")]
        [Tooltip("文件夹项预制体（包含Button和TMP_Text）")]
        public GameObject FolderItemPrefab;
        [Tooltip("文件项预制体（包含Button和TMP_Text）")]
        public GameObject FileItemPrefab;

        [Header("状态")]
        [Tooltip("当前选中的文件夹路径")]
        public string SelectedFolderPath;
        [Tooltip("当前选中的文件路径")]
        public string SelectedFilePath;

        [Header("动画设置")]
        [Tooltip("项目淡入持续时间")]
        public float FadeDuration = 0.2f;
        [Tooltip("项目移动距离")]
        public float MoveDistance = 20f;
        [Tooltip("项目之间的动画延迟")]
        public float ItemDelay = 0.03f;

        [Header("谱面预览")]
        [Tooltip("谱面预览组件")]
        public ChartPreview ChartPreview;

        [Header("刷新按钮")]
        [Tooltip("刷新按钮")]
        public Button RefreshButton;

        private string _levelsRootPath;
        private List<GameObject> _folderItems = new List<GameObject>();
        private List<GameObject> _fileItems = new List<GameObject>();

        private void Start()
        {
            // 初始化路径
            switch (Application.platform)
            {
                /*case RuntimePlatform.Android:
                    _levelsRootPath = ExtraUtils.GetSDCardPath() + "/ADOFAILevels";
                    File.WriteAllText(_levelsRootPath + "/.nomedia"," ");
                    break;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    _levelsRootPath = RDFile.FindCanUseDisk() + "\\ADOFAILevels";
                    break;*/
                default:
                    _levelsRootPath = Path.Combine(Application.persistentDataPath, "ADOFAILevels");
                    break;
            }
            
            // 确保目录存在
            if (!Directory.Exists(_levelsRootPath))
            {
                Directory.CreateDirectory(_levelsRootPath);
            }

            // 绑定刷新按钮
            if (RefreshButton != null)
            {
                RefreshButton.onClick.AddListener(OnRefreshButtonClick);
            }

            // 加载文件夹列表
            LoadFolders();
        }

        /// <summary>
        /// 加载文件夹列表
        /// </summary>
        private void LoadFolders()
        {
            // 清空现有项
            ClearItems(_folderItems);

            // 获取所有文件夹
            string[] folders = Directory.GetDirectories(_levelsRootPath);

            for (int i = 0; i < folders.Length; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath);
                CreateFolderItem(folderName, folderPath, i);
            }
        }

        /// <summary>
        /// 创建文件夹项
        /// </summary>
        private void CreateFolderItem(string folderName, string folderPath, int index)
        {
            GameObject item = Instantiate(FolderItemPrefab, FolderContent);
            _folderItems.Add(item);

            // 获取FolderItem组件并初始化
            FolderItem folderItem = item.GetComponent<FolderItem>();
            if (folderItem != null)
            {
                folderItem.Initialize(folderName, folderPath, OnFolderClick);
            }

            // 播放进入动画
            PlayItemEnterAnimation(item, index);
        }

        /// <summary>
        /// 文件夹点击事件
        /// </summary>
        private void OnFolderClick(string folderPath)
        {
            SelectedFolderPath = folderPath;
            SelectedFilePath = null; // 清空文件选择
            LoadFiles(folderPath);
        }

        /// <summary>
        /// 加载文件列表
        /// </summary>
        private void LoadFiles(string folderPath)
        {
            // 清空现有项
            ClearItems(_fileItems);

            // 获取所有.adofai文件
            string[] files = Directory.GetFiles(folderPath, "*.adofai");

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string fileName = Path.GetFileName(filePath);
                CreateFileItem(fileName, filePath, i);
            }
        }

        /// <summary>
        /// 创建文件项
        /// </summary>
        private void CreateFileItem(string fileName, string filePath, int index)
        {
            GameObject item = Instantiate(FileItemPrefab, FileContent);
            _fileItems.Add(item);

            // 获取FileItem组件并初始化
            FileItem fileItem = item.GetComponent<FileItem>();
            if (fileItem != null)
            {
                fileItem.Initialize(fileName, filePath, OnFileClick);
            }

            // 播放进入动画
            PlayItemEnterAnimation(item, index);
        }

        /// <summary>
        /// 文件点击事件
        /// </summary>
        private void OnFileClick(string filePath)
        {
            SelectedFilePath = filePath;

            // 调用谱面预览
            if (ChartPreview != null)
            {
                ChartPreview.LoadChart(filePath);
            }
        }

        /// <summary>
        /// 清空列表项
        /// </summary>
        private void ClearItems(List<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                Destroy(item);
            }
            items.Clear();
        }

        /// <summary>
        /// 播放项目进入动画
        /// </summary>
        private void PlayItemEnterAnimation(GameObject item, int index)
        {
            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = item.AddComponent<CanvasGroup>();
            }

            // 设置初始状态
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true; // 允许交互
            canvasGroup.blocksRaycasts = true; // 允许接收射线检测

            // 计算延迟
            float delay = index * ItemDelay;

            // 只播放淡入动画，不改变位置
            canvasGroup.DOFade(1f, FadeDuration).SetDelay(delay).SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void OnRefreshButtonClick()
        {
            // 重新加载文件夹列表
            LoadFolders();

            // 如果有选中的文件夹，也刷新文件列表
            if (!string.IsNullOrEmpty(SelectedFolderPath) && Directory.Exists(SelectedFolderPath))
            {
                LoadFiles(SelectedFolderPath);
            }
        }
    }
}