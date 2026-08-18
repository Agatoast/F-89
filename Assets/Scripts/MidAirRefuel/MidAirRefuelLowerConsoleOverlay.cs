using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Decorative lower tanker console strip filling the brown interior below the belly window.
    /// Top edge sits 10px below the window bottom border, scaled edge-to-edge without stretch.
    /// </summary>
    public static class MidAirRefuelLowerConsoleOverlay
    {
        private const string LowerConsolePath = "MidAirRefuel/lower_refuel_console";
        private const float GapBelowWindowPx = 10f;

        private static Texture2D lowerConsoleTexture;

        public static bool TryGetBandRect(out Rect rect)
        {
            var top = MidAirRefuelViewport.FrameBottomPx + GapBelowWindowPx;
            var height = Screen.height - top;
            if (height <= 1f)
            {
                rect = default;
                return false;
            }

            rect = new Rect(0f, top, Screen.width, height);
            return true;
        }

        public static bool TryGetDrawRect(out Rect rect)
        {
            EnsureLoaded();
            if (lowerConsoleTexture == null || !TryGetBandRect(out var band))
            {
                rect = default;
                return false;
            }

            var aspect = lowerConsoleTexture.height / (float)lowerConsoleTexture.width;
            var width = band.width;
            var height = width * aspect;
            var y = band.y;
            if (height > band.height)
            {
                height = band.height;
                width = height / aspect;
            }

            rect = new Rect(
                band.x + (band.width - width) * 0.5f,
                y,
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

            if (!TryGetDrawRect(out var rect))
            {
                return;
            }

            GUI.color = Color.white;
            GUI.DrawTexture(rect, lowerConsoleTexture, ScaleMode.ScaleToFit, alphaBlend: true);
        }

        private static void EnsureLoaded()
        {
            if (lowerConsoleTexture != null)
            {
                return;
            }

            lowerConsoleTexture = Resources.Load<Texture2D>(LowerConsolePath);
            if (lowerConsoleTexture == null)
            {
                Debug.LogWarning($"F-89 MA Refuel: Missing lower console at Resources/{LowerConsolePath}.");
            }
        }
    }
}
