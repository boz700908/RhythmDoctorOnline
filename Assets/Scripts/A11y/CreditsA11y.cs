using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using RDOnline.A11y;

namespace RDOnline.A11y
{
    /// <summary>
    /// OnlineCredits（制作人员字幕）的无障碍朗读。
    ///
    /// 这个场景是一段音频驱动的字符动画：屏幕上 80×24 个字符格子，几乎全部
    /// 是拿 # ~ @ 之类的符号拼出来的画面（噪点、海浪、节拍方块、加载条、
    /// 乱码）。它们是画画的原料，不是给人念的文本，逐字念出来只有杂音。
    /// 所以这里一个字都不念，而是按画面上真正换内容的那一刻写好描述，
    /// 画面推进到哪个节点就读哪一句。
    ///
    /// 不新建无障碍开关，也不绕开游戏设置：所有发声都走 Narration.Say 的
    /// NarrateSubtitles 分类，读不读、念什么由玩家自己的设置决定；
    /// 无障碍总开关关掉时这里什么都不说，只保留 Esc 返回这个实际功能。
    ///
    /// 不做导航、不占键盘。只接管 Esc：任何时候都能回大厅。
    /// 播放本身的按键（P 暂停、逗号句号斜杠加速）原样保留，在开场说明里讲清。
    ///
    /// 节点位置按当前节拍判定。节拍是唯一的画面时钟：暂停、加速、跳段都只
    /// 改变节拍推进的速度，不改变节拍和画面的对应关系，所以按节拍判定不会
    /// 和时间线错位。
    /// </summary>
    public class CreditsA11y : MonoBehaviour
    {
        /// <summary>画面节点：从这一拍开始，屏幕上是什么。</summary>
        private struct Node
        {
            public readonly int Beat;
            public readonly string Zh;
            public readonly string En;

            public Node(int beat, string zh, string en)
            {
                Beat = beat;
                Zh = zh;
                En = en;
            }
        }

        /// <summary>
        /// 朗读分类。用 NarrationCategory.Subtitles 是因为这段内容的性质就是
        /// 屏幕上滚动的字幕／歌词：玩家在设置里关掉「朗读字幕」就不会出声。
        /// 想改成跟着「朗读描述」走，把这里换成 NarrationCategory.Description 即可。
        /// </summary>
        private const NarrationCategory Category = NarrationCategory.Subtitles;

        /// <summary>
        /// 节点表，节拍数取自 CreditsTimeline.Build()。
        /// 只列画面上真正换了内容的那一刻，装饰性的抖动、变色不单列。
        /// </summary>
        private static readonly Node[] Nodes =
        {
            new Node(0, "屏幕是黑的，只有音乐在走。",
                "The screen is black; only the music plays."),

            new Node(60, "老式气象字幕机开始播报国家气象局预报：马萨诸塞州东部、四九五号公路以内包括波士顿地区，十月二十二日星期四晚上七点二十一分发布。",
                "An old weather teletype starts the national weather service forecast for eastern Massachusetts inside route 495, including Boston, issued at 7:21 PM on Thursday, October 22."),

            new Node(320, "今晚多云，午夜前有零星阵雨，之后转晴；最低气温华氏四十度出头，西风每小时十到十五英里，阵风可达二十五英里，降雨概率百分之二十。",
                "Tonight: mostly cloudy with isolated showers until midnight, then mostly clear. Lows in the low 40s, west winds 10 to 15 miles per hour, gusts up to 25, chance of rain 20 percent."),

            new Node(652, "周五晴，最高气温华氏五十度出头，西北风每小时十到十五英里，阵风二十五英里；周五夜间基本晴朗，最低气温华氏三十度出头，北风每小时十到十五英里。",
                "Friday: sunny with highs in the low 50s, northwest winds 10 to 15 miles per hour, gusts up to 25. Friday night mostly clear, lows in the low 30s, north winds 10 to 15."),

            new Node(1000, "周六晴间多云——话音未落，字符开始失控膨胀，整段预报被撕碎。",
                "Saturday: partly sunny, and then the characters break apart and swallow the screen."),

            new Node(1080, "天气预报表退场，背景跟着节拍跳动，屏幕上开始出现制作名单。",
                "The forecast clears; blocks start pulsing on the beat and the credit lines appear."),

            new Node(1144, "动画：Unity，C 井号。",
                "Animation: Unity, C sharp."),

            new Node(1160, "游戏：RD Online。",
                "Game: RD Online."),

            new Node(1176, "开发：memsyslizi。",
                "Development: memsyslizi."),

            new Node(1192, "协助：StArray。",
                "Assistance: StArray."),

            new Node(1200, "内测：二零二六年二月一日。",
                "Open test: February 1, 2026."),

            new Node(1204, "发布：二月十五日。",
                "Release: February 15."),

            new Node(1208, "背景音乐：Frums 的 Credits。",
                "Music: Credits, by Frums."),

            new Node(1272, "运行平台：C 井号。",
                "Runtime: C sharp."),

            new Node(1336, "作者：memsyslizi 与 StArray，节拍方块开始左右分跳。",
                "Author: memsyslizi and StArray; the beat blocks start splitting left and right."),

            new Node(1400, "屏幕下方画出第 21 行的分隔横线。",
                "A separator rule is drawn on line 21."),

            new Node(1464, "横线下面出现命令行的输入提示符。",
                "A command line prompt appears below the rule."),

            new Node(1528, "终端里画出一个天气信息框。",
                "A weather panel is drawn inside the terminal."),

            new Node(1592, "框里写着日期：二零零九年十月二十二日。",
                "The panel shows the date: October 22, 2009."),

            new Node(1656, "框里再画一条分隔线，把上面的信息和下面的天气数据隔开。",
                "Another rule separates the header from the weather data below."),

            new Node(1720, "气温四十三华氏度，西风每小时十三英里。",
                "43 degrees, west wind at 13 miles per hour."),

            new Node(1784, "降水百分之二十点三。",
                "Precipitation 20.3 percent."),

            new Node(1848, "天气信息框被整个撤掉，画面转成筹款名录：赞助者的名字一行行往上滚，本节目由像你一样的观众资助这句话反复打出来。",
                "The weather panel is removed and the funding list begins: supporter names scroll upward while the line this program was made possible by viewers like you repeats."),

            new Node(2352, "字样变成广播公司，信号随即中断，屏幕写满连接已断开。",
                "The wording becomes a broadcasting corporation, the signal drops, and the screen fills with connection lost."),

            new Node(2976, "噪点把画面整个擦掉。",
                "Noise wipes the screen clean."),

            new Node(3132, "屏幕中间出现一条加载条，一格一格往前填。",
                "A loading bar appears in the middle of the screen and slowly fills."),

            new Node(3372, "加载条卡住了，进度数字乱跳，文字变成红色并抖动。",
                "The loading bar stalls, its progress numbers jump around, and the text turns red and shakes."),

            new Node(3376, "致命错误画面一闪而过：红色文字报出错误码八百零一，下面四行十六进制乱码。",
                "A fatal error frame flashes by: red text with error code 801 and four lines of hex garbage."),

            new Node(3380, "换成一条绿色的快速加载条，一下就跑到底。",
                "A green express loading bar replaces it and races to the end."),

            new Node(3390, "自动诊断失败的红色大字压满屏幕，两倍速的筹款字样在下面来回滚。",
                "Red text saying the automatic diagnosis failed covers the screen, with doubled-up funding text scrolling underneath."),

            new Node(3896, "红色文字抖动闪烁，一遍遍播报波士顿地区晚上七点的气象观测：洛根机场多云，气温华氏六十八度，露点四十七，相对湿度百分之四十六，西南风每小时十三英里，气压二十九点九九英寸并在上升。",
                "Shaking red text repeats the 7 PM Boston observations: at Logan Airport it was cloudy, 68 degrees, dew point 47, humidity 46 percent, southwest wind 13 miles per hour, pressure 29.99 inches and rising."),

            new Node(4400, "文字被清空，海面闪了几下之后转黑。",
                "The text clears; the sea flashes a few times and goes black."),

            new Node(4460, "搜索接入点画面：屏幕说没有接入点在广播，正在手动搜索，上次搜索是二零一九年二月二十七日；成排的编号反复出现又被扫掉，下方同时滚着感谢观众资助的字幕。",
                "Searching for access points: the screen says no access points are broadcasting, manual search in progress, last search February 27, 2019, while numbered blocks appear and get swept away and the viewer funding line types along the bottom."),

            new Node(4534, "屏幕下方那行感谢资助的字幕单独滚动起来。",
                "The funding thanks line scrolls on its own along the bottom."),

            new Node(5428, "找到一个接入点：P B S 十四号，状态活动。",
                "An access point is found: PBS number 14, active."),

            new Node(5500, "画面回到跳动的节拍方块，屏幕下方出现一行感谢资助的字幕。",
                "Back to the pulsing beats, with a line of funding thanks along the bottom."),

            new Node(5788, "这行感谢文字开始向下滚动。",
                "The funding line starts scrolling downward."),

            new Node(5916, "更多感谢字幕接着往下滚。",
                "More funding thanks scroll down."),

            new Node(6270, "画面切回左右分跳的节拍方块。",
                "The beats split left and right again."),

            new Node(6508, "字幕结束，音乐收尾后自动回到大厅。",
                "The credits end; the lobby loads automatically after the music."),
        };

        private global::Credits.CreditsController _credits;
        private FieldInfo _beatField;
        private int _node = -1;
        private bool _intro;
        private bool _leaving;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            global::Credits.CreditsController credits =
                Object.FindObjectOfType<global::Credits.CreditsController>();
            if (credits == null) return;

            if (credits.GetComponent<CreditsA11y>() != null) return;
            credits.gameObject.AddComponent<CreditsA11y>();
        }

        private void Awake()
        {
            _credits = GetComponent<global::Credits.CreditsController>();
        }

        private void Update()
        {
            if (_leaving) return;

            if (!_intro)
            {
                _intro = true;
                SetInstructions();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Leave();
                return;
            }

            // 玩家自己关掉了字幕朗读，就什么都不念，画面照旧走。
            if (!CanSpeak()) return;

            int beat = CurrentBeat();
            if (beat < 0) return;

            int index = IndexFor(beat);
            if (index == _node) return;

            _node = index;
            Say(A11yPhrases.Text(Nodes[index].Zh, Nodes[index].En));
        }

        /// <summary>
        /// 当前节拍，对齐 CreditsTimeline 的编号。
        /// 控制器内部的计数器从 -60 起算，时间线从 0 起算，差一个固定偏移；
        /// 跳段时两者同步加，所以这个偏移始终成立。
        /// </summary>
        private int CurrentBeat()
        {
            if (_credits == null) return -1;

            if (_beatField == null)
            {
                _beatField = typeof(global::Credits.CreditsController).GetField(
                    "_beat", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
            if (_beatField == null) return -1;

            object raw = _beatField.GetValue(_credits);
            return raw is int ? (int)raw + BeatOffset : -1;
        }

        /// <summary>控制器从 -60 起算，时间线从 0 起算。</summary>
        private const int BeatOffset = 60;

        /// <summary>最大不超过当前节拍的那个节点；开播前算第一段。</summary>
        private static int IndexFor(int beat)
        {
            for (int i = Nodes.Length - 1; i >= 0; i--)
            {
                if (beat >= Nodes[i].Beat) return i;
            }
            return 0;
        }

        /// <summary>
        /// 是否该开口。两个条件都必须是玩家自己打开的：
        /// 无障碍总开关，以及字幕朗读分类。
        /// </summary>
        private static bool CanSpeak()
        {
            if (FindNarration() == null) return false;
            if (!Narration.IsEnabled) return false;
            return Narration.CheckCategoryEnabled(Category);
        }

        /// <summary>
        /// 开场说明同样只写进 Narration 的说明位，由它决定要不要念。
        /// </summary>
        private void SetInstructions()
        {
            if (FindNarration() == null) return;
            if (!Narration.IsEnabled) return;

            Narration.SetInstructions(A11yPhrases.Text(
                "制作人员字幕。屏幕上是一段逐格推进的字符动画，画面换内容时会说明这段在演什么。" +
                "P 键暂停或继续，逗号、句号、斜杠连按可以加速。按 Esc 返回大厅。",
                "Credits. The screen is a step by step character animation; each change is described as it happens. " +
                "P pauses, and comma, period or slash speeds it up. Escape returns to the lobby."));
        }

        /// <summary>
        /// 立即朗读，不排队。这一段是连着的，排队会让听到的永远比画面晚一截；
        /// 念不完就让它重来，也不能落后于画面。
        /// </summary>
        private static void Say(string speech)
        {
            if (string.IsNullOrEmpty(speech)) return;
            if (!CanSpeak()) return;

            Narration.Say(speech, Category, true);
        }

        private static Narration _narration;

        private static Narration FindNarration()
        {
            if (_narration == null) _narration = Object.FindObjectOfType<Narration>();
            return _narration;
        }

        private void Leave()
        {
            _leaving = true;
            Say(A11yPhrases.Text("正在返回大厅", "Returning to the lobby"));
            StartCoroutine(Go(SceneAfterCredits()));
        }

        /// <summary>等一帧再切场景，别在键盘处理里立刻卸载自己的场景。</summary>
        private IEnumerator Go(string scene)
        {
            yield return null;
            SceneManager.LoadScene(scene);
        }

        private string SceneAfterCredits()
        {
            if (_credits != null)
            {
                FieldInfo field = typeof(global::Credits.CreditsController).GetField(
                    "sceneAfterCredits", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)
                {
                    string value = field.GetValue(_credits) as string;
                    if (!string.IsNullOrEmpty(value)) return value;
                }
            }

            return "ScnLobby";
        }
    }
}
