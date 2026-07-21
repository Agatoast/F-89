using System.Collections.Generic;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class PlaneRadarOverlay : MonoBehaviour
    {
        private const float DisplayDiameter = 420f;
        private const float ScreenMargin = 16f;
        private const float RadarVerticalOffsetPixels = 40f;
        private const float HeaderBlockHeight = 42f;
        private const float HeaderLineHeight = 18f;
        private const float BlipHitRadius = 28f;
        private const float ContactRefreshSeconds = 0.2f;
        private const float HostileDotSize = 12f;
        private const float FriendlyDotSize = 10f;
        private const float SelectedRingSize = 24f;
        private const float OwnshipDotSize = 10f;

        private static readonly Color FriendlyDotColor = Color.white;
        private static readonly Color HostileDotColor = new Color(0.92f, 0.15f, 0.1f);

        public static Color GetBlipColor(LockableTarget target)
        {
            if (target != null && !target.IsFriendly)
            {
                return HostileDotColor;
            }

            return FriendlyDotColor;
        }

        public static Color FriendlyBlipColor => FriendlyDotColor;
        public static Color HostileBlipColor => HostileDotColor;

        [SerializeField] private AircraftController aircraft;
        [SerializeField] private MissileLockController lockController;
        [SerializeField] private PlayerWeaponController weaponController;

        private readonly List<RadarContact> contacts = new List<RadarContact>();
        private readonly List<RadarBlipLayout> blipLayouts = new List<RadarBlipLayout>();
        private float nextContactRefreshTime;

        private GUIStyle headerStyle;
        private GUIStyle ringLabelStyle;
        private Texture2D circleFaceTexture;
        private Texture2D circleBorderTexture;
        private Texture2D dotTexture;
        private Texture2D outerRingTexture;
        private Texture2D innerRingTexture;
        private int cachedTextureDiameter = -1;

        private struct RadarBlipLayout
        {
            public RadarContact Contact;
            public Vector2 GuiCenter;
        }

        public void Configure(
            AircraftController aircraftController,
            MissileLockController lockController,
            PlayerWeaponController weapons)
        {
            aircraft = aircraftController;
            this.lockController = lockController;
            weaponController = weapons;
        }

        private void Update()
        {
            if (GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null
                || aircraft.WorldMap == null
                || aircraft.Profile == null)
            {
                return;
            }

            if (Time.unscaledTime >= nextContactRefreshTime)
            {
                nextContactRefreshTime = Time.unscaledTime + ContactRefreshSeconds;
                RadarContactScanner.CollectVisibleContacts(
                    aircraft.transform.position,
                    aircraft.WorldMap,
                    aircraft.Profile.ticSizeWorldUnits,
                    contacts);
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryHandleRadarClick(GetGuiMousePosition());
            }
        }

        private static Vector2 GetGuiMousePosition()
        {
            var mouse = Input.mousePosition;
            return new Vector2(mouse.x, Screen.height - mouse.y);
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null
                || aircraft.WorldMap == null
                || aircraft.Profile == null)
            {
                return;
            }

            var displayRadius = DisplayDiameter * 0.5f;
            var center = GetRadarGuiCenter(displayRadius);
            EnsureContactsFresh();
            RebuildBlipLayouts(center, displayRadius);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();
            EnsureTextures();

            var scopeRect = new Rect(
                center.x - displayRadius,
                center.y - displayRadius,
                DisplayDiameter,
                DisplayDiameter);

            DrawCircularScope(center, displayRadius);
            DrawRangeRings(center, displayRadius);
            DrawContacts(center, displayRadius);
            DrawScopeHeader(center, displayRadius);
        }

        private void DrawCircularScope(Vector2 center, float displayRadius)
        {
            var hudColor = FlightHudColorPalette.Current;
            var diameter = DisplayDiameter;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.14f);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, diameter, diameter),
                circleFaceTexture);

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.35f);
            GUI.DrawTexture(
                new Rect(center.x - 1f, center.y - displayRadius + 2f, 2f, (displayRadius - 2f) * 2f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius + 2f, center.y - 1f, (displayRadius - 2f) * 2f, 2f),
                Texture2D.whiteTexture);

            GUI.color = hudColor;
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, diameter, diameter),
                circleBorderTexture);
            DrawDot(center, OwnshipDotSize, hudColor);
            GUI.color = Color.white;
        }

        private void DrawRangeRings(Vector2 center, float displayRadius)
        {
            var hudColor = FlightHudColorPalette.Current;
            var innerRadius = displayRadius * (RadarContactScanner.HostileDetectionMiles / RadarContactScanner.RangeMiles);
            var innerSize = innerRadius * 2f;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, DisplayDiameter, DisplayDiameter),
                outerRingTexture);
            GUI.DrawTexture(
                new Rect(center.x - innerRadius, center.y - innerRadius, innerSize, innerSize),
                innerRingTexture);
            GUI.color = Color.white;

            var jamLabel = "50 MI ID";
            var jamSize = ringLabelStyle.CalcSize(new GUIContent(jamLabel));
            ringLabelStyle.normal.textColor = hudColor;
            GUI.Label(
                new Rect(
                    center.x - jamSize.x * 0.5f,
                    center.y + innerRadius + 4f,
                    jamSize.x,
                    jamSize.y),
                jamLabel,
                ringLabelStyle);
        }

        private void DrawContacts(Vector2 center, float displayRadius)
        {
            var activeTarget = weaponController != null ? weaponController.GetActiveHudTarget() : null;

            foreach (var layout in blipLayouts)
            {
                var contact = layout.Contact;
                var guiCenter = layout.GuiCenter;
                var isSelected = contact.Target != null && contact.Target == activeTarget;
                var dotSize = contact.IsHostile ? HostileDotSize : FriendlyDotSize;
                var dotColor = contact.IsHostile ? HostileDotColor : FriendlyDotColor;

                if (isSelected)
                {
                    var hudColor = FlightHudColorPalette.Current;
                    DrawDot(guiCenter, SelectedRingSize, new Color(hudColor.r, hudColor.g, hudColor.b, 0.35f));
                    dotSize += 2f;
                }

                DrawDot(guiCenter, dotSize, dotColor);
            }
        }

        private void EnsureContactsFresh()
        {
            if (aircraft == null || aircraft.WorldMap == null || aircraft.Profile == null)
            {
                return;
            }

            if (contacts.Count == 0 || Time.unscaledTime >= nextContactRefreshTime)
            {
                nextContactRefreshTime = Time.unscaledTime + ContactRefreshSeconds;
                RadarContactScanner.CollectVisibleContacts(
                    aircraft.transform.position,
                    aircraft.WorldMap,
                    aircraft.Profile.ticSizeWorldUnits,
                    contacts);
            }
        }

        private void RebuildBlipLayouts(Vector2 center, float displayRadius)
        {
            blipLayouts.Clear();
            if (aircraft?.Profile == null)
            {
                return;
            }

            var scale = displayRadius / RadarContactScanner.RangeMiles;
            var worldUnitsPerMile = GetWorldUnitsPerMile();

            foreach (var contact in contacts)
            {
                if (!TryGetBlipGuiCenter(center, scale, worldUnitsPerMile, contact, out var guiCenter))
                {
                    continue;
                }

                blipLayouts.Add(new RadarBlipLayout { Contact = contact, GuiCenter = guiCenter });
            }
        }

        private bool TryGetBlipGuiCenter(
            Vector2 radarCenter,
            float scale,
            float worldUnitsPerMile,
            RadarContact contact,
            out Vector2 guiCenter)
        {
            guiCenter = default;
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            var observer = aircraft.transform.position;
            var toTarget = contact.WorldPosition - observer;
            toTarget.y = 0f;

            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var right = new Vector3(forward.z, 0f, -forward.x);
            var forwardMiles = Vector3.Dot(toTarget, forward) / worldUnitsPerMile;
            var rightMiles = Vector3.Dot(toTarget, right) / worldUnitsPerMile;

            // Body-relative: nose toward top of scope, right wing toward screen right.
            var guiOffset = new Vector2(rightMiles * scale, -forwardMiles * scale);
            guiCenter = radarCenter + guiOffset;

            var maxRadius = RadarContactScanner.RangeMiles * scale;
            return guiOffset.magnitude <= maxRadius + 0.5f;
        }

        private void DrawDot(Vector2 center, float diameter, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter),
                dotTexture);
            GUI.color = previous;
        }

        private void DrawScopeHeader(Vector2 center, float displayRadius)
        {
            var hudColor = FlightHudColorPalette.Current;
            headerStyle.normal.textColor = hudColor;
            ringLabelStyle.normal.textColor = hudColor;

            var scopeTop = center.y - displayRadius;
            var headerTop = scopeTop - HeaderBlockHeight;
            var headerWidth = DisplayDiameter;
            var headerLeft = center.x - headerWidth * 0.5f;

            GUI.Label(
                new Rect(headerLeft, headerTop, headerWidth, HeaderLineHeight),
                "RADAR 150 MI",
                headerStyle);

            GUI.Label(
                new Rect(headerLeft, headerTop + HeaderLineHeight, headerWidth, HeaderLineHeight),
                "JAMMING — HOSTILES ≤50 MI",
                ringLabelStyle);
        }

        private bool TryHandleRadarClick(Vector2 guiPoint)
        {
            if (lockController == null)
            {
                return false;
            }

            var displayRadius = DisplayDiameter * 0.5f;
            var center = GetRadarGuiCenter(displayRadius);
            if (Vector2.Distance(guiPoint, center) > displayRadius + BlipHitRadius)
            {
                return false;
            }

            if (!TrySelectBlipAtGuiPoint(guiPoint))
            {
                return false;
            }

            if (Event.current != null)
            {
                Event.current.Use();
            }

            return true;
        }

        public bool TrySelectBlipAtScreenPosition(Vector2 screenPosition)
        {
            return TrySelectBlipAtGuiPoint(ScreenToGuiPoint(screenPosition));
        }

        private bool TrySelectBlipAtGuiPoint(Vector2 guiPoint)
        {
            if (lockController == null
                || aircraft == null
                || aircraft.WorldMap == null
                || aircraft.Profile == null)
            {
                return false;
            }

            var displayRadius = DisplayDiameter * 0.5f;
            var center = GetRadarGuiCenter(displayRadius);

            EnsureContactsFresh();
            RebuildBlipLayouts(center, displayRadius);

            LockableTarget bestTarget = null;
            var bestDistance = BlipHitRadius;

            foreach (var layout in blipLayouts)
            {
                var contact = layout.Contact;
                var target = contact.Target;
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var distance = Vector2.Distance(guiPoint, layout.GuiCenter);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = target;
            }

            if (bestTarget == null)
            {
                return false;
            }

            return lockController.TrySelectRadarContact(bestTarget);
        }

        private static Vector2 GetRadarGuiCenter(float displayRadius)
        {
            return new Vector2(
                Screen.width - ScreenMargin - displayRadius,
                Screen.height - ScreenMargin - displayRadius - HeaderBlockHeight + RadarVerticalOffsetPixels);
        }

        private static Vector2 ScreenToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private float GetWorldUnitsPerMile()
        {
            return aircraft.WorldMap.GridSpacingTics
                * aircraft.Profile.ticSizeWorldUnits
                / aircraft.WorldMap.milesPerGrid;
        }

        private void EnsureStyles()
        {
            var hudColor = FlightHudColorPalette.Current;
            if (headerStyle == null)
            {
                headerStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.MiddleCenter, hudColor);
            }
            else
            {
                headerStyle.normal.textColor = hudColor;
            }

            if (ringLabelStyle == null)
            {
                ringLabelStyle = HudStyleFactory.CreateLabel(12, FontStyle.Bold, TextAnchor.MiddleCenter, hudColor);
            }
            else
            {
                ringLabelStyle.normal.textColor = hudColor;
            }
        }

        private void EnsureTextures()
        {
            var diameter = Mathf.RoundToInt(DisplayDiameter);
            if (cachedTextureDiameter == diameter)
            {
                return;
            }

            circleFaceTexture = CreateFilledCircleTexture(diameter, 1f);
            circleBorderTexture = CreateCircleBorderTexture(diameter, 2f);
            dotTexture = CreateFilledCircleTexture(24, 1f);
            outerRingTexture = CreateDottedCircleTexture(diameter, 5);
            var innerDiameter = Mathf.RoundToInt(
                DisplayDiameter * (RadarContactScanner.HostileDetectionMiles / RadarContactScanner.RangeMiles));
            innerRingTexture = CreateDottedCircleTexture(innerDiameter, 4);
            cachedTextureDiameter = diameter;
        }

        private static Texture2D CreateFilledCircleTexture(int diameter, float edgeSoftness)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[diameter * diameter];
            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius)
                    {
                        pixels[y * diameter + x] = Color.clear;
                        continue;
                    }

                    var alpha = distance > radius - edgeSoftness
                        ? Mathf.Clamp01((radius - distance) / edgeSoftness)
                        : 1f;
                    pixels[y * diameter + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateCircleBorderTexture(int diameter, float thickness)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[diameter * diameter];
            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var onBorder = Mathf.Abs(distance - radius) <= thickness;
                    pixels[y * diameter + x] = onBorder ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateDottedCircleTexture(int diameter, int segmentPixels)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[diameter * diameter];
            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;
            var dashPeriod = segmentPixels * 2;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(distance - radius) > 0.6f)
                    {
                        pixels[y * diameter + x] = Color.clear;
                        continue;
                    }

                    var angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    var arcLength = angle * radius;
                    pixels[y * diameter + x] = Mathf.FloorToInt(arcLength / dashPeriod) % 2 == 0
                        ? Color.white
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
