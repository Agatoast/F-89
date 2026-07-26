using UnityEngine;

namespace F89.UI
{
    public static class MenuNavigationState
    {
        public enum SubpageMode
        {
            Message = 0,
            Settings = 1
        }

        public static string SubpageTitle { get; set; } = string.Empty;
        public static string SubpageMessage { get; set; } = string.Empty;
        public static SubpageMode Mode { get; set; } = SubpageMode.Message;
    }

    public static class StartPageMenuStyles
    {
        private static GUIStyle menuLabelStyle;
        private static GUIStyle subpageTitleStyle;
        private static GUIStyle subpageMessageStyle;
        private static Texture2D whiteTexture;
        private static Texture2D storyFlagTexture;
        private static Texture2D saveAntarcticaLogoTexture;
        private static Vector2 subpageScroll;

        public static bool DrawMenuButton(Rect rect, string label, float panelAlpha = 0.94f, int fontSize = -1)
        {
            EnsureStyles(fontSize);
            DrawMenuButtonChrome(rect, panelAlpha);

            GUI.color = Color.white;
            GUI.Label(rect, label, menuLabelStyle);

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Color.clear;
            var clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
            GUI.backgroundColor = previous;
            return clicked;
        }

        public static void DrawFullscreenBackground(Texture2D texture)
        {
            UiFitCanvas.DrawLetterboxedBackground(texture);
        }

        public static void DrawSubpage(string title, string message)
        {
            EnsureStyles();

            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), GetWhiteTexture());
            GUI.color = Color.white;

            Rect titleRect;
            if (title == StoryPageContent.Title)
            {
                titleRect = DrawStoryPageLogo();
            }
            else
            {
                subpageTitleStyle.normal.textColor = new Color(0.78f, 0.86f, 0.95f);
                titleRect = new Rect(0f, Screen.height * 0.06f, Screen.width, 48f);
                GUI.Label(titleRect, title, subpageTitleStyle);
            }

            const float buttonWidth = 240f;
            const float buttonHeight = 48f;
            var backRect = new Rect(
                (Screen.width - buttonWidth) * 0.5f,
                Screen.height - buttonHeight - Screen.height * 0.05f,
                buttonWidth,
                buttonHeight);

            var messageArea = new Rect(
                Screen.width * 0.12f,
                titleRect.yMax + Screen.height * 0.02f,
                Screen.width * 0.76f,
                backRect.y - (titleRect.yMax + Screen.height * 0.04f));

            var content = new GUIContent(message ?? string.Empty);
            var isStory = title == StoryPageContent.Title;
            var textWidth = isStory ? messageArea.width : messageArea.width - 24f;
            var contentHeight = Mathf.Max(
                messageArea.height,
                subpageMessageStyle.CalcHeight(content, textWidth) + 16f);
            var viewRect = new Rect(0f, 0f, textWidth, contentHeight);

            if (isStory)
            {
                subpageScroll = GUI.BeginScrollView(
                    messageArea,
                    subpageScroll,
                    viewRect,
                    GUIStyle.none,
                    GUIStyle.none);
            }
            else
            {
                subpageScroll = GUI.BeginScrollView(messageArea, subpageScroll, viewRect, false, true);
            }

            GUI.Label(viewRect, content, subpageMessageStyle);
            GUI.EndScrollView();

            if (isStory)
            {
                DrawStoryFlag(message, messageArea, textWidth);
            }

            if (DrawMenuButton(backRect, "BACK", fontSize: 28))
            {
                subpageScroll = Vector2.zero;
                UnityEngine.SceneManagement.SceneManager.LoadScene(F89.Core.GameScenes.StartPage);
            }
        }

        private static Rect DrawStoryPageLogo()
        {
            if (saveAntarcticaLogoTexture == null)
            {
                saveAntarcticaLogoTexture = Resources.Load<Texture2D>("CharacterPage/save_antarctica_logo");
            }

            var topY = Screen.height * 0.04f;
            if (saveAntarcticaLogoTexture == null)
            {
                return new Rect(0f, topY, Screen.width, 48f);
            }

            var aspect = saveAntarcticaLogoTexture.height / (float)Mathf.Max(1, saveAntarcticaLogoTexture.width);
            // Keep the layout slot unchanged so story text / flag / back stay put.
            var layoutWidth = Screen.width * 0.42f * 0.5f;
            var layoutHeight = layoutWidth * aspect;
            var layoutRect = new Rect(
                (Screen.width - layoutWidth) * 0.5f - UiFitCanvas.Px(100f),
                topY,
                layoutWidth,
                layoutHeight);

            // Fill the header band as large as possible, bottom-aligned to the layout slot.
            var topMargin = UiFitCanvas.Px(4f);
            var sideMargin = UiFitCanvas.Px(10f);
            var maxHeight = Mathf.Max(layoutHeight, layoutRect.yMax - topMargin);
            var maxWidth = maxHeight / aspect;
            maxWidth = Mathf.Min(maxWidth, Screen.width - sideMargin * 2f);
            maxHeight = maxWidth * aspect;

            var drawX = (Screen.width - maxWidth) * 0.5f - UiFitCanvas.Px(100f);
            drawX = Mathf.Clamp(drawX, sideMargin, Screen.width - maxWidth - sideMargin);
            var drawRect = new Rect(drawX, layoutRect.yMax - maxHeight, maxWidth, maxHeight);

            GUI.color = Color.white;
            GUI.DrawTexture(drawRect, saveAntarcticaLogoTexture, ScaleMode.ScaleToFit, true);
            return layoutRect;
        }

        private static void DrawStoryFlag(string message, Rect messageArea, float textWidth)
        {
            if (storyFlagTexture == null)
            {
                storyFlagTexture = Resources.Load<Texture2D>(StoryPageContent.FlagResourcePath);
            }

            if (storyFlagTexture == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            var anchorIndex = message.IndexOf(StoryPageContent.FlagAnchorLine, System.StringComparison.Ordinal);
            if (anchorIndex < 0)
            {
                return;
            }

            var prefix = message.Substring(0, anchorIndex);
            var lineY = string.IsNullOrEmpty(prefix)
                ? 0f
                : subpageMessageStyle.CalcHeight(new GUIContent(prefix), textWidth);

            var lineWidth = subpageMessageStyle.CalcSize(new GUIContent(StoryPageContent.FlagAnchorLine)).x;
            var lineHeight = Mathf.Max(
                28f,
                subpageMessageStyle.CalcHeight(new GUIContent("Ag"), textWidth));
            var flagLeft = messageArea.x + Mathf.Min(lineWidth + 28f, textWidth * 0.52f);

            // Sit beside the four short mission lines; 2x prior size, shifted right.
            var flagHeight = lineHeight * 4.2f * 2f * 0.9f * 0.9f * 0.9f;
            var aspect = storyFlagTexture.width / (float)Mathf.Max(1, storyFlagTexture.height);
            var flagWidth = flagHeight * aspect;

            var flagX = flagLeft + UiFitCanvas.Px(400f) + UiFitCanvas.Px(200f) + UiFitCanvas.Px(200f) + UiFitCanvas.Px(200f) + UiFitCanvas.Px(100f) + UiFitCanvas.Px(200f) + UiFitCanvas.Px(200f);
            var flagY = messageArea.y + lineY - subpageScroll.y - UiFitCanvas.Px(80f) - UiFitCanvas.Px(50f) - UiFitCanvas.Px(20f) - UiFitCanvas.Px(10f) - UiFitCanvas.Px(10f);
            var flagRect = new Rect(flagX, flagY, flagWidth, flagHeight);

            if (flagRect.yMax < messageArea.y || flagRect.y > messageArea.yMax)
            {
                return;
            }

            GUI.BeginClip(new Rect(0f, messageArea.y, Screen.width, messageArea.height));
            flagRect.y -= messageArea.y;
            GUI.color = Color.white;
            GUI.DrawTexture(flagRect, storyFlagTexture, ScaleMode.ScaleToFit, true);
            GUI.EndClip();
        }

        public static Rect GetMenuButtonRect(int index, int totalButtons)
        {
            GetMenuButtonSize(out var buttonWidth, out var buttonHeight);
            var spacing = UiFitCanvas.Px(14f);
            var startY = UiFitCanvas.NormY(0.54f);
            var x = UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f;
            var y = startY + index * (buttonHeight + spacing);

            return new Rect(x, y, buttonWidth, buttonHeight);
        }

        public static Rect GetMainMenuButtonRect(int index, int totalButtons)
        {
            // Fit the baked black menu plate on main_menu.png.
            // Leaves room under the jet for the SAVE Antarctica logo.
            const float panelLeftNorm = 0.12f;
            const float panelRightNorm = 0.88f;
            const float panelTopNorm = 0.54f;
            const float panelBottomNorm = 0.965f;
            const float horizontalInsetDesignPx = 18f;
            const float verticalInsetDesignPx = 16f;
            const float spacingDesignPx = 12f;

            totalButtons = Mathf.Max(1, totalButtons);
            var panel = UiFitCanvas.NormRect(
                panelLeftNorm,
                panelTopNorm,
                panelRightNorm - panelLeftNorm,
                panelBottomNorm - panelTopNorm);
            var hInset = UiFitCanvas.Px(horizontalInsetDesignPx);
            var vInset = UiFitCanvas.Px(verticalInsetDesignPx);
            var spacing = UiFitCanvas.Px(spacingDesignPx);
            var width = Mathf.Max(1f, panel.width - hInset * 2f);
            var usableHeight = Mathf.Max(1f, panel.height - vInset * 2f);
            var height = (usableHeight - spacing * (totalButtons - 1)) / totalButtons;
            var x = panel.x + hInset;
            var y = panel.y + vInset + index * (height + spacing);
            return new Rect(x, y, width, height);
        }

        public static Rect GetSaveAntarcticaLogoRect(Texture2D logoTexture)
        {
            // Above the main menu buttons; right edge aligned with button right edge.
            const float gapAbovePlayDesignPx = 10f;
            const float shiftUpDesignPx = 10f;
            const float widthNorm = 0.792f; // 0.72 * 1.10
            var firstButton = GetMainMenuButtonRect(0, 5);
            var aspect = logoTexture != null && logoTexture.width > 0
                ? (float)logoTexture.height / logoTexture.width
                : 0.34f;
            var width = UiFitCanvas.Rect.width * widthNorm;
            var height = width * aspect;
            var x = firstButton.xMax - width;
            var y = firstButton.y
                - UiFitCanvas.Px(gapAbovePlayDesignPx)
                - height
                - UiFitCanvas.Px(shiftUpDesignPx);
            y = Mathf.Max(UiFitCanvas.NormY(0.28f), y);
            return new Rect(x, y, width, height);
        }

        public static void DrawSaveAntarcticaLogo(Texture2D logoTexture)
        {
            if (logoTexture == null)
            {
                return;
            }

            var rect = GetSaveAntarcticaLogoRect(logoTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, logoTexture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        public static void GetMenuButtonSize(out float width, out float height)
        {
            var scale = Mathf.Clamp(UiFitCanvas.Scale, 0.55f, 1.35f);
            width = 430f * scale;
            height = 54f * scale;
        }

        public static void DrawMenuButtonChrome(Rect rect, float panelAlpha = 0.94f)
        {
            var opaque = panelAlpha >= 1f;
            var glowAlpha = opaque ? 0.5f : 0.22f;
            var outerBorderAlpha = opaque ? 1f : 0.95f;
            var innerBorderAlpha = opaque ? 0.75f : 0.45f;

            var glowRect = new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f);
            GUI.color = new Color(0.18f, 0.55f, 0.95f, glowAlpha);
            GUI.DrawTexture(glowRect, GetWhiteTexture());

            GUI.color = new Color(0.11f, 0.13f, 0.16f, panelAlpha);
            GUI.DrawTexture(rect, GetWhiteTexture());

            DrawBorder(rect, 1.5f, new Color(0.42f, 0.72f, 1f, outerBorderAlpha));
            DrawBorder(
                new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f),
                1f,
                new Color(0.18f, 0.42f, 0.72f, innerBorderAlpha));
            GUI.color = Color.white;
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

        private static Texture2D GetWhiteTexture()
        {
            if (whiteTexture == null)
            {
                whiteTexture = Texture2D.whiteTexture;
            }

            return whiteTexture;
        }

        private static void EnsureStyles(int fontSizeOverride = -1)
        {
            var fontSize = fontSizeOverride > 0
                ? fontSizeOverride
                : Mathf.RoundToInt(24f * Mathf.Clamp(UiFitCanvas.Scale, 0.55f, 1.6f));
            if (menuLabelStyle == null)
            {
                menuLabelStyle = HudStyleFactory.CreateLabel(
                    fontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    new Color(0.82f, 0.88f, 0.94f));
            }
            else
            {
                menuLabelStyle.fontSize = fontSize;
            }

            if (subpageTitleStyle == null)
            {
                subpageTitleStyle = HudStyleFactory.CreateLabel(
                    32,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white);
            }

            if (subpageMessageStyle == null)
            {
                subpageMessageStyle = HudStyleFactory.CreateLabel(
                    36,
                    FontStyle.Normal,
                    TextAnchor.UpperLeft,
                    Color.white,
                    wordWrap: true);
            }
            else
            {
                subpageMessageStyle.fontSize = 28;
                subpageMessageStyle.normal.textColor = Color.white;
            }
        }
    }
}
