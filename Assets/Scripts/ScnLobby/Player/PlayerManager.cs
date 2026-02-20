using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using RDOnline;
using RDOnline.Network;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 大厅玩家管理器 - 生成本地玩家与网络玩家，监听 lobby 推送与定时同步
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        [Header("出生点")]
        [Tooltip("玩家生成父节点（空物体），坐标相对此节点")]
        public Transform SpawnPoint;

        [Header("预制体")]
        [Tooltip("本地玩家预制体（需挂 LobbyPlayerController + Animator）")]
        public GameObject LocalPlayerPrefab;
        [Tooltip("网络玩家预制体（需挂 NetworkPlayerView + Animator）")]
        public GameObject NetworkPlayerPrefab;

        private GameObject _localPlayer;
        private readonly Dictionary<int, GameObject> _networkPlayers = new Dictionary<int, GameObject>();

        /// <summary> 本地玩家 Transform，生成本地玩家后才有。相机“返回跟随玩家”时可用此引用。 </summary>
        public Transform LocalPlayerTransform => _localPlayer != null ? _localPlayer.transform : null;

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
            RegisterLobbyEvents();
        }

        private void OnDestroy()
        {
            UnregisterLobbyEvents();
            if (WebSocketManager.Instance != null && WebSocketManager.Instance.IsConnected)
                WebSocketManager.Instance.Send("lobby/leave", new { });
        }

        private void RegisterLobbyEvents()
        {
            if (WebSocketManager.Instance == null) return;
            WebSocketManager.Instance.Register("lobby/playerJoin", OnPlayerJoin);
            WebSocketManager.Instance.Register("lobby/playerLeave", OnPlayerLeave);
            WebSocketManager.Instance.Register("lobby/playerMove", OnPlayerMove);
            WebSocketManager.Instance.Register("lobby/sync", OnLobbySync);
        }

        private void UnregisterLobbyEvents()
        {
            if (WebSocketManager.Instance == null) return;
            WebSocketManager.Instance.Unregister("lobby/playerJoin", OnPlayerJoin);
            WebSocketManager.Instance.Unregister("lobby/playerLeave", OnPlayerLeave);
            WebSocketManager.Instance.Unregister("lobby/playerMove", OnPlayerMove);
            WebSocketManager.Instance.Unregister("lobby/sync", OnLobbySync);
        }

        /// <summary>
        /// 加入大厅成功后初始化玩家（由 LobbyJoiner 回调调用）
        /// data.players 为当前已在厅内的其他玩家，不含自己
        /// </summary>
        public void InitWithPlayers(JObject data)
        {
            if (data == null) return;

            Transform root = SpawnPoint != null ? SpawnPoint : transform;

            // 生成本地玩家（仅一次）
            if (_localPlayer == null && LocalPlayerPrefab != null)
            {
                _localPlayer = Instantiate(LocalPlayerPrefab, root);
                _localPlayer.transform.localPosition = new Vector3(0f, 0f, 0f);
                if (PlayerCamera.Instance != null)
                    PlayerCamera.Instance.SetFollowTarget(_localPlayer.transform);
                Debug.Log("[PlayerManager] 本地玩家已生成");
            }

            // 根据返回的其他玩家列表生成网络玩家
            var players = data["players"] as JArray;
            if (players != null)
            {
                foreach (var p in players)
                {
                    var obj = p as JObject;
                    if (obj == null) continue;
                    ParsePlayerAndCreateOrUpdate(obj, root);
                }
            }
        }

        private void OnPlayerJoin(ResponseMessage msg)
        {
            if (msg?.Data == null || !msg.Data.ContainsKey("player")) return;
            var player = msg.Data["player"] as JObject;
            if (player == null) return;
            int userId = player["userId"]?.ToObject<int>() ?? 0;
            if (userId <= 0) return;
            if (IsSelf(userId)) return;
            Transform root = SpawnPoint != null ? SpawnPoint : transform;
            CreateOrUpdateNetworkPlayer(root, userId, player);
            Debug.Log($"[PlayerManager] 玩家进入大厅: {userId}");
        }

        private void OnPlayerLeave(ResponseMessage msg)
        {
            if (msg?.Data == null || !msg.Data.ContainsKey("userId")) return;
            int userId = (msg.Data["userId"] as JToken)?.ToObject<int>() ?? 0;
            RemoveNetworkPlayer(userId);
        }

        private void OnPlayerMove(ResponseMessage msg)
        {
            if (msg?.Data == null) return;
            int userId = msg.Data["userId"]?.ToObject<int>() ?? 0;
            float x = msg.Data["x"]?.ToObject<float>() ?? 0f;
            float y = msg.Data["y"]?.ToObject<float>() ?? 0f;
            // 本地玩家只负责发送位置，不应用服务器回显/广播的自己的位置，否则高延迟会被拉回
            if (IsSelf(userId))
                return;
            if (_networkPlayers.TryGetValue(userId, out var go))
            {
                var view = go.GetComponent<NetworkPlayerView>();
                if (view != null)
                    view.SetPosition(x, y);
            }
        }

        /// <summary>
        /// 定时广播 lobby/sync：用 data.players 全量同步，data.rooms 由 RoomListUI 处理
        /// </summary>
        private void OnLobbySync(ResponseMessage msg)
        {
            if (msg?.Data == null || !msg.Data.ContainsKey("players")) return;
            var players = msg.Data["players"] as JArray;
            if (players == null) return;

            Transform root = SpawnPoint != null ? SpawnPoint : transform;
            var serverUserIds = new HashSet<int>();
            int selfId = GetSelfUserId();

            foreach (var p in players)
            {
                var obj = p as JObject;
                if (obj == null) continue;
                int userId = obj["userId"]?.ToObject<int>() ?? 0;
                if (userId <= 0) continue;
                serverUserIds.Add(userId);
                float x = obj["x"]?.ToObject<float>() ?? 0f;
                float y = obj["y"]?.ToObject<float>() ?? 0f;
                // 本地玩家不应用 sync 里的自己的坐标，避免高延迟时被拉回
                if (IsSelf(userId))
                    continue;
                CreateOrUpdateNetworkPlayer(root, userId, obj);
            }

            // 移除服务器列表里已不存在的网络玩家
            List<int> toRemove = new List<int>();
            foreach (var kv in _networkPlayers)
            {
                if (!serverUserIds.Contains(kv.Key))
                    toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove)
                RemoveNetworkPlayer(id);
        }

        private bool IsSelf(int userId)
        {
            return userId > 0 && UserData.Instance != null && UserData.Instance.Id == userId;
        }

        private int GetSelfUserId()
        {
            return UserData.Instance != null ? UserData.Instance.Id : 0;
        }

        private void ParsePlayerAndCreateOrUpdate(JObject player, Transform root)
        {
            int userId = player["userId"]?.ToObject<int>() ?? 0;
            if (userId <= 0) return;
            float x = player["x"]?.ToObject<float>() ?? 0f;
            float y = player["y"]?.ToObject<float>() ?? 0f;
            CreateOrUpdateNetworkPlayer(root, userId, player);
        }

        private void CreateOrUpdateNetworkPlayer(Transform root, int userId, JObject playerData)
        {
            if (NetworkPlayerPrefab == null || root == null) return;
            float x = playerData["x"]?.ToObject<float>() ?? 0f;
            float y = playerData["y"]?.ToObject<float>() ?? 0f;
            if (_networkPlayers.TryGetValue(userId, out var go))
            {
                var view = go.GetComponent<NetworkPlayerView>();
                if (view != null)
                    view.SetPosition(x, y);
                return;
            }
            string username = playerData["username"]?.ToString();
            string avatar = playerData["avatar"]?.ToString();
            string avatarFrame = playerData["avatarFrame"]?.ToString();
            string nameColor = playerData["nameColor"]?.ToString();

            var obj = Instantiate(NetworkPlayerPrefab, root);
            obj.transform.localPosition = new Vector3(x, y, 0f);
            var nv = obj.GetComponent<NetworkPlayerView>();
            if (nv != null)
                nv.Init(userId, x, y, username, avatar, avatarFrame, nameColor);
            _networkPlayers[userId] = obj;
        }

        private void RemoveNetworkPlayer(int userId)
        {
            if (!_networkPlayers.TryGetValue(userId, out var go)) return;
            _networkPlayers.Remove(userId);
            if (go != null)
                Destroy(go);
            Debug.Log($"[PlayerManager] 移除网络玩家: {userId}");
        }
    }
}
