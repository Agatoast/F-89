using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Vehicle/building explosion sheet — 17 frames in a 7×3 grid (last row has 3 frames).
    /// Black background keyed to transparent at load.
    /// </summary>
    public static class ExplosionSpriteSheet
    {
        public const string ResourcePath = "Vehicles/explosion_sprites";
        public const float PixelsPerUnit = 100f;
        public const float AnimationFps = 14f;
        public const int ColumnCount = 7;
        public const int RowCount = 3;
        public const int FrameCount = 17;

        private static Sprite[] frames;
        private static bool loaded;
        private static Texture2D keyedTexture;

        public static bool TryGetFrames(out Sprite[] spriteFrames)
        {
            EnsureLoaded();
            spriteFrames = frames;
            return spriteFrames != null && spriteFrames.Length > 0;
        }

        public static bool TryGetFirstFrame(out Sprite sprite)
        {
            EnsureLoaded();
            if (frames != null && frames.Length > 0)
            {
                sprite = frames[0];
                return sprite != null;
            }

            sprite = null;
            return false;
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            frames = null;

            var source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"F-89: Missing explosion sprite sheet at Resources/{ResourcePath}.");
                return;
            }

            keyedTexture = CreateBlackKeyedCopy(source);
            if (keyedTexture == null)
            {
                return;
            }

            keyedTexture.filterMode = FilterMode.Bilinear;
            keyedTexture.wrapMode = TextureWrapMode.Clamp;

            // Integer cell edges avoid float overflow on the last column/row
            // (e.g. 1024/7 * 7 can exceed texture width by a fraction of a pixel).
            var texWidth = keyedTexture.width;
            var texHeight = keyedTexture.height;
            frames = new Sprite[FrameCount];
            var frameIndex = 0;

            for (var row = 0; row < RowCount && frameIndex < FrameCount; row++)
            {
                var columnsThisRow = row < RowCount - 1 ? ColumnCount : 3;
                var yMax = texHeight - row * texHeight / RowCount;
                var yMin = texHeight - (row + 1) * texHeight / RowCount;
                var cellHeight = yMax - yMin;
                for (var col = 0; col < columnsThisRow && frameIndex < FrameCount; col++)
                {
                    var xMin = col * texWidth / ColumnCount;
                    var xMax = (col + 1) * texWidth / ColumnCount;
                    var cellWidth = xMax - xMin;
                    var rect = new Rect(xMin, yMin, cellWidth, cellHeight);
                    frames[frameIndex] = Sprite.Create(
                        keyedTexture,
                        rect,
                        new Vector2(0.5f, 0.5f),
                        PixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect);
                    frames[frameIndex].name = $"Explosion_{frameIndex:00}";
                    frameIndex++;
                }
            }
        }

        private static Texture2D CreateBlackKeyedCopy(Texture2D source)
        {
            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogError(
                    $"F-89: Explosion sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
                return null;
            }

            const byte blackThreshold = 24;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.r <= blackThreshold && pixel.g <= blackThreshold && pixel.b <= blackThreshold)
                {
                    pixel.a = 0;
                    pixels[i] = pixel;
                }
            }

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "ExplosionSprites_Keyed",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }
    }
}
