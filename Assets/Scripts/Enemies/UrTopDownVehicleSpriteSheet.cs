using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// UR ground vehicle top-down sheet. Five rotation frames per column (0°–180°, mirror for 180°–360°).
    /// White background keyed to transparent at load. TDP uses <see cref="UrTdpSpriteSheet"/>;
    /// ARW uses <see cref="UrArwSpriteSheet"/>.
    /// </summary>
    public static class UrTopDownVehicleSpriteSheet
    {
        public const string ResourcePath = "Vehicles/UR/ur_topdown_sprites";
        public const float PixelsPerUnit = 100f;
        public const int RotationFrameCount = 5;

        private static readonly Dictionary<string, Vector4[]> RotationRectsTopLeft =
            new Dictionary<string, Vector4[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                {
                    "PHT",
                    new[]
                    {
                        new Vector4(32f, 34f, 45f, 79f),
                        new Vector4(32f, 130f, 109f, 78f),
                        new Vector4(32f, 240f, 64f, 45f),
                        new Vector4(32f, 313f, 63f, 76f),
                        new Vector4(33f, 418f, 46f, 80f)
                    }
                },
                {
                    "MBT",
                    new[]
                    {
                        new Vector4(158f, 30f, 50f, 92f),
                        new Vector4(141f, 130f, 91f, 91f),
                        new Vector4(141f, 242f, 98f, 50f),
                        new Vector4(149f, 317f, 112f, 79f),
                        new Vector4(159f, 400f, 65f, 111f)
                    }
                },
                {
                    "FW",
                    new[]
                    {
                        new Vector4(275f, 30f, 55f, 96f),
                        new Vector4(226f, 130f, 115f, 91f),
                        new Vector4(226f, 225f, 115f, 71f),
                        new Vector4(226f, 300f, 113f, 96f),
                        new Vector4(271f, 400f, 54f, 73f)
                    }
                },
                {
                    "VHS",
                    new[]
                    {
                        new Vector4(408f, 30f, 63f, 96f),
                        new Vector4(393f, 130f, 78f, 91f),
                        new Vector4(402f, 225f, 69f, 71f),
                        new Vector4(389f, 300f, 82f, 96f),
                        new Vector4(409f, 400f, 62f, 69f)
                    }
                },
                {
                    "MC",
                    new[]
                    {
                        new Vector4(474f, 30f, 105f, 81f),
                        new Vector4(474f, 130f, 122f, 91f),
                        new Vector4(474f, 225f, 122f, 58f),
                        new Vector4(474f, 300f, 122f, 84f),
                        new Vector4(514f, 402f, 63f, 83f)
                    }
                },
                {
                    "HAR",
                    new[]
                    {
                        new Vector4(632f, 30f, 61f, 96f),
                        new Vector4(598f, 130f, 114f, 86f),
                        new Vector4(598f, 236f, 112f, 59f),
                        new Vector4(616f, 316f, 98f, 80f),
                        new Vector4(631f, 400f, 64f, 111f)
                    }
                },
                {
                    "HCT",
                    new[]
                    {
                        new Vector4(842f, 30f, 78f, 96f),
                        new Vector4(842f, 130f, 78f, 91f),
                        new Vector4(842f, 225f, 78f, 71f),
                        new Vector4(842f, 300f, 78f, 78f),
                        new Vector4(842f, 400f, 78f, 74f)
                    }
                },
                {
                    "AH",
                    new[]
                    {
                        new Vector4(920f, 30f, 62f, 96f),
                        new Vector4(920f, 130f, 99f, 91f),
                        new Vector4(920f, 225f, 76f, 71f),
                        new Vector4(920f, 300f, 90f, 96f),
                        new Vector4(920f, 400f, 91f, 98f)
                    }
                }
            };

        private static readonly Dictionary<string, Sprite[]> rotationFramesByAbbrev =
            new Dictionary<string, Sprite[]>(System.StringComparer.OrdinalIgnoreCase);

        private static bool loaded;
        private static Texture2D keyedTexture;

        public static bool TryGetRotationFrames(string abbreviation, out Sprite[] frames)
        {
            EnsureLoaded();
            frames = null;
            if (string.IsNullOrWhiteSpace(abbreviation))
            {
                return false;
            }

            return rotationFramesByAbbrev.TryGetValue(abbreviation.Trim(), out frames)
                && frames != null
                && frames.Length == RotationFrameCount;
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            var source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"F-89: Missing UR top-down vehicle sheet at Resources/{ResourcePath}.");
                return;
            }

            keyedTexture = VehicleSpriteSheetUtility.CreateEdgeWhiteKeyedCopy(
                source,
                "UR_TopDown_Vehicles_Keyed");
            if (keyedTexture == null)
            {
                Debug.LogError(
                    $"F-89: UR top-down sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
                return;
            }

            keyedTexture.filterMode = FilterMode.Point;
            keyedTexture.wrapMode = TextureWrapMode.Clamp;

            foreach (var pair in RotationRectsTopLeft)
            {
                var rects = pair.Value;
                if (rects == null || rects.Length != RotationFrameCount)
                {
                    continue;
                }

                var frames = new Sprite[RotationFrameCount];
                var validCount = 0;
                for (var i = 0; i < RotationFrameCount; i++)
                {
                    var sprite = CreateSpriteFromTopLeftRect(pair.Key, i, rects[i]);
                    frames[i] = sprite;
                    if (sprite != null)
                    {
                        validCount++;
                    }
                }

                if (validCount == RotationFrameCount)
                {
                    rotationFramesByAbbrev[pair.Key] = frames;
                }
            }

            loaded = true;
        }

        private static Sprite CreateSpriteFromTopLeftRect(string abbrev, int frameIndex, Vector4 topLeftRect)
        {
            var width = topLeftRect.z;
            var height = topLeftRect.w;
            if (width < 1f || height < 1f)
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
                Debug.LogWarning(
                    $"F-89: UR top-down sprite rect out of bounds for {abbrev} frame {frameIndex}: {rect}");
                return null;
            }

            var sprite = Sprite.Create(
                keyedTexture,
                rect,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"UR_{abbrev}_TopDown_{frameIndex}";
            return sprite;
        }

    }
}
