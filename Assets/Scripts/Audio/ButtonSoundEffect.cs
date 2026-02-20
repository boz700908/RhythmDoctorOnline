using UnityEngine;
using UnityEngine.UI;

namespace RDOnline.Audio
{
    /// <summary>
    /// 按钮音效组件 - 挂载到按钮上自动播放点击音效
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSoundEffect : MonoBehaviour
    {
        [Header("音效设置")]
        [Tooltip("点击音效片段")]
        public AudioClip ClickSoundClip;

        [Tooltip("使用默认音效（如果未设置音效片段）")]
        public bool UseDefaultSound = true;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }
        }

        /// <summary>
        /// 按钮点击事件
        /// </summary>
        private void OnButtonClick()
        {
            PlayClickSound();
        }

        /// <summary>
        /// 播放点击音效
        /// </summary>
        private void PlayClickSound()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("[ButtonSoundEffect] AudioManager 实例不存在");
                return;
            }

            if (ClickSoundClip != null)
            {
                AudioManager.Instance.PlaySoundEffect(ClickSoundClip);
            }
            else if (UseDefaultSound)
            {
                Debug.LogWarning("[ButtonSoundEffect] 未设置音效片段，且默认音效未实现");
                // TODO: 可以在这里加载默认音效
            }
        }
    }
}
