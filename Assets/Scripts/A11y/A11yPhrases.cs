namespace RDOnline.A11y
{
    /// <summary>
    /// 朗读用的连接词。集中在这里，方便日后整句替换为游戏自带词条，
    /// 控件本身的文字一律直接读控件，不从这里取。
    /// </summary>
    internal static class A11yPhrases
    {
        public const string Separator = "，";
        public const string Filled = "，已填写";
        public const string Unavailable = "，不可用";
        /// <summary>同一条目里并列的几项信息（歌名、作者、难度、BPM），朗读时略作停顿。</summary>
        public const string ItemSeparator = "；";
        public const string Editing = "正在编辑，按回车结束编辑";
        public const string CannotGoBack = "无法返回";
        public const string On = "开";
        public const string Off = "关";

        /// <summary>
        /// 跟随游戏语言取一条文案。界面上没有现成文字可读时（组名、动作名）
        /// 用这里，中文和英文各给一份，与主菜单入口的做法一致。
        /// </summary>
        public static string Text(string zh, string en)
        {
            bool chinese = true;
            try { chinese = RDString.isChinese; } catch { }
            return chinese ? zh : en;
        }
    }
}
