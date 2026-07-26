using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageStyles
    {
        public const float SlotTextScale = 1.8f;
        private const int SmallSlotLabelBaseFontSize = 9;

        private static GUIStyle nameBarStyle;
        private static GUIStyle bodyStyle;
        private static GUIStyle scoreLabelStyle;
        private static GUIStyle scoreValueStyle;
        private static GUIStyle footlockerTitleStyle;
        private static GUIStyle killFolderLabelStyle;
        private static GUIStyle panelCaptionStyle;
        private static GUIStyle smallSlotLabelStyle;

        public static GUIStyle NameBarStyle => Ensure(
            ref nameBarStyle,
            34,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            new Color(1f, 0.86f, 0.08f));
        public static GUIStyle BodyStyle => Ensure(ref bodyStyle, 13, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, wordWrap: true);
        public static GUIStyle ScoreLabelStyle => Ensure(ref scoreLabelStyle, 14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
        public static GUIStyle ScoreValueStyle => Ensure(ref scoreValueStyle, 14, FontStyle.Bold, TextAnchor.UpperRight, Color.white);
        public static GUIStyle FootlockerTitleStyle => Ensure(ref footlockerTitleStyle, 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.08f));
        public static GUIStyle KillFolderLabelStyle => Ensure(ref killFolderLabelStyle, 30, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
        public static GUIStyle PanelCaptionStyle => Ensure(ref panelCaptionStyle, 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f), wordWrap: true);
        public static GUIStyle StatBoxStyle => Ensure(ref statBoxStyle, ScaleSlotFont(12), FontStyle.Bold, TextAnchor.LowerCenter, Color.black, wordWrap: true);
        public static GUIStyle SmallSlotLabelStyle => Ensure(ref smallSlotLabelStyle, ScaleSlotFont(SmallSlotLabelBaseFontSize), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, wordWrap: true);

        private static GUIStyle statBoxStyle;

        public static int ScaleSlotFont(int baseFontSize) =>
            Mathf.Max(8, Mathf.RoundToInt(baseFontSize * SlotTextScale));

        private static GUIStyle Ensure(
            ref GUIStyle style,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor anchor,
            Color color,
            bool wordWrap = false)
        {
            if (style != null)
            {
                style.fontSize = fontSize;
                style.fontStyle = fontStyle;
                style.alignment = anchor;
                style.normal.textColor = color;
                style.wordWrap = wordWrap;
                return style;
            }

            style = HudStyleFactory.CreateLabel(fontSize, fontStyle, anchor, color, wordWrap);
            return style;
        }
    }
}
