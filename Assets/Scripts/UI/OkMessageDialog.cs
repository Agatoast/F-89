using UnityEngine;

namespace F89.UI
{
    public static class OkMessageDialog
    {
        public enum Result
        {
            None = 0,
            Confirmed = 1
        }

        private const float DialogWidth = 520f;
        private const float DialogHeight = 180f;
        private const float ButtonWidth = 120f;
        private const float ButtonHeight = 40f;
        private const int ButtonFontSize = 16;

        private static GUIStyle messageStyle;

        public static Result Draw(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Result.None;
            }

            EnsureStyles();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var dialogRect = new Rect(
                (Screen.width - DialogWidth) * 0.5f,
                (Screen.height - DialogHeight) * 0.5f,
                DialogWidth,
                DialogHeight);

            GUI.color = new Color(0.93f, 0.93f, 0.93f);
            GUI.DrawTexture(dialogRect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            HudGuiUtility.DrawWireBox(dialogRect, 2f);

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 28f, dialogRect.width - 48f, dialogRect.height - 90f),
                message,
                messageStyle);

            var okRect = new Rect(
                dialogRect.x + (dialogRect.width - ButtonWidth) * 0.5f,
                dialogRect.yMax - ButtonHeight - 24f,
                ButtonWidth,
                ButtonHeight);
            if (StartPageMenuStyles.DrawMenuButton(okRect, "OK", fontSize: ButtonFontSize))
            {
                return Result.Confirmed;
            }

            return Result.None;
        }

        private static void EnsureStyles()
        {
            if (messageStyle != null)
            {
                return;
            }

            messageStyle = HudStyleFactory.CreateLabel(
                22,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.black,
                wordWrap: true);
        }
    }
}
