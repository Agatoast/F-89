using UnityEngine;

namespace F89.Flight
{
    public static class ProceduralFlameTexture
    {
        private static Texture2D cachedTexture;

        public static Texture2D Get()
        {
            if (cachedTexture != null)
            {
                return cachedTexture;
            }

            const int width = 64;
            const int height = 128;
            cachedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "ProceduralBlueFlame",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                var core = Mathf.Clamp01(1f - t * 1.15f);
                var flickerBand = Mathf.PerlinNoise(0.15f, t * 6f) * 0.15f;
                core = Mathf.Clamp01(core + flickerBand);

                var alpha = Mathf.Clamp01(Mathf.Pow(1f - t, 1.35f));
                var r = (byte)Mathf.Clamp(255f * (0.35f + core * 0.65f), 0f, 255f);
                var g = (byte)Mathf.Clamp(255f * (0.55f + core * 0.45f), 0f, 255f);
                var b = 255;
                var a = (byte)Mathf.Clamp(255f * alpha * (0.25f + core * 0.85f), 0f, 255f);

                for (var x = 0; x < width; x++)
                {
                    var edge = 1f - Mathf.Abs((x / (width - 1f)) - 0.5f) * 2f;
                    edge = Mathf.Pow(edge, 0.65f);
                    var pixel = pixels[y * width + x];
                    pixel.r = (byte)(r * edge);
                    pixel.g = (byte)(g * edge);
                    pixel.b = (byte)(b * edge);
                    pixel.a = (byte)(a * edge);
                    pixels[y * width + x] = pixel;
                }
            }

            cachedTexture.SetPixels32(pixels);
            cachedTexture.Apply(false, true);
            return cachedTexture;
        }
    }
}
