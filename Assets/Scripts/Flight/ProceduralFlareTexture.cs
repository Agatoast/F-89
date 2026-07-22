using UnityEngine;

namespace F89.Flight
{
    public static class ProceduralFlareTexture
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
                name = "ProceduralFlareBurn",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
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

            cachedTexture.SetPixels32(pixels);
            cachedTexture.Apply(false, true);
            return cachedTexture;
        }
    }
}
