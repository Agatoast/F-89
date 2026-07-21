using UnityEngine;

namespace F89.UI
{
    public static class CarrierMarkerArt
    {
        private const string ResourcePath = "F89_UssMartinVanBurenCarrier";
        private const float FallbackWidthOverHeight = 0.3f;
        private const float FlightViewHeightPixels = 88f;

        private static Texture2D carrierTexture;

        public static float FlightViewHeight => FlightViewHeightPixels;

        public static bool TryGetTexture(out Texture2D texture)
        {
            if (carrierTexture == null)
            {
                carrierTexture = Resources.Load<Texture2D>(ResourcePath);
            }

            texture = carrierTexture;
            return texture != null;
        }

        public static float GetWidthOverHeight()
        {
            if (!TryGetTexture(out var texture) || texture.height <= 0)
            {
                return FallbackWidthOverHeight;
            }

            return texture.width / (float)texture.height;
        }

        public static float GetWidthForHeight(float heightPixels)
        {
            return heightPixels * GetWidthOverHeight();
        }

        public static void GetMarkerRect(Vector2 guiCenter, float heightPixels, out Rect rect)
        {
            var width = GetWidthForHeight(heightPixels);
            rect = new Rect(
                guiCenter.x - width * 0.5f,
                guiCenter.y - heightPixels * 0.5f,
                width,
                heightPixels);
        }

        public static void DrawNorthUpMarker(Vector2 guiCenter, float heightPixels, float rotationDegrees = 0f)
        {
            if (!TryGetTexture(out var texture))
            {
                return;
            }

            GetMarkerRect(guiCenter, heightPixels, out var rect);
            DrawRotatedTexture(rect, texture, guiCenter, rotationDegrees);
        }

        private static void DrawRotatedTexture(Rect rect, Texture2D texture, Vector2 pivot, float rotationDegrees)
        {
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUI.color = Color.white;

            if (Mathf.Abs(rotationDegrees) > 0.01f)
            {
                GUIUtility.RotateAroundPivot(rotationDegrees, pivot);
            }

            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
    }
}
