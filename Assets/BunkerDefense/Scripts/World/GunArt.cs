using System.Collections.Generic;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public static class GunArt
    {
        public const string ResourcePath = "Bunker/minigun";

        public static Sprite CreateSightSprite()
        {
            var source = Resources.Load<Texture2D>(ResourcePath);
            if (source == null)
            {
                Debug.LogError($"F-89 Bunker Defense: missing gun art at Resources/{ResourcePath}.");
                return null;
            }

            var readable = MakeReadable(source);
            var pixels = readable.GetPixels32();
            KeyBackdrop(pixels, readable.width, readable.height);
            FillInteriorHoles(pixels, readable.width, readable.height);
            CloseThinGaps(pixels, readable.width, readable.height, 4);
            SealColumnGaps(pixels, readable.width, readable.height);
            ForceOpaqueSilhouette(pixels, readable.width, readable.height);
            SolidifyGunPixels(pixels);
            var bounds = OpaqueBounds(pixels, readable.width, readable.height);
            if (bounds.width < 8 || bounds.height < 8)
            {
                bounds = new RectInt(0, 0, readable.width, readable.height);
            }

            var cropped = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var crop = new Color32[bounds.width * bounds.height];
            for (var y = 0; y < bounds.height; y++)
            {
                for (var x = 0; x < bounds.width; x++)
                {
                    crop[y * bounds.width + x] = pixels[(bounds.y + y) * readable.width + (bounds.x + x)];
                }
            }

            for (var i = 0; i < crop.Length; i++)
            {
                if (crop[i].a < 1)
                {
                    continue;
                }

                crop[i].a = 255;
            }

            cropped.SetPixels32(crop);
            cropped.Apply(false, false);

            var pivot = FindTripodPivot(bounds);
            var sprite = Sprite.Create(
                cropped,
                new Rect(0f, 0f, bounds.width, bounds.height),
                pivot,
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "MinigunSight";
            return sprite;
        }

        private static void KeyBackdrop(Color32[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var queue = new Queue<int>(width * 4);

            void EnqueueIfBackdrop(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                var i = y * width + x;
                if (visited[i] || !IsBackdrop(pixels[i]))
                {
                    return;
                }

                visited[i] = true;
                queue.Enqueue(i);
            }

            for (var x = 0; x < width; x++)
            {
                EnqueueIfBackdrop(x, 0);
                EnqueueIfBackdrop(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                EnqueueIfBackdrop(0, y);
                EnqueueIfBackdrop(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                var pixel = pixels[i];
                pixel.a = 0;
                pixels[i] = pixel;

                var x = i % width;
                var y = i / width;
                EnqueueIfBackdrop(x - 1, y);
                EnqueueIfBackdrop(x + 1, y);
                EnqueueIfBackdrop(x, y - 1);
                EnqueueIfBackdrop(x, y + 1);
            }
        }

        private static void FillInteriorHoles(Color32[] pixels, int width, int height)
        {
            var exterior = new bool[pixels.Length];
            var queue = new Queue<int>(width * 4);

            void EnqueueExterior(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                var i = y * width + x;
                if (exterior[i] || pixels[i].a >= 16)
                {
                    return;
                }

                exterior[i] = true;
                queue.Enqueue(i);
            }

            for (var x = 0; x < width; x++)
            {
                EnqueueExterior(x, 0);
                EnqueueExterior(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                EnqueueExterior(0, y);
                EnqueueExterior(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                var x = i % width;
                var y = i / width;
                EnqueueExterior(x - 1, y);
                EnqueueExterior(x + 1, y);
                EnqueueExterior(x, y - 1);
                EnqueueExterior(x, y + 1);
            }

            var fill = new Color32(48, 50, 52, 255);
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a >= 16 || exterior[i])
                {
                    continue;
                }

                pixels[i] = fill;
            }
        }

        private static void SolidifyGunPixels(Color32[] pixels)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 1)
                {
                    continue;
                }

                pixels[i].a = 255;
            }
        }

        private static void CloseThinGaps(Color32[] pixels, int width, int height, int iterations)
        {
            for (var pass = 0; pass < iterations; pass++)
            {
                var source = (Color32[])pixels.Clone();
                for (var y = 1; y < height - 1; y++)
                {
                    for (var x = 1; x < width - 1; x++)
                    {
                        var i = y * width + x;
                        if (source[i].a >= 16)
                        {
                            continue;
                        }

                        var neighbors = 0;
                        var r = 0;
                        var g = 0;
                        var b = 0;
                        for (var oy = -1; oy <= 1; oy++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                {
                                    continue;
                                }

                                var sample = source[(y + oy) * width + (x + ox)];
                                if (sample.a < 16)
                                {
                                    continue;
                                }

                                neighbors++;
                                r += sample.r;
                                g += sample.g;
                                b += sample.b;
                            }
                        }

                        if (neighbors < 3)
                        {
                            continue;
                        }

                        pixels[i] = new Color32(
                            (byte)(r / neighbors),
                            (byte)(g / neighbors),
                            (byte)(b / neighbors),
                            255);
                    }
                }
            }
        }

        private static void SealColumnGaps(Color32[] pixels, int width, int height)
        {
            var bounds = OpaqueBounds(pixels, width, height);
            if (bounds.width < 8 || bounds.height < 8)
            {
                return;
            }

            var xMin = Mathf.Max(0, bounds.xMin);
            var xMax = Mathf.Min(width - 1, bounds.xMax);
            var yMin = Mathf.Max(0, bounds.yMin);
            var yMax = Mathf.Min(height - 1, bounds.yMax);
            var fill = new Color32(40, 42, 44, 255);

            for (var x = xMin; x <= xMax; x++)
            {
                GetColumnSpan(pixels, width, x, yMin, yMax, out var first, out var last);
                if (first < 0 && x > xMin && x < xMax)
                {
                    GetColumnSpan(pixels, width, x - 1, yMin, yMax, out var leftFirst, out var leftLast);
                    GetColumnSpan(pixels, width, x + 1, yMin, yMax, out var rightFirst, out var rightLast);
                    if (leftFirst >= 0 && rightFirst >= 0)
                    {
                        first = Mathf.Min(leftFirst, rightFirst);
                        last = Mathf.Max(leftLast, rightLast);
                    }
                }

                if (first < 0)
                {
                    continue;
                }

                for (var y = first; y <= last; y++)
                {
                    var i = y * width + x;
                    if (pixels[i].a >= 16)
                    {
                        continue;
                    }

                    pixels[i] = fill;
                }
            }
        }

        private static void GetColumnSpan(Color32[] pixels, int width, int x, int yMin, int yMax, out int first, out int last)
        {
            first = -1;
            last = -1;
            if (x < 0)
            {
                return;
            }

            for (var y = yMin; y <= yMax; y++)
            {
                if (pixels[y * width + x].a < 16)
                {
                    continue;
                }

                if (first < 0)
                {
                    first = y;
                }

                last = y;
            }
        }

        private static void ForceOpaqueSilhouette(Color32[] pixels, int width, int height)
        {
            var bounds = OpaqueBounds(pixels, width, height);
            if (bounds.width < 8 || bounds.height < 8)
            {
                return;
            }

            for (var y = bounds.yMin; y <= bounds.yMax; y++)
            {
                for (var x = bounds.xMin; x <= bounds.xMax; x++)
                {
                    var i = y * width + x;
                    if (pixels[i].a < 16)
                    {
                        continue;
                    }

                    var pixel = pixels[i];
                    pixel.a = 255;
                    pixels[i] = pixel;
                }
            }
        }

        private static bool IsBackdrop(Color32 pixel)
        {
            var max = Mathf.Max(pixel.r, pixel.g, pixel.b);
            var min = Mathf.Min(pixel.r, pixel.g, pixel.b);
            var isBlack = max <= 20;
            var isWhite = max > 228 && (max - min) < 22;
            return isBlack || isWhite;
        }

        private static Vector2 FindTripodPivot(RectInt bounds)
        {
            return new Vector2(0.5f, 0.06f);
        }

        private static RectInt OpaqueBounds(Color32[] pixels, int width, int height)
        {
            var minX = width;
            var minY = height;
            var maxX = 0;
            var maxY = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (pixels[y * width + x].a < 16)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            return new RectInt(minX, minY, Mathf.Max(1, maxX - minX + 1), Mathf.Max(1, maxY - minY + 1));
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
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                copy.Apply(false, false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                return copy;
            }
        }
    }
}
