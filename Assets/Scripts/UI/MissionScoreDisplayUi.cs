using UnityEngine;

namespace F89.UI
{
    public static class MissionScoreDisplayUi
    {
        private const int CharacterPageFontSize = 44;

        private static GUIStyle characterPageLabelStyle;
        private static GUIStyle characterPageValueStyle;
        private static GUIStyle dossierValueStyle;

        public static string FormatScore(int score) => score.ToString("N0");

        public static void DrawDossierValue(Rect rect, int score)
        {
            GUI.Label(rect, FormatScore(score), GetDossierValueStyle());
        }

        public static void DrawCharacterPageRow(
            Rect rect,
            string label,
            int score,
            float labelColumnWidth,
            float onesColumnX)
        {
            var labelStyle = GetCharacterPageLabelStyle();
            var valueStyle = GetCharacterPageValueStyle();
            GUI.Label(new Rect(rect.x, rect.y, labelColumnWidth, rect.height), label, labelStyle);
            DrawCharacterPageValue(onesColumnX, rect.y, rect.height, score, valueStyle);
        }

        private static void DrawCharacterPageValue(float onesColumnX, float y, float height, int score, GUIStyle style)
        {
            var value = FormatScore(score);
            var valueSize = style.CalcSize(new GUIContent(value));
            var lastCharWidth = style.CalcSize(new GUIContent(value[value.Length - 1].ToString())).x;
            var drawX = onesColumnX - (valueSize.x - lastCharWidth);
            GUI.Label(new Rect(drawX, y, valueSize.x, height), value, style);
        }

        private static GUIStyle GetCharacterPageLabelStyle()
        {
            if (characterPageLabelStyle == null)
            {
                characterPageLabelStyle = HudStyleFactory.CreateLabel(
                    CharacterPageFontSize,
                    FontStyle.Bold,
                    TextAnchor.UpperRight,
                    GetTextColor());
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
                    TextAnchor.UpperLeft,
                    GetTextColor());
            }

            return characterPageValueStyle;
        }

        private static GUIStyle GetDossierValueStyle()
        {
            if (dossierValueStyle == null)
            {
                dossierValueStyle = HudStyleFactory.CreateLabel(
                    GetDossierFontSize(),
                    FontStyle.Bold,
                    TextAnchor.MiddleRight,
                    GetTextColor());
                dossierValueStyle.padding = new RectOffset(0, 0, 0, 0);
                dossierValueStyle.clipping = TextClipping.Overflow;
            }
            else
            {
                dossierValueStyle.fontSize = GetDossierFontSize();
            }

            return dossierValueStyle;
        }

        private static int GetDossierFontSize()
        {
            var scale = Mathf.Clamp(Screen.width / 1080f, 0.75f, 1.35f);
            return Mathf.RoundToInt(40f * scale);
        }

        private static Color GetTextColor() => new Color(0.98f, 0.92f, 0.18f);
    }
}
