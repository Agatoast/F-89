using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// ARW pod vehicle sheet — six top-down rotation frames on black (N→SW). Five frames (0°–180°)
    /// drive flight-map rotation; Character Page vehicle #8 uses the east frame mirrored to face west.
    /// Black keyed to transparent at load.
    /// </summary>
    public static class UrArwSpriteSheet
    {
        public const string ResourcePath = "Vehicles/UR/arw_sprites";
        public const string Abbreviation = "ARW";
        public const float PixelsPerUnit = 100f;
        public const int RotationFrameCount = 5;
        public const int SheetFrameCount = 6;
        /// <summary>East rotation frame (90°) — mirrored horizontally for west-facing CP icon.</summary>
        private const int VehicleKillFolderSourceFrameIndex = 2;

        // Top-left origin: x, y, width, height (tight bounds per frame, north through SW).
        private static readonly Vector4[] FrameRectsTopLeft =
        {
            new Vector4(49f, 53f, 86f, 131f),
            new Vector4(31f, 210f, 124f, 134f),
            new Vector4(34f, 397f, 126f, 82f),
            new Vector4(25f, 534f, 134f, 116f),
            new Vector4(48f, 696f, 82f, 126f),
            new Vector4(21f, 867f, 130f, 117f)
        };

        private static Sprite[] rotationFrames;
        private static Sprite vehicleKillFolderSprite;
        private static Sprite northSprite;
        private static bool loadAttempted;

        public static bool TryGetRotationFrames(out Sprite[] frames)
        {
            EnsureLoaded();
            frames = rotationFrames;
            return frames != null && frames.Length == RotationFrameCount;
        }

        public static bool TryGetVehicleKillFolderSprite(out Sprite sprite)
        {
            EnsureLoaded();
            sprite = vehicleKillFolderSprite;
            return sprite != null;
        }

        public static bool TryGetNorthFacingSprite(out Sprite sprite)
        {
            EnsureLoaded();
            sprite = northSprite;
            return sprite != null;
        }

        public static void EnsureLoaded()
        {
            if (loadAttempted)
            {
                return;
            }

            loadAttempted = true;
            var source = LoadSourceTexture(ResourcePath);
            if (source == null)
            {
                Debug.LogError($"F-89: Missing ARW sprite sheet at Resources/{ResourcePath}.");
                rotationFrames = System.Array.Empty<Sprite>();
                return;
            }

            var keyedTexture = CreateBlackKeyedCopy(source);
            if (keyedTexture == null)
            {
                rotationFrames = System.Array.Empty<Sprite>();
                return;
            }

            rotationFrames = new Sprite[RotationFrameCount];
            for (var i = 0; i < RotationFrameCount; i++)
            {
                rotationFrames[i] = CreateSpriteFromTopLeftRect(keyedTexture, i, FrameRectsTopLeft[i]);
            }

            if (VehicleKillFolderSourceFrameIndex >= 0
                && VehicleKillFolderSourceFrameIndex < FrameRectsTopLeft.Length)
            {
                vehicleKillFolderSprite = CreateWestFacingKillFolderSprite(
                    source,
                    FrameRectsTopLeft[VehicleKillFolderSourceFrameIndex]);
            }

            if (rotationFrames.Length > 0 && rotationFrames[0] != null)
            {
                northSprite = rotationFrames[0];
            }
        }

        private static Sprite CreateSpriteFromTopLeftRect(
            Texture2D keyedTexture,
            int frameIndex,
            Vector4 topLeftRect)
        {
            var width = topLeftRect.z;
            var height = topLeftRect.w;
            if (width < 1f || height < 1f || keyedTexture == null)
            {
                return null;
            }

            var unityY = keyedTexture.height - topLeftRect.y - height;
            var rect = new Rect(topLeftRect.x, unityY, width, height);
            if (rect.xMin < 0f
                || rect.yMin < 0f
                || rect.xMax > keyedTexture.width + 0.01f
                || rect.yMax > keyedTexture.height + 0.01f)
            {
                Debug.LogWarning($"F-89: ARW frame {frameIndex} rect out of bounds: {rect}");
                return null;
            }

            var sprite = Sprite.Create(
                keyedTexture,
                rect,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"UR_ARW_{frameIndex}";
            return sprite;
        }

        private static Texture2D LoadSourceTexture(string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return texture;
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null && sprite.texture != null)
            {
                return sprite.texture;
            }

            var assets = Resources.LoadAll(resourcePath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Texture2D loadedTexture)
                {
                    return loadedTexture;
                }

                if (assets[i] is Sprite loadedSprite && loadedSprite.texture != null)
                {
                    return loadedSprite.texture;
                }
            }

            return null;
        }

        private static Texture2D CreateBlackKeyedCopy(Texture2D source)
        {
            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                Debug.LogError(
                    $"F-89: ARW sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
                return null;
            }

            const byte blackThreshold = 12;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.r <= blackThreshold && p.g <= blackThreshold && p.b <= blackThreshold)
                {
                    p.a = 0;
                    pixels[i] = p;
                }
            }

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = "UR_ARW_Keyed",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }

        private static Sprite CreateWestFacingKillFolderSprite(Texture2D source, Vector4 topLeftRect)
        {
            var width = Mathf.RoundToInt(topLeftRect.z);
            var height = Mathf.RoundToInt(topLeftRect.w);
            if (source == null || width < 1 || height < 1)
            {
                return null;
            }

            Color32[] pixels;
            try
            {
                var startX = Mathf.RoundToInt(topLeftRect.x);
                var startY = Mathf.RoundToInt(source.height - topLeftRect.y - height);
                var allPixels = source.GetPixels32();
                pixels = new Color32[width * height];
                for (var y = 0; y < height; y++)
                {
                    var sourceRow = (startY + y) * source.width + startX;
                    var destRow = y * width;
                    for (var x = 0; x < width; x++)
                    {
                        pixels[destRow + x] = allPixels[sourceRow + x];
                    }
                }
            }
            catch (UnityException)
            {
                Debug.LogError(
                    $"F-89: ARW west kill-folder frame unreadable. Enable Read/Write on Resources/{ResourcePath}.");
                return null;
            }

            const byte blackThreshold = 12;
            var mirrored = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var sample = pixels[y * width + (width - 1 - x)];
                    if (sample.r <= blackThreshold
                        && sample.g <= blackThreshold
                        && sample.b <= blackThreshold)
                    {
                        sample.a = 0;
                    }

                    mirrored[y * width + x] = sample;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "UR_ARW_WestKillFolder",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(mirrored);
            texture.Apply(false, true);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "UR_ARW_WestKillFolder";
            return sprite;
        }
    }
}
