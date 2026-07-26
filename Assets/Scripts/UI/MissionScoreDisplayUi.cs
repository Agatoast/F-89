using UnityEngine;

namespace F89.UI
{
    public static class MissionScoreDisplayUi
    {
        private const int CharacterPageFontSize = 34;

        private static GUIStyle characterPageLabelStyle;
        private static GUIStyle characterPageValueStyle;
        private static GUIStyle dossierValueStyle;
        private static GUIStyle dossierValueOverrideStyle;

        public static string FormatScore(int score) => score.ToString("N0");

        public static void DrawDossierValue(Rect rect, int score)
        {
            DrawDossierValue(rect, score, GetTextColor(), TextAnchor.MiddleRight);
        }

        public static void DrawDossierValue(
            Rect rect,
            int score,
            Color color,
            TextAnchor alignment,
            int fontSizeOffset = 0)
        {
            var style = GetDossierValueStyle(color, alignment, fontSizeOffset);
            GUI.Label(rect, FormatScore(score), style);
        }

        public static float MeasureDossierValueTextWidth(string text, int fontSizeOffset = 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            return GetDossierValueStyle(GetTextColor(), TextAnchor.MiddleRight, fontSizeOffset)
                .CalcSize(new GUIContent(text)).x;
        }

        public static void DrawCharacterPageRow(
            Rect rect,
            string label,
            int score,
            float labelColumnWidth,
            float onesColumnX)
        {
            const float valueGapFromLabelDesignPx = 6f;
            var labelStyle = GetCharacterPageLabelStyle();
            var valueStyle = GetCharacterPageValueStyle();
            var lineHeight = rect.height * 0.5f;
            var valueGap = UiFitCanvas.Px(valueGapFromLabelDesignPx);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, lineHeight), label, labelStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + lineHeight + valueGap, rect.width, lineHeight),
                FormatScore(score),
                valueStyle);
        }

        private static GUIStyle GetCharacterPageLabelStyle()
        {
            if (characterPageLabelStyle == null)
            {
                characterPageLabelStyle = HudStyleFactory.CreateLabel(
                    CharacterPageFontSize,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    GetTextColor());
            }
            else
            {
                characterPageLabelStyle.fontSize = CharacterPageFontSize;
                characterPageLabelStyle.alignment = TextAnchor.UpperCenter;
            }

            return characterPageLabelStyle;
        }

        private static GUIStyle GetCharacterPageValueStyle()
        {
            if (characterPageValueStyle == null)
            {
                characterPageValueStyle = HudStyleFactory.CreateLabel(
                    CharacterPageFontSize,
                    FontStyle.Bold,
                    TextAnchor.UpperCenter,
                    Color.white);
            }
            else
            {
                characterPageValueStyle.fontSize = CharacterPageFontSize;
                characterPageValueStyle.alignment = TextAnchor.UpperCenter;
                characterPageValueStyle.normal.textColor = Color.white;
            }

            return characterPageValueStyle;
        }

        private static GUIStyle GetDossierValueStyle(Color color, TextAnchor alignment, int fontSizeOffset = 0)
        {
            var fontSize = GetDossierFontSize() + fontSizeOffset;
            if (color == GetTextColor() && alignment == TextAnchor.MiddleRight && fontSizeOffset == 0)
            {
                if (dossierValueStyle == null)
                {
                    dossierValueStyle = HudStyleFactory.CreateLabel(
                        fontSize,
                        FontStyle.Bold,
                        alignment,
                        color);
                    dossierValueStyle.padding = new RectOffset(0, 0, 0, 0);
                    dossierValueStyle.clipping = TextClipping.Overflow;
                }
                else
                {
                    dossierValueStyle.fontSize = fontSize;
                }

                return dossierValueStyle;
            }

            if (dossierValueOverrideStyle == null)
            {
                dossierValueOverrideStyle = HudStyleFactory.CreateLabel(
                    fontSize,
                    FontStyle.Bold,
                    alignment,
                    color);
                dossierValueOverrideStyle.padding = new RectOffset(0, 0, 0, 0);
                dossierValueOverrideStyle.clipping = TextClipping.Overflow;
            }
            else
            {
                dossierValueOverrideStyle.fontSize = fontSize;
                dossierValueOverrideStyle.alignment = alignment;
                dossierValueOverrideStyle.normal.textColor = color;
            }

            return dossierValueOverrideStyle;
        }

        private static GUIStyle GetDossierValueStyle()
        {
            return GetDossierValueStyle(GetTextColor(), TextAnchor.MiddleRight);
        }

        private static int GetDossierFontSize()
        {
            var scale = Mathf.Clamp(Screen.width / 1080f, 0.75f, 1.35f);
            return Mathf.RoundToInt(40f * scale);
        }

        private static Color GetTextColor() => new Color(0.98f, 0.92f, 0.18f);
    }
}
