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
        private static GUIStyle researchTitleStyle;
        private static GUIStyle killFolderLabelStyle;
        private static GUIStyle panelCaptionStyle;
        private static GUIStyle smallSlotLabelStyle;
        private static GUIStyle researchDropHintStyle;
        private static GUIStyle researchFooterStyle;
        private static GUIStyle researchSlotLabelStyle;
        private static GUIStyle researchSlotTlValueStyle;
        private static GUIStyle researchSlotChanceStyle;

        public static GUIStyle NameBarStyle => Ensure(
            ref nameBarStyle,
            34,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            Color.white);
        public static GUIStyle BodyStyle => Ensure(ref bodyStyle, 13, FontStyle.Normal, TextAnchor.UpperLeft, Color.white, wordWrap: true);
        public static GUIStyle ScoreLabelStyle => Ensure(ref scoreLabelStyle, 14, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
        public static GUIStyle ScoreValueStyle => Ensure(ref scoreValueStyle, 14, FontStyle.Bold, TextAnchor.UpperRight, Color.white);
        public static GUIStyle FootlockerTitleStyle => Ensure(ref footlockerTitleStyle, 44, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.08f));
        public static GUIStyle ResearchTitleStyle => Ensure(ref researchTitleStyle, 44, FontStyle.Bold, TextAnchor.UpperRight, new Color(1f, 0.86f, 0.08f));
        public static GUIStyle KillFolderLabelStyle => Ensure(ref killFolderLabelStyle, 30, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
        public static GUIStyle PanelCaptionStyle => Ensure(ref panelCaptionStyle, 12, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.92f, 0.92f, 0.92f), wordWrap: true);
        public static GUIStyle ResearchDropHintStyle => Ensure(
            ref researchDropHintStyle,
            17,
            FontStyle.Normal,
            TextAnchor.UpperCenter,
            new Color(0.92f, 0.94f, 0.98f),
            wordWrap: true);
        public static GUIStyle ResearchFooterStyle => Ensure(
            ref researchFooterStyle,
            15,
            FontStyle.Normal,
            TextAnchor.UpperCenter,
            new Color(0.92f, 0.94f, 0.98f),
            wordWrap: true);
        public static GUIStyle ResearchSlotLabelStyle => Ensure(
            ref researchSlotLabelStyle,
            13,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            new Color(0.92f, 0.94f, 0.98f));
        public static GUIStyle ResearchSlotTlValueStyle => Ensure(
            ref researchSlotTlValueStyle,
            13,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            new Color(1f, 0.86f, 0.08f));
        public static GUIStyle ResearchSlotChanceStyle => Ensure(
            ref researchSlotChanceStyle,
            34,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.86f, 0.08f));
        public static GUIStyle StatBoxStyle => Ensure(ref statBoxStyle, ScaleSlotFont(12), FontStyle.Bold, TextAnchor.LowerCenter, Color.black, wordWrap: true);
        public static GUIStyle SmallSlotLabelStyle => Ensure(ref smallSlotLabelStyle, ScaleSlotFont(SmallSlotLabelBaseFontSize), FontStyle.Bold, TextAnchor.MiddleCenter, Color.white, wordWrap: true);

        private static GUIStyle statBoxStyle;

        public static int ScaleSlotFont(int baseFontSize) =>
            Mathf.Max(8, Mathf.RoundToInt(baseFontSize * SlotTextScale));

        public static void DrawYellowLabelWithBlackOutline(Rect rect, string text, GUIStyle style)
        {
            var previousContentColor = GUI.contentColor;
            var fillColor = style.normal.textColor;

            GUI.contentColor = Color.black;
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - 1f, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);

            GUI.contentColor = fillColor;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

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
