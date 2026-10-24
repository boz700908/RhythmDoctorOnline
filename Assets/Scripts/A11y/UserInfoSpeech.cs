using System.Collections.Generic;
using TMPro;
using RDOnline.Auth;

namespace RDOnline.A11y
{
    /// <summary>
    /// 用户信息（头像旁那一行）转成朗读文本。
    ///
    /// 大厅和房间各挂一份 UserInfoUI，所以读法写在这里，两边共用。
    /// 屏幕上三样东西：用户名、延迟、在线人数。延迟每几秒变一次，
    /// 自动念会把旁白堵死，所以只有在用户把焦点移到「延迟」这一项时才读；
    /// 在线人数变化慢且有意义，变化时直接读一句。
    /// </summary>
    public static class UserInfoSpeech
    {
        public static string UsernameName => A11yPhrases.Text("用户名", "User name");
        public static string PingName => A11yPhrases.Text("网络延迟", "Ping");
        public static string OnlineName => A11yPhrases.Text("在线人数", "Online");

        /// <summary>用户名。控件文本里可能带富文本标签，念之前去掉。</summary>
        public static string Username(UserInfoUI ui)
        {
            string text = ui != null && ui.UsernameText != null ? ui.UsernameText.text : null;
            text = PlainText(text);

            return string.IsNullOrEmpty(text)
                ? UsernameName + A11yPhrases.Separator + A11yPhrases.Text("未知", "unknown")
                : UsernameName + A11yPhrases.Separator + text;
        }

        /// <summary>
        /// 延迟。控件文字是「45ms」，中文旁白读字母缩写不顺，所以取出数字说毫秒。
        /// </summary>
        public static string Ping(UserInfoUI ui)
        {
            string raw = ui != null && ui.PingText != null ? ui.PingText.text : null;

            int ms = Number(raw);
            if (ms > 0) return PingName + A11yPhrases.Separator + ms + A11yPhrases.Text(" 毫秒", " milliseconds");

            // 还没收到第一次测量结果
            return PingName + A11yPhrases.Separator +
                (string.IsNullOrEmpty(raw)
                    ? A11yPhrases.Text("测量中", "measuring")
                    : PlainText(raw));
        }

        /// <summary>
        /// 在线人数。控件文字是「12在线」。
        /// 还没统计出来时也要占住这一栏，只说正在统计，免得这一项时有时无。
        /// </summary>
        public static string Online(UserInfoUI ui)
        {
            return OnlineOrNull(ui) ?? OnlineName + A11yPhrases.Separator +
                A11yPhrases.Text("正在统计", "counting");
        }

        /// <summary>真拿到数字或文字才算数，给「变化时才读」用。</summary>
        private static string OnlineOrNull(UserInfoUI ui)
        {
            string raw = ui != null && ui.OnlineCountText != null ? ui.OnlineCountText.text : null;
            if (string.IsNullOrEmpty(raw)) return null;

            int n = Number(raw);
            if (n > 0) return OnlineName + A11yPhrases.Separator + A11yPhrases.Text(n + " 人", n + " players");

            string text = PlainText(raw);
            return string.IsNullOrEmpty(text) ? null : OnlineName + A11yPhrases.Separator + text;
        }

        /// <summary>
        /// 在线人数变了就返回新的一句话，没变返回 null。调用方拿它决定要不要直读。
        /// </summary>
        public static string WatchOnline(UserInfoUI ui, ref string last)
        {
            string now = OnlineOrNull(ui);
            if (now == null) return null;
            if (now == last) return null;
            last = now;
            return now;
        }

        /// <summary>取一段文字里第一串数字。取值失败给 0。</summary>
        private static int Number(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            int sum = 0;
            bool any = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9')
                {
                    any = true;
                    sum = sum * 10 + (c - '0');
                    if (sum > 99999) return 99999;
                }
                else if (any)
                {
                    break;
                }
            }

            return any ? sum : 0;
        }

        /// <summary>去掉富文本标签，免得旁白念出 color、b 之类的内容。</summary>
        public static string PlainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string result = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", string.Empty);
            return result.Trim();
        }

        /// <summary>
        /// 把三样东西各做成一个只念不触发的项：用户上下光标能逐条听到，
        /// 但它们不抢焦点以外的东西，也不占额外位置。
        /// </summary>
        public static void AddItems(List<A11yNavItem> into, UserInfoUI ui)
        {
            if (ui == null) return;

            into.Add(Item(ui, ui.UsernameText, UsernameName, () => Username(ui)));
            into.Add(Item(ui, ui.PingText, PingName, () => Ping(ui)));
            into.Add(Item(ui, ui.OnlineCountText, OnlineName, () => Online(ui)));
        }

        private static A11yNavItem Item(UserInfoUI ui, TMP_Text text, string name,
            System.Func<string> speech)
        {
            UnityEngine.GameObject root = text != null ? text.gameObject : ui.gameObject;
            return A11yNavItem.Custom(root, A11yItemKind.Label, name, speech, null);
        }
    }
}
