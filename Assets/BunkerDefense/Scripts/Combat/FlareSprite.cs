using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>F-89 procedural flare burn streak adapted for 2D vehicle rockets.</summary>
    public static class FlareSprite
    {
        public const float PixelsPerUnit = 48f;

        private static Sprite _streak;
        private static Sprite _bloom;

        public static Sprite Streak
        {
            get
            {
                _streak ??= CreateStreak();
                return _streak;
            }
        }

        public static Sprite Bloom
        {
            get
            {
                _bloom ??= CreateBloom();
                return _bloom;
            }
        }

        private static Sprite CreateStreak()
        {
            const int width = 64;
            const int height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                var core = Mathf.Clamp01(1f - t * 1.05f);
                var flickerBand = Mathf.PerlinNoise(0.42f, t * 8f) * 0.22f;
                core = Mathf.Clamp01(core + flickerBand);

                var alpha = Mathf.Clamp01(Mathf.Pow(1f - t * 0.85f, 1.1f));
                var r = (byte)Mathf.Clamp(255f * (0.85f + core * 0.15f), 0f, 255f);
                var g = (byte)Mathf.Clamp(255f * (0.45f + core * 0.55f), 0f, 255f);
                var b = (byte)Mathf.Clamp(255f * core * 0.25f, 0f, 255f);
                var a = (byte)Mathf.Clamp(255f * alpha * (0.35f + core * 0.95f), 0f, 255f);

                for (var x = 0; x < width; x++)
                {
                    var edge = 1f - Mathf.Abs((x / (width - 1f)) - 0.5f) * 2f;
                    edge = Mathf.Pow(edge, 0.55f);
                    pixels[y * width + x] = new Color32(
                        (byte)(r * edge),
                        (byte)(g * edge),
                        (byte)(b * edge),
                        (byte)(a * edge));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.Tight);
        }

        private static Sprite CreateBloom()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.48f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / radius;
                    var dy = (y - center) / radius;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var core = Mathf.Clamp01(1f - dist);
                    core = Mathf.Pow(core, 0.55f);
                    var ring = Mathf.Clamp01(1f - Mathf.Abs(dist - 0.55f) * 4f) * 0.35f;
                    var brightness = Mathf.Clamp01(core + ring);
                    var noise = Mathf.PerlinNoise(x * 0.11f, y * 0.11f) * 0.18f;
                    brightness = Mathf.Clamp01(brightness + noise);

                    var alpha = Mathf.Clamp01(Mathf.Pow(1f - dist, 1.35f) * (0.25f + brightness * 0.85f));
                    var r = (byte)Mathf.Clamp(255f * (0.75f + brightness * 0.25f), 0f, 255f);
                    var g = (byte)Mathf.Clamp(255f * (0.28f + brightness * 0.62f), 0f, 255f);
                    var b = (byte)Mathf.Clamp(255f * brightness * 0.18f, 0f, 255f);
                    pixels[y * size + x] = new Color32(r, g, b, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.Tight);
        }
    }
}
