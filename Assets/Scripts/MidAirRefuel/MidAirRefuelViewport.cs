using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Belly window: top edge at 25% of screen, bottom edge at 75% (50% tall aperture).
    /// Opaque tanker interior masks everything above the top edge and below the bottom edge.
    /// </summary>
    public static class MidAirRefuelViewport
    {
        public const int LineCount = 5;
        public const float WindowTopPageFraction = 0.25f;
        public const float WindowBottomPageFraction = 0.75f;

        public static readonly Color InteriorColor = new(0x3F / 255f, 0x23 / 255f, 0x15 / 255f);

        public static float LineHeightPx => Screen.height / (float)LineCount;

        public static float FrameTopPx => Screen.height * WindowTopPageFraction;

        public static float FrameBottomPx => Screen.height * WindowBottomPageFraction;

        public static Rect GetWindowRect()
        {
            return new Rect(0f, FrameTopPx, Screen.width, FrameBottomPx - FrameTopPx);
        }

        public static Rect GetInteriorRect()
        {
            return new Rect(0f, FrameBottomPx, Screen.width, Screen.height - FrameBottomPx);
        }

        public static void DrawFrameMasks()
        {
            GUI.color = InteriorColor;

            if (FrameTopPx > 0f)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, FrameTopPx), Texture2D.whiteTexture);
            }

            if (FrameBottomPx < Screen.height)
            {
                GUI.DrawTexture(
                    new Rect(0f, FrameBottomPx, Screen.width, Screen.height - FrameBottomPx),
                    Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        public static void ClampToWindow(ref Vector2 screenPoint)
        {
            var window = GetWindowRect();
            screenPoint.x = Mathf.Clamp(screenPoint.x, window.xMin + 12f, window.xMax - 12f);
            screenPoint.y = Mathf.Clamp(screenPoint.y, window.yMin + 12f, window.yMax - 12f);
        }
    }

}
