using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Loads the UR Arctic Soldier sheet. Art faces left; flipX when facing right.
    /// </summary>
    public static class LandEnemySpriteSheet
    {
        public const string ResourcePath = "LandCombat/Enemy/ur_arctic_soldier";
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

        // Vector4 = x, y (top-left), width, height.
        private static readonly Vector4[][] SheetRectsTopLeft =
        {
            // Idle (8)
            new[]
            {
                new Vector4(147, 48, 48, 66), new Vector4(239, 48, 46, 66), new Vector4(328, 48, 46, 66),
                new Vector4(420, 48, 44, 66), new Vector4(512, 48, 44, 66), new Vector4(603, 48, 46, 66),
                new Vector4(695, 48, 44, 66), new Vector4(782, 48, 44, 66)
            },
            // Move (8)
            new[]
            {
                new Vector4(147, 121, 46, 66), new Vector4(248, 121, 46, 66), new Vector4(341, 121, 46, 66),
                new Vector4(433, 121, 46, 66), new Vector4(520, 121, 50, 66), new Vector4(619, 121, 46, 66),
                new Vector4(702, 121, 48, 66), new Vector4(799, 121, 40, 66)
            },
            // Run (8)
            new[]
            {
                new Vector4(132, 197, 58, 55), new Vector4(236, 197, 56, 55), new Vector4(334, 197, 58, 55),
                new Vector4(423, 197, 56, 55), new Vector4(513, 197, 56, 55), new Vector4(606, 197, 54, 55),
                new Vector4(693, 197, 60, 55), new Vector4(790, 197, 50, 55)
            },
            // Shot hip (8)
            new[]
            {
                new Vector4(142, 267, 48, 63), new Vector4(232, 267, 48, 63), new Vector4(323, 267, 48, 63),
                new Vector4(414, 267, 48, 63), new Vector4(505, 267, 44, 63), new Vector4(604, 267, 46, 63),
                new Vector4(704, 267, 46, 63), new Vector4(799, 267, 48, 63)
            },
            // Shot aim (8)
            new[]
            {
                new Vector4(145, 338, 52, 56), new Vector4(239, 338, 52, 56), new Vector4(333, 338, 52, 56),
                new Vector4(423, 338, 58, 56), new Vector4(522, 338, 50, 56), new Vector4(625, 338, 50, 56),
                new Vector4(720, 338, 50, 56), new Vector4(817, 338, 48, 56)
            },
            // Grenade throw (8; explosion frame omitted)
            new[]
            {
                new Vector4(151, 401, 48, 57), new Vector4(248, 401, 46, 57), new Vector4(341, 401, 50, 57),
                new Vector4(421, 401, 48, 57), new Vector4(507, 401, 60, 57), new Vector4(621, 401, 60, 57),
                new Vector4(723, 401, 54, 57), new Vector4(800, 401, 50, 57)
            },
            // Dead (5)
            new[]
            {
                new Vector4(141, 473, 58, 43), new Vector4(257, 473, 56, 43), new Vector4(376, 473, 64, 43),
                new Vector4(482, 473, 88, 43), new Vector4(609, 473, 98, 43)
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
                Debug.LogError($"F-89 Land: Missing enemy sprite sheet at Resources/{ResourcePath}.");
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
                    frames[i].name = $"UR_{(Clip)c}_{i}";
                }

                clips[c] = frames;
            }

            loaded = true;
        }
    }
}
