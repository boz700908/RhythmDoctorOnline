using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace RDOnline.Component
{
    /// <summary>
    /// 谱面上传。原实现走 123 云盘的「取 token → 创建文件 → 分片 → 完毕轮询 → 取直链」四步协议，
    /// 现在改为直接 POST 到自建接口，一次把字节流传完，服务器落盘后返回真实下载地址。
    ///
    /// 类名保留 Pan123ChartUploader，避免改动调用方；协议已与 123 云盘无关。
    /// </summary>
    public static class Pan123ChartUploader
    {
        private const int MaxRetries = 3;
        private const float RetryDelaySeconds = 1.5f;

        /// <summary>
        /// 上传谱面包并返回真实下载链接。由调用方 StartCoroutine 执行。
        /// </summary>
        /// <param name="zipData">谱面包二进制（.rdzip，本质是 zip 容器）</param>
        /// <param name="onProgress">上传进度 0~1，可为 null</param>
        /// <param name="onSuccess">成功时返回真实下载链接</param>
        /// <param name="onError">失败时返回错误说明</param>
        public static IEnumerator Upload(
            byte[] zipData,
            Action<float> onProgress,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (zipData == null || zipData.Length == 0)
            {
                onError?.Invoke("谱面文件为空");
                yield break;
            }

            string uploadBase = ModConfig.ChartUploadUrl;
            if (string.IsNullOrEmpty(uploadBase))
            {
                onError?.Invoke("未配置谱面上传地址");
                yield break;
            }

            // 文件名带时间戳，避免服务器上同名覆盖导致旧链接指向新内容
            string filename = "chart_"
                + DateTime.Now.ToString("yyyyMMddHHmmss")
                + "_" + UnityEngine.Random.Range(10000, 99999)
                + ".rdzip";

            string url = uploadBase
                + (uploadBase.IndexOf('?') >= 0 ? "&" : "?")
                + "name=" + UnityWebRequest.EscapeURL(filename);

            string lastError = null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                if (attempt > 0)
                    yield return new WaitForSeconds(RetryDelaySeconds);

                using (var req = new UnityWebRequest(url, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(zipData);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/octet-stream");
                    req.SendWebRequest();

                    while (!req.isDone)
                    {
                        onProgress?.Invoke(req.uploadProgress);
                        yield return null;
                    }

                    onProgress?.Invoke(1f);

                    string body = req.downloadHandler != null ? req.downloadHandler.text : null;

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        // 服务器若给出说明（如「文件过大」「不是有效的 rdzip 包」），直接用它
                        string serverMsg = ParseJsonString(body, "message");
                        if (!string.IsNullOrEmpty(serverMsg))
                        {
                            onError?.Invoke(serverMsg);
                            yield break;
                        }

                        lastError = NormalizeNetworkError(req.error);
                        // 4xx 是请求本身有问题，重试没有意义
                        long code = req.responseCode;
                        if (code >= 400 && code < 500)
                        {
                            onError?.Invoke("上传失败（HTTP " + code + "）");
                            yield break;
                        }
                        continue;
                    }

                    string downloadUrl = ParseJsonString(body, "url");
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        onError?.Invoke("上传失败：服务器未返回下载地址");
                        yield break;
                    }

                    onSuccess?.Invoke(downloadUrl);
                    yield break;
                }
            }

            onError?.Invoke("上传失败：" + (string.IsNullOrEmpty(lastError) ? "多次重试后仍无法完成" : lastError));
        }

        /// <summary>
        /// 将 Unity 的 Unknown Error 或连接被关闭等情况转为更易读的提示。
        /// </summary>
        private static string NormalizeNetworkError(string error)
        {
            if (string.IsNullOrEmpty(error) || error.Contains("Unknown Error"))
                return "网络或协议异常，请稍后重试";
            if (error.IndexOf("PROTOCOL_ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                error.IndexOf("stream", StringComparison.OrdinalIgnoreCase) >= 0)
                return "连接被异常关闭（可稍后重试）";
            return error;
        }

        private static string ParseJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
