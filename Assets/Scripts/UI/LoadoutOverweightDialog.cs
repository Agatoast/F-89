using UnityEngine;

namespace F89.UI
{
    public static class LoadoutOverweightDialog
    {
        private const float DialogWidth = 520f;
        private const float DialogHeight = 160f;
        private const float OkWidth = 120f;
        private const float OkHeight = 40f;

        private static GUIStyle messageStyle;
        private static GUIStyle buttonStyle;

        public static bool Draw(bool visible)
        {
            if (!visible)
            {
                return false;
            }

            EnsureStyles();
            Cursor.visible = true;

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
                new Rect(dialogRect.x + 24f, dialogRect.y + 28f, dialogRect.width - 48f, 60f),
                "Can't place weapon. Over payload.",
                messageStyle);

            var okRect = new Rect(
                dialogRect.x + (dialogRect.width - OkWidth) * 0.5f,
                dialogRect.yMax - OkHeight - 24f,
                OkWidth,
                OkHeight);

            HudGuiUtility.DrawWireBox(okRect, 2f);
            GUI.Label(okRect, "OK", buttonStyle);

            return GUI.Button(okRect, GUIContent.none, GUIStyle.none);
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
