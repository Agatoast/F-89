using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>F-89 UR arctic soldier sheet. Art faces left; flipX when facing right.</summary>
    public static class UrArcticSoldierSpriteSheet
    {
        public const string ResourcePath = "Enemies/ur_arctic_soldier";
        public const float PixelsPerUnit = 48f;

        public enum Camouflage
        {
            White = 0,
            Black = 1
        }

        public enum Clip
        {
            Idle = 0,
            Move = 1,
            Run = 2,
            ShotHip = 3,
            ShotAim = 4,
            Grenade = 5,
            Dead = 6
        }

        private static readonly Vector4[][] SheetRectsTopLeft =
        {
            new[]
            {
                new Vector4(147, 48, 48, 66), new Vector4(239, 48, 46, 66), new Vector4(328, 48, 46, 66),
                new Vector4(420, 48, 44, 66), new Vector4(512, 48, 44, 66), new Vector4(603, 48, 46, 66),
                new Vector4(695, 48, 44, 66), new Vector4(782, 48, 44, 66)
            },
            new[]
            {
                new Vector4(147, 121, 46, 66), new Vector4(248, 121, 46, 66), new Vector4(341, 121, 46, 66),
                new Vector4(433, 121, 46, 66), new Vector4(520, 121, 50, 66), new Vector4(619, 121, 46, 66),
                new Vector4(702, 121, 48, 66), new Vector4(799, 121, 40, 66)
            },
            new[]
            {
                new Vector4(132, 197, 58, 55), new Vector4(236, 197, 56, 55), new Vector4(334, 197, 58, 55),
                new Vector4(423, 197, 56, 55), new Vector4(513, 197, 56, 55), new Vector4(606, 197, 54, 55),
                new Vector4(693, 197, 60, 55), new Vector4(790, 197, 50, 55)
            },
            new[]
            {
                new Vector4(142, 267, 48, 63), new Vector4(232, 267, 48, 63), new Vector4(323, 267, 48, 63),
                new Vector4(414, 267, 48, 63), new Vector4(505, 267, 44, 63), new Vector4(604, 267, 46, 63),
                new Vector4(704, 267, 46, 63), new Vector4(799, 267, 48, 63)
            },
            new[]
            {
                new Vector4(145, 338, 52, 56), new Vector4(239, 338, 52, 56), new Vector4(333, 338, 52, 56),
                new Vector4(423, 338, 58, 56), new Vector4(522, 338, 50, 56), new Vector4(625, 338, 50, 56),
                new Vector4(720, 338, 50, 56), new Vector4(817, 338, 48, 56)
            },
            new[]
            {
                new Vector4(151, 401, 48, 57), new Vector4(248, 401, 46, 57), new Vector4(341, 401, 50, 57),
                new Vector4(421, 401, 48, 57), new Vector4(507, 401, 60, 57), new Vector4(621, 401, 60, 57),
                new Vector4(723, 401, 54, 57), new Vector4(800, 401, 50, 57)
            },
            new[]
            {
                new Vector4(141, 473, 58, 43), new Vector4(257, 473, 56, 43), new Vector4(376, 473, 64, 43),
                new Vector4(482, 473, 88, 43), new Vector4(609, 473, 98, 43)
            }
        };

        private static Sprite[][][] _clipsByCamouflage;
        private static bool _loaded;

        public static Sprite[] GetClip(Clip clip, Camouflage camouflage = Camouflage.White)
        {
            EnsureLoaded();
            return _clipsByCamouflage[(int)camouflage][(int)clip];
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            var sourceTexture = Resources.Load<Texture2D>(ResourcePath);
            if (sourceTexture == null)
            {
                Debug.LogError($"Missing UR soldier sprite sheet at Resources/{ResourcePath}.");
                _clipsByCamouflage = new Sprite[2][][];
                for (var camo = 0; camo < 2; camo++)
                {
                    _clipsByCamouflage[camo] = new Sprite[SheetRectsTopLeft.Length][];
                    for (var i = 0; i < _clipsByCamouflage[camo].Length; i++)
                    {
                        _clipsByCamouflage[camo][i] = System.Array.Empty<Sprite>();
                    }
                }

                _loaded = true;
                return;
            }

            sourceTexture.filterMode = FilterMode.Point;
            var textures = new[]
            {
                CreateWhiteCamoTexture(sourceTexture),
                sourceTexture
            };

            _clipsByCamouflage = new Sprite[textures.Length][][];
            for (var camoIndex = 0; camoIndex < textures.Length; camoIndex++)
            {
                var texture = textures[camoIndex];
                texture.filterMode = FilterMode.Point;
                _clipsByCamouflage[camoIndex] = new Sprite[SheetRectsTopLeft.Length][];
                for (var c = 0; c < SheetRectsTopLeft.Length; c++)
                {
                    var src = SheetRectsTopLeft[c];
                    var frames = new Sprite[src.Length];
                    for (var i = 0; i < src.Length; i++)
                    {
                        var r = src[i];
                        var unityY = texture.height - r.y - r.w;
                        frames[i] = Sprite.Create(
                            texture,
                            new Rect(r.x, unityY, r.z, r.w),
                            new Vector2(0.5f, 0.5f),
                            PixelsPerUnit,
                            0,
                            SpriteMeshType.FullRect);
                        frames[i].name = $"UR_{(Camouflage)camoIndex}_{(Clip)c}_{i}";
                    }

                    _clipsByCamouflage[camoIndex][c] = frames;
                }
            }

            _loaded = true;
        }

        private static Texture2D CreateWhiteCamoTexture(Texture2D source)
        {
            var grayCamo = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = source.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.a == 0)
                {
                    continue;
                }

                var luminance = (pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f) / 255f;
                var arctic = new Color(
                    0.48f + (0.78f - 0.48f) * luminance,
                    0.50f + (0.80f - 0.50f) * luminance,
                    0.54f + (0.84f - 0.54f) * luminance);
                pixels[i] = new Color32(
                    (byte)(arctic.r * 255f),
                    (byte)(arctic.g * 255f),
                    (byte)(arctic.b * 255f),
                    pixel.a);
            }

            grayCamo.SetPixels32(pixels);
            grayCamo.Apply(false, false);
            return grayCamo;
        }
    }
}
