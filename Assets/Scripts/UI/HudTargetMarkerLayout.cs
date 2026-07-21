using UnityEngine;

namespace F89.UI
{
    public static class HudTargetMarkerLayout
    {
        public const float SquareSize = 32f;
        public const float DiamondSize = 20f;
        public const float PickPadding = 8f;

        public static Vector2 ScreenToGui(Vector2 screenBottomLeft)
        {
            return new Vector2(screenBottomLeft.x, Screen.height - screenBottomLeft.y);
        }

        public static bool TryGetGuiCenter(Camera camera, Vector3 worldPosition, out Vector2 guiCenter)
        {
            guiCenter = default;
            if (camera == null)
            {
                return false;
            }

            var screen = camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f)
            {
                return false;
            }

            guiCenter = new Vector2(screen.x, Screen.height - screen.y);
            return true;
        }

        public static bool IsPointInsideSquare(Vector2 screenBottomLeft, Vector2 guiCenter, float halfSize)
        {
            var guiPoint = ScreenToGui(screenBottomLeft);
            return Mathf.Abs(guiPoint.x - guiCenter.x) <= halfSize
                && Mathf.Abs(guiPoint.y - guiCenter.y) <= halfSize;
        }
    }
}
