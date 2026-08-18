using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>17-frame vehicle destruction explosion: 7 + 7 + 3 on black (keyed transparent).</summary>
    public static class ExplosionSpriteSheet
    {
        public const string ResourcePath = "Bunker/vehicle_explosion";
        public const float PixelsPerUnit = 48f;
        public const float FramesPerSecond = 14f;

        private static readonly Vector4[] SheetRectsTopLeft =
        {
            new(14, 14, 143, 122), new(157, 14, 143, 122), new(300, 14, 137, 122), new(437, 14, 159, 122),
            new(596, 14, 155, 122), new(751, 14, 147, 122), new(898, 14, 126, 122),
            new(14, 145, 143, 119), new(157, 145, 143, 119), new(300, 145, 137, 119), new(437, 145, 159, 119),
            new(596, 145, 153, 119), new(751, 145, 147, 119), new(898, 145, 124, 119),
            new(11, 291, 134, 120), new(145, 291, 134, 120), new(279, 291, 135, 120)
        };

        private static Sprite[] _frames;
        private static bool _loaded;

        public static Sprite[] Frames
        {
            get
            {
                EnsureLoaded();
                return _frames;
            }
        }

        public static float Duration => SheetRectsTopLeft.Length / FramesPerSecond;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            var sourceTexture = Resources.Load<Texture2D>(ResourcePath);
            if (sourceTexture == null)
            {
                Debug.LogError($"Missing explosion sprite sheet at Resources/{ResourcePath}.");
                _frames = System.Array.Empty<Sprite>();
                _loaded = true;
                return;
            }

            var readable = MakeReadable(sourceTexture);
            KeyBlackBackdrop(readable);
            readable.filterMode = FilterMode.Point;

            _frames = new Sprite[SheetRectsTopLeft.Length];
            for (var i = 0; i < SheetRectsTopLeft.Length; i++)
            {
                var rect = SheetRectsTopLeft[i];
                var unityY = readable.height - rect.y - rect.w;
                _frames[i] = Sprite.Create(
                    readable,
                    new Rect(rect.x, unityY, rect.z, rect.w),
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.Tight);
                _frames[i].name = $"VehicleExplosion_{i:00}";
            }

            _loaded = true;
        }

        private static void KeyBlackBackdrop(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (Mathf.Max(pixel.r, pixel.g, pixel.b) > 20)
                {
                    continue;
                }

                pixel.a = 0;
                pixels[i] = pixel;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static Texture2D MakeReadable(Texture2D source)
        {
            try
            {
                _ = source.GetPixels32();
                return source;
            }
            catch (UnityException)
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
        }
    }
}
