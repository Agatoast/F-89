using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Utilities
{
    public static class MilitaryFont
    {
        private static Font _labelFont;

        public static Font LabelFont
        {
            get
            {
                if (_labelFont != null)
                {
                    return _labelFont;
                }

                _labelFont = Resources.Load<Font>("Fonts/StardosStencil-Regular")
                    ?? Resources.Load<Font>("Fonts/StardosStencil-Bold")
                    ?? Resources.Load<Font>("Fonts/Stencil")
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                if (_labelFont == null)
                {
                    _labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Verdana" }, 16);
                }

                return _labelFont;
            }
        }
    }
}
