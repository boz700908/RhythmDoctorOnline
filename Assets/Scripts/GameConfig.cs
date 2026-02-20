using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 游戏全局配置
    /// </summary>
    public class GameConfig : MonoBehaviour
    {
        public static GameConfig Instance { get; private set; }

        [Header("开发模式")]
        public bool IsDev = true;

        [Header("服务器配置")]
        public string DevServerUrl = "localhost:4004";
        public string ProdServerUrl = "69.165.65.93:4004";

        [Header("BPM配置")]
        public float BPM = 120f;

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerUrl => IsDev ? DevServerUrl : ProdServerUrl;

        /// <summary>
        /// 游戏运行时BPM（可动态修改）
        /// </summary>
        public float GameBPM { get; set; } = 120f;

        /// <summary>
        /// 每拍时长（秒）
        /// </summary>
        public float BeatDuration => 60f / BPM;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
