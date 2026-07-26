using UnityEngine;

namespace F89.UI
{
    public static class ResearchDestroyConfirmDialog
    {
        public enum Result
        {
            None = 0,
            Confirmed = 1,
            Cancelled = 2
        }

        private const float DialogWidth = 520f;
        private const float DialogHeight = 180f;
        private const float ChoiceWidth = 120f;
        private const float ChoiceHeight = 40f;

        private static GUIStyle messageStyle;
        private static GUIStyle buttonStyle;

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
                new Rect(dialogRect.x + 24f, dialogRect.y + 28f, dialogRect.width - 48f, 70f),
                "Destroy this item for R&D? It cannot be recovered.",
                messageStyle);

            var choiceY = dialogRect.yMax - ChoiceHeight - 24f;
            var yesRect = new Rect(dialogRect.x + dialogRect.width * 0.5f - ChoiceWidth - 12f, choiceY, ChoiceWidth, ChoiceHeight);
            var noRect = new Rect(dialogRect.x + dialogRect.width * 0.5f + 12f, choiceY, ChoiceWidth, ChoiceHeight);

            DrawChoiceButton(yesRect, "Yes");
            DrawChoiceButton(noRect, "No");

            if (GUI.Button(yesRect, GUIContent.none, GUIStyle.none))
            {
                return Result.Confirmed;
            }

            if (GUI.Button(noRect, GUIContent.none, GUIStyle.none))
            {
                return Result.Cancelled;
            }

            return Result.None;
        }

        private static void DrawChoiceButton(Rect rect, string label)
        {
            HudGuiUtility.DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
        }

        private static void EnsureStyles()
        {
            if (messageStyle != null)
            {
                return;
            }

            messageStyle = HudStyleFactory.CreateLabel(16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black, wordWrap: true);
            buttonStyle = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
        }
    }
}
