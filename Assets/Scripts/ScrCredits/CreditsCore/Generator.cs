using System;

namespace Credits
{
    /// <summary>
    /// Produces content for a scene each beat when condition(beat) is true. Port of Python animator.Generator.
    /// </summary>
    public class Generator : DataStoringObject
    {
        public SceneManager Parent { get; private set; }
        public Scene Scene { get; private set; }

        public int StartBeat { get; }
        public Func<int, bool> Condition { get; }
        public Action<Generator> OnCreate { get; }
        public Action<Generator, int> Request { get; }
        public Action<Generator, int> RequestClear { get; }

        public Generator(
            int startBeat,
            Func<int, bool> condition,
            Action<Generator> onCreate,
            Action<Generator, int> request,
            Action<Generator, int> requestClear)
        {
            StartBeat = startBeat;
            Condition = condition ?? (_ => true);
            OnCreate = onCreate ?? (_ => { });
            Request = request ?? ((_, __) => { });
            RequestClear = requestClear ?? ((_, __) => { });
        }

        public void SetParent(SceneManager parent) => Parent = parent;
        public void SetScene(Scene scene) => Scene = scene;

        public static Func<int, bool> CombineConditions(params Func<int, bool>[] conditions)
        {
            return b =>
            {
                foreach (var c in conditions)
                    if (!c(b)) return false;
                return true;
            };
        }

        public static Func<int, bool> Always() => _ => true;

        public static Func<int, bool> EveryNBeats(int n) => b => n > 0 && (b % n) == 0;

        public static Func<int, bool> EveryOnOff(int on, int off) => b => (b % (on + off)) < on;

        public static Func<int, bool> EveryOffOn(int off, int on) => b => (b % (off + on)) >= off;

        public static Func<int, bool> BeforeN(int beat) => b => b < beat;

        public static Func<int, bool> AtBeat(int beat) => b => b == beat;

        public static Action<Generator> NoCreate() => _ => { };

        public static Action<Generator, int> NoRequest() => (_, __) => { };
    }
}
