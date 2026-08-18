using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class AircraftDamageWireLibrary
    {
        private const string WireResourcePath = "FlightHud/aircraft_damage_wire";
        private const string Hit1ResourcePath = "FlightHud/aircraft_damage_hit1";
        private const string Hit2ResourcePath = "FlightHud/aircraft_damage_hit2";
        private const string Hit3ResourcePath = "FlightHud/aircraft_damage_hit3";

        private static Texture2D wireTexture;
        private static Texture2D[] damageFillTextures;

        public static Texture2D GetWireTexture()
        {
            if (wireTexture != null)
            {
                return wireTexture;
            }

            var source = Resources.Load<Texture2D>(WireResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"F-89: missing aircraft damage wire at Resources/{WireResourcePath}.");
                return null;
            }

            wireTexture = CreateWireTexture(source);
            return wireTexture;
        }

        public static Texture2D GetDamageFillTexture(int hitsTaken, int sortieHitBudget)
        {
            if (hitsTaken <= 0)
            {
                return null;
            }

            EnsureDamageFillTexturesLoaded();
            if (damageFillTextures == null)
            {
                return null;
            }

            var index = hitsTaken switch
            {
                1 => 0,
                2 => 1,
                _ => sortieHitBudget >= PlayerAircraftGhp.MaxSortieHitBudget ? 2 : 1
            };
            index = Mathf.Clamp(index, 0, damageFillTextures.Length - 1);
            return damageFillTextures[index];
        }

        private static void EnsureDamageFillTexturesLoaded()
        {
            if (damageFillTextures != null)
            {
                return;
            }

            damageFillTextures = new Texture2D[3];
            var paths = new[] { Hit1ResourcePath, Hit2ResourcePath, Hit3ResourcePath };
            for (var i = 0; i < paths.Length; i++)
            {
                damageFillTextures[i] = Resources.Load<Texture2D>(paths[i]);
                if (damageFillTextures[i] == null)
                {
                    Debug.LogWarning($"F-89: missing aircraft damage fill at Resources/{paths[i]}.");
                }
            }
        }

        private static Texture2D CreateWireTexture(Texture2D source)
        {
            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogWarning($"F-89: enable Read/Write on Resources/{WireResourcePath}.");
                return source;
            }

            var width = source.width;
            var height = source.height;
            var luminance = new float[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                luminance[i] = (pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f) / 255f;
            }

            var output = SampleCornerLuminance(width, height, luminance) > 0.5f
                ? BuildLinesFromWhiteBackground(width, height, luminance)
                : BuildLinesFromBlackBackground(width, height, luminance);

            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            copy.SetPixels32(output);
            copy.Apply(false, false);
            return copy;
        }

        private static float SampleCornerLuminance(int width, int height, float[] luminance)
        {
            var samples = new[]
            {
                luminance[0],
                luminance[width - 1],
                luminance[(height - 1) * width],
                luminance[height * width - 1],
                luminance[width + 1],
                luminance[(height - 2) * width + 1]
            };

            var sum = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                sum += samples[i];
            }

            return sum / samples.Length;
        }

        private static Color32[] BuildLinesFromWhiteBackground(int width, int height, float[] luminance)
        {
            const float backgroundThreshold = 0.82f;
            var output = new Color32[width * height];
            for (var i = 0; i < luminance.Length; i++)
            {
                if (luminance[i] >= backgroundThreshold)
                {
                    output[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                var alpha = (byte)Mathf.Clamp(
                    Mathf.RoundToInt((backgroundThreshold - luminance[i]) / backgroundThreshold * 255f),
                    48,
                    255);
                output[i] = new Color32(255, 255, 255, alpha);
            }

            return output;
        }

        private static Color32[] BuildLinesFromBlackBackground(int width, int height, float[] luminance)
        {
            const float brightThreshold = 0.72f;
            const float darkThreshold = 0.42f;
            var output = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var index = y * width + x;
                    output[index] = new Color32(0, 0, 0, 0);
                    if (luminance[index] > brightThreshold
                        || !IsWirePixel(x, y, width, height, luminance, brightThreshold, darkThreshold))
                    {
                        continue;
                    }

                    output[index] = new Color32(255, 255, 255, 255);
                }
            }

            return output;
        }

        private static bool IsWirePixel(
            int x,
            int y,
            int width,
            int height,
            float[] luminance,
            float brightThreshold,
            float darkThreshold)
        {
            var index = y * width + x;
            if (luminance[index] > darkThreshold)
            {
                return false;
            }

            for (var oy = -1; oy <= 1; oy++)
            {
                for (var ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    var nx = x + ox;
                    var ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    if (luminance[ny * width + nx] >= brightThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
