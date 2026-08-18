using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Computes IMGUI draw rects that preserve a texture's aspect ratio inside a bounding box.
    /// </summary>
    public static class UiTextureFit
    {
        public static Rect FitInRect(Rect bounds, Texture2D texture, float inset = 0f)
        {
            var maxWidth = Mathf.Max(0f, bounds.width - inset * 2f);
            var maxHeight = Mathf.Max(0f, bounds.height - inset * 2f);
            var anchorX = bounds.x + inset;
            var anchorY = bounds.y + inset;

            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return new Rect(anchorX, anchorY, maxWidth, maxHeight);
            }

            var aspect = (float)texture.height / texture.width;
            var width = maxWidth;
            var height = width * aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height / aspect;
            }

            return new Rect(
                anchorX + (maxWidth - width) * 0.5f,
                anchorY + (maxHeight - height) * 0.5f,
                width,
                height);
        }

        public static void DrawTexture(Rect bounds, Texture2D texture, float inset = 0f, bool alphaBlend = true)
        {
            if (texture == null)
            {
                return;
            }

            var drawRect = FitInRect(bounds, texture, inset);
            DrawTextureExact(drawRect, texture, alphaBlend);
        }

        /// <summary>Draws into a rect that is already aspect-correct for the texture.</summary>
        public static void DrawTextureExact(Rect drawRect, Texture2D texture, bool alphaBlend = true)
        {
            if (texture == null)
            {
                return;
            }

            GUI.DrawTexture(drawRect, texture, ScaleMode.ScaleToFit, alphaBlend);
        }
    }
}
