using UnityEngine;

namespace F89.UI
{
    public static class PortraitDisplayUtility
    {
        public static void DrawPortraitFrame(Rect frameRect, Texture2D portraitTexture, string promptText, GUIStyle promptStyle)
        {
            GUI.BeginGroup(frameRect);

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, frameRect.width, frameRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (portraitTexture != null && portraitTexture.width > 0 && portraitTexture.height > 0)
            {
                var drawRect = GetCoverRectWithoutUpscale(
                    new Rect(0f, 0f, frameRect.width, frameRect.height),
                    portraitTexture.width,
                    portraitTexture.height);
                GUI.DrawTexture(drawRect, portraitTexture, ScaleMode.StretchToFill, false);
            }
            else if (!string.IsNullOrEmpty(promptText) && promptStyle != null)
            {
                GUI.Label(new Rect(0f, 0f, frameRect.width, frameRect.height), promptText, promptStyle);
            }

            GUI.EndGroup();
        }

        public static Rect GetCoverRectWithoutUpscale(Rect bounds, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f)
            {
                return bounds;
            }

            var scale = Mathf.Max(bounds.width / sourceWidth, bounds.height / sourceHeight);
            scale = Mathf.Min(scale, 1f);

            var drawWidth = sourceWidth * scale;
            var drawHeight = sourceHeight * scale;
            var drawX = bounds.x + (bounds.width - drawWidth) * 0.5f;
            var drawY = bounds.y + (bounds.height - drawHeight) * 0.5f;
            return new Rect(drawX, drawY, drawWidth, drawHeight);
        }
    }
}
