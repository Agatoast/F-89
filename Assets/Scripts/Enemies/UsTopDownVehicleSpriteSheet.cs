using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// US ground vehicle top-down sheet. Five rotation frames per column (0°–180°, mirror for 180°–360°).
    /// White background keyed to transparent at load. AH-64 pending art.
    /// </summary>
    public static class UsTopDownVehicleSpriteSheet
    {
        public const string ResourcePath = "Vehicles/US/us_topdown_sprites";
        public const float PixelsPerUnit = 100f;
        public const int RotationFrameCount = 5;

        private static readonly Vector4[] M109A7Frames =
        {
            new Vector4(592f, 30f, 113f, 94f),
            new Vector4(590f, 142f, 115f, 79f),
            new Vector4(590f, 225f, 115f, 64f),
            new Vector4(590f, 319f, 115f, 77f),
            new Vector4(590f, 416f, 115f, 95f)
        };

        private static readonly Dictionary<string, Vector4[]> RotationRectsTopLeft =
            new Dictionary<string, Vector4[]>(System.StringComparer.OrdinalIgnoreCase)
            {
                {
                    "ISV",
                    new[]
                    {
                        new Vector4(59f, 30f, 35f, 83f),
                        new Vector4(59f, 135f, 54f, 77f),
                        new Vector4(59f, 239f, 50f, 37f),
                        new Vector4(59f, 330f, 47f, 65f),
                        new Vector4(59f, 420f, 25f, 84f)
                    }
                },
                {
                    "FMTV",
                    new[]
                    {
                        new Vector4(162f, 30f, 38f, 90f),
                        new Vector4(162f, 132f, 66f, 89f),
                        new Vector4(162f, 241f, 62f, 39f),
                        new Vector4(162f, 321f, 52f, 71f),
                        new Vector4(162f, 418f, 26f, 93f)
                    }
                },
                {
                    "HEMTT",
                    new[]
                    {
                        new Vector4(275f, 30f, 41f, 92f),
                        new Vector4(275f, 130f, 73f, 85f),
                        new Vector4(275f, 240f, 68f, 41f),
                        new Vector4(275f, 317f, 67f, 79f),
                        new Vector4(275f, 400f, 51f, 111f)
                    }
                },
                {
                    "MRAP",
                    new[]
                    {
                        new Vector4(390f, 30f, 39f, 89f),
                        new Vector4(390f, 134f, 60f, 78f),
                        new Vector4(390f, 242f, 57f, 40f),
                        new Vector4(390f, 325f, 57f, 71f),
                        new Vector4(390f, 400f, 38f, 110f)
                    }
                },
                {
                    "JLTV",
                    new[]
                    {
                        new Vector4(512f, 31f, 31f, 88f),
                        new Vector4(512f, 138f, 78f, 83f),
                        new Vector4(512f, 225f, 78f, 63f),
                        new Vector4(512f, 319f, 78f, 77f),
                        new Vector4(512f, 400f, 78f, 110f)
                    }
                },
                { "M109A7", M109A7Frames },
                { "M109", M109A7Frames },
                {
                    "ICV",
                    new[]
                    {
                        new Vector4(705f, 32f, 43f, 92f),
                        new Vector4(705f, 137f, 66f, 82f),
                        new Vector4(705f, 243f, 63f, 46f),
                        new Vector4(705f, 318f, 69f, 78f),
                        new Vector4(705f, 415f, 46f, 92f)
                    }
                },
                {
                    "M2A4",
                    new[]
                    {
                        new Vector4(824f, 30f, 42f, 94f),
                        new Vector4(824f, 142f, 69f, 79f),
                        new Vector4(824f, 252f, 67f, 44f),
                        new Vector4(824f, 323f, 54f, 73f),
                        new Vector4(824f, 415f, 32f, 95f)
                    }
                },
                {
                    "M1A2",
                    new[]
                    {
                        new Vector4(954f, 30f, 37f, 96f),
                        new Vector4(954f, 147f, 57f, 74f),
                        new Vector4(954f, 225f, 66f, 71f),
                        new Vector4(954f, 300f, 54f, 96f),
                        new Vector4(954f, 400f, 54f, 111f)
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
                Debug.LogWarning($"F-89: Missing US top-down vehicle sheet at Resources/{ResourcePath}.");
                return;
            }

            keyedTexture = VehicleSpriteSheetUtility.CreateEdgeWhiteKeyedCopy(
                source,
                "US_TopDown_Vehicles_Keyed");
            if (keyedTexture == null)
            {
                Debug.LogError(
                    $"F-89: US top-down sheet is not readable. Enable Read/Write on Resources/{ResourcePath}.");
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

                if (rotationFramesByAbbrev.TryGetValue(pair.Key, out var existing)
                    && existing != null
                    && existing.Length == RotationFrameCount)
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
                    $"F-89: US top-down sprite rect out of bounds for {abbrev} frame {frameIndex}: {rect}");
                return null;
            }

            var sprite = Sprite.Create(
                keyedTexture,
                rect,
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"US_{abbrev}_TopDown_{frameIndex}";
            return sprite;
        }

    }
}
