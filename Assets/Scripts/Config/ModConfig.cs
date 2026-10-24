using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 外部地址配置。所有服务器地址都从 mod 目录下的 rdol.config.json 读取，
    /// 代码与场景里都不再写死任何主机名；文件不存在或字段缺失时用出厂默认值。
    ///
    /// 文件放在 RDOL.dll 旁边（BepInEx/plugins/RDOnline/rdol.config.json）。
    /// </summary>
    public static class ModConfig
    {
        public const string FileName = "rdol.config.json";

        private const string DefaultWsUrl = "ws://47.238.109.184:3005";
        private const string DefaultApiBase = "http://47.238.109.184:3004";

        private static bool _loaded;
        private static string _wsUrl;
        private static string _apiBase;
        private static string _versionCheckUrl;
        private static string _chartUploadUrl;
        private static string _chartListUrl;

        /// <summary>配置文件所在目录，即 mod 所在目录。</summary>
        public static string ConfigDir
        {
            get
            {
                string dir = BepInModEntry.modPath;
                if (string.IsNullOrEmpty(dir))
                    dir = Path.GetDirectoryName(typeof(ModConfig).Assembly.Location);
                return dir;
            }
        }

        public static string ConfigPath => Path.Combine(ConfigDir, FileName);

        /// <summary>联机用的 WebSocket 完整地址，例如 ws://1.2.3.4:3005。</summary>
        public static string WsUrl { get { EnsureLoaded(); return _wsUrl; } }

        /// <summary>HTTP 接口根地址（谱面、版本检查、健康检查）。</summary>
        public static string ApiBase { get { EnsureLoaded(); return _apiBase; } }

        /// <summary>版本检查根地址，默认与 ApiBase 相同。</summary>
        public static string VersionCheckUrl { get { EnsureLoaded(); return _versionCheckUrl; } }

        /// <summary>谱面上传地址。</summary>
        public static string ChartUploadUrl { get { EnsureLoaded(); return _chartUploadUrl; } }

        /// <summary>
        /// 社区谱面列表（Cafe）地址，返回 Typesense 形状的搜索结果。
        /// 原来的两个源都已失效，默认指向自建服务，与官方上游 rhythm.cafe 同步。
        /// </summary>
        public static string ChartListUrl { get { EnsureLoaded(); return _chartListUrl; } }

        /// <summary>兼容旧字段：不带协议头的主机:端口。</summary>
        public static string WsHost
        {
            get
            {
                string url = WsUrl;
                int i = url.IndexOf("://", StringComparison.Ordinal);
                return i >= 0 ? url.Substring(i + 3) : url;
            }
        }

        /// <summary>重新读取配置文件，改完地址不必重启游戏。</summary>
        public static void Reload()
        {
            _loaded = false;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _wsUrl = DefaultWsUrl;
            _apiBase = DefaultApiBase;
            _versionCheckUrl = DefaultApiBase;
            _chartUploadUrl = Combine(DefaultApiBase, "/charts/upload");
            _chartListUrl = Combine(DefaultApiBase, "/api/get_chartlist.php");

            string path = ConfigPath;
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[RDOL] 未找到 {path}，使用默认地址 {DefaultApiBase}");
                return;
            }

            try
            {
                JObject jo = JObject.Parse(File.ReadAllText(path));

                string api = Str(jo, "apiBase");
                if (!string.IsNullOrEmpty(api)) _apiBase = Normalize(api);

                string ws = Str(jo, "wsUrl");
                _wsUrl = string.IsNullOrEmpty(ws) ? ToWsUrl(_apiBase, 3005) : Normalize(ws);

                string ver = Str(jo, "versionCheckUrl");
                _versionCheckUrl = string.IsNullOrEmpty(ver) ? _apiBase : Normalize(ver);

                string up = Str(jo, "chartUploadUrl");
                _chartUploadUrl = string.IsNullOrEmpty(up)
                    ? Combine(_apiBase, "/charts/upload")
                    : Combine(Normalize(up), string.Empty);

                string list = Str(jo, "chartListUrl");
                _chartListUrl = string.IsNullOrEmpty(list)
                    ? Combine(_apiBase, "/api/get_chartlist.php")
                    : Combine(Normalize(list), string.Empty);

                Debug.Log($"[RDOL] 已读取配置 {path}: ws={_wsUrl} api={_apiBase} version={_versionCheckUrl} list={_chartListUrl}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RDOL] 读取 {path} 失败，使用默认地址：{e.Message}");
            }
        }

        private static string Str(JObject jo, string key)
        {
            JToken t = jo[key];
            return t == null ? null : t.ToString().Trim();
        }

        private static string Normalize(string url)
        {
            return url.Trim().TrimEnd('/');
        }

        public static string Combine(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(path)) return baseUrl;
            if (path.StartsWith("/")) return baseUrl + path;
            return baseUrl + "/" + path;
        }

        /// <summary>由 HTTP 根地址推出同主机的 WebSocket 地址。</summary>
        public static string ToWsUrl(string apiBase, int port)
        {
            string s = apiBase;
            int i = s.IndexOf("://", StringComparison.Ordinal);
            string scheme = "http";
            if (i >= 0)
            {
                scheme = s.Substring(0, i);
                s = s.Substring(i + 3);
            }
            string host = s;
            int slash = host.IndexOf('/');
            if (slash >= 0) host = host.Substring(0, slash);
            int colon = host.IndexOf(':');
            if (colon >= 0) host = host.Substring(0, colon);
            return $"{(scheme == "https" ? "wss" : "ws")}://{host}:{port}";
        }
    }
}
