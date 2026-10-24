using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RDOnline.ScnLobby
{
    /// <summary>
    /// 一个谱面来源条目
    /// </summary>
    public class ChartSourceEntry
    {
        /// <summary>官方关卡：关卡 enum 名。玩家关卡库：主谱面文件路径。</summary>
        public string Id;
        /// <summary>显示名（官方走游戏本地化词条，关卡库走谱面 song 字段）</summary>
        public string Display;
        /// <summary>附加说明（作者 / 目录名）</summary>
        public string Detail;
        /// <summary>是否官方谱面</summary>
        public bool IsOfficial;
        /// <summary>关卡库条目的关卡目录</summary>
        public string Directory;

        public override string ToString()
        {
            return string.IsNullOrEmpty(Detail) ? Display : Display + "，" + Detail;
        }
    }

    /// <summary>
    /// 谱面来源。房间的 chartUrl 只有一个字符串字段，这里把两种来源编码进去：
    ///   官方谱面   -> "internal://关卡名"（关卡名即 Level 枚举名，如 OrientalTechno）
    ///   玩家关卡库 -> 真实的上传 URL（http/https），其它玩家下载解压后使用
    /// 官方谱面的关卡与音频都在游戏本体里，各端必然一致，所以只传标识不传文件。
    /// </summary>
    public static class ChartSource
    {
        public const string InternalScheme = "internal://";

        /// <summary>
        /// 是否是官方谱面标识
        /// </summary>
        public static bool IsInternal(string chartUrl)
        {
            return !string.IsNullOrEmpty(chartUrl)
                && chartUrl.StartsWith(InternalScheme, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 由官方关卡 enum 名构造标识
        /// </summary>
        public static string Internal(string levelName)
        {
            return InternalScheme + (levelName ?? string.Empty).Trim();
        }

        /// <summary>
        /// 从标识里取出官方关卡名（非官方谱面返回 null）
        /// </summary>
        public static string GetLevelName(string chartUrl)
        {
            if (!IsInternal(chartUrl)) return null;
            return chartUrl.Substring(InternalScheme.Length).Trim();
        }

        /// <summary>
        /// 官方关卡 enum 名是否真实存在，避免把拼错的关卡名送进 scnGame
        /// </summary>
        public static bool IsValidLevelName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return false;
            try
            {
                return Enum.TryParse(typeof(Level), levelName, false, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 官方关卡的显示名，沿用游戏自己的本地化词条 levelSelect.&lt;关卡名&gt;。
        /// 取不到时回退成关卡名本身，绝不新造语言。
        /// </summary>
        public static string GetLevelDisplayName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return string.Empty;
            try
            {
                string text = RDString.Get("levelSelect." + levelName);
                if (!string.IsNullOrEmpty(text) && text != "levelSelect." + levelName)
                    return text;
            }
            catch
            {
                // RDString 在极早期可能未初始化，忽略即可
            }
            return levelName;
        }

        /// <summary>
        /// 界面上要显示的谱面名。chartUrl 是官方标识时，一律用本端语言的关卡名，
        /// 不用远端传来的 chartName —— 否则房主用中文客户端，其它语言玩家会看到中文。
        /// 非官方谱面没有本地化词条，才回退到远端给的名称。
        /// </summary>
        public static string GetDisplayName(string chartUrl, string chartName)
        {
            string levelName = GetLevelName(chartUrl);
            if (!string.IsNullOrEmpty(levelName))
            {
                string localized = GetLevelDisplayName(levelName);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }
            return chartName;
        }

        /// <summary>
        /// 官方关卡列表。排除非关卡项（None、Cutscene_*、OST、Trailer 之类）。
        /// </summary>
        public static List<ChartSourceEntry> GetOfficialLevels()
        {
            var list = new List<ChartSourceEntry>();
            foreach (string name in Enum.GetNames(typeof(Level)))
            {
                if (name == "None") continue;
                if (name.StartsWith("Cutscene", StringComparison.Ordinal)) continue;
                if (name == "OST" || name == "Trailer") continue;
                list.Add(new ChartSourceEntry
                {
                    Id = name,
                    Display = GetLevelDisplayName(name),
                    Detail = null,
                    IsOfficial = true
                });
            }
            return list;
        }

        /// <summary>
        /// 玩家关卡库：游戏自己的 Levels 目录下每个关卡文件夹算一条。
        /// 主谱面优先 main.rdlevel，取不到就退回文件夹内第一个 .rdlevel。
        /// </summary>
        public static List<ChartSourceEntry> GetLibraryLevels()
        {
            var list = new List<ChartSourceEntry>();
            string root = null;
            try
            {
                root = LevelValidation.CustomLevelsPath;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[ChartSource] 无法获取关卡库路径: " + e.Message);
                return list;
            }

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return list;

            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(root);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[ChartSource] 读取关卡库目录失败: " + e.Message);
                return list;
            }

            foreach (string dir in dirs)
            {
                string main = FindMainRdlevel(dir);
                if (string.IsNullOrEmpty(main)) continue;

                string song = null;
                string author = null;
                try
                {
                    // 只读开头一段，足够解析出 song/author，避免大文件整读
                    using (var reader = new StreamReader(main))
                    {
                        char[] buffer = new char[8192];
                        int read = reader.Read(buffer, 0, buffer.Length);
                        string head = new string(buffer, 0, read);
                        song = ExtractField(head, "song");
                        author = ExtractField(head, "author");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("[ChartSource] 读取谱面信息失败: " + e.Message);
                }

                string folderName = Path.GetFileName(dir);
                list.Add(new ChartSourceEntry
                {
                    Id = main,
                    Display = string.IsNullOrEmpty(song) ? folderName : StripHtml(song),
                    Detail = string.IsNullOrEmpty(author) ? folderName : StripHtml(author),
                    IsOfficial = false,
                    Directory = dir
                });
            }

            list.Sort((a, b) => string.Compare(a.Display, b.Display, StringComparison.CurrentCulture));
            return list;
        }

        /// <summary>
        /// 在关卡目录里找主谱面
        /// </summary>
        public static string FindMainRdlevel(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            string main = Path.Combine(dir, LevelValidation.MainFilename);
            if (File.Exists(main)) return main;

            string backup = Path.Combine(dir, LevelValidation.BackupFilename);
            if (File.Exists(backup)) return backup;

            try
            {
                string[] files = Directory.GetFiles(dir, "*.rdlevel", SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                    return files[0];
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[ChartSource] 查找谱面失败: " + e.Message);
            }
            return null;
        }

        private static string ExtractField(string content, string fieldName)
        {
            if (string.IsNullOrEmpty(content)) return null;
            Match m = Regex.Match(content, "\"" + fieldName + "\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string StripHtml(string text)
        {
            return string.IsNullOrEmpty(text) ? text : Regex.Replace(text, "<.*?>", "");
        }
    }
}
