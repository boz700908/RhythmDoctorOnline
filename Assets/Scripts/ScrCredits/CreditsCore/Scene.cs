using System;

namespace Credits
{
    /// <summary>
    /// A named set of generators that run each beat. Port of Python animator.Scene.
    /// </summary>
    public class Scene : DataStoringObject
    {
        public SceneManager Parent { get; private set; }
        public string Name { get; }
        public Generator[] Generators { get; }
        public int StartBeat { get; set; }
        public int InternalBeat { get; set; }

        public Scene(string name, Generator[] generators)
        {
            Name = name ?? "";
            Generators = generators ?? Array.Empty<Generator>();
        }

        public void SetParent(SceneManager parent) => Parent = parent;

        /// <summary>
        /// Requests a frame: for each generator that matches condition, clear previous then request current.
        /// </summary>
        public void RequestFrame(bool render = true)
        {
            int beat = InternalBeat;
            if (render)
            {
                foreach (var g in Generators)
                {
                    if (beat >= g.StartBeat && g.Condition(beat))
                    {
                        if (beat != StartBeat && beat != g.StartBeat)
                            g.RequestClear(g, beat - 1);
                        g.Request(g, beat);
                    }
                }
            }
            InternalBeat++;
        }

        /// <summary>
        /// Starts the scene at the given beat; wires parent/scene and calls on_create, then one request_frame.
        /// </summary>
        public void Start(int at)
        {
            foreach (var g in Generators)
            {
                g.SetParent(Parent);
                g.SetScene(this);
                g.OnCreate(g);
            }
            StartBeat = at;
            InternalBeat = at;
            RequestFrame(true);
        }
    }
}
