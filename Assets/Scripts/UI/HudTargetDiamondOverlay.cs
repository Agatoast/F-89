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

                if (IsCarrierTarget(target))
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
                squareTexture = CreateSquareOutlineTexture(Mathf.RoundToInt(squareSize));
            }

            if (diamondTexture == null)
            {
                diamondTexture = CreateDiamondOutlineTexture(Mathf.RoundToInt(diamondSize));
            }
        }

        private static Texture2D CreateSquareOutlineTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var onBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    var inHollow = x >= 2 && x <= size - 3 && y >= 2 && y <= size - 3;
                    if (onBorder && !inHollow)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateDiamondOutlineTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            var center = (size - 1) * 0.5f;
            var radius = size * 0.42f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var diamondDistance = Mathf.Abs(x - center) / radius + Mathf.Abs(y - center) / radius;
                    if (Mathf.Abs(diamondDistance - 1f) <= 0.12f)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
