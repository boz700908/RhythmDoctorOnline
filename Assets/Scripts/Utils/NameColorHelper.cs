using System.Collections;
using UnityEngine;
using TMPro;

namespace RDOnline.Utils
{
    /// <summary>
    /// 根据 name_color 对 TMP_Text 应用颜色：空/null 白色，"rainbow" 彩虹渐变（LeftToRight，tiling 0.01），否则解析 hex 颜色。
    /// </summary>
    public static class NameColorHelper
    {
        public static void ApplyNameColor(TMP_Text text, string nameColor)
        {
            if (text == null) return;

            bool isRainbow = !string.IsNullOrEmpty(nameColor) &&
                string.Equals(nameColor.Trim(), "rainbow", System.StringComparison.OrdinalIgnoreCase);

            var controller = text.GetComponent<TMPRainbowController>();
            if (isRainbow)
            {
                if (controller == null)
                    controller = text.gameObject.AddComponent<TMPRainbowController>();
                controller.direction = TMPRainbowController.Direction.LeftToRight;
                controller.tiling = 0.01f;
                controller.useRainbow = true;
                text.color = Color.white;
            }
            else
            {
                if (controller != null)
                {
                    controller.useRainbow = false;
                    controller.solidColor = ParseColor(nameColor);
                }
                text.color = ParseColor(nameColor);
            }
        }

        private static Color ParseColor(string nameColor)
        {
            if (string.IsNullOrEmpty(nameColor))
                return Color.white;
            if (ColorUtility.TryParseHtmlString(nameColor.Trim(), out Color c))
                return c;
            return Color.white;
        }
    }
}
