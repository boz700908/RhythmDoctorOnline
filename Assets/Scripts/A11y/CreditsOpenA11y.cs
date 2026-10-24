using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using RDOnline.A11y;

namespace RDOnline.A11y
{
    /// <summary>
    /// 大厅彩蛋按钮（CreditsOpen）的提示朗读。
    ///
    /// 这个按钮每点一次弹一句「别点我！」之类的提示，几秒后自动消失，
    /// 屏幕上没有任何焦点能停在提示上，读屏用户点完只会听到一声按钮回执。
    /// 所以这里盯住提示文本，一出现就读一句，不占焦点、不建导航项。
    ///
    /// 不改场景文件：场景加载后自己找 CreditsOpen，找不到就什么都不做。
    /// 提示文本字段在上游脚本里是私有的，这里用反射读，避免改动上游代码。
    /// </summary>
    public class CreditsOpenA11y : MonoBehaviour
    {
        private CreditsOpen _open;
        private TMP_Text _message;
        private string _shown;
        private float _nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CreditsOpen open = Object.FindObjectOfType<CreditsOpen>();
            if (open == null) return;

            // 一个场景挂一份就够
            if (open.GetComponent<CreditsOpenA11y>() != null) return;
            open.gameObject.AddComponent<CreditsOpenA11y>();
        }

        private void Awake()
        {
            _open = GetComponent<CreditsOpen>();
        }

        private void Update()
        {
            if (_message == null && Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.5f;
                _message = FindMessage();
            }

            if (_message == null) return;

            string text = _message.text;
            if (string.IsNullOrEmpty(text) || text == _shown) return;

            _shown = text;
            A11yNavigator.SayInfo(text, NarrationCategory.Notification);
        }

        /// <summary>
        /// 取提示文本。字段是私有的，先找场景里叫得出名字的那一个，
        /// 反射失败时退而求其次：找 CreditsOpen 下属第一个不是按钮的文本。
        /// </summary>
        private TMP_Text FindMessage()
        {
            if (_open != null)
            {
                FieldInfo field = typeof(CreditsOpen).GetField(
                    "messageText", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)
                {
                    TMP_Text text = field.GetValue(_open) as TMP_Text;
                    if (text != null) return text;
                }
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null) continue;
                if (texts[i].GetComponentInParent<UnityEngine.UI.Button>() != null) continue;
                return texts[i];
            }

            return null;
        }
    }
}
