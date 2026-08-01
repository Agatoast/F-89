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

        private static readonly Color FriendlyMarkerColor = new Color(0.25f, 0.78f, 0.35f, 1f);
        private static readonly Color HostileMarkerColor = new Color(0.92f, 0.15f, 0.1f, 1f);

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

        private const float HudMarkerMaxRangeMiles = 80f;

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

            var autopilot = AutopilotController.Instance;
            if (autopilot != null && autopilot.IsFlying)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureTextures();

            var squareHalf = squareSize * 0.5f;
            var diamondDrawSize = HudTargetMarkerLayout.DiamondSize;
            var diamondHalf = diamondDrawSize * 0.5f;
            var activeTarget = weaponController != null ? weaponController.GetActiveHudTarget() : null;
            var targets = CombatThreatRange.GetCachedLockableTargets();
            var observer = aircraft.transform.position;
            var maxRangeSqr = HudMarkerMaxRangeMiles * HudMarkerMaxRangeMiles;
            var worldMap = aircraft.WorldMap;
            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                if (worldMap != null && ticSize > 0f)
                {
                    var miles = CombatThreatRange.DistanceMiles(
                        observer,
                        target.transform.position,
                        worldMap,
                        ticSize);
                    if (miles * miles > maxRangeSqr)
                    {
                        continue;
                    }
                }

                if (!IsTargetOnScreen(target, out var guiCenter))
                {
                    continue;
                }

                if (IsCarrierTarget(target) || target.IsFlareDecoy || target.IsPlayerAircraft || target.IsNeutral)
                {
                    continue;
                }

                var outpostBuilding = target.GetComponent<OutpostBuilding>();
                if (outpostBuilding != null
                    && !OutpostPrimaryObjective.IsMissionHostileBuilding(
                        outpostBuilding.BuildingType,
                        target.TargetLabel))
                {
                    continue;
                }

                var markerColor = target.IsFriendly ? FriendlyMarkerColor : HostileMarkerColor;
                DrawMarker(guiCenter, squareHalf, squareSize, markerColor, squareTexture);
            }

            if (activeTarget != null
                && activeTarget.IsAlive
                && !IsCarrierTarget(activeTarget)
                && IsTargetOnScreen(activeTarget, out var activeCenter))
            {
                var diamondColor = activeTarget.IsFriendly ? FriendlyMarkerColor : HostileMarkerColor;
                DrawMarker(activeCenter, diamondHalf, diamondDrawSize, diamondColor, diamondTexture);
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

            var diamondPixels = Mathf.RoundToInt(HudTargetMarkerLayout.DiamondSize);
            if (diamondTexture == null || diamondTexture.width != diamondPixels)
            {
                diamondTexture = CreateOutlineDiamondTexture(
                    diamondPixels,
                    HudTargetMarkerLayout.DiamondOutlineThickness);
            }
        }

        private static Texture2D CreateOutlineDiamondTexture(int size, int thickness = 1)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var center = (size - 1) * 0.5f;
            var halfExtent = size * 0.46f;
            var edgeWidth = thickness / halfExtent;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center) / halfExtent;
                    var dy = Mathf.Abs(y - center) / halfExtent;
                    var sum = dx + dy;
                    if (sum > 1f)
                    {
                        continue;
                    }

                    var edgeDistance = 1f - sum;
                    if (edgeDistance <= edgeWidth)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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
