using System.Collections.Generic;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class PlaneRadarOverlay : MonoBehaviour
    {
        public enum RadarScopeKind
        {
            LongRange,
            ShortRange
        }

        private const float PanelGapFromRadar = 8f * RadarMfdBezelRenderer.LayoutScale;
        private const float LongRangeBlipHitRadius = 28f * RadarMfdBezelRenderer.LayoutScale;
        private const float ShortRangeBlipHitRadius = 14f * RadarMfdBezelRenderer.LayoutScale;
        private const float ContactRefreshSeconds = 0.2f;
        private const float LongRangeHostileDotSize = 12f * RadarMfdBezelRenderer.LayoutScale;
        private const float ShortRangeHostileDotSize = 6f * RadarMfdBezelRenderer.LayoutScale;
        private const float LongRangeFriendlyDotSize = 10f * RadarMfdBezelRenderer.LayoutScale;
        private const float ShortRangeFriendlyDotSize = 5f * RadarMfdBezelRenderer.LayoutScale;
        private const float LongRangeSelectedRingSize = 24f * RadarMfdBezelRenderer.LayoutScale;
        private const float ShortRangeSelectedRingSize = 12f * RadarMfdBezelRenderer.LayoutScale;
        private const float OwnshipDotSize = 10f * RadarMfdBezelRenderer.LayoutScale;

        private static readonly Color FriendlyDotColor = Color.white;
        private static readonly Color HostileDotColor = new Color(0.92f, 0.15f, 0.1f);
        private static readonly Color DestroyedOutpostColor = new Color(0.48f, 0.82f, 1f);

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
        [SerializeField] private RadarScopeKind scopeKind = RadarScopeKind.LongRange;

        public RadarScopeKind ScopeKind => scopeKind;

        private readonly List<RadarContact> contacts = new List<RadarContact>();
        private readonly List<RadarBlipLayout> blipLayouts = new List<RadarBlipLayout>();
        private readonly List<MissileBlipLayout> missileBlipLayouts = new List<MissileBlipLayout>();
        private readonly List<HomingMissile> incomingMissiles = new List<HomingMissile>();
        private float nextContactRefreshTime;

        private GUIStyle headerStyle;
        private GUIStyle ringLabelStyle;
        private GUIStyle mfdLabelStyle;
        private GUIStyle osbLabelStyle;
        private GUIStyle annotationStyle;
        private Texture2D circleFaceTexture;
        private Texture2D circleBorderTexture;
        private Texture2D dotTexture;
        private Texture2D triangleTexture;
        private Texture2D outerRingTexture;
        private Texture2D[] bandRingTextures = System.Array.Empty<Texture2D>();
        private RadarScopeKind cachedTextureScope = (RadarScopeKind)(-1);
        private int cachedTextureDiameter = -1;

        private struct RadarBlipLayout
        {
            public RadarContact Contact;
            public Vector2 GuiCenter;
        }

        private enum RadarBlipShape
        {
            Circle,
            Square,
            Triangle,
            WireSquare
        }

        private struct MissileBlipLayout
        {
            public Vector2 GuiCenter;
        }

        private struct MfdLayout
        {
            public Rect AssemblyRect;
            public Rect ScopeRect;
            public Vector2 ScopeCenter;
            public float ScopeRadius;

            public static MfdLayout From(RadarMfdBezelRenderer.Layout layout)
            {
                return new MfdLayout
                {
                    AssemblyRect = layout.AssemblyRect,
                    ScopeRect = layout.ScopeRect,
                    ScopeCenter = layout.ScopeCenter,
                    ScopeRadius = layout.ScopeRadius
                };
            }
        }

        private static readonly string[] TopOsbLabels = { "CRM", "RWS", "NORM", "OVRD", "CNTL" };
        private static readonly string[] BottomOsbLabels = { "SWAP", "FCR", "TEST", "DTE", "DCLT" };

        public void Configure(
            AircraftController aircraftController,
            MissileLockController lockController,
            PlayerWeaponController weapons)
        {
            Configure(aircraftController, lockController, weapons, RadarScopeKind.LongRange);
        }

        public void Configure(
            AircraftController aircraftController,
            MissileLockController lockController,
            PlayerWeaponController weapons,
            RadarScopeKind kind)
        {
            aircraft = aircraftController;
            this.lockController = lockController;
            weaponController = weapons;
            scopeKind = kind;
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
                    GetRangeCapMiles(),
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

            var bezelLayout = GetBezelLayout();
            var layout = MfdLayout.From(bezelLayout);
            var center = layout.ScopeCenter;
            var displayRadius = layout.ScopeRadius;

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            RebuildBlipLayouts(center, displayRadius);

            EnsureStyles();
            EnsureTextures();

            var bezel = RadarMfdBezelRenderer.GetBezelTexture(bezelLayout);

            var previousDepth = GUI.depth;
            GUI.depth = 100;
            GUI.color = Color.white;
            GUI.DrawTexture(layout.AssemblyRect, bezel);

            GUI.depth = 0;
            DrawCircularScope(center, displayRadius);
            DrawRangeRings(center, displayRadius);
            DrawContacts(center, displayRadius);
            DrawIncomingMissileThreats(center, displayRadius);

            GUI.depth = -100;
            DrawMfdLabels(layout, FlightHudColorPalette.Mfd);
            GUI.depth = previousDepth;
            GUI.color = Color.white;
        }

        private void DrawMfdLabels(MfdLayout layout, Color hudColor)
        {
            var assembly = layout.AssemblyRect;
            var scopeRect = layout.ScopeRect;
            var corner = RadarMfdBezelRenderer.CornerRockerSize;
            var s = RadarMfdBezelRenderer.LayoutScale;

            annotationStyle.normal.textColor = hudColor;
            osbLabelStyle.normal.textColor = hudColor;
            mfdLabelStyle.normal.textColor = hudColor;
            ringLabelStyle.normal.textColor = hudColor;

            var innerLeft = assembly.x + corner;
            var innerWidth = assembly.width - corner * 2f;
            var slotWidth = innerWidth / TopOsbLabels.Length;
            for (var i = 0; i < TopOsbLabels.Length; i++)
            {
                var labelSlotWidth = scopeRect.width / TopOsbLabels.Length;
                osbLabelStyle.normal.textColor = i == 1
                    ? hudColor
                    : new Color(hudColor.r, hudColor.g, hudColor.b, 0.75f);
                GUI.Label(
                    new Rect(scopeRect.x + labelSlotWidth * i, scopeRect.y + 2f * s, labelSlotWidth, 14f * s),
                    TopOsbLabels[i],
                    osbLabelStyle);
            }

            var rangeLabel = scopeKind == RadarScopeKind.ShortRange ? "10 MI" : "RDY";
            GUI.Label(new Rect(scopeRect.x, scopeRect.yMax - 18f * s, scopeRect.width, 14f * s), rangeLabel, ringLabelStyle);

            for (var i = 0; i < BottomOsbLabels.Length; i++)
            {
                var slotX = innerLeft + slotWidth * i;
                DrawTickMark(new Vector2(slotX + slotWidth * 0.5f, scopeRect.yMax - 2f * s), hudColor, s);
                var labelRect = new Rect(slotX, assembly.yMax - 14f * s, slotWidth, 14f * s);
                if (BottomOsbLabels[i] == "FCR")
                {
                    DrawSelectedLabelBox(labelRect, BottomOsbLabels[i], hudColor);
                }
                else
                {
                    osbLabelStyle.normal.textColor = new Color(hudColor.r, hudColor.g, hudColor.b, 0.75f);
                    GUI.Label(labelRect, BottomOsbLabels[i], osbLabelStyle);
                }
            }

            DrawScopeOverlayAnnotations(layout, hudColor);
        }

        private void DrawScopeOverlayAnnotations(MfdLayout layout, Color hudColor)
        {
            var scope = layout.ScopeRect;
            var s = RadarMfdBezelRenderer.LayoutScale;
            annotationStyle.normal.textColor = hudColor;
            mfdLabelStyle.normal.textColor = hudColor;

            GUI.Label(new Rect(scope.x + 4f * s, scope.y + 8f * s, 28f * s, 36f * s), $"▲\n{Mathf.RoundToInt(GetRangeMiles())}\n▼", annotationStyle);
            GUI.Label(new Rect(scope.x + 4f * s, scope.y + 52f * s, 28f * s, 16f * s), "A 6", mfdLabelStyle);
            GUI.Label(new Rect(scope.x + 4f * s, scope.y + 70f * s, 28f * s, 16f * s), "4 B", mfdLabelStyle);
            GUI.Label(new Rect(scope.x + 4f * s, scope.y + 88f * s, 28f * s, 16f * s), "M 4", mfdLabelStyle);

            var circleCenter = new Vector2(scope.x + 16f * s, scope.yMax - 28f * s);
            DrawAnnotationCircle(circleCenter, "99", hudColor, 18f * s);
            GUI.Label(new Rect(scope.x + 4f * s, scope.yMax - 14f * s, 28f * s, 12f * s), "300", annotationStyle);

            GUI.Label(new Rect(scope.xMax - 34f * s, scope.y + 8f * s, 30f * s, 14f * s), "CONT", mfdLabelStyle);
            var previous = GUI.color;
            GUI.color = hudColor;
            for (var i = 0; i < 5; i++)
            {
                var tickY = scope.y + 36f * s + i * 22f * s;
                GUI.DrawTexture(new Rect(scope.xMax - 10f * s, tickY, 8f * s, 2f * s), Texture2D.whiteTexture);
            }

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.45f);
            GUI.DrawTexture(
                new Rect(scope.x + 8f * s, scope.y + scope.height * 0.5f - 1f, scope.width - 16f * s, 2f * s),
                Texture2D.whiteTexture);
            GUI.color = previous;

            var bottomCenterX = scope.x + scope.width * 0.5f;
            var bracketTop = scope.yMax - 34f * s;
            GUI.Label(new Rect(bottomCenterX - 18f * s, bracketTop, 16f * s, 28f * s), "|\n35", annotationStyle);
            GUI.Label(new Rect(bottomCenterX + 2f * s, bracketTop, 16f * s, 28f * s), "|\n04", annotationStyle);
        }

        private void DrawSelectedLabelBox(Rect rect, string label, Color hudColor)
        {
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            DrawMfdBorder(rect, 1f, Color.white);
            GUI.color = previous;

            osbLabelStyle.normal.textColor = hudColor;
            GUI.Label(rect, label, osbLabelStyle);
        }

        private void DrawAnnotationCircle(Vector2 center, string label, Color hudColor, float diameter)
        {
            var previous = GUI.color;
            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.25f);
            GUI.DrawTexture(
                new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter),
                Texture2D.whiteTexture);
            GUI.color = hudColor;
            DrawMfdBorder(new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter), 1f, hudColor);
            GUI.color = previous;

            annotationStyle.normal.textColor = hudColor;
            GUI.Label(
                new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter),
                label,
                annotationStyle);
        }

        private static void DrawTickMark(Vector2 center, Color hudColor, float scale)
        {
            var previous = GUI.color;
            GUI.color = hudColor;
            GUI.DrawTexture(new Rect(center.x - 1f * scale, center.y - 4f * scale, 2f * scale, 8f * scale), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawMfdBorder(Rect rect, float thickness, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void DrawCircularScope(Vector2 center, float displayRadius)
        {
            var hudColor = FlightHudColorPalette.Mfd;
            var diameter = displayRadius * 2f;

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
            var hudColor = FlightHudColorPalette.Mfd;
            var rangeMiles = GetRangeMiles();
            var rangeScale = displayRadius / rangeMiles;
            var bandMiles = GetRangeBandMiles();
            var outerSize = displayRadius * 2f;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, outerSize, outerSize),
                outerRingTexture);

            for (var i = 0; i < bandMiles.Length && i < bandRingTextures.Length; i++)
            {
                var bandRadius = bandMiles[i] * rangeScale;
                var bandSize = bandRadius * 2f;
                GUI.DrawTexture(
                    new Rect(center.x - bandRadius, center.y - bandRadius, bandSize, bandSize),
                    bandRingTextures[i]);
            }

            GUI.color = Color.white;

            ringLabelStyle.normal.textColor = hudColor;
            for (var i = 0; i < bandMiles.Length; i++)
            {
                var bandRadius = bandMiles[i] * rangeScale;
                var label = scopeKind == RadarScopeKind.ShortRange
                    ? $"{bandMiles[i]:0} MI"
                    : i == 0
                        ? "50 MI ID"
                        : "100 MI ID";
                var labelSize = ringLabelStyle.CalcSize(new GUIContent(label));
                GUI.Label(
                    new Rect(
                        center.x - labelSize.x * 0.5f,
                        center.y + bandRadius + 4f,
                        labelSize.x,
                        labelSize.y),
                    label,
                    ringLabelStyle);
            }
        }

        private void DrawContacts(Vector2 center, float displayRadius)
        {
            var activeTarget = weaponController != null ? weaponController.GetActiveHudTarget() : null;

            foreach (var layout in blipLayouts)
            {
                var contact = layout.Contact;
                var guiCenter = layout.GuiCenter;
                var isSelected = contact.CanBeTargeted && contact.Target != null && contact.Target == activeTarget;
                var dotSize = contact.IsHostile ? GetHostileDotSize() : GetFriendlyDotSize();
                ResolveBlipSymbology(contact, out var dotColor, out var shape);

                if (isSelected)
                {
                    var hudColor = FlightHudColorPalette.Mfd;
                    DrawDot(guiCenter, GetSelectedRingSize(), new Color(hudColor.r, hudColor.g, hudColor.b, 0.35f));
                    dotSize += 2f;
                }

                if (contact.IsDestroyed)
                {
                    DrawWireSquare(guiCenter, dotSize, DestroyedOutpostColor, lineThickness: 1f);
                    continue;
                }

                if (contact.HasBunker)
                {
                    DrawBunkerBlip(guiCenter, dotSize, contact.IsDestroyed);
                    continue;
                }

                DrawBlip(guiCenter, dotSize, dotColor, shape);
            }
        }

        private static void ResolveBlipSymbology(
            RadarContact contact,
            out Color color,
            out RadarBlipShape shape)
        {
            if (contact.IsBase || contact.BaseSite != null)
            {
                color = FriendlyDotColor;
                shape = contact.IsDestroyed ? RadarBlipShape.WireSquare : RadarBlipShape.Square;
                return;
            }

            if (contact.IsHostile)
            {
                color = HostileDotColor;
                shape = contact.Target != null && contact.Target.IsGroundVehicle
                    ? RadarBlipShape.Square
                    : RadarBlipShape.Circle;
                return;
            }

            color = FriendlyDotColor;
            shape = RadarBlipShape.Triangle;
        }

        private void DrawIncomingMissileThreats(Vector2 center, float displayRadius)
        {
            RebuildMissileBlipLayouts(center, displayRadius);
            foreach (var layout in missileBlipLayouts)
            {
                DrawDot(layout.GuiCenter, GetHostileDotSize() * 0.25f, HostileDotColor);
            }
        }

        private void RebuildMissileBlipLayouts(Vector2 center, float displayRadius)
        {
            missileBlipLayouts.Clear();
            if (aircraft?.Profile == null)
            {
                return;
            }

            HomingMissile.CollectIncomingEnemyThreats(incomingMissiles);
            if (incomingMissiles.Count == 0)
            {
                return;
            }

            var scale = displayRadius / GetRangeMiles();
            var worldUnitsPerMile = GetWorldUnitsPerMile();

            foreach (var missile in incomingMissiles)
            {
                if (missile == null)
                {
                    continue;
                }

                if (TryGetWorldBlipGuiCenter(
                        center,
                        scale,
                        worldUnitsPerMile,
                        missile.WorldPosition,
                        out var guiCenter))
                {
                    missileBlipLayouts.Add(new MissileBlipLayout { GuiCenter = guiCenter });
                }
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
                    GetRangeCapMiles(),
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

            var scale = displayRadius / GetRangeMiles();
            var worldUnitsPerMile = GetWorldUnitsPerMile();

            foreach (var contact in contacts)
            {
                if (TryGetWorldBlipGuiCenter(
                        center,
                        scale,
                        worldUnitsPerMile,
                        contact.WorldPosition,
                        out var guiCenter))
                {
                    blipLayouts.Add(new RadarBlipLayout { Contact = contact, GuiCenter = guiCenter });
                }
            }
        }

        private bool TryGetBlipGuiCenter(
            Vector2 radarCenter,
            float scale,
            float worldUnitsPerMile,
            RadarContact contact,
            out Vector2 guiCenter)
        {
            return TryGetWorldBlipGuiCenter(
                radarCenter,
                scale,
                worldUnitsPerMile,
                contact.WorldPosition,
                out guiCenter);
        }

        private bool TryGetWorldBlipGuiCenter(
            Vector2 radarCenter,
            float scale,
            float worldUnitsPerMile,
            Vector3 worldPosition,
            out Vector2 guiCenter)
        {
            guiCenter = default;
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            var observer = aircraft.transform.position;
            var toTarget = worldPosition - observer;
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

            var guiOffset = new Vector2(rightMiles * scale, -forwardMiles * scale);
            guiCenter = radarCenter + guiOffset;

            var maxRadius = GetRangeMiles() * scale;
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

        private void DrawBlip(Vector2 center, float size, Color color, RadarBlipShape shape)
        {
            if (shape == RadarBlipShape.Circle)
            {
                DrawDot(center, size, color);
                return;
            }

            if (shape == RadarBlipShape.WireSquare)
            {
                DrawWireSquare(center, size, color);
                return;
            }

            var previous = GUI.color;
            GUI.color = color;
            var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            GUI.DrawTexture(rect, shape == RadarBlipShape.Square ? Texture2D.whiteTexture : triangleTexture);
            GUI.color = previous;
        }

        private static void DrawBunkerBlip(Vector2 center, float size, bool isDestroyed)
        {
            var frameColor = isDestroyed ? Color.black : FriendlyDotColor;
            var xColor = isDestroyed ? Color.black : HostileDotColor;
            DrawWireSquare(center, size, frameColor);
            DrawX(center, size, xColor);
        }

        private static void DrawWireSquare(Vector2 center, float size, Color color, float lineThickness = 2f)
        {
            var previous = GUI.color;
            GUI.color = color;
            var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, lineThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - lineThickness, rect.width, lineThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, lineThickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - lineThickness, rect.y, lineThickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawX(Vector2 center, float size, Color color)
        {
            const float lineThickness = 2f;
            var previous = GUI.color;
            GUI.color = color;
            var inset = Mathf.Max(1f, size * 0.2f);
            var span = Mathf.Max(1f, size - inset * 2f);
            var steps = Mathf.Max(1, Mathf.CeilToInt(span));
            for (var i = 0; i < steps; i++)
            {
                var offset = i / (float)steps * span;
                GUI.DrawTexture(
                    new Rect(center.x - span * 0.5f + offset, center.y - span * 0.5f + offset, lineThickness, lineThickness),
                    Texture2D.whiteTexture);
                GUI.DrawTexture(
                    new Rect(center.x + span * 0.5f - offset, center.y - span * 0.5f + offset, lineThickness, lineThickness),
                    Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        private bool TryHandleRadarClick(Vector2 guiPoint)
        {
            if (lockController == null)
            {
                return false;
            }

            var layout = MfdLayout.From(GetBezelLayout());
            if (Vector2.Distance(guiPoint, layout.ScopeCenter) > layout.ScopeRadius + GetBlipHitRadius())
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

            var layout = MfdLayout.From(GetBezelLayout());
            var center = layout.ScopeCenter;
            var displayRadius = layout.ScopeRadius;

            EnsureContactsFresh();
            RebuildBlipLayouts(center, displayRadius);

            LockableTarget bestTarget = null;
            var bestDistance = GetBlipHitRadius();

            foreach (var blipLayout in blipLayouts)
            {
                var contact = blipLayout.Contact;
                var target = contact.Target;
                if (!contact.CanBeTargeted || target == null || !target.IsAlive)
                {
                    continue;
                }

                var distance = Vector2.Distance(guiPoint, blipLayout.GuiCenter);
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

        private static MfdLayout GetMfdLayout()
        {
            return MfdLayout.From(RadarMfdBezelRenderer.ComputeBottomLeftLayout());
        }

        public static Rect GetRectAboveRadar(float height)
        {
            var layout = MfdLayout.From(RadarMfdBezelRenderer.ComputeBottomLeftLayout());
            return new Rect(
                layout.AssemblyRect.x,
                layout.AssemblyRect.y - PanelGapFromRadar - height,
                layout.AssemblyRect.width,
                height);
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
            var hudColor = FlightHudColorPalette.Mfd;
            var s = RadarMfdBezelRenderer.LayoutScale;
            if (headerStyle == null)
            {
                headerStyle = HudStyleFactory.CreateLabel(Mathf.RoundToInt(14f * s), FontStyle.Bold, TextAnchor.MiddleLeft, hudColor);
            }
            else
            {
                headerStyle.normal.textColor = hudColor;
            }

            if (ringLabelStyle == null)
            {
                ringLabelStyle = HudStyleFactory.CreateLabel(Mathf.RoundToInt(12f * s), FontStyle.Bold, TextAnchor.MiddleCenter, hudColor);
            }
            else
            {
                ringLabelStyle.normal.textColor = hudColor;
            }

            if (mfdLabelStyle == null)
            {
                mfdLabelStyle = HudStyleFactory.CreateLabel(Mathf.RoundToInt(11f * s), FontStyle.Bold, TextAnchor.MiddleLeft, hudColor);
            }
            else
            {
                mfdLabelStyle.normal.textColor = hudColor;
            }

            if (osbLabelStyle == null)
            {
                osbLabelStyle = HudStyleFactory.CreateLabel(
                    Mathf.RoundToInt(10f * s),
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    hudColor);
            }
            else
            {
                osbLabelStyle.normal.textColor = hudColor;
            }

            if (annotationStyle == null)
            {
                annotationStyle = HudStyleFactory.CreateLabel(
                    Mathf.RoundToInt(9f * s),
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    hudColor);
            }
            else
            {
                annotationStyle.normal.textColor = hudColor;
            }
        }

        private void EnsureTextures()
        {
            var diameter = Mathf.RoundToInt(RadarMfdBezelRenderer.DisplayDiameter);
            if (cachedTextureScope == scopeKind && cachedTextureDiameter == diameter)
            {
                return;
            }

            circleFaceTexture = CreateFilledCircleTexture(diameter, 1f);
            circleBorderTexture = CreateCircleBorderTexture(diameter, 2f);
            dotTexture = CreateFilledCircleTexture(Mathf.RoundToInt(24f * RadarMfdBezelRenderer.LayoutScale), 1f);
            triangleTexture = CreateTriangleTexture(Mathf.RoundToInt(24f * RadarMfdBezelRenderer.LayoutScale));
            outerRingTexture = CreateDottedCircleTexture(diameter, 5);

            var bandMiles = GetRangeBandMiles();
            bandRingTextures = new Texture2D[bandMiles.Length];
            for (var i = 0; i < bandMiles.Length; i++)
            {
                var bandDiameter = Mathf.RoundToInt(
                    RadarMfdBezelRenderer.DisplayDiameter * (bandMiles[i] / GetRangeMiles()));
                bandRingTextures[i] = CreateDottedCircleTexture(bandDiameter, 4);
            }

            cachedTextureScope = scopeKind;
            cachedTextureDiameter = diameter;
        }

        private RadarMfdBezelRenderer.Layout GetBezelLayout()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? RadarMfdBezelRenderer.ComputeBottomRightLayout()
                : RadarMfdBezelRenderer.ComputeBottomLeftLayout();
        }

        private float GetBlipHitRadius()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? ShortRangeBlipHitRadius
                : LongRangeBlipHitRadius;
        }

        private float GetHostileDotSize()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? ShortRangeHostileDotSize
                : LongRangeHostileDotSize;
        }

        private float GetFriendlyDotSize()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? ShortRangeFriendlyDotSize
                : LongRangeFriendlyDotSize;
        }

        private float GetSelectedRingSize()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? ShortRangeSelectedRingSize
                : LongRangeSelectedRingSize;
        }

        private float? GetRangeCapMiles()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? RadarContactScanner.ShortRangeMiles
                : null;
        }

        private float GetRangeMiles()
        {
            return scopeKind == RadarScopeKind.ShortRange
                ? RadarContactScanner.ShortRangeMiles
                : RadarContactScanner.RangeMiles;
        }

        private float[] GetRangeBandMiles()
        {
            if (scopeKind == RadarScopeKind.ShortRange)
            {
                return new[] { RadarContactScanner.ShortRangeBandMiles };
            }

            return new[]
            {
                RadarContactScanner.HostileDetectionMiles,
                RadarContactScanner.MidRangeBandMiles
            };
        }

        private static Texture2D CreateTriangleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                var halfWidth = (y + 1) * 0.5f;
                var center = (size - 1) * 0.5f;
                for (var x = 0; x < size; x++)
                {
                    if (Mathf.Abs(x - center) <= halfWidth)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
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
