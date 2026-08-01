using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// TDP saucer sheet — 3×3 top-down rotation frames on black. Black keyed to transparent at load.
    /// </summary>
    public static class UrTdpSpriteSheet
    {
        public const string ResourcePath = "Vehicles/UR/tdp_sprites";
        public const string Abbreviation = "TDP";
        public const float PixelsPerUnit = 100f;
        public const float AnimationFps = 12f;

        // Top-left origin: x, y, width, height (row-major, clockwise red-light cycle).
        private static readonly Vector4[] FrameRectsTopLeft =
        {
            new Vector4(7, 15, 294, 295),
            new Vector4(346, 23, 294, 293),
            new Vector4(687, 23, 295, 294),
            new Vector4(3, 353, 293, 295),
            new Vector4(353, 352, 294, 294),
            new Vector4(691, 367, 295, 293),
            new Vector4(2, 691, 296, 294),
            new Vector4(348, 686, 293, 294),
            new Vector4(717, 699, 294, 295)
        };

        private static Sprite[] frames;
        private static bool loaded;
        private static Texture2D keyedTexture;

        public static Sprite[] GetFrames()
        {
            EnsureLoaded();
            return frames ?? System.Array.Empty<Sprite>();
        }

        public static bool TryGetFrames(out Sprite[] spriteFrames)
        {
            EnsureLoaded();
            spriteFrames = frames;
            return spriteFrames != null && spriteFrames.Length > 0;
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            var source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogError($"F-89: Missing TDP sprite sheet at Resources/{ResourcePath}.");
                frames = System.Array.Empty<Sprite>();
                return;
            }

            keyedTexture = CreateBlackKeyedCopy(source);
            if (keyedTexture == null)
            {
                frames = System.Array.Empty<Sprite>();
                return;
            }

            frames = new Sprite[FrameRectsTopLeft.Length];
            for (var i = 0; i < FrameRectsTopLeft.Length; i++)
            {
                var r = FrameRectsTopLeft[i];
                var width = r.z;
                var height = r.w;
                var unityY = keyedTexture.height - r.y - height;
                var rect = new Rect(r.x, unityY, width, height);
                if (rect.xMin < 0f
                    || rect.yMin < 0f
                    || rect.xMax > keyedTexture.width + 0.01f
                    || rect.yMax > keyedTexture.height + 0.01f)
                {
                    Debug.LogWarning($"F-89: TDP frame {i} rect out of bounds: {rect}");
                    continue;
                }

                var sprite = Sprite.Create(
                    keyedTexture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = $"UR_TDP_{i}";
                frames[i] = sprite;
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
                    $"F-89: TDP sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
                return null;
            }

            const byte blackThreshold = 12;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.r <= blackThreshold && p.g <= blackThreshold && p.b <= blackThreshold)
                {
                    p.a = 0;
                    pixels[i] = p;
                }
            }

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "UR_TDP_Keyed",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }
    }
}
