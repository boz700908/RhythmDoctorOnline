using System;

namespace Credits
{
    /// <summary>
    /// Single timeline event at a given beat. Port of Python animator.Event.
    /// </summary>
    public class CreditsEvent
    {
        public int Beat { get; }
        public Action<SceneManager> Do { get; }

        public CreditsEvent(int beat, Action<SceneManager> doAction)
        {
            Beat = beat;
            Do = doAction ?? (_ => { });
        }

        public static Action<SceneManager> SwapScene(string sceneName, int at = 0)
        {
            return sm => sm.StartScene(sceneName, at);
        }

        public static Action<SceneManager> LayerScene(string sceneName, int at = 0)
        {
            return sm => sm.AddScene(sceneName, at);
        }

        public static Action<SceneManager> RemoveScene(string sceneName)
        {
            return sm => sm.RemoveScene(sceneName);
        }
    }
}
