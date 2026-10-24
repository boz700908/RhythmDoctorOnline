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
        public string DevServerUrl = "47.238.109.184:3005";
        public string ProdServerUrl = "47.238.109.184:3005";

        [Header("123 云盘 Token")]
        public string Pan123TokenUrlDev = "http://47.238.109.184:3004/pan123/token";
        public string Pan123TokenUrlProd = "http://47.238.109.184:3004/pan123/token";

        [Header("BPM配置")]
        public float BPM = 120f;

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerUrl => ModConfig.WsHost;

        /// <summary>
        /// 123 云盘 Token 接口（根据 IsDev 切换开发/生产）
        /// </summary>
        public string Pan123TokenUrl => ModConfig.ChartUploadUrl;

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
