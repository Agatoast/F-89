using UnityEngine;

namespace F89.UI
{
    public static class HudStyleFactory
    {
        private static Font labelFont;
        private static Font arialFont;
        private static Font stencilFont;

        public static Font LabelFont
        {
            get
            {
                if (labelFont == null)
                {
                    labelFont = Resources.Load<Font>("Fonts/StardosStencil-Regular")
                        ?? Resources.Load<Font>("Fonts/StardosStencil-Bold")
                        ?? Resources.Load<Font>("Fonts/Stencil")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (labelFont == null)
                    {
                        labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Verdana" }, 16);
                    }
                }

                return labelFont;
            }
        }

        public static Font ArialFont
        {
            get
            {
                if (arialFont == null)
                {
                    arialFont = Font.CreateDynamicFontFromOSFont("Arial", 16) ?? LabelFont;
                }

                return arialFont;
            }
        }

        public static Font StencilFont
        {
            get
            {
                if (stencilFont == null)
                {
                    stencilFont = Resources.Load<Font>("Fonts/Stencil")
                        ?? Resources.Load<Font>("Fonts/StardosStencil-Bold")
                        ?? Resources.Load<Font>("Fonts/StardosStencil-Regular")
                        ?? LabelFont;
                }

                return stencilFont;
            }
        }

        public static GUIStyle CreateLabel(
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color textColor,
            bool wordWrap = false,
            Font font = null)
        {
            var style = new GUIStyle
            {
                font = font != null ? font : LabelFont,
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
