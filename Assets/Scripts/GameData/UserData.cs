using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 用户数据管理器，存储当前登录用户信息
    /// </summary>
    public class UserData : MonoBehaviour
    {
        public static UserData Instance { get; private set; }

    [Header("用户信息")]
    public int Id;
    public string Username;
    public string NameColor;
    public string Email;
    public string Avatar;
    public string AvatarFrame;
    public string QQ;
    public int Role;

    [Header("认证信息")]
    public string Token;

        /// <summary>
        /// 是否已登录
        /// </summary>
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 设置登录数据
        /// </summary>
        public void SetLoginData(int id, string username, string nameColor, string email, string avatar, string avatarFrame, string qq, int role, string token)
        {
            Id = id;
            Username = username;
            NameColor = nameColor;
            Email = email;
            Avatar = avatar;
            AvatarFrame = avatarFrame;
            QQ = qq;
            Role = role;
            Token = token;
        }

        /// <summary>
        /// 清除登录数据
        /// </summary>
        public void Clear()
        {
            Id = 0;
            Username = null;
            NameColor = null;
            Email = null;
            Avatar = null;
            AvatarFrame = null;
            QQ = null;
            Role = 0;
            Token = null;
        }
    }
}
