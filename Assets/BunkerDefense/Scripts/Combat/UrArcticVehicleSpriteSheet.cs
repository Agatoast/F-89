using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>UR arctic vehicle sheet: 8 rows x 3 views (front, oblique, side).</summary>
    public static class UrArcticVehicleSpriteSheet
    {
        public const string ResourcePath = "Enemies/ur_arctic_vehicles";
        public const float PixelsPerUnit = 48f;

        public enum VehicleType
        {
            HeavyTank = 0,
            TwinBarrelTank = 1,
            LightTank = 2,
            Bulldozer = 3,
            Artillery = 4,
            InfantryFightingVehicle = 5,
            MissileCarrier = 6,
            RocketLauncher = 7
        }

        public enum View
        {
            Front = 0,
            Oblique = 1,
            Side = 2
        }

        private static readonly Vector4[][] SheetRectsTopLeft =
        {
            new[] { new Vector4(48, 16, 82, 101), new Vector4(206, 16, 120, 101), new Vector4(384, 16, 138, 101) },
            new[] { new Vector4(48, 155, 82, 98), new Vector4(206, 155, 120, 98), new Vector4(384, 155, 138, 98) },
            new[] { new Vector4(48, 286, 82, 80), new Vector4(206, 286, 120, 80), new Vector4(384, 286, 138, 80) },
            new[] { new Vector4(48, 392, 82, 99), new Vector4(206, 392, 120, 99), new Vector4(384, 392, 138, 99) },
            new[] { new Vector4(48, 522, 82, 100), new Vector4(206, 522, 120, 100), new Vector4(384, 522, 138, 100) },
            new[] { new Vector4(48, 649, 82, 103), new Vector4(206, 649, 120, 103), new Vector4(384, 649, 138, 103) },
            new[] { new Vector4(48, 775, 82, 97), new Vector4(206, 775, 120, 97), new Vector4(384, 775, 138, 97) },
            new[] { new Vector4(48, 890, 82, 98), new Vector4(206, 890, 120, 98), new Vector4(384, 890, 138, 98) }
        };

        private static Sprite[][] _sprites;
        private static bool _loaded;

        public static Sprite GetSprite(VehicleType type, View view)
        {
            EnsureLoaded();
            return _sprites[(int)type][(int)view];
        }

        public static VehicleType RandomType()
        {
            return (VehicleType)Random.Range(0, SheetRectsTopLeft.Length);
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
                Debug.LogError($"Missing UR vehicle sprite sheet at Resources/{ResourcePath}.");
                _sprites = new Sprite[SheetRectsTopLeft.Length][];
                for (var i = 0; i < _sprites.Length; i++)
                {
                    _sprites[i] = new Sprite[SheetRectsTopLeft[i].Length];
                }

                _loaded = true;
                return;
            }

            var readable = MakeReadable(sourceTexture);
            KeyBlackBackdrop(readable);
            readable.filterMode = FilterMode.Point;

            _sprites = new Sprite[SheetRectsTopLeft.Length][];
            for (var type = 0; type < SheetRectsTopLeft.Length; type++)
            {
                var views = SheetRectsTopLeft[type];
                _sprites[type] = new Sprite[views.Length];
                for (var view = 0; view < views.Length; view++)
                {
                    var rect = views[view];
                    var unityY = readable.height - rect.y - rect.w;
                    _sprites[type][view] = Sprite.Create(
                        readable,
                        new Rect(rect.x, unityY, rect.z, rect.w),
                        new Vector2(0.5f, 0.5f),
                        PixelsPerUnit,
                        0,
                        SpriteMeshType.Tight);
                    _sprites[type][view].name = $"URVehicle_{(VehicleType)type}_{(View)view}";
                }
            }

            _loaded = true;
        }

        private static void KeyBlackBackdrop(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (Mathf.Max(pixel.r, pixel.g, pixel.b) > 20)
                {
                    continue;
                }

                pixel.a = 0;
                pixels[i] = pixel;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        private static Texture2D MakeReadable(Texture2D source)
        {
            try
            {
                _ = source.GetPixels32();
                return source;
            }
            catch (UnityException)
            {
                var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
        }
    }
}
