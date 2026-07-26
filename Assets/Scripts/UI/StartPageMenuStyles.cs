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
        private static Texture2D whiteTexture;

        public static bool DrawMenuButton(Rect rect, string label, float panelAlpha = 0.94f)
        {
            EnsureStyles();
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
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), GetWhiteTexture());
            GUI.color = Color.white;

            if (texture == null)
            {
                return;
            }

            var imageRect = GetFullscreenImageRect(texture);
            GUI.DrawTexture(imageRect, texture, ScaleMode.StretchToFill, true);
        }

        public static void DrawSubpage(string title, string message)
        {
            EnsureStyles();

            GUI.color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), GetWhiteTexture());
            GUI.color = Color.white;

            subpageTitleStyle.normal.textColor = new Color(0.78f, 0.86f, 0.95f);
            GUI.Label(new Rect(0f, Screen.height * 0.22f, Screen.width, 48f), title, subpageTitleStyle);

            var messageStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Normal,
                TextAnchor.UpperCenter,
                new Color(0.72f, 0.78f, 0.86f),
                wordWrap: true);
            GUI.Label(
                new Rect(Screen.width * 0.18f, Screen.height * 0.32f, Screen.width * 0.64f, 120f),
                message,
                messageStyle);

            const float buttonWidth = 240f;
            const float buttonHeight = 48f;
            var backRect = new Rect(
                (Screen.width - buttonWidth) * 0.5f,
                Screen.height * 0.72f,
                buttonWidth,
                buttonHeight);

            if (DrawMenuButton(backRect, "BACK"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(F89.Core.GameScenes.StartPage);
            }
        }

        public static Rect GetMenuButtonRect(int index, int totalButtons)
        {
            GetMenuButtonSize(out var buttonWidth, out var buttonHeight);
            var scale = Mathf.Clamp(Screen.width / 1080f, 0.75f, 1.35f);
            var spacing = 14f * scale;
            var startY = Screen.height * 0.53f;
            var x = (Screen.width - buttonWidth) * 0.5f;
            var y = startY + index * (buttonHeight + spacing);

            return new Rect(x, y, buttonWidth, buttonHeight);
        }

        public static Rect GetSaveAntarcticaLogoRect(Texture2D logoTexture)
        {
            const float gapAbovePlayPx = 12f;
            const float widthScale = 1.08f;
            var playButton = GetMenuButtonRect(0, 5);
            GetMenuButtonSize(out var buttonWidth, out _);
            var aspect = logoTexture != null && logoTexture.width > 0
                ? (float)logoTexture.height / logoTexture.width
                : 0.34f;
            var width = buttonWidth * widthScale;
            var height = width * aspect;
            var x = (Screen.width - width) * 0.5f;
            var y = playButton.y - gapAbovePlayPx - height;
            return new Rect(x, Mathf.Max(0f, y), width, height);
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
            var scale = Mathf.Clamp(Screen.width / 1080f, 0.75f, 1.35f);
            width = 430f * scale;
            height = 54f * scale;
        }

        private static void DrawMenuButtonChrome(Rect rect, float panelAlpha = 0.94f)
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
            DrawBorder(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), 1f, new Color(0.18f, 0.42f, 0.72f, innerBorderAlpha));
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

        private static Rect GetFullscreenImageRect(Texture2D texture)
        {
            var screenAspect = (float)Screen.width / Screen.height;
            var textureAspect = (float)texture.width / texture.height;

            if (textureAspect > screenAspect)
            {
                var height = Screen.width / textureAspect;
                return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
            }

            var width = Screen.height * textureAspect;
            return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
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
            if (menuLabelStyle != null)
            {
                return;
            }

            menuLabelStyle = HudStyleFactory.CreateLabel(
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.88f, 0.94f));
            subpageTitleStyle = HudStyleFactory.CreateLabel(
                32,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
        }
    }
}
