using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Ground combat crosshair: black tip + arms with HUD-green flanks on both sides of each arm.
    /// Hotspot is the center tip so aiming matches the mouse position.
    /// </summary>
    public static class LandGroundCrosshair
    {
        private const int ArmLength = 10;
        private const int Gap = 1;
        private const int DotRadius = 0; // single center pixel

        private static Texture2D texture;
        private static bool applied;

        public static void Apply()
        {
            EnsureTexture();
            var hotspot = new Vector2(texture.width * 0.5f - 0.5f, texture.height * 0.5f - 0.5f);
            Cursor.SetCursor(texture, hotspot, CursorMode.ForceSoftware);
            applied = true;
        }

        public static void Clear()
        {
            if (!applied && texture == null)
            {
                return;
            }

            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            applied = false;
        }

        private static void EnsureTexture()
        {
            if (texture != null)
            {
                return;
            }

            // Center tip + 1px gap + 10px arm + 1px green flanks on both sides of each arm.
            var extent = DotRadius + 1 + Gap + ArmLength;
            var size = extent * 2 + 1 + 2; // +2 for green flanks beyond the arm axes
            var cx = size / 2;
            var cy = size / 2;

            texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "LandGroundCrosshair",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var clear = new Color32(0, 0, 0, 0);
            var black = new Color32(0, 0, 0, 255);
            var hudGreen = (Color32)FlightHudColorPalette.Default;
            hudGreen.a = 255;

            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            SetPixel(pixels, size, cx, cy, black);

            var armStart = DotRadius + 1 + Gap;
            for (var i = 0; i < ArmLength; i++)
            {
                var d = armStart + i;

                // Horizontal arms — green above and below the black line.
                SetPixel(pixels, size, cx + d, cy + 1, hudGreen);
                SetPixel(pixels, size, cx + d, cy - 1, hudGreen);
                SetPixel(pixels, size, cx - d, cy + 1, hudGreen);
                SetPixel(pixels, size, cx - d, cy - 1, hudGreen);
                SetPixel(pixels, size, cx + d, cy, black);
                SetPixel(pixels, size, cx - d, cy, black);

                // Vertical arms — green left and right of the black line.
                SetPixel(pixels, size, cx + 1, cy + d, hudGreen);
                SetPixel(pixels, size, cx - 1, cy + d, hudGreen);
                SetPixel(pixels, size, cx + 1, cy - d, hudGreen);
                SetPixel(pixels, size, cx - 1, cy - d, hudGreen);
                SetPixel(pixels, size, cx, cy + d, black);
                SetPixel(pixels, size, cx, cy - d, black);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
        }

        private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
            {
                return;
            }

            pixels[y * size + x] = color;
        }
    }
}
