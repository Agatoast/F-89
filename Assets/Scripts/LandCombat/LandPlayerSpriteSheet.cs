using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Loads the American Commando (Arctic) sheet and builds per-animation sprites.
    /// Sheet faces right; backdrop is transparent in the imported PNG.
    /// </summary>
    public static class LandPlayerSpriteSheet
    {
        public const string ResourcePath = "LandCombat/Player/american_commando_arctic";
        public const float PixelsPerUnit = 48f;

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

        // Tight content rects measured from the sheet (x, y, w, h). Unity rect origin is bottom-left.
        // Source image is top-down; ConvertY remaps from top-left authoring coords.
        private static readonly Vector4[][] SheetRectsTopLeft =
        {
            // Idle (8)
            new[]
            {
                new Vector4(147, 47, 46, 66), new Vector4(237, 47, 46, 66), new Vector4(326, 47, 46, 66),
                new Vector4(418, 47, 46, 66), new Vector4(512, 47, 46, 66), new Vector4(610, 47, 46, 66),
                new Vector4(707, 47, 46, 66), new Vector4(802, 47, 46, 66)
            },
            // Move (8)
            new[]
            {
                new Vector4(148, 119, 46, 66), new Vector4(246, 119, 46, 66), new Vector4(339, 119, 48, 66),
                new Vector4(432, 119, 48, 66), new Vector4(522, 119, 50, 66), new Vector4(627, 119, 42, 66),
                new Vector4(715, 119, 48, 66), new Vector4(816, 119, 42, 66)
            },
            // Run (8)
            new[]
            {
                new Vector4(134, 197, 58, 54), new Vector4(232, 197, 58, 54), new Vector4(332, 197, 58, 54),
                new Vector4(431, 197, 50, 54), new Vector4(514, 197, 58, 54), new Vector4(608, 197, 56, 54),
                new Vector4(698, 197, 62, 54), new Vector4(796, 197, 48, 54)
            },
            // Shot hip (8)
            new[]
            {
                new Vector4(143, 267, 48, 63), new Vector4(231, 267, 48, 63), new Vector4(323, 267, 48, 63),
                new Vector4(416, 267, 48, 63), new Vector4(507, 267, 48, 63), new Vector4(607, 267, 48, 63),
                new Vector4(707, 267, 48, 63), new Vector4(803, 267, 48, 63)
            },
            // Shot aim (8)
            new[]
            {
                new Vector4(147, 339, 56, 54), new Vector4(241, 339, 58, 54), new Vector4(336, 339, 56, 54),
                new Vector4(426, 339, 58, 54), new Vector4(526, 339, 56, 54), new Vector4(629, 339, 54, 54),
                new Vector4(724, 339, 56, 54), new Vector4(820, 339, 54, 54)
            },
            // Grenade throw only (8; explosion frame omitted)
            new[]
            {
                new Vector4(151, 415, 50, 54), new Vector4(249, 415, 46, 54), new Vector4(342, 415, 50, 54),
                new Vector4(422, 415, 50, 54), new Vector4(510, 415, 64, 54), new Vector4(625, 415, 62, 54),
                new Vector4(727, 415, 56, 54), new Vector4(805, 415, 54, 54)
            },
            // Dead (5)
            new[]
            {
                new Vector4(141, 492, 58, 42), new Vector4(257, 492, 58, 42), new Vector4(376, 492, 66, 42),
                new Vector4(483, 492, 86, 42), new Vector4(612, 492, 98, 42)
            }
        };

        private static Sprite[][] clips;
        private static bool loaded;

        public static Sprite[] GetClip(Clip clip)
        {
            EnsureLoaded();
            return clips[(int)clip];
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            var texture = Resources.Load<Texture2D>(ResourcePath);
            if (texture == null)
            {
                Debug.LogError($"F-89 Land: Missing player sprite sheet at Resources/{ResourcePath}.");
                clips = new Sprite[SheetRectsTopLeft.Length][];
                for (var i = 0; i < clips.Length; i++)
                {
                    clips[i] = System.Array.Empty<Sprite>();
                }

                loaded = true;
                return;
            }

            texture.filterMode = FilterMode.Point;
            clips = new Sprite[SheetRectsTopLeft.Length][];
            for (var c = 0; c < SheetRectsTopLeft.Length; c++)
            {
                var src = SheetRectsTopLeft[c];
                var frames = new Sprite[src.Length];
                for (var i = 0; i < src.Length; i++)
                {
                    // Vector4 = x, y (top-left), width, height.
                    var r = src[i];
                    var width = r.z;
                    var height = r.w;
                    var unityY = texture.height - r.y - height;
                    var rect = new Rect(r.x, unityY, width, height);
                    frames[i] = Sprite.Create(
                        texture,
                        rect,
                        new Vector2(0.5f, 0.5f),
                        PixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect);
                    frames[i].name = $"{(Clip)c}_{i}";
                }

                clips[c] = frames;
            }

            loaded = true;
        }
    }
}
