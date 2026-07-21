using UnityEngine;

namespace F89.UI
{
    public static class HudStyleFactory
    {
        private static Font labelFont;

        public static Font LabelFont
        {
            get
            {
                if (labelFont == null)
                {
                    labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return labelFont;
            }
        }

        public static GUIStyle CreateLabel(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color textColor,
            bool wordWrap = false)
        {
            var style = new GUIStyle
            {
                font = LabelFont,
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                wordWrap = wordWrap,
                clipping = TextClipping.Overflow,
                richText = false
            };
            style.normal.textColor = textColor;
            return style;
        }
    }
}
