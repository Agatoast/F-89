using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    /// <summary>Loads centered star-device overlays for multi-award ribbons.</summary>
    public static class MilitaryRibbonDeviceService
    {
        private const string DeviceResourceRoot = "CharacterPage/RibbonDevices/";
        private const byte BlackKeyThreshold = 28;

        private static readonly Dictionary<string, Texture2D> Textures = new();

        public static Texture2D GetDeviceTexture(RibbonAwardDeviceMetal metal, int starCount)
        {
            if (metal == RibbonAwardDeviceMetal.None
                || starCount < 1
                || starCount > MilitaryRibbonAwardDevices.MaxStarsPerMetal)
            {
                return null;
            }

            var key = $"{metal}_{starCount}";
            if (Textures.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var metalName = metal switch
            {
                RibbonAwardDeviceMetal.Silver => "silver",
                RibbonAwardDeviceMetal.Gold => "gold",
                _ => "bronze"
            };

            var resourceName = $"{starCount}_{metalName}_star";
            var loaded = Resources.Load<Texture2D>(DeviceResourceRoot + resourceName);
            if (loaded != null)
            {
                var keyed = CreateKeyedCopy(loaded, metal);
                Textures[key] = keyed;
                return keyed;
            }

            // Fall back to bronze art, recolored for silver/gold until those assets are provided.
            if (metal != RibbonAwardDeviceMetal.Bronze)
            {
                var bronze = Resources.Load<Texture2D>(DeviceResourceRoot + $"{starCount}_bronze_star");
                if (bronze != null)
                {
                    var tinted = CreateKeyedCopy(bronze, metal);
                    Textures[key] = tinted;
                    return tinted;
                }
            }

            Textures[key] = null;
            return null;
        }

        public static void DrawComposition(Rect ribbonRect, RibbonDeviceComposition composition)
        {
            if (!composition.HasDevices)
            {
                return;
            }

            var bounds = GetDeviceBounds(ribbonRect);
            if (bounds.width <= 1f || bounds.height <= 1f)
            {
                return;
            }

            var gap = Mathf.Max(1f, bounds.width * 0.04f);
            var groups = new (RibbonAwardDeviceMetal metal, int starCount)[]
            {
                (RibbonAwardDeviceMetal.Gold, composition.GoldStars),
                (RibbonAwardDeviceMetal.Silver, composition.SilverStars),
                (RibbonAwardDeviceMetal.Bronze, composition.BronzeStars)
            };

            var layouts = new System.Collections.Generic.List<(Texture2D texture, float width)>();
            foreach (var (metal, starCount) in groups)
            {
                if (starCount <= 0)
                {
                    continue;
                }

                var texture = GetDeviceTexture(metal, starCount);
                if (texture == null || texture.width <= 0 || texture.height <= 0)
                {
                    continue;
                }

                var fitHeight = bounds.height;
                var fitWidth = fitHeight * (texture.width / (float)texture.height);
                layouts.Add((texture, fitWidth));
            }

            if (layouts.Count == 0)
            {
                return;
            }

            var totalWidth = 0f;
            for (var i = 0; i < layouts.Count; i++)
            {
                totalWidth += layouts[i].width;
                if (i > 0)
                {
                    totalWidth += gap;
                }
            }

            var scale = totalWidth > bounds.width ? bounds.width / totalWidth : 1f;
            var cursorX = bounds.x + (bounds.width - totalWidth * scale) * 0.5f;
            var drawHeight = bounds.height * scale;
            var drawY = bounds.y + (bounds.height - drawHeight) * 0.5f;

            foreach (var (texture, width) in layouts)
            {
                var drawWidth = width * scale;
                var drawRect = new Rect(cursorX, drawY, drawWidth, drawHeight);
                UiTextureFit.DrawTextureExact(drawRect, texture);
                cursorX += drawWidth + gap * scale;
            }
        }

        private static Rect GetDeviceBounds(Rect ribbonRect)
        {
            var insetX = ribbonRect.width * 0.14f;
            var insetY = ribbonRect.height * 0.19f;
            return new Rect(
                ribbonRect.x + insetX,
                ribbonRect.y + insetY,
                ribbonRect.width - insetX * 2f,
                ribbonRect.height - insetY * 2f);
        }

        public static Rect GetCenteredDeviceRect(Rect ribbonRect, Texture2D deviceTexture)
        {
            if (deviceTexture == null || ribbonRect.width <= 1f || ribbonRect.height <= 1f)
            {
                return Rect.zero;
            }

            return UiTextureFit.FitInRect(GetDeviceBounds(ribbonRect), deviceTexture);
        }

        public static void ClearCache()
        {
            foreach (var pair in Textures)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            Textures.Clear();
        }

        private static Texture2D CreateKeyedCopy(Texture2D source, RibbonAwardDeviceMetal metal)
        {
            if (source == null)
            {
                return null;
            }

            Color32[] pixels;
            try
            {
                if (!source.isReadable)
                {
                    // Non-readable import: draw via temporary RenderTexture.
                    return CreateViaBlit(source, metal);
                }

                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                return CreateViaBlit(source, metal);
            }

            ApplyKeyAndTint(pixels, metal);
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = $"RibbonDevice_{metal}_{source.width}x{source.height}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }

        private static Texture2D CreateViaBlit(Texture2D source, RibbonAwardDeviceMetal metal)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = readable.GetPixels32();
            UnityEngine.Object.Destroy(readable);
            ApplyKeyAndTint(pixels, metal);

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = $"RibbonDevice_{metal}_{source.width}x{source.height}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }

        private static void ApplyKeyAndTint(Color32[] pixels, RibbonAwardDeviceMetal metal)
        {
            Color32 tint = metal switch
            {
                RibbonAwardDeviceMetal.Silver => new Color32(210, 214, 220, 255),
                RibbonAwardDeviceMetal.Gold => new Color32(228, 190, 72, 255),
                _ => new Color32(255, 255, 255, 255)
            };

            var tintMetal = metal != RibbonAwardDeviceMetal.Bronze;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.r <= BlackKeyThreshold && pixel.g <= BlackKeyThreshold && pixel.b <= BlackKeyThreshold)
                {
                    pixel.a = 0;
                    pixels[i] = pixel;
                    continue;
                }

                if (!tintMetal)
                {
                    continue;
                }

                // Preserve luminance, apply metal hue.
                var luma = (pixel.r * 0.35f + pixel.g * 0.45f + pixel.b * 0.20f) / 255f;
                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(tint.r * luma), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(tint.g * luma), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(tint.b * luma), 0, 255),
                    pixel.a);
            }
        }
    }
}
