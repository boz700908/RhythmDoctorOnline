using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RDOnline.Utils;

namespace RDOnline.Lobby
{
    /// <summary>
    /// 大厅玩家头顶 UI - 头像、头像框、名字（本地与网络玩家共用）
    /// 使用 ResourceLoader 加载网络图片到 RawImage
    /// </summary>
    public class LobbyPlayerHeadUI : MonoBehaviour
    {
        [Header("头顶 UI")]
        [Tooltip("头像")]
        public RawImage AvatarImage;
        [Tooltip("头像框")]
        public RawImage AvatarFrameImage;
        [Tooltip("名字")]
        public TMP_Text NameText;

        /// <summary>
        /// 设置显示内容：名字 + name_color（空/白、hex、rainbow）+ 异步加载头像与头像框
        /// </summary>
        public void SetPlayerInfo(string username, string avatarUrl, string avatarFrameUrl, string nameColor = null)
        {
            if (NameText != null)
            {
                NameText.text = string.IsNullOrEmpty(username) ? "" : username;
                NameColorHelper.ApplyNameColor(NameText, nameColor);
            }

            if (AvatarImage != null)
                StartCoroutine(LoadAndSetTexture(avatarUrl, AvatarImage));

            if (AvatarFrameImage != null)
                StartCoroutine(LoadAndSetTexture(avatarFrameUrl, AvatarFrameImage));
        }

        private IEnumerator LoadAndSetTexture(string url, RawImage target)
        {
            if (target == null) yield break;
            if (string.IsNullOrEmpty(url))
            {
                target.texture = null;
                target.gameObject.SetActive(false);
                yield break;
            }
            target.gameObject.SetActive(true);
            yield return StartCoroutine(ResourceLoader.LoadTexture(url,
                t => { if (target != null) target.texture = t; },
                _ => { }));
        }
    }
}
