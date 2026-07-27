using UnityEngine;

namespace F89.UI
{
    public static class DeleteCharacterDialog
    {
        public enum Result
        {
            None = 0,
            Confirmed = 1,
            Back = 2
        }

        private const float DialogWidth = 480f;
        private const float DialogHeight = 180f;
        private const float ChoiceWidth = 120f;
        private const float ChoiceHeight = 38f;
        private const int ButtonFontSize = 16;

        private static GUIStyle titleStyle;
        private static GUIStyle messageStyle;

        public static Result Draw(bool visible, string characterDisplayName)
        {
            if (!visible)
            {
                return Result.None;
            }

            EnsureStyles();

            GUI.color = new Color(0f, 0f, 0f, 0.62f);
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
                new Rect(dialogRect.x + 20f, dialogRect.y + 16f, dialogRect.width - 40f, 32f),
                "Delete Character",
                titleStyle);

            var displayName = string.IsNullOrWhiteSpace(characterDisplayName)
                ? "this character"
                : characterDisplayName.Trim();

            GUI.Label(
                new Rect(dialogRect.x + 24f, dialogRect.y + 56f, dialogRect.width - 48f, 60f),
                $"Delete {displayName}?",
                messageStyle);

            var choiceY = dialogRect.yMax - ChoiceHeight - 24f;
            var deleteRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f - ChoiceWidth - 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);
            var backRect = new Rect(
                dialogRect.x + dialogRect.width * 0.5f + 12f,
                choiceY,
                ChoiceWidth,
                ChoiceHeight);

            if (StartPageMenuStyles.DrawMenuButton(deleteRect, "DELETE", fontSize: ButtonFontSize))
            {
                return Result.Confirmed;
            }

            if (StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: ButtonFontSize))
            {
                return Result.Back;
            }

            return Result.None;
        }

        private static void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperCenter, Color.black);
            messageStyle = HudStyleFactory.CreateLabel(16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
        }
    }
}
