using System;
using System.Collections;
using UnityEngine;
using RDOnline.Network;

namespace RDOnline.Network
{
    /// <summary>
    /// 网络延迟管理器 - 负责定期发送ping请求并计算延迟
    /// </summary>
    public class NetworkPingManager : MonoBehaviour
    {
        public static NetworkPingManager Instance { get; private set; }

        [Header("设置")]
        [Tooltip("Ping检测间隔（秒）")]
        public float PingInterval = 5f;

        /// <summary>
        /// 当前延迟值（毫秒）
        /// </summary>
        public int CurrentPing { get; private set; }

        /// <summary>
        /// 延迟更新事件
        /// </summary>
        public event Action<int> OnPingUpdated;

        private long _pingStartTime;
        private Coroutine _pingLoopCoroutine;

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
            // 注册pong事件监听
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.Register("pong", OnPongReceived);
            }

            // 开始Ping循环
            StartPingLoop();
        }

        private void OnDestroy()
        {
            // 取消注册
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.Unregister("pong", OnPongReceived);
            }

            // 停止协程
            if (_pingLoopCoroutine != null)
            {
                StopCoroutine(_pingLoopCoroutine);
            }
        }

        /// <summary>
        /// 开始Ping循环
        /// </summary>
        public void StartPingLoop()
        {
            if (_pingLoopCoroutine != null)
            {
                StopCoroutine(_pingLoopCoroutine);
            }
            _pingLoopCoroutine = StartCoroutine(PingLoop());
        }

        /// <summary>
        /// 停止Ping循环
        /// </summary>
        public void StopPingLoop()
        {
            if (_pingLoopCoroutine != null)
            {
                StopCoroutine(_pingLoopCoroutine);
                _pingLoopCoroutine = null;
            }
        }

        /// <summary>
        /// Ping循环协程
        /// </summary>
        private IEnumerator PingLoop()
        {
            while (true)
            {
                if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
                {
                    SendPing();
                }
                yield return new WaitForSeconds(PingInterval);
            }
        }

        /// <summary>
        /// 发送Ping请求
        /// </summary>
        private void SendPing()
        {
            _pingStartTime = GetCurrentTimestamp();
            WebSocketManager.Instance.Send("ping");
        }

        /// <summary>
        /// 处理Pong响应
        /// </summary>
        private void OnPongReceived(ResponseMessage res)
        {
            long currentTime = GetCurrentTimestamp();
            CurrentPing = (int)(currentTime - _pingStartTime);

            // 触发延迟更新事件
            OnPingUpdated?.Invoke(CurrentPing);
        }

        /// <summary>
        /// 获取当前时间戳（毫秒）
        /// </summary>
        private long GetCurrentTimestamp()
        {
            return (long)(Time.realtimeSinceStartup * 1000);
        }
    }
}
