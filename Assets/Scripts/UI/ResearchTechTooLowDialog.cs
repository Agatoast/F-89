using UnityEngine;

namespace F89.UI
{
    public static class ResearchTechTooLowDialog
    {
        public enum Result
        {
            None = 0,
            Acknowledged = 1
        }

        private const float DialogWidth = 480f;
        private const float DialogHeight = 160f;
        private const float ChoiceWidth = 120f;
        private const float ChoiceHeight = 40f;
        private const int ButtonFontSize = 16;

        private static GUIStyle messageStyle;

        public static Result Draw(bool visible)
        {
            if (!visible)
            {
                return Result.None;
            }

            EnsureStyles();

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
                new Rect(dialogRect.x + 24f, dialogRect.y + 28f, dialogRect.width - 48f, 50f),
                "Item Tech Level Too Low",
                messageStyle);

            var okRect = new Rect(
                dialogRect.x + (dialogRect.width - ChoiceWidth) * 0.5f,
                dialogRect.yMax - ChoiceHeight - 24f,
                ChoiceWidth,
                ChoiceHeight);

            if (StartPageMenuStyles.DrawMenuButton(okRect, "OK", fontSize: ButtonFontSize))
            {
                return Result.Acknowledged;
            }

            return Result.None;
        }

        private static void EnsureStyles()
        {
            if (messageStyle != null)
            {
                return;
            }

            messageStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
        }
    }
}
