#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace F89.EditorTools
{
    public static class AircraftAssetUtility
    {
        private static readonly string[] TexturePaths =
        {
            "Assets/Resources/F89_Placeholder.png",
            "Assets/Art/Aircraft/F89_Placeholder.png"
        };

        [MenuItem("F-89/Reimport Aircraft Texture")]
        public static void ReimportAircraftTexture()
        {
            RemovePlaneBackground();
        }

        [MenuItem("F-89/Remove Plane Background")]
        public static void RemovePlaneBackground()
        {
            var changed = 0;
            foreach (var assetPath in TexturePaths)
            {
                if (RemoveBackgroundAtPath(assetPath))
                {
                    changed++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log(changed > 0
                ? "F-89: Removed outer black background from aircraft texture."
                : "F-89: No aircraft textures were updated.");
        }

        private static bool RemoveBackgroundAtPath(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"F-89: Missing texture at {assetPath}");
                return false;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return false;
            }

            var previousReadable = importer.isReadable;
            var previousAlpha = importer.alphaIsTransparency;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.textureType = TextureImporterType.Default;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return false;
            }

            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;
            var transparentCount = FloodFillBackgroundTransparent(pixels, width, height, 20);

            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            output.SetPixels32(pixels);
            output.Apply();
            File.WriteAllBytes(assetPath, output.EncodeToPNG());
            Object.DestroyImmediate(output);

            importer.isReadable = previousReadable;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();

            Debug.Log($"F-89: {assetPath} updated ({transparentCount} background pixels made transparent).");
            return true;
        }

        private static int FloodFillBackgroundTransparent(Color32[] pixels, int width, int height, byte threshold)
        {
            var background = new bool[width * height];
            var queue = new Queue<(int x, int y)>();

            void TryEnqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                {
                    return;
                }

                var index = y * width + x;
                if (background[index] || !IsBackgroundPixel(pixels[index], threshold))
                {
                    return;
                }

                background[index] = true;
                queue.Enqueue((x, y));
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
                var (x, y) = queue.Dequeue();
                TryEnqueue(x + 1, y);
                TryEnqueue(x - 1, y);
                TryEnqueue(x, y + 1);
                TryEnqueue(x, y - 1);
            }

            var transparentCount = 0;
            for (var i = 0; i < background.Length; i++)
            {
                if (!background[i])
                {
                    continue;
                }

                pixels[i].a = 0;
                transparentCount++;
            }

            return transparentCount;
        }

        private static bool IsBackgroundPixel(Color32 pixel, byte threshold)
        {
            var maxChannel = pixel.r;
            if (pixel.g > maxChannel)
            {
                maxChannel = pixel.g;
            }

            if (pixel.b > maxChannel)
            {
                maxChannel = pixel.b;
            }

            return maxChannel < threshold;
        }
    }
}
#endif
