using UnityEngine;

namespace SaveAntarctica.BunkerDefense.UI
{
    public static class HudColorPalette
    {
        public static readonly Color MfdGreen = Hex("#00EE00");
        public static readonly Color MfdGreenDim = Hex("#008E00");
        public static readonly Color PanelBackground = new Color(0.02f, 0.025f, 0.03f, 0.94f);
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextMuted = new Color(0.72f, 0.78f, 0.72f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.35f, 0.3f, 1f);

        private static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : Color.green;
        }
    }
}
