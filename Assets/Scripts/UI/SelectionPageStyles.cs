using UnityEngine;

namespace F89.UI
{
    public static class SelectionPageStyles
    {
        private const float PressDurationSeconds = 0.14f;
        private const float PressScale = 0.94f;
        private const float PressDownOffsetPx = 3f;
        private const float SelectionBorderLeftInsetPx = 50f;
        private const float PlaqueTextVerticalPaddingPx = 5f;
        private const float PlaqueExtraHeightPx = 10f;

        private static GUIStyle plaqueLabelStyle;
        private static GUIStyle dossierNameStyle;
        private static GUIStyle dossierPhotoPromptStyle;
        private static GUIStyle dossierVehicleKillsStyle;
        private static GUIStyle dossierTroopKillsStyle;
        private static GUIStyle hiddenScrollbarStyle;
        private static GUIStyle plaqueVerticalScrollbarStyle;
        private static GUIStyle plaqueVerticalScrollbarThumbStyle;
        private static Texture2D whiteTexture;

        public static float PressDuration => PressDurationSeconds;

        public static float GetPlaqueHeight()
        {
            EnsureStyles();
            var capHeight = plaqueLabelStyle.CalcSize(new GUIContent("T")).y;
            return capHeight + PlaqueTextVerticalPaddingPx * 2f + PlaqueExtraHeightPx;
        }

        public static GUIStyle GetPlaqueLabelStyle()
        {
            EnsureStyles();
            return plaqueLabelStyle;
        }

        public static bool DrawTexturedButton(Rect hitRect, Texture2D texture, float pressUntil)
        {
            if (texture != null)
            {
                var drawRect = ApplyPressAnimation(hitRect, pressUntil);
                GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
            }

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            var clicked = GUI.Button(hitRect, GUIContent.none, GUIStyle.none);
            GUI.backgroundColor = previous;
            return clicked;
        }

        public static bool DrawInvisibleButton(Rect hitRect)
        {
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            var clicked = GUI.Button(hitRect, GUIContent.none, GUIStyle.none);
            GUI.backgroundColor = previous;
            return clicked;
        }

        public static void DrawPlaqueSelection(Rect plaqueRect, Rect labelRect, string label, bool selected)
        {
            if (!selected || string.IsNullOrEmpty(label))
            {
                return;
            }

            EnsureStyles();
            var fitted = FitTextToWidth(label, plaqueLabelStyle, labelRect.width);
            var textWidth = plaqueLabelStyle.CalcSize(new GUIContent(fitted)).x;
            var charWidth = plaqueLabelStyle.CalcSize(new GUIContent("M")).x;
            var leftX = plaqueRect.x + SelectionBorderLeftInsetPx;
            var rightX = labelRect.x + textWidth + charWidth * 4f;
            var width = Mathf.Max(charWidth * 4f, rightX - leftX);

            var selectionRect = new Rect(leftX, plaqueRect.y, width, plaqueRect.height);
            DrawBorder(selectionRect, 3f, Color.white);
        }

        public static void DrawPlaqueLabel(Rect rect, string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            EnsureStyles();
            var fitted = FitTextToWidth(label, plaqueLabelStyle, rect.width);
            GUI.Label(rect, fitted, plaqueLabelStyle);
        }

        public static void DrawDossierName(Rect rect, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            EnsureStyles();
            var fitted = FitTextToWidth(name, dossierNameStyle, rect.width);
            GUI.Label(rect, fitted, dossierNameStyle);
        }

        public static void DrawDossierPortrait(Rect rect, Texture2D portraitTexture)
        {
            EnsureStyles();
            if (portraitTexture == null)
            {
                PortraitDisplayUtility.DrawPortraitFrame(rect, null, "Click to Add Photo", dossierPhotoPromptStyle);
                return;
            }

            PortraitDisplayUtility.DrawPortraitFrame(rect, portraitTexture, null, null);
        }

        public static void DrawDossierHighestAwardMedal(Rect rect, Texture2D medalTexture)
        {
            if (medalTexture == null)
            {
                return;
            }

            GUI.DrawTexture(rect, medalTexture, ScaleMode.ScaleToFit, false);
        }

        public static void DrawDossierStats(
            int vehicleKills,
            int troopKills,
            int bestMissionScore,
            int totalScore)
        {
            EnsureStyles();
            DrawDossierStatValue(SelectionPageLayout.GetDossierVehicleKillsValueRect(), vehicleKills, dossierVehicleKillsStyle);
            DrawDossierStatValue(SelectionPageLayout.GetDossierTroopKillsValueRect(), troopKills, dossierTroopKillsStyle);
            MissionScoreDisplayUi.DrawDossierValue(SelectionPageLayout.GetDossierBestScoreValueRect(), bestMissionScore);
            MissionScoreDisplayUi.DrawDossierValue(SelectionPageLayout.GetDossierTotalScoreValueRect(), totalScore);
        }

        private static void DrawDossierStatValue(Rect rect, int value, GUIStyle style)
        {
            GUI.Label(rect, value.ToString("N0"), style);
        }

        public static GUIStyle GetPlaqueVerticalScrollbar()
        {
            EnsureStyles();
            return plaqueVerticalScrollbarStyle;
        }

        public static GUIStyle GetPlaqueVerticalScrollbarThumb()
        {
            EnsureStyles();
            return plaqueVerticalScrollbarThumbStyle;
        }

        public static GUIStyle GetHiddenVerticalScrollbar()
        {
            EnsureStyles();
            return hiddenScrollbarStyle;
        }

        private static Rect ApplyPressAnimation(Rect rect, float pressUntil)
        {
            if (Time.unscaledTime >= pressUntil)
            {
                return rect;
            }

            var t = 1f - ((pressUntil - Time.unscaledTime) / PressDurationSeconds);
            var scale = Mathf.Lerp(1f, PressScale, t);
            var width = rect.width * scale;
            var height = rect.height * scale;
            var x = rect.x + (rect.width - width) * 0.5f;
            var y = rect.y + (rect.height - height) * 0.5f + PressDownOffsetPx * t;
            return new Rect(x, y, width, height);
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), GetWhiteTexture());
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), GetWhiteTexture());
            GUI.color = Color.white;
        }

        private static string FitTextToWidth(string text, GUIStyle style, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || style.CalcSize(new GUIContent(text)).x <= maxWidth)
            {
                return text;
            }

            const string ellipsis = "...";
            var trimmed = text;
            while (trimmed.Length > 1 && style.CalcSize(new GUIContent(trimmed + ellipsis)).x > maxWidth)
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 1);
            }

            return trimmed + ellipsis;
        }

        private static Texture2D GetWhiteTexture()
        {
            if (whiteTexture == null)
            {
                whiteTexture = Texture2D.whiteTexture;
            }

            return whiteTexture;
        }

        private static void EnsureStyles()
        {
            if (plaqueLabelStyle != null)
            {
                return;
            }

            var scale = Mathf.Clamp(Screen.width / 1920f, 0.72f, 1.35f);
            var plaqueSize = Mathf.RoundToInt(34f * scale);
            plaqueLabelStyle = HudStyleFactory.CreateLabel(
                plaqueSize,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);
            plaqueLabelStyle.padding = new RectOffset(0, 0, 0, 0);
            plaqueLabelStyle.margin = new RectOffset(0, 0, 0, 0);
            plaqueLabelStyle.clipping = TextClipping.Overflow;

            var dossierSize = Mathf.RoundToInt(28f * scale);
            dossierNameStyle = HudStyleFactory.CreateLabel(
                dossierSize,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Color(0.93f, 0.9f, 0.78f));
            dossierNameStyle.padding = new RectOffset(0, 0, 0, 0);
            dossierNameStyle.clipping = TextClipping.Overflow;

            dossierPhotoPromptStyle = HudStyleFactory.CreateLabel(
                Mathf.RoundToInt(22f * scale),
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.95f, 0.88f, 0.35f),
                wordWrap: true);
            dossierPhotoPromptStyle.padding = new RectOffset(8, 8, 8, 8);

            var statSize = Mathf.RoundToInt(40f * scale);
            var dossierStatColor = new Color(0.98f, 0.92f, 0.18f);
            dossierVehicleKillsStyle = CreateDossierStatStyle(statSize, dossierStatColor);
            dossierTroopKillsStyle = CreateDossierStatStyle(statSize, dossierStatColor);

            hiddenScrollbarStyle = new GUIStyle(GUIStyle.none)
            {
                fixedWidth = 0f,
                fixedHeight = 0f
            };

            var trackColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
            var thumbColor = new Color(0.72f, 0.68f, 0.52f, 0.95f);
            plaqueVerticalScrollbarStyle = new GUIStyle(GUI.skin.verticalScrollbar)
            {
                normal = { background = MakeSolidTexture(trackColor) },
                fixedWidth = 18f
            };
            plaqueVerticalScrollbarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb)
            {
                normal = { background = MakeSolidTexture(thumbColor) },
                fixedWidth = 18f
            };
        }

        private static GUIStyle CreateDossierStatStyle(int fontSize, Color color)
        {
            var style = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                color);
            style.padding = new RectOffset(0, 0, 0, 0);
            style.clipping = TextClipping.Overflow;
            return style;
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
