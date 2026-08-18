using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    /// <summary>Side-view TDP saucer sheet — 2×3 grid on black (keyed transparent).</summary>
    public static class SideSaucerSpriteSheet
    {
        public const string ResourcePath = "Bunker/td_side_saucer";
        public const float PixelsPerUnit = 110f;
        public const float FlightFps = 10f;
        public const float CrashFps = 12f;

        private const float FrameWidth = 352f;
        private const float FlightRowHeight = 316f;
        private const float CrashRowTop = 316f;
        private const float CrashRowHeight = 295f;

        public enum Clip
        {
            Flight = 0,
            Crash = 1
        }

        // Top-left rects: (x, y, width, height). Flight ends before crash smoke bleed (~y=316).
        private static readonly Vector4[] SheetRectsTopLeft =
        {
            new(0f, 0f, FrameWidth, FlightRowHeight),
            new(FrameWidth, 0f, FrameWidth, FlightRowHeight),
            new(0f, CrashRowTop, FrameWidth, CrashRowHeight),
            new(FrameWidth, CrashRowTop, FrameWidth, CrashRowHeight)
        };

        private static Sprite[][] _clips;
        private static bool _loaded;

        public static Sprite[] GetClip(Clip clip)
        {
            EnsureLoaded();
            return _clips[(int)clip];
        }

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            var source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogError($"F-89 Bunker Defense: missing TDP side saucer at Resources/{ResourcePath}.");
                _clips = new Sprite[2][];
                _clips[0] = System.Array.Empty<Sprite>();
                _clips[1] = System.Array.Empty<Sprite>();
                _loaded = true;
                return;
            }

            var texture = CreateBlackKeyedCopy(source);
            texture.filterMode = FilterMode.Point;
            _clips = new Sprite[2][];
            _clips[(int)Clip.Flight] = CreateSprites(texture, 0, 2);
            _clips[(int)Clip.Crash] = CreateSprites(texture, 2, 2);
            _loaded = true;
        }

        private static Sprite[] CreateSprites(Texture2D texture, int startIndex, int count)
        {
            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                var rect = SheetRectsTopLeft[startIndex + i];
                var unityY = texture.height - rect.y - rect.w;
                frames[i] = Sprite.Create(
                    texture,
                    new Rect(rect.x, unityY, rect.z, rect.w),
                    new Vector2(0.5f, 0.5f),
                    PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                frames[i].name = $"SideSaucer_{startIndex + i}";
            }

            return frames;
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
                    $"F-89 Bunker Defense: enable Read/Write on Resources/{ResourcePath}.");
                return source;
            }

            const byte threshold = 12;
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                if (pixel.r <= threshold && pixel.g <= threshold && pixel.b <= threshold)
                {
                    pixel.a = 0;
                    pixels[i] = pixel;
                }
            }

            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, false);
            return copy;
        }
    }
}
