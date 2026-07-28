using UnityEngine;

namespace F89.UI
{
    public static class CarrierMarkerArt
    {
        private const string ResourcePath = "F89_UssMartinVanBurenCarrier";
        private const float FallbackWidthOverHeight = 0.35f;
        private const float FallbackMinWidthPixels = 18f;
        private static readonly Color FallbackFillColor = new Color(0.78f, 0.78f, 0.78f);
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
            var width = heightPixels * GetWidthOverHeight();
            if (!TryGetTexture(out _))
            {
                width = Mathf.Max(width, FallbackMinWidthPixels);
            }

            return width;
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
            if (TryGetTexture(out var texture))
            {
                GetMarkerRect(guiCenter, heightPixels, out var rect);
                DrawRotatedTexture(rect, texture, guiCenter, rotationDegrees);
                return;
            }

            DrawFallbackMarker(guiCenter, heightPixels);
        }

        private static void DrawFallbackMarker(Vector2 guiCenter, float heightPixels)
        {
            GetMarkerRect(guiCenter, heightPixels, out var rect);
            const float border = 2f;
            var previous = GUI.color;
            GUI.color = FallbackFillColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - border, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
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
