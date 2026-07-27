using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Short centered HUD notice (e.g. "BANDAGES FULL").</summary>
    public static class LandHudNotice
    {
        private const float DisplaySeconds = 1.8f;

        private static string message = string.Empty;
        private static float hideAt;

        public static void Show(string text)
        {
            message = text ?? string.Empty;
            hideAt = Time.unscaledTime + DisplaySeconds;
        }

        public static void Draw()
        {
            if (string.IsNullOrEmpty(message) || Time.unscaledTime >= hideAt)
            {
                return;
            }

            var style = HudStyleFactory.CreateLabel(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var size = style.CalcSize(new GUIContent(message));
            var width = Mathf.Clamp(size.x + 36f, 280f, Screen.width * 0.9f);
            var height = Mathf.Max(36f, size.y + 12f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.42f, width, height);
            var previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, message, style);
        }
    }
}
