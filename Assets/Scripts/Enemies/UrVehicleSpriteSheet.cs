using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// UR vehicle sheet (side views). Art faces left; white background keyed to transparent at load.
    /// Layout is 4×2 (left: MBT/HCT/FW/HAR, right: AH/PHT/VHS/MC). TDP uses <see cref="UrTdpSpriteSheet"/>; ARW has no art yet.
    /// </summary>
    public static class UrVehicleSpriteSheet
    {
        public const string ResourcePath = "Vehicles/UR/ur_tanks_sprites";
        public const float PixelsPerUnit = 100f;

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
                Debug.LogError($"F-89: Missing UR vehicle sprite sheet at Resources/{ResourcePath}.");
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
