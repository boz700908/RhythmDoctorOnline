using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Static helpers for credit animations. Port of animation_functions.py.
    /// </summary>
    public static class AnimationFunctions
    {
        private static readonly string[] FuckChars = { ".", ".", ".", " ", " ", "`", "=", "/", "?", "-", "$", "%" };
        private static readonly string[] WindDirs = { "N ", "NE", "E ", "SE", "S ", "SW", "W ", "NW" };

        public static string GenerateRandomHex(int length)
        {
            int max = 1;
            for (int i = 0; i < length; i++) max *= 16;
            return UnityEngine.Random.Range(0, max).ToString("x" + length);
        }

        public static string ReplaceTextWithSpaces(string s, int chance)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                sb.Append(UnityEngine.Random.Range(0, 100) < chance ? ' ' : c);
            return sb.ToString();
        }

        public static void RenderWeather(CreditsCanvas c, int layer, int x, int y, Weather weather, int mutationsAfter = 0, int spcChance = 0)
        {
            if (weather.Precip >= 0)
                c.SetString(layer, new Vector2Int(x + 5, y - 2), ReplaceTextWithSpaces(weather.WeatherName.PadLeft(14), spcChance), CanvasColor.Yellow);
            else
                c.SetString(layer, new Vector2Int(x, y - 2), ReplaceTextWithSpaces(weather.WeatherName.PadLeft(14), spcChance), CanvasColor.BrightRed);

            int windIdx = (weather.WindDir >= 0 && weather.WindDir < WindDirs.Length) ? weather.WindDir : 0;
            CanvasColor tempCol = TempColor(weather.Temp);
            c.SetString(layer, new Vector2Int(x, y), ReplaceTextWithSpaces($"{Mathf.RoundToInt(weather.Temp):D2}°F  ", spcChance), tempCol);
            c.SetString(layer, new Vector2Int(x + 5, y), ReplaceTextWithSpaces($"Wind {Mathf.RoundToInt(weather.Wind):D2} mph {WindDirs[windIdx]}", spcChance), CanvasColor.BrightCyan);
            c.SetString(layer, new Vector2Int(x + 5, y + 2), ReplaceTextWithSpaces("Precipitation ", spcChance), CanvasColor.BrightBlue);
            c.SetString(layer, new Vector2Int(x + 5, y + 3), ReplaceTextWithSpaces($"{ (weather.Precip * 100):F2}% ".PadLeft(13), spcChance), CanvasColor.BrightBlue);
            if (weather.Precip >= 0 && weather.WindDir >= 0)
                weather.Mutate(mutationsAfter);
        }

        private static CanvasColor TempColor(float temp)
        {
            int t = Mathf.FloorToInt(temp);
            if (t < 23) return CanvasColor.White;
            if (t < 46) return CanvasColor.Cyan;
            if (t < 69) return CanvasColor.Yellow;
            return CanvasColor.Red;
        }

        public static void Noise(CreditsCanvas c, int layer, int amount, string[] chars, CanvasColor[] colours)
        {
            for (int n = 0; n < amount; n++)
            {
                int lx = UnityEngine.Random.Range(0, CreditsCanvas.LogicalWidth);
                int ly = UnityEngine.Random.Range(0, CreditsCanvas.Height);
                string ch = chars != null && chars.Length > 0 ? chars[UnityEngine.Random.Range(0, chars.Length)] : "  ";
                CanvasColor col = colours != null && colours.Length > 0 ? colours[UnityEngine.Random.Range(0, colours.Length)] : CanvasColor.White;
                c.SetChar(layer, new Vector2Int(lx, ly), ch.Length >= 2 ? ch.Substring(0, 2) : ch + " ", col);
            }
        }

        public static void SetMultilineString(CreditsCanvas c, int layer, int x, int y, string str, CanvasColor col)
        {
            if (string.IsNullOrEmpty(str)) return;
            int offset = 0;
            foreach (string line in str.Split('\n'))
            {
                c.SetString(layer, new Vector2Int(x, y + offset), line, col);
                offset++;
            }
        }

        public static void TypeText(CreditsCanvas c, Generator g, int layer, int x, int y, CanvasColor col, bool render = true)
        {
            string textGet = g.GetData<string>("text");
            int offset = g.GetData("offset", 0);
            int totalChars = 0;
            int addOffset = 0;
            if (string.IsNullOrEmpty(textGet)) return;

            if (textGet.StartsWith("[##CLEAR|"))
            {
                var parts = textGet.Split('|')[1].Split(';');
                int clearW = int.Parse(parts[0]);
                int clearH = int.Parse(parts[1]);
                for (int yclear = 0; yclear < clearH && render; yclear++)
                    c.SetString(layer, new Vector2Int(x, y + yclear), new string(' ', clearW), col);
                return;
            }

            var lines = textGet.Split('\n');
            for (int linecount = 0; linecount < lines.Length; linecount++)
            {
                string textLine = lines[linecount];
                int localOffset = offset - totalChars;
                if (localOffset >= 0 && localOffset < textLine.Length + (linecount < lines.Length - 1 ? 1 : 0))
                {
                    bool placeTyper = localOffset < textLine.Length - 1;
                    string display = textLine.Length > 0 ? textLine.Substring(0, Mathf.Min(localOffset, textLine.Length)) : "";
                    if (placeTyper) display += "_";
                    if (localOffset < textLine.Length && textLine[localOffset] == '@') addOffset += 3;
                    display = display.Replace("~", "").Replace("@", "");
                    if (render)
                        c.SetString(layer, new Vector2Int(x, y + linecount), display, col);
                    g.OperData("offset", _ => offset + 1 + addOffset);
                    return;
                }
                totalChars += textLine.Length;
            }
        }

        public static string FuckUpText(string s, int chance, string alsoIgnore = "")
        {
            var sb = new StringBuilder(s.Length);
            string ignore = "@~\n" + alsoIgnore;
            foreach (char ch in s)
            {
                if (ch == '\n') sb.Append(ch);
                else if (UnityEngine.Random.Range(1, 1001) < chance && !ignore.Contains(ch.ToString()))
                    sb.Append(FuckChars[UnityEngine.Random.Range(0, FuckChars.Length)]);
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        public static void Clear(CreditsCanvas c, int layer) => c.ClearLayer(layer);

        public static void BeatToggle(CreditsCanvas c, Generator g, int layer, int x, int x2, int y, int y2, string charStr, CanvasColor col)
        {
            bool tog = g.GetData("beat_toggle", true);
            string chars = tog ? (charStr?.Length >= 2 ? charStr.Substring(0, 2) : "  ") : "..";
            int xDiff = x2 - x;
            string line = string.Concat(System.Linq.Enumerable.Repeat(chars, xDiff));
            for (int yn = y; yn < y2; yn++)
                c.SetString(layer, new Vector2Int(x, yn), line, col);
            g.SetData("beat_toggle", !tog);
        }

        public static string WorkOutDate(int b, int dayOffset = 0)
        {
            int[] lengths = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int daysNeeded = (b / 64) + dayOffset;
            int monthLoc = 9;
            int dayLoc = 22;
            int year = 2009;
            while (daysNeeded > 0)
            {
                int leapYear = (year % 4 == 0 && monthLoc == 1) ? 1 : 0;
                int untilEndMonth = lengths[monthLoc] + 1 - dayLoc + leapYear;
                dayLoc += Mathf.Min(daysNeeded, untilEndMonth);
                daysNeeded -= Mathf.Min(daysNeeded, untilEndMonth);
                if (dayLoc > lengths[monthLoc] + leapYear)
                {
                    monthLoc++;
                    dayLoc = 1;
                    if (monthLoc >= 12) { monthLoc = 0; year++; }
                }
            }
            return $"{dayLoc:D2}.{monthLoc + 1:D2}.{year:D4}";
        }

        public static string[][] SplitWordTemplate(string str)
        {
            var result = new List<string[]>();
            foreach (string sp in str.Split('\n'))
                result.Add(sp.Split('#'));
            return result.ToArray();
        }

        public static void TypewriteByWord(CreditsCanvas c, Generator g, int layer, int x, int y, CanvasColor col, bool render = true, string historyVar = "history")
        {
            var textGet = g.GetData<string[][]>("text");
            int offset = g.GetData("offset", 0);
            int lineno = g.GetData("lineno", 0);
            if (textGet == null || lineno >= textGet.Length) return;

            var lineGet = textGet[lineno];
            if (lineGet == null || lineGet.Length == 0) { g.OperData("offset", o => (int)o + 1); return; }

            var lineTotalBuilder = new StringBuilder();
            for (int i = 0; i < offset && i < lineGet.Length; i++)
                if (lineGet[i] != null) lineTotalBuilder.Append(lineGet[i]);
            string lineTotal = lineTotalBuilder.ToString();

            if (lineGet[0] != null && lineGet[0].StartsWith("[~~CLEAR|"))
            {
                int clearW = int.Parse(lineGet[0].Split('|')[1]);
                if (render) c.SetString(layer, new Vector2Int(x, y), new string(' ', clearW), col);
                return;
            }

            if (render)
            {
                if (!string.IsNullOrEmpty(lineTotal))
                    c.SetString(layer, new Vector2Int(x, y), lineTotal.Replace("~", ""), col);
                else
                    c.SetString(layer, new Vector2Int(x, y), new string(' ', 60), col);
            }

            if (offset >= lineGet.Length)
            {
                var history = g.Parent?.GetData<List<(string, CanvasColor)>>(historyVar);
                bool isFluff = lineTotal.StartsWith(" ") || string.IsNullOrEmpty(lineTotal);
                bool isImportant = lineTotal.EndsWith("~");
                CanvasColor colourSelect = CanvasColor.Yellow;
                string prefix = "- ";
                if (isFluff) { prefix = "  "; colourSelect = CanvasColor.BrightBlack; }
                else if (isImportant) { prefix = "> "; colourSelect = CanvasColor.Green; }

                string entry = prefix + lineTotal.Trim().Replace("~", "");
                if (history == null)
                    g.Parent?.SetData(historyVar, new List<(string, CanvasColor)> { (entry, colourSelect) });
                else
                    history.Add((entry, colourSelect));
                g.Parent?.SetData("refresh", true);
                g.SetData("offset", 0);
                g.SetData("lineno", lineno + 1);
            }
            else
                g.SetData("offset", offset + 1);
        }

        public static void WriteHistory(CreditsCanvas c, Generator g, int layer, int x, int y, CanvasColor col, int stop, string var = "history")
        {
            var history = g.Parent?.GetData<List<(string, CanvasColor)>>(var);
            bool needRefresh = g.Parent != null && (bool)(g.Parent.GetData("refresh") ?? false);
            if (!needRefresh) return;

            g.Parent?.SetData("refresh", false);
            if (history != null && history.Count > 0)
            {
                int lineid = 0;
                for (int ypos = y; ypos > stop - 1; ypos--)
                {
                    var line = lineid < history.Count ? history[history.Count - 1 - lineid] : ("", CanvasColor.BrightBlack);
                    c.SetString(layer, new Vector2Int(x, ypos), line.Item1.PadRight(50).Substring(0, 50), line.Item2);
                    lineid++;
                }
            }
            else
            {
                for (int ypos = y; ypos > stop - 1; ypos--)
                    c.SetString(layer, new Vector2Int(x, ypos), new string(' ', 50), CanvasColor.Black);
            }
        }

        public static void ShowAccessPointVisual(CreditsCanvas c, Generator g, int layer, int x, int y)
        {
            int counter = g.GetData("counter", 0);
            int blockCounter = g.GetData("block", 0);
            int locationX = 5 * (blockCounter % 6) + x;
            int locationY = 4 * (blockCounter / 6) + y;

            if (counter < 8)
            {
                SetMultilineString(c, layer, locationX, locationY,
                    $"  ###  \nPBS {(blockCounter + 1):D2}\nPing  {counter + 1}", CanvasColor.Yellow);
                g.OperData("counter", o => (int)o + 1);
            }
            else
            {
                SetMultilineString(c, layer, locationX, locationY,
                    $"  ...  \nPBS {(blockCounter + 1):D2}\n-------", CanvasColor.BrightBlack);
                int nx = 5 * ((blockCounter + 1) % 6) + x;
                int ny = 4 * ((blockCounter + 1) / 6) + y;
                SetMultilineString(c, layer, nx, ny, $"  ###  \nPBS {(blockCounter + 2):D2}\nPing  1", CanvasColor.Yellow);
                g.SetData("counter", 1);
                g.OperData("block", o => (int)o + 1);
            }
        }

        public static void MakePoweroffBars(CreditsCanvas c, int b, int layer, CanvasColor col)
        {
            int height = Mathf.Max(0, Mathf.FloorToInt(24f / Mathf.Pow(b, 1.3f)));
            int locY = 12 - (height / 2);
            string line = new string('#', 80);
            for (int h = 0; h < height; h++)
                c.SetString(layer, new Vector2Int(0, locY + h), line, col);
        }
    }
}
