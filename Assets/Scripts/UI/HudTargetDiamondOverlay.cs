using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class HudTargetDiamondOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float squareSize = HudTargetMarkerLayout.SquareSize;
        [SerializeField] private float diamondSize = HudTargetMarkerLayout.DiamondSize;

        private static readonly Color FriendlyMarkerColor = Color.white;

        private Texture2D squareTexture;
        private Texture2D diamondTexture;

        public void Configure(
            PlayerWeaponController weapons,
            AircraftController aircraftController,
            Camera camera = null)
        {
            weaponController = weapons;
            aircraft = aircraftController;
            worldCamera = camera != null ? camera : Camera.main;
            EnsureTextures();
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null
                || worldCamera == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureTextures();

            var hudColor = FlightHudColorPalette.Current;
            var squareHalf = squareSize * 0.5f;
            var diamondHalf = diamondSize * 0.5f;
            var activeTarget = weaponController != null ? weaponController.GetActiveHudTarget() : null;
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !IsTargetOnScreen(target, out var guiCenter))
                {
                    continue;
                }

                if (IsCarrierTarget(target) || target.IsFlareDecoy || target.IsPlayerAircraft)
                {
                    continue;
                }

                var markerColor = target.IsFriendly ? FriendlyMarkerColor : hudColor;
                DrawMarker(guiCenter, squareHalf, squareSize, markerColor, squareTexture);
            }

            if (activeTarget != null
                && activeTarget.IsAlive
                && !IsCarrierTarget(activeTarget)
                && IsTargetOnScreen(activeTarget, out var activeCenter))
            {
                var diamondColor = activeTarget.IsFriendly ? FriendlyMarkerColor : hudColor;
                DrawMarker(activeCenter, diamondHalf, diamondSize, diamondColor, diamondTexture);
            }
        }

        private static bool IsCarrierTarget(LockableTarget target)
        {
            if (target == null)
            {
                return false;
            }

            var baseSite = target.GetComponent<AntarcticaBase>();
            return baseSite != null && baseSite.SiteKind == BaseSiteKind.Carrier;
        }

        private bool IsTargetOnScreen(LockableTarget target, out Vector2 guiCenter)
        {
            return HudTargetMarkerLayout.TryGetGuiCenter(worldCamera, target.transform.position, out guiCenter);
        }

        private static void DrawMarker(Vector2 guiCenter, float half, float size, Color color, Texture2D texture)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(guiCenter.x - half, guiCenter.y - half, size, size), texture);
            GUI.color = previous;
        }

        private void EnsureTextures()
        {
            if (squareTexture == null)
            {
                squareTexture = CreateCornerBracketTexture(Mathf.RoundToInt(squareSize));
            }

            if (diamondTexture == null)
            {
                diamondTexture = CreateCornerBracketTexture(Mathf.RoundToInt(diamondSize));
            }
        }

        private static Texture2D CreateCornerBracketTexture(int size, int thickness = 2)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var armLength = Mathf.Clamp(Mathf.RoundToInt(size * 0.28f), thickness + 2, size / 2);
            var cornerRadius = Mathf.Max(1f, thickness * 0.85f);

            FillCornerBracket(
                pixels,
                size,
                thickness,
                armLength,
                cornerRadius,
                topLeft: true,
                topRight: true,
                bottomLeft: true,
                bottomRight: true);

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void FillCornerBracket(
            Color[] pixels,
            int size,
            int thickness,
            int armLength,
            float cornerRadius,
            bool topLeft,
            bool topRight,
            bool bottomLeft,
            bool bottomRight)
        {
            if (topLeft)
            {
                FillRect(pixels, size, 0, 0, armLength, thickness);
                FillRect(pixels, size, 0, 0, thickness, armLength);
                RoundOuterCorner(pixels, size, 0, 0, cornerRadius, 1f, 1f);
            }

            if (topRight)
            {
                FillRect(pixels, size, size - armLength, 0, armLength, thickness);
                FillRect(pixels, size, size - thickness, 0, thickness, armLength);
                RoundOuterCorner(pixels, size, size - 1, 0, cornerRadius, -1f, 1f);
            }

            if (bottomLeft)
            {
                FillRect(pixels, size, 0, size - thickness, armLength, thickness);
                FillRect(pixels, size, 0, size - armLength, thickness, armLength);
                RoundOuterCorner(pixels, size, 0, size - 1, cornerRadius, 1f, -1f);
            }

            if (bottomRight)
            {
                FillRect(pixels, size, size - armLength, size - thickness, armLength, thickness);
                FillRect(pixels, size, size - thickness, size - armLength, thickness, armLength);
                RoundOuterCorner(pixels, size, size - 1, size - 1, cornerRadius, -1f, -1f);
            }
        }

        private static void FillRect(Color[] pixels, int size, int x, int y, int width, int height)
        {
            for (var py = y; py < y + height; py++)
            {
                if (py < 0 || py >= size)
                {
                    continue;
                }

                for (var px = x; px < x + width; px++)
                {
                    if (px < 0 || px >= size)
                    {
                        continue;
                    }

                    pixels[py * size + px] = Color.white;
                }
            }
        }

        private static void RoundOuterCorner(
            Color[] pixels,
            int size,
            int cornerX,
            int cornerY,
            float radius,
            float xSign,
            float ySign)
        {
            var radiusSquared = radius * radius;
            var minX = xSign > 0f ? cornerX : cornerX - Mathf.CeilToInt(radius);
            var maxX = xSign > 0f ? cornerX + Mathf.CeilToInt(radius) : cornerX;
            var minY = ySign > 0f ? cornerY : cornerY - Mathf.CeilToInt(radius);
            var maxY = ySign > 0f ? cornerY + Mathf.CeilToInt(radius) : cornerY;

            for (var y = minY; y <= maxY; y++)
            {
                if (y < 0 || y >= size)
                {
                    continue;
                }

                for (var x = minX; x <= maxX; x++)
                {
                    if (x < 0 || x >= size)
                    {
                        continue;
                    }

                    var dx = (x - cornerX) * xSign;
                    var dy = (y - cornerY) * ySign;
                    if (dx >= 0f && dy >= 0f && dx * dx + dy * dy <= radiusSquared)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }
        }
    }
}
