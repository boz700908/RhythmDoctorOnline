using System;
using System.Collections.Generic;
using System.Linq;

namespace Credits
{
    /// <summary>
    /// Manages scenes and beat-driven events. Port of Python animator.SceneManager.
    /// </summary>
    public class SceneManager : DataStoringObject
    {
        private readonly Dictionary<string, Scene> _scenes = new Dictionary<string, Scene>();
        private readonly Dictionary<int, List<CreditsEvent>> _events = new Dictionary<int, List<CreditsEvent>>();
        private readonly List<Scene> _activeScenes = new List<Scene>();

        public IReadOnlyList<Scene> ActiveScenes => _activeScenes;
        public int CurBeat { get; set; }

        public SceneManager(Scene[] scenes, CreditsEvent[] events)
        {
            foreach (var s in scenes ?? Array.Empty<Scene>())
            {
                if (s != null && !string.IsNullOrEmpty(s.Name))
                    _scenes[s.Name] = s;
            }

            foreach (var s in scenes ?? Array.Empty<Scene>())
                s?.SetParent(this);

            if (events != null)
            {
                foreach (var e in events)
                {
                    if (e == null) continue;
                    if (!_events.TryGetValue(e.Beat, out var list))
                    {
                        list = new List<CreditsEvent>();
                        _events[e.Beat] = list;
                    }
                    list.Add(e);
                }
            }
        }

        public void StartScene(string sceneName, int at = 0)
        {
            if (!_scenes.TryGetValue(sceneName, out var scene)) return;
            if (_activeScenes.Count == 0)
                _activeScenes.Add(null);
            _activeScenes[0] = scene;
            scene.Start(at);
        }

        public void AddScene(string sceneName, int at = 0)
        {
            if (!_scenes.TryGetValue(sceneName, out var scene)) return;
            _activeScenes.Add(scene);
            scene.Start(at);
        }

        public void RemoveScene(string sceneName)
        {
            if (!_scenes.TryGetValue(sceneName, out var scene)) return;
            _activeScenes.Remove(scene);
        }

        public void RequestNext(bool render = true)
        {
            foreach (var scene in _activeScenes)
                scene?.RequestFrame(render);
            NextBeat();
        }

        public void NextBeat()
        {
            CurBeat++;
            if (_events.TryGetValue(CurBeat, out var list))
            {
                foreach (var ev in list)
                    ev.Do(this);
            }
        }

        public void SetSceneData(string sceneName, params object[] ident)
        {
            if (_scenes.TryGetValue(sceneName, out var scene))
                scene.SetData(ident);
        }

        public void SetGeneratorData(string sceneName, int generatorIndex, params object[] ident)
        {
            if (!_scenes.TryGetValue(sceneName, out var scene)) return;
            if (generatorIndex < 0 || generatorIndex >= scene.Generators.Length) return;
            scene.Generators[generatorIndex].SetData(ident);
        }

        /// <summary>
        /// Add or replace events at a beat (for skip logic). Port of Python assigning controller.events[...].
        /// </summary>
        public void SetEventsAtBeat(int beat, CreditsEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                _events.Remove(beat);
                return;
            }
            _events[beat] = new List<CreditsEvent>(events);
        }
    }
}
