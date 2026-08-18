using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Upper tanker console strip centered above the belly window.
    /// Bottom edge sits 10px above the window top border.
    /// </summary>
    public static class MidAirRefuelUpperConsoleOverlay
    {
        private const string UpperConsolePath = "MidAirRefuel/upper_refuel_console";
        private const float GapAboveWindowPx = 10f;
        private const float MaxScreenWidthFraction = 0.92f;

        private static Texture2D upperConsoleTexture;

        public static bool TryGetConsoleRect(out Rect rect)
        {
            EnsureLoaded();
            if (upperConsoleTexture == null)
            {
                rect = default;
                return false;
            }

            var windowTop = MidAirRefuelViewport.FrameTopPx;
            var bottom = windowTop - GapAboveWindowPx;
            var maxHeight = Mathf.Max(1f, bottom);
            var aspect = upperConsoleTexture.height / (float)upperConsoleTexture.width;

            var width = Screen.width * MaxScreenWidthFraction;
            var height = width * aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height / aspect;
            }

            rect = new Rect(
                (Screen.width - width) * 0.5f,
                bottom - height,
                width,
                height);
            return true;
        }

        public static void Draw()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!TryGetConsoleRect(out var rect))
            {
                return;
            }

            GUI.color = Color.white;
            GUI.DrawTexture(rect, upperConsoleTexture, ScaleMode.ScaleToFit, alphaBlend: true);
        }

        private static void EnsureLoaded()
        {
            if (upperConsoleTexture != null)
            {
                return;
            }

            upperConsoleTexture = Resources.Load<Texture2D>(UpperConsolePath);
            if (upperConsoleTexture == null)
            {
                Debug.LogWarning($"F-89 MA Refuel: Missing upper console at Resources/{UpperConsolePath}.");
            }
        }
    }
}
