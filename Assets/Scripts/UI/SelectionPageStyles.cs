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
        private static GUIStyle sectionHeaderStyle;
        private static GUIStyle dossierStatLabelStyle;
        private static GUIStyle dossierScoreLabelStyle;
        private static GUIStyle dossierTotalScoreLabelStyle;
        private static GUIStyle hiddenScrollbarStyle;
        private static GUIStyle plaqueVerticalScrollbarStyle;
        private static GUIStyle plaqueVerticalScrollbarThumbStyle;
        private static Texture2D whiteTexture;

        private static readonly string[] DossierStatLabels =
        {
            "Number of Enemy Vehicles Destroyed:",
            "Number of Enemy Troops Killed:",
            "Best Mission Score:",
            "TOTAL SCORE:"
        };

        public static float GetDossierStatLabelColumnWidthPx()
        {
            EnsureStyles();
            var maxWidth = 0f;
            for (var i = 0; i < 3; i++)
            {
                maxWidth = Mathf.Max(
                    maxWidth,
                    dossierStatLabelStyle.CalcSize(new GUIContent(DossierStatLabels[i])).x);
            }

            return maxWidth;
        }

        public static float GetDossierStatValueFiveSpacesWidthPx()
        {
            return MissionScoreDisplayUi.MeasureDossierValueTextWidth("     ");
        }

        public static float GetTotalScoreLabelWidthPx()
        {
            EnsureStyles();
            return dossierTotalScoreLabelStyle.CalcSize(new GUIContent(DossierStatLabels[3])).x;
        }

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

        public static void DrawSectionHeaders()
        {
            EnsureStyles();
            DrawGlowingHeader(SelectionPageLayout.GetCharacterSelectHeaderRect(), "Character Select", TextAnchor.MiddleLeft);
            DrawGlowingHeader(SelectionPageLayout.GetCharacterDossierHeaderRect(), "Character Dossier", TextAnchor.MiddleCenter);
        }

        public static void DrawCharacterListBackdrop()
        {
            var rect = SelectionPageLayout.GetCharacterListBackdropRect();
            GUI.color = new Color(0.04f, 0.05f, 0.05f, 0.72f);
            GUI.DrawTexture(rect, GetWhiteTexture());
            GUI.color = Color.white;
            DrawBorder(rect, 2f, new Color(0.55f, 0.48f, 0.28f, 0.85f));
        }

        public static void DrawDossierPanelChrome()
        {
            var rect = SelectionPageLayout.GetDossierPanelRect();
            GUI.color = new Color(0.12f, 0.14f, 0.12f, 0.82f);
            GUI.DrawTexture(rect, GetWhiteTexture());
            GUI.color = Color.white;

            DrawBorder(rect, 3f, new Color(0.62f, 0.52f, 0.28f, 0.95f));
            DrawBorder(
                new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f),
                1.5f,
                new Color(0.35f, 0.32f, 0.22f, 0.9f));

            DrawCornerRivets(rect);
        }

        public static void DrawDossierStaticLabels()
        {
            EnsureStyles();
            var highestAwardLabelRect = SelectionPageLayout.GetHighestAwardLabelRect();
            var previousAlignment = dossierNameStyle.alignment;
            dossierNameStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(highestAwardLabelRect, "Highest Award:", dossierNameStyle);
            dossierNameStyle.alignment = previousAlignment;
            GUI.Label(
                SelectionPageLayout.GetVehicleKillsLabelRect(),
                DossierStatLabels[0],
                dossierStatLabelStyle);
            GUI.Label(
                SelectionPageLayout.GetTroopKillsLabelRect(),
                DossierStatLabels[1],
                dossierStatLabelStyle);

            GUI.Label(
                SelectionPageLayout.GetBestMissionScoreLabelRect(),
                DossierStatLabels[2],
                dossierStatLabelStyle);

            GUI.Label(
                SelectionPageLayout.GetTotalScoreLabelRect(),
                DossierStatLabels[3],
                dossierTotalScoreLabelStyle);

            var awardSlot = SelectionPageLayout.GetHighestAwardSlotRect();
            GUI.color = Color.white;
            GUI.DrawTexture(awardSlot, GetWhiteTexture());
            GUI.color = Color.white;
            DrawBorder(awardSlot, 1.5f, new Color(0.45f, 0.4f, 0.25f, 0.9f));
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

        public static void DrawDossierPortrait(Rect rect, Texture2D portraitTexture, Texture2D frameTexture)
        {
            EnsureStyles();
            PortraitDisplayUtility.DrawMetallicFramedPortrait(
                rect,
                portraitTexture,
                frameTexture,
                portraitTexture == null ? "Click to Add Photo" : null,
                dossierPhotoPromptStyle);
        }

        public static void DrawDossierHighestAwardMedal(Rect rect, Texture2D medalTexture)
        {
            if (medalTexture == null)
            {
                return;
            }

            UiTextureFit.DrawTextureExact(rect, medalTexture);
        }

        public static void DrawDossierStats(
            int vehicleKills,
            int troopKills,
            int bestMissionScore,
            int totalScore)
        {
            EnsureStyles();
            MissionScoreDisplayUi.DrawDossierValue(SelectionPageLayout.GetDossierVehicleKillsValueRect(), vehicleKills);
            MissionScoreDisplayUi.DrawDossierValue(SelectionPageLayout.GetDossierTroopKillsValueRect(), troopKills);
            MissionScoreDisplayUi.DrawDossierValue(SelectionPageLayout.GetDossierBestScoreValueRect(), bestMissionScore);
            MissionScoreDisplayUi.DrawDossierValue(
                SelectionPageLayout.GetDossierTotalScoreValueRect(),
                totalScore,
                new Color(0.42f, 0.72f, 1f),
                TextAnchor.MiddleLeft,
                fontSizeOffset: 10);
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

        private static void DrawGlowingHeader(Rect rect, string text, TextAnchor alignment)
        {
            sectionHeaderStyle.alignment = alignment;
            var glow = new Color(0.85f, 0.78f, 0.45f, 0.35f);
            var previous = sectionHeaderStyle.normal.textColor;
            sectionHeaderStyle.normal.textColor = glow;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, sectionHeaderStyle);
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, sectionHeaderStyle);
            sectionHeaderStyle.normal.textColor = previous;
            GUI.Label(rect, text, sectionHeaderStyle);
        }

        private static void DrawUnderline(Rect labelRect, GUIStyle style, string text)
        {
            var size = style.CalcSize(new GUIContent(text));
            var y = labelRect.yMax - 4f;
            GUI.color = new Color(0.85f, 0.85f, 0.85f, 0.75f);
            GUI.DrawTexture(new Rect(labelRect.x, y, Mathf.Min(size.x, labelRect.width), 1.5f), GetWhiteTexture());
            GUI.color = Color.white;
        }

        private static void DrawCornerRivets(Rect rect)
        {
            var rivet = UiFitCanvas.Px(7f);
            var inset = UiFitCanvas.Px(10f);
            var color = new Color(0.72f, 0.62f, 0.34f, 0.95f);
            DrawRivet(rect.x + inset, rect.y + inset, rivet, color);
            DrawRivet(rect.xMax - inset - rivet, rect.y + inset, rivet, color);
            DrawRivet(rect.x + inset, rect.yMax - inset - rivet, rivet, color);
            DrawRivet(rect.xMax - inset - rivet, rect.yMax - inset - rivet, rivet, color);
        }

        private static void DrawRivet(float x, float y, float size, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, size, size), GetWhiteTexture());
            GUI.color = new Color(0.25f, 0.2f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(x + size * 0.28f, y + size * 0.28f, size * 0.44f, size * 0.44f), GetWhiteTexture());
            GUI.color = Color.white;
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
            var scale = Mathf.Clamp(UiFitCanvas.Scale, 0.72f, 1.35f);
            if (plaqueLabelStyle != null)
            {
                if (dossierStatLabelStyle != null)
                {
                    dossierStatLabelStyle.alignment = TextAnchor.MiddleRight;
                    dossierStatLabelStyle.wordWrap = false;
                }

                if (dossierTotalScoreLabelStyle != null)
                {
                    dossierTotalScoreLabelStyle.alignment = TextAnchor.MiddleLeft;
                    dossierTotalScoreLabelStyle.wordWrap = false;
                    dossierTotalScoreLabelStyle.fontSize = Mathf.RoundToInt(34f * scale) + 20;
                }

                return;
            }

            var plaqueSize = Mathf.RoundToInt(34f * scale);
            plaqueLabelStyle = HudStyleFactory.CreateLabel(
                plaqueSize,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                Color.white);
            plaqueLabelStyle.padding = new RectOffset(0, 0, 0, 0);
            plaqueLabelStyle.margin = new RectOffset(0, 0, 0, 0);
            plaqueLabelStyle.clipping = TextClipping.Overflow;

            var dossierNameColor = Color.white;
            const int dossierSize = 50;
            dossierNameStyle = HudStyleFactory.CreateLabel(
                dossierSize,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                dossierNameColor);
            dossierNameStyle.padding = new RectOffset(0, 0, 0, 0);
            dossierNameStyle.clipping = TextClipping.Overflow;

            dossierPhotoPromptStyle = HudStyleFactory.CreateLabel(
                Mathf.RoundToInt(22f * scale),
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.95f, 0.88f, 0.35f),
                wordWrap: true);
            dossierPhotoPromptStyle.padding = new RectOffset(8, 8, 8, 8);

            sectionHeaderStyle = HudStyleFactory.CreateLabel(
                72,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.92f, 0.88f, 0.72f),
                font: HudStyleFactory.StencilFont);

            var dossierLabelSize = Mathf.RoundToInt(34f * scale);
            dossierStatLabelStyle = HudStyleFactory.CreateLabel(
                dossierLabelSize,
                FontStyle.Bold,
                TextAnchor.MiddleRight,
                dossierNameColor,
                wordWrap: false);

            dossierScoreLabelStyle = dossierStatLabelStyle;
            dossierTotalScoreLabelStyle = HudStyleFactory.CreateLabel(
                dossierLabelSize + 20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                dossierNameColor,
                wordWrap: false);

            var dossierStatColor = new Color(0.98f, 0.92f, 0.18f);
            var statSize = Mathf.RoundToInt(40f * scale);
            dossierVehicleKillsStyle = CreateDossierStatStyle(statSize, dossierStatColor);
            dossierTroopKillsStyle = dossierVehicleKillsStyle;

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
