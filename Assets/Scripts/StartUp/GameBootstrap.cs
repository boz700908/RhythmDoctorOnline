using UnityEngine;

namespace RDOnline
{
    /// <summary>
    /// 游戏启动引导，负责保持子物体不被销毁
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
