using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Ocean wave character grid. Port of ocean.py (the-sea.js).
    /// Rendered as 10 lines; each line is a string that scrolls left, new slice appended right.
    /// </summary>
    public static class OceanLogic
    {
        private static int _oceanTime;
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

        /// <summary>贡献者/开发者名字彩蛋，穿插在波浪中逐字出现。</summary>
        private static readonly string[] _contributorNames = new[]
        {
            "Nexus", "jlj1102", "ReinaHikari", "是若夌呀", "堂祚", "7unSgELE", "liseeliot",
            "\u30d6\u30eb\u30fc\u30a8\u30c3\u30c1\u30f3\u30b0", "\u9b54mo", "\u665a\u98ce~", "\u7965\u6faa", "\u7c89\u7ea2\u732b",
            "memsyslizi", "StArray", "无光亦无影（music）"
        };
        private static int _injectLine = -1;
        private static int _injectNameIndex;
        private static int _injectCharIndex = -1;

        public static List<StringBuilder> BeginOcean()
        {
            _oceanTime = UnityEngine.Random.Range(0, 2000);
            return GetOceanSlices(_oceanTime, _oceanTime + 80);
        }

        private static List<StringBuilder> GetOceanSlices(int x1, int x2)
        {
            var contList = new List<StringBuilder>();
            for (int i = 0; i < 10; i++)
                contList.Add(new StringBuilder());

            for (int xr = x1; xr < x2; xr++)
            {
                float x = xr / 5f;
                float c = Mathf.Cos(0.2f * x) + Mathf.Sin(0.3f * x) * Mathf.Sin(0.23f * x);
                int y = -Mathf.FloorToInt(2 * c * Mathf.Sin(x)) + 3;

                for (int i = 0; i < 10; i++)
                {
                    if (UnityEngine.Random.value <= 0.002f)
                        contList[i].Append(Alphabet[UnityEngine.Random.Range(0, 26)]);
                    else if (i == y)
                        contList[i].Append('#');
                    else if (i > y)
                        contList[i].Append('.');
                    else
                        contList[i].Append(' ');
                }
            }
            return contList;
        }

        private static void GetOceanSlice(int xr, int glitch, char[] outSlice)
        {
            float x = xr / 5f;
            float c = Mathf.Cos(0.2f * x) + Mathf.Sin(0.3f * x) * Mathf.Sin(0.23f * x);
            int y = -Mathf.FloorToInt(2 * c * Mathf.Sin(x)) + 3;

            // 若正在穿插名字彩蛋，这一列在对应行输出名字的下一字
            if (_injectCharIndex >= 0 && _injectNameIndex < _contributorNames.Length)
            {
                string name = _contributorNames[_injectNameIndex];
                if (_injectCharIndex < name.Length)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i == _injectLine)
                            outSlice[i] = name[_injectCharIndex];
                        else if (UnityEngine.Random.value <= 0.002f * glitch)
                            outSlice[i] = Alphabet[UnityEngine.Random.Range(0, 26)];
                        else if (i == y)
                            outSlice[i] = '#';
                        else if (i > y)
                            outSlice[i] = '.';
                        else
                            outSlice[i] = ' ';
                    }
                    _injectCharIndex++;
                    if (_injectCharIndex >= name.Length)
                        _injectCharIndex = -1;
                    return;
                }
                _injectCharIndex = -1;
            }
            else if (_injectCharIndex < 0 && UnityEngine.Random.value <= 0.8f)
            {
                // 小概率开始穿插一个名字：随机选一行、一个名字
                _injectLine = UnityEngine.Random.Range(0, 10);
                _injectNameIndex = UnityEngine.Random.Range(0, _contributorNames.Length);
                _injectCharIndex = 0;
                GetOceanSlice(xr, glitch, outSlice);
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                if (UnityEngine.Random.value <= 0.002f * glitch)
                    outSlice[i] = Alphabet[UnityEngine.Random.Range(0, 26)];
                else if (i == y)
                    outSlice[i] = '#';
                else if (i > y)
                    outSlice[i] = '.';
                else
                    outSlice[i] = ' ';
            }
        }

        private static string MutateText(string txt, int glitch)
        {
            var sb = new StringBuilder(txt.Length);
            float glitchFactor = 0.0002f + ((_oceanTime % 1200) / 1200000f) * glitch;
            for (int i = 0; i < txt.Length; i++)
            {
                char ch = txt[i];
                if ((ch == '#' || ch == '.' || ch == ' ') && UnityEngine.Random.value <= glitchFactor)
                    sb.Append(Alphabet[UnityEngine.Random.Range(0, 26)]);
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Updates ocean in place: shift left, add new slice. Returns full text with newlines for 10 lines.
        /// </summary>
        public static string UpdateOceanSlices(List<StringBuilder> oceanContent, int oceanGlitch)
        {
            var slice = new char[10];
            GetOceanSlice(_oceanTime, oceanGlitch, slice);
            _oceanTime++;

            for (int i = 0; i < 10; i++)
            {
                if (oceanContent[i].Length > 0)
                    oceanContent[i].Remove(0, 1);
                oceanContent[i].Append(slice[i]);
            }

            var raw = new StringBuilder();
            foreach (var line in oceanContent)
                raw.Append(line.ToString());
            return MutateText(raw.ToString(), oceanGlitch);
        }
    }
}
