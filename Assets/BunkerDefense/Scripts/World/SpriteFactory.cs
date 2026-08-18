using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public static class SpriteFactory
    {
        public static Sprite Solid(Color color, int width = 8, int height = 8)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            var packed = (Color32)color;
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = packed;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite Circle(Color color, int size = 32)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center - 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var t = 1f - Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    var alpha = Mathf.Clamp01(t * 6f);
                    var c = color;
                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>25x25 aim reticle: center dot, 3x3 clear gap, cardinal arms, green side ticks.</summary>
        public static Sprite Reticle25()
        {
            const int size = 25;
            const int center = 12;
            const int armLength = 11;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color32(0, 0, 0, 0);
            var black = new Color32(0, 0, 0, 255);
            var green = new Color32(0, 238, 0, 255);
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            pixels[center * size + center] = black;

            var upEnd = center - 2;
            var upStart = upEnd - (armLength - 1);
            var downStart = center + 2;
            var downEnd = downStart + (armLength - 1);
            var leftEnd = center - 2;
            var leftStart = leftEnd - (armLength - 1);
            var rightStart = center + 2;
            var rightEnd = rightStart + (armLength - 1);

            for (var y = upStart; y <= upEnd; y++)
            {
                pixels[y * size + center] = black;
                pixels[y * size + (center - 1)] = green;
                pixels[y * size + (center + 1)] = green;
            }

            for (var y = downStart; y <= downEnd; y++)
            {
                pixels[y * size + center] = black;
                pixels[y * size + (center - 1)] = green;
                pixels[y * size + (center + 1)] = green;
            }

            for (var x = leftStart; x <= leftEnd; x++)
            {
                pixels[center * size + x] = black;
                pixels[(center - 1) * size + x] = green;
                pixels[(center + 1) * size + x] = green;
            }

            for (var x = rightStart; x <= rightEnd; x++)
            {
                pixels[center * size + x] = black;
                pixels[(center - 1) * size + x] = green;
                pixels[(center + 1) * size + x] = green;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 1f);
        }

        public static Sprite VerticalGradient(Color bottom, Color top, int width = 16, int height = 64)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)(height - 1);
                var color = (Color32)Color.Lerp(bottom, top, t);
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
