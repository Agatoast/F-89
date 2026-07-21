using UnityEngine;

namespace F89.UI
{
    public static class FlightHudColorPalette
    {
        private static readonly Color[] Colors =
        {
            HexToColor("00ee00"),
            HexToColor("008e00"),
            HexToColor("005f00"),
            HexToColor("002f00"),
            HexToColor("000000")
        };

        private static int colorIndex;

        public static Color Current => Colors[colorIndex];

        public static string CurrentHex => ColorUtility.ToHtmlStringRGB(Current);

        public static void CycleNext()
        {
            colorIndex = (colorIndex + 1) % Colors.Length;
        }

        public static void ResetToDefault()
        {
            colorIndex = 0;
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var color))
            {
                return color;
            }

            return Color.green;
        }
    }
}
