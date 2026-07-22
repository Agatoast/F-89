using UnityEngine;

namespace F89.Flight
{
    public static class ProceduralFlareBloomTexture
    {
        private static Texture2D cachedTexture;

        public static Texture2D Get()
        {
            if (cachedTexture != null)
            {
                return cachedTexture;
            }

            const int size = 128;
            cachedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ProceduralFlareBloom",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
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

            cachedTexture.SetPixels32(pixels);
            cachedTexture.Apply(false, true);
            return cachedTexture;
        }
    }
}
