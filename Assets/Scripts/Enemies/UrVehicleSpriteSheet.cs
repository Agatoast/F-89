using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// UR vehicle sheet — three views per row (north / three-quarter / side). Side rects drive flight
    /// fallbacks; north-facing rects feed character-page kill folder boxes. Flight map ground units use
    /// <see cref="UrTopDownVehicleSpriteSheet"/> or <see cref="UrArwSpriteSheet"/>; TDP uses
    /// <see cref="UrTdpSpriteSheet"/>.
    /// </summary>
    public static class UrVehicleSpriteSheet
    {
        public const string ResourcePath = "Vehicles/UR/ur_tanks_sprites";
        public const float PixelsPerUnit = 100f;
        private const int LabelStripWidthPx = 130;

        private readonly struct VehicleSheetSlot
        {
            public VehicleSheetSlot(string abbrev, int x0, int x1, int y0, int y1)
            {
                Abbrev = abbrev;
                X0 = x0;
                X1 = x1;
                Y0 = y0;
                Y1 = y1;
            }

            public string Abbrev { get; }
            public int X0 { get; }
            public int X1 { get; }
            public int Y0 { get; }
            public int Y1 { get; }
        }

        // Row-major 4×2 layout on ur_tanks_sprites (matches Tools/_extract_ur_side.py).
        private static readonly VehicleSheetSlot[] NorthSlots =
        {
            new VehicleSheetSlot("MBT", 0, 512, 17, 129),
            new VehicleSheetSlot("AH", 512, 1024, 17, 129),
            new VehicleSheetSlot("HCT", 0, 512, 157, 263),
            new VehicleSheetSlot("PHT", 512, 1024, 157, 263),
            new VehicleSheetSlot("FW", 0, 512, 286, 387),
            new VehicleSheetSlot("VHS", 512, 1024, 286, 387),
            new VehicleSheetSlot("HAR", 0, 512, 405, 508),
            new VehicleSheetSlot("MC", 512, 1024, 405, 508)
        };

        // Top-left origin rects: x, y, width, height (side view).
        private static readonly Dictionary<string, Vector4> SideRectsTopLeft =
            new Dictionary<string, Vector4>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "MBT", new Vector4(369, 17, 121, 95) },
                { "AH", new Vector4(881, 36, 111, 80) },
                { "HCT", new Vector4(369, 171, 118, 86) },
                { "PHT", new Vector4(881, 163, 111, 92) },
                { "FW", new Vector4(369, 305, 111, 71) },
                { "VHS", new Vector4(881, 292, 111, 91) },
                { "HAR", new Vector4(369, 416, 110, 82) },
                { "MC", new Vector4(881, 422, 111, 82) }
            };

        private static readonly Dictionary<string, Sprite> sprites =
            new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Sprite> northSprites =
            new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        private static bool loaded;
        private static Texture2D keyedTexture;

        public static bool TryGetSideSprite(string abbreviation, out Sprite sprite)
        {
            EnsureLoaded();
            sprite = null;
            if (string.IsNullOrWhiteSpace(abbreviation))
            {
                return false;
            }

            var normalized = abbreviation.Trim();
            if (string.Equals(normalized, UrArwSpriteSheet.Abbreviation, System.StringComparison.OrdinalIgnoreCase)
                && UrArwSpriteSheet.TryGetVehicleKillFolderSprite(out sprite))
            {
                return true;
            }

            return sprites.TryGetValue(normalized, out sprite) && sprite != null;
        }

        public static bool TryGetNorthFacingSprite(string abbreviation, out Sprite sprite)
        {
            EnsureLoaded();
            sprite = null;
            if (string.IsNullOrWhiteSpace(abbreviation))
            {
                return false;
            }

            var normalized = abbreviation.Trim();
            if (northSprites.TryGetValue(normalized, out sprite) && sprite != null)
            {
                return true;
            }

            if (string.Equals(normalized, UrArwSpriteSheet.Abbreviation, System.StringComparison.OrdinalIgnoreCase)
                && UrArwSpriteSheet.TryGetNorthFacingSprite(out sprite))
            {
                return true;
            }

            return false;
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
                Debug.LogError($"F-89: Missing UR vehicle sprite sheet at Resources/{ResourcePath}.");
                return;
            }

            keyedTexture = CreateWhiteKeyedCopy(source, out var keyedPixels);
            if (keyedTexture == null || keyedPixels == null)
            {
                return;
            }

            keyedTexture.filterMode = FilterMode.Point;
            keyedTexture.wrapMode = TextureWrapMode.Clamp;

            foreach (var pair in SideRectsTopLeft)
            {
                var r = pair.Value;
                var width = r.z;
                var height = r.w;
                if (width < 1f || height < 1f)
                {
                    continue;
                }

                var unityY = keyedTexture.height - r.y - height;
                var rect = new Rect(r.x, unityY, width, height);
                if (rect.xMin < 0f
                    || rect.yMin < 0f
                    || rect.xMax > keyedTexture.width + 0.01f
                    || rect.yMax > keyedTexture.height + 0.01f)
                {
                    Debug.LogWarning($"F-89: UR vehicle sprite rect out of bounds for {pair.Key}: {rect}");
                    continue;
                }

                var sprite = Sprite.Create(
                    keyedTexture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = $"UR_{pair.Key}_Side";
                sprites[pair.Key] = sprite;
            }

            BuildNorthFacingSprites(keyedPixels, keyedTexture.width, keyedTexture.height);
        }

        private static void BuildNorthFacingSprites(Color32[] pixels, int textureWidth, int textureHeight)
        {
            if (keyedTexture == null || pixels == null || textureWidth <= 0 || textureHeight <= 0)
            {
                return;
            }

            for (var i = 0; i < NorthSlots.Length; i++)
            {
                var slot = NorthSlots[i];
                var contentX0 = slot.X0 + LabelStripWidthPx;
                var contentWidth = slot.X1 - contentX0;
                if (contentWidth < 32)
                {
                    continue;
                }

                var northWidth = Mathf.Max(32, Mathf.RoundToInt(contentWidth / 3f));
                var regionX1 = Mathf.Min(slot.X1, contentX0 + northWidth);
                if (!TryGetTightBoundsTopLeft(
                        pixels,
                        textureWidth,
                        textureHeight,
                        contentX0,
                        slot.Y0,
                        regionX1,
                        slot.Y1,
                        out var minX,
                        out var minYTop,
                        out var width,
                        out var height))
                {
                    continue;
                }

                var unityY = textureHeight - minYTop - height;
                var rect = new Rect(minX, unityY, width, height);
                var sprite = Sprite.Create(
                    keyedTexture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = $"UR_{slot.Abbrev}_North";
                northSprites[slot.Abbrev] = sprite;
            }
        }

        private static bool TryGetTightBoundsTopLeft(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            int x0,
            int y0Top,
            int x1,
            int y1Top,
            out int minX,
            out int minYTop,
            out int width,
            out int height)
        {
            minX = int.MaxValue;
            minYTop = int.MaxValue;
            var maxX = -1;
            var maxYTop = -1;
            var found = false;

            for (var yTop = y0Top; yTop < y1Top; yTop++)
            {
                var unityY = textureHeight - 1 - yTop;
                var row = unityY * textureWidth;
                for (var x = x0; x < x1; x++)
                {
                    if (pixels[row + x].a <= 8)
                    {
                        continue;
                    }

                    found = true;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minYTop = Mathf.Min(minYTop, yTop);
                    maxYTop = Mathf.Max(maxYTop, yTop);
                }
            }

            if (!found)
            {
                minX = 0;
                minYTop = 0;
                width = 0;
                height = 0;
                return false;
            }

            width = maxX - minX + 1;
            height = maxYTop - minYTop + 1;
            return width > 0 && height > 0;
        }

        private static Texture2D CreateWhiteKeyedCopy(Texture2D source, out Color32[] pixels)
        {
            pixels = null;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogError(
                    $"F-89: UR vehicle sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
                return null;
            }

            const byte whiteThreshold = 248;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.r >= whiteThreshold && p.g >= whiteThreshold && p.b >= whiteThreshold)
                {
                    p.a = 0;
                    pixels[i] = p;
                }
            }

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "UR_Vehicles_Keyed",
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
