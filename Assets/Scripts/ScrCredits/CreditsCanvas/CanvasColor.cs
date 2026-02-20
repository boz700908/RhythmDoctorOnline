using UnityEngine;

namespace Credits
{
    /// <summary>
    /// Terminal-style color (Fore + Style). Maps to Unity Color for TMP. Port of colorama Fore/Style.
    /// </summary>
    public struct CanvasColor
    {
        public Color Color;
        public bool Bright;

        public CanvasColor(Color color, bool bright = false)
        {
            Color = color;
            Bright = bright;
        }

        public string ToTmpRichText()
        {
            Color c = Bright ? Color.Lerp(Color.white, Color, 0.85f) : Color;
            return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>";
        }

        /// <summary>转为 Unity 颜色，供 IMGUI 等使用。</summary>
        public Color ToUnityColor()
        {
            return Bright ? Color.Lerp(Color.white, Color, 0.85f) : Color;
        }

        private static readonly Color Dim = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color BrightMult = new Color(0.85f, 0.85f, 0.85f);

        public static CanvasColor Black => new CanvasColor(new Color(0.2f, 0.2f, 0.2f), false);
        public static CanvasColor BrightBlack => new CanvasColor(new Color(0.4f, 0.4f, 0.4f), true);
        public static CanvasColor Red => new CanvasColor(new Color(0.8f, 0.2f, 0.2f), false);
        public static CanvasColor BrightRed => new CanvasColor(new Color(1f, 0.3f, 0.3f), true);
        public static CanvasColor Green => new CanvasColor(new Color(0.2f, 0.7f, 0.2f), false);
        public static CanvasColor BrightGreen => new CanvasColor(new Color(0.3f, 1f, 0.3f), true);
        public static CanvasColor Yellow => new CanvasColor(new Color(0.8f, 0.8f, 0.2f), false);
        public static CanvasColor BrightYellow => new CanvasColor(new Color(1f, 1f, 0.4f), true);
        public static CanvasColor Blue => new CanvasColor(new Color(0.2f, 0.3f, 0.9f), false);
        public static CanvasColor BrightBlue => new CanvasColor(new Color(0.4f, 0.5f, 1f), true);
        public static CanvasColor Magenta => new CanvasColor(new Color(0.8f, 0.2f, 0.8f), false);
        public static CanvasColor BrightMagenta => new CanvasColor(new Color(1f, 0.4f, 1f), true);
        public static CanvasColor Cyan => new CanvasColor(new Color(0.2f, 0.7f, 0.8f), false);
        public static CanvasColor BrightCyan => new CanvasColor(new Color(0.4f, 1f, 1f), true);
        public static CanvasColor White => new CanvasColor(new Color(0.85f, 0.85f, 0.85f), false);
        public static CanvasColor BrightWhite => new CanvasColor(new Color(1f, 1f, 1f), true);

        public static CanvasColor Normal(CanvasColor c) => new CanvasColor(c.Color, false);
        public static CanvasColor AsBright(CanvasColor c) => new CanvasColor(c.Color, true);
    }
}
