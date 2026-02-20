using UnityEngine;

namespace RDOnline
{
    public class StartUp : MonoBehaviour
    {
        [Header("跳转延迟(秒)")]
        public float Delay = 2f;

        [Header("目标场景")]
        public string NextScene = "Login";

        private void Start()
        {
            Invoke(nameof(LoadNextScene), Delay);
        }

        private void LoadNextScene()
        {
            ScnLoading.LoadScenes(NextScene);
        }
    }
}
