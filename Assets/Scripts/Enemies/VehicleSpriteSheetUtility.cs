using System.Collections.Generic;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Shared sprite-sheet helpers. Edge flood-fill removes only outer white matte — not in-vehicle highlights.
    /// </summary>
    public static class VehicleSpriteSheetUtility
    {
        public static Texture2D CreateEdgeWhiteKeyedCopy(
            Texture2D source,
            string keyedTextureName,
            byte whiteThreshold = 248)
        {
            if (source == null)
            {
                return null;
            }

            Color32[] pixels;
            try
            {
                pixels = source.GetPixels32();
            }
            catch (UnityException)
            {
                return null;
            }

            var width = source.width;
            var height = source.height;
            if (width <= 0 || height <= 0 || pixels.Length != width * height)
            {
                return null;
            }

            var background = new bool[pixels.Length];
            var queue = new Queue<int>(width * 2 + height * 2);

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                var index = y * width + x;
                if (background[index] || !IsNearWhite(pixels[index], whiteThreshold))
                {
                    return;
                }

                background[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, height - 1);
            }

            for (var y = 0; y < height; y++)
            {
                TryEnqueue(0, y);
                TryEnqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;
                TryEnqueue(x - 1, y);
                TryEnqueue(x + 1, y);
                TryEnqueue(x, y - 1);
                TryEnqueue(x, y + 1);
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                if (!background[i])
                {
                    continue;
                }

                var pixel = pixels[i];
                pixel.a = 0;
                pixels[i] = pixel;
            }

            var copy = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = keyedTextureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            copy.SetPixels32(pixels);
            copy.Apply(false, true);
            return copy;
        }

        private static bool IsNearWhite(Color32 pixel, byte threshold)
        {
            return pixel.r >= threshold && pixel.g >= threshold && pixel.b >= threshold;
        }
    }
}
