using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// US vehicle sheet (side views). Art faces left; white background keyed to transparent at load.
    /// Flight map ground units use <see cref="UsTopDownVehicleSpriteSheet"/>; AH-64 has no top-down art yet.
    /// </summary>
    public static class UsVehicleSpriteSheet
    {
        public const string ResourcePath = "Vehicles/US/usa_tanks_sprites";
        public const float PixelsPerUnit = 100f;

        // Top-left origin rects: x, y, width, height (side view column).
        private static readonly Dictionary<string, Vector4> SideRectsTopLeft =
            new Dictionary<string, Vector4>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "ISV", new Vector4(546, 8, 155, 67) },
                { "FMTV", new Vector4(528, 75, 195, 76) },
                { "HEMTT", new Vector4(504, 151, 265, 76) },
                { "MRAP", new Vector4(529, 227, 175, 76) },
                { "JLTV", new Vector4(528, 303, 168, 75) },
                { "M109A7", new Vector4(484, 378, 240, 76) },
                { "M109", new Vector4(484, 378, 240, 76) },
                { "ICV", new Vector4(521, 454, 207, 76) },
                { "M2A4", new Vector4(527, 530, 205, 76) },
                { "M1A2", new Vector4(488, 606, 299, 76) }
            };

        private static readonly Dictionary<string, Sprite> sprites =
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

            return sprites.TryGetValue(abbreviation.Trim(), out sprite) && sprite != null;
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
                Debug.LogError($"F-89: Missing US vehicle sprite sheet at Resources/{ResourcePath}.");
                return;
            }

            keyedTexture = CreateWhiteKeyedCopy(source);
            if (keyedTexture == null)
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
                    Debug.LogWarning($"F-89: US vehicle sprite rect out of bounds for {pair.Key}: {rect}");
                    continue;
                }

                var sprite = Sprite.Create(
                    keyedTexture,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = $"US_{pair.Key}_Side";
                sprites[pair.Key] = sprite;
            }
        }

        private static Texture2D CreateWhiteKeyedCopy(Texture2D source)
        {
            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogError(
                    $"F-89: US vehicle sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
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
                name = "US_Vehicles_Keyed",
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
