using UnityEngine;

namespace F89.MidAirRefuel
{
    /// <summary>
    /// Seamless antarctica ground scroll — fills the belly window at native aspect, no gaps.
    /// </summary>
    public sealed class MidAirRefuelGroundScroller
    {
        private const float DefaultScrollSpeedPx = 220f;

        private float totalScrollPx;

        public float ScrollSpeedPx { get; set; } = DefaultScrollSpeedPx;

        public void Reset()
        {
            totalScrollPx = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            totalScrollPx += ScrollSpeedPx * deltaTime;
        }

        public void Draw(Rect windowRect)
        {
            if (windowRect.height <= 1f || windowRect.width <= 1f)
            {
                return;
            }

            GUI.BeginGroup(windowRect);
            var localRect = new Rect(0f, 0f, windowRect.width, windowRect.height);

            var frames = MidAirRefuelArtCatalog.GroundFrames;
            if (frames == null || frames.Length == 0)
            {
                DrawScrollingPlaceholder(localRect);
                GUI.EndGroup();
                return;
            }

            if (frames.Length == 1)
            {
                DrawRepeatingTexture(localRect, frames[0]);
            }
            else
            {
                var tileHeight = GetTileHeight(localRect, frames[0]);
                if (tileHeight <= 1f)
                {
                    tileHeight = localRect.height;
                }

                DrawScrollingFilmstrip(localRect, frames, tileHeight);
            }

            GUI.EndGroup();
        }

        /// <summary>
        /// Texture repeat at correct aspect — always covers the full band (no gray gaps).
        /// </summary>
        private void DrawRepeatingTexture(Rect bandRect, Texture2D texture)
        {
            var tileHeight = GetTileHeight(bandRect, texture);
            if (tileHeight <= 1f)
            {
                tileHeight = bandRect.height;
            }

            texture.wrapMode = TextureWrapMode.Repeat;

            var scrollNorm = (totalScrollPx % tileHeight) / tileHeight;
            var visibleTiles = bandRect.height / tileHeight;
            var texCoords = new Rect(0f, 1f - scrollNorm, 1f, visibleTiles);
            GUI.DrawTextureWithTexCoords(bandRect, texture, texCoords, alphaBlend: false);
        }

        private void DrawScrollingFilmstrip(Rect bandRect, Texture2D[] frames, float tileHeight)
        {
            var frameCount = frames.Length;
            var loopHeight = tileHeight * frameCount;
            var offset = totalScrollPx % loopHeight;
            var bottom = bandRect.yMax - offset;

            while (bottom < bandRect.yMax + tileHeight)
            {
                bottom += tileHeight;
            }

            var startIndex = Mathf.FloorToInt(totalScrollPx / tileHeight);

            while (bottom > bandRect.y - tileHeight)
            {
                var frame = frames[Mod(startIndex, frameCount)];
                var y = bottom - tileHeight;
                var rect = new Rect(bandRect.x, y, bandRect.width, tileHeight + 1f);
                GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, alphaBlend: false);
                bottom -= tileHeight;
                startIndex--;
            }
        }

        private void DrawScrollingPlaceholder(Rect bandRect)
        {
            var tileHeight = Mathf.Max(bandRect.height * 0.5f, 24f);
            var offset = totalScrollPx % tileHeight;
            var bottom = bandRect.yMax - offset;

            while (bottom < bandRect.yMax + tileHeight)
            {
                bottom += tileHeight;
            }

            var tileIndex = 0;

            while (bottom > bandRect.y - tileHeight)
            {
                var shade = tileIndex % 2 == 0
                    ? new Color(0.36f, 0.34f, 0.28f)
                    : new Color(0.44f, 0.41f, 0.34f);
                GUI.color = shade;
                var rect = new Rect(bandRect.x, bottom - tileHeight, bandRect.width, tileHeight + 1f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                bottom -= tileHeight;
                tileIndex++;
            }

            GUI.color = Color.white;
        }

        private static float GetTileHeight(Rect bandRect, Texture2D frame)
        {
            if (frame == null || frame.width <= 0)
            {
                return bandRect.height;
            }

            return bandRect.width * (frame.height / (float)frame.width);
        }

        private static int Mod(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var mod = value % count;
            return mod < 0 ? mod + count : mod;
        }
    }
}
