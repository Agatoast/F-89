using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Letterboxes full-screen page art and maps layout norms/pixel offsets into that rect
    /// so overlays stay locked to baked background features across resolutions/aspects.
    /// </summary>
    public static class UiFitCanvas
    {
        public const float ReferenceWidthPx = 1920f;

        public static Rect Rect { get; private set; } = new Rect(0f, 0f, ReferenceWidthPx, 1080f);

        public static float Scale { get; private set; } = 1f;

        public static void Begin(Texture2D art)
        {
            if (art != null && art.width > 0 && art.height > 0)
            {
                Begin((float)art.width / art.height);
                return;
            }

            Begin((float)Screen.width / Mathf.Max(1, Screen.height));
        }

        public static void Begin(float artAspect)
        {
            Rect = GetLetterboxedRect(artAspect);
            Scale = Rect.width / ReferenceWidthPx;
        }

        public static float Px(float designPixels) => designPixels * Scale;

        public static Rect NormRect(float xNorm, float yNorm, float wNorm, float hNorm)
        {
            return new Rect(
                Rect.x + xNorm * Rect.width,
                Rect.y + yNorm * Rect.height,
                wNorm * Rect.width,
                hNorm * Rect.height);
        }

        public static float NormX(float xNorm) => Rect.x + xNorm * Rect.width;

        public static float NormY(float yNorm) => Rect.y + yNorm * Rect.height;

        public static void DrawLetterboxedBackground(Texture2D art)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (art == null)
            {
                return;
            }

            Begin(art);
            GUI.DrawTexture(Rect, art, ScaleMode.StretchToFill, true);
        }

        public static Rect GetLetterboxedRect(float artAspect)
        {
            if (artAspect <= 0.01f)
            {
                return new Rect(0f, 0f, Screen.width, Screen.height);
            }

            var screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (artAspect > screenAspect)
            {
                var height = Screen.width / artAspect;
                return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
            }

            var width = Screen.height * artAspect;
            return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
        }
    }
}
