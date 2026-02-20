using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RDOnline.Utils
{
    /// <summary>
    /// 资源加载器 - 封装网络资源加载的通用方法
    /// </summary>
    public static class ResourceLoader
    {
        /// <summary>
        /// 加载 Texture2D（用于头像等图片资源）
        /// </summary>
        /// <param name="url">图片URL</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">失败回调</param>
        public static IEnumerator LoadTexture(string url, Action<Texture2D> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("URL为空");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    onSuccess?.Invoke(texture);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }

        /// <summary>
        /// 加载二进制数据（用于文件下载）
        /// </summary>
        /// <param name="url">文件URL</param>
        /// <param name="onSuccess">成功回调</param>
        /// <param name="onError">失败回调</param>
        public static IEnumerator LoadBytes(string url, Action<byte[]> onSuccess, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                onError?.Invoke("URL为空");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] data = request.downloadHandler.data;
                    onSuccess?.Invoke(data);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }
    }
}
