using System.Collections.Generic;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class PlaneRadarOverlay : MonoBehaviour
    {
        private const float PanelGapFromRadar = 8f * RadarMfdBezelRenderer.LayoutScale;
        private const float BlipHitRadius = 28f * RadarMfdBezelRenderer.LayoutScale;
        private const float ContactRefreshSeconds = 0.2f;
        private const float HostileDotSize = 12f * RadarMfdBezelRenderer.LayoutScale;
        private const float FriendlyDotSize = 10f * RadarMfdBezelRenderer.LayoutScale;
        private const float IncomingMissileDotSize = HostileDotSize * 0.25f;
        private const float SelectedRingSize = 24f * RadarMfdBezelRenderer.LayoutScale;
        private const float OwnshipDotSize = 10f * RadarMfdBezelRenderer.LayoutScale;

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
        private Texture2D outerRingTexture;
        private Texture2D midRingTexture;
        private Texture2D innerRingTexture;
        private int cachedTextureDiameter = -1;

        private struct RadarBlipLayout
        {
            public RadarContact Contact;
            public Vector2 GuiCenter;
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

            var bezelLayout = RadarMfdBezelRenderer.ComputeLayout();
            var layout = MfdLayout.From(bezelLayout);
            var center = layout.ScopeCenter;
            var displayRadius = layout.ScopeRadius;
            EnsureContactsFresh();
            RebuildBlipLayouts(center, displayRadius);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

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

            GUI.Label(new Rect(scopeRect.x, scopeRect.yMax - 18f * s, scopeRect.width, 14f * s), "RDY", ringLabelStyle);

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

            GUI.Label(new Rect(scope.x + 4f * s, scope.y + 8f * s, 28f * s, 36f * s), "▲\n150\n▼", annotationStyle);
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
            var rangeScale = displayRadius / RadarContactScanner.RangeMiles;
            var innerRadius = RadarContactScanner.HostileDetectionMiles * rangeScale;
            var midRadius = RadarContactScanner.MidRangeBandMiles * rangeScale;
            var innerSize = innerRadius * 2f;
            var midSize = midRadius * 2f;

            var outerSize = displayRadius * 2f;
            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, outerSize, outerSize),
                outerRingTexture);
            GUI.DrawTexture(
                new Rect(center.x - midRadius, center.y - midRadius, midSize, midSize),
                midRingTexture);
            GUI.DrawTexture(
                new Rect(center.x - innerRadius, center.y - innerRadius, innerSize, innerSize),
                innerRingTexture);
            GUI.color = Color.white;

            ringLabelStyle.normal.textColor = hudColor;

            var midLabel = "100 MI ID";
            var midSizeLabel = ringLabelStyle.CalcSize(new GUIContent(midLabel));
            GUI.Label(
                new Rect(
                    center.x - midSizeLabel.x * 0.5f,
                    center.y + midRadius + 4f,
                    midSizeLabel.x,
                    midSizeLabel.y),
                midLabel,
                ringLabelStyle);

            var innerLabel = "50 MI ID";
            var innerSizeLabel = ringLabelStyle.CalcSize(new GUIContent(innerLabel));
            GUI.Label(
                new Rect(
                    center.x - innerSizeLabel.x * 0.5f,
                    center.y + innerRadius + 4f,
                    innerSizeLabel.x,
                    innerSizeLabel.y),
                innerLabel,
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
                    var hudColor = FlightHudColorPalette.Mfd;
                    DrawDot(guiCenter, SelectedRingSize, new Color(hudColor.r, hudColor.g, hudColor.b, 0.35f));
                    dotSize += 2f;
                }

                DrawDot(guiCenter, dotSize, dotColor);
            }
        }

        private void DrawIncomingMissileThreats(Vector2 center, float displayRadius)
        {
            RebuildMissileBlipLayouts(center, displayRadius);
            foreach (var layout in missileBlipLayouts)
            {
                DrawDot(layout.GuiCenter, IncomingMissileDotSize, HostileDotColor);
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

            var scale = displayRadius / RadarContactScanner.RangeMiles;
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

        private bool TryHandleRadarClick(Vector2 guiPoint)
        {
            if (lockController == null)
            {
                return false;
            }

            var layout = MfdLayout.From(RadarMfdBezelRenderer.ComputeLayout());
            if (Vector2.Distance(guiPoint, layout.ScopeCenter) > layout.ScopeRadius + BlipHitRadius)
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

            var layout = MfdLayout.From(RadarMfdBezelRenderer.ComputeLayout());
            var center = layout.ScopeCenter;
            var displayRadius = layout.ScopeRadius;

            EnsureContactsFresh();
            RebuildBlipLayouts(center, displayRadius);

            LockableTarget bestTarget = null;
            var bestDistance = BlipHitRadius;

            foreach (var blipLayout in blipLayouts)
            {
                var contact = blipLayout.Contact;
                var target = contact.Target;
                if (target == null || !target.IsAlive)
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
            return MfdLayout.From(RadarMfdBezelRenderer.ComputeLayout());
        }

        public static Rect GetRectAboveRadar(float height)
        {
            var layout = MfdLayout.From(RadarMfdBezelRenderer.ComputeLayout());
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
            if (cachedTextureDiameter == diameter)
            {
                return;
            }

            circleFaceTexture = CreateFilledCircleTexture(diameter, 1f);
            circleBorderTexture = CreateCircleBorderTexture(diameter, 2f);
            dotTexture = CreateFilledCircleTexture(Mathf.RoundToInt(24f * RadarMfdBezelRenderer.LayoutScale), 1f);
            outerRingTexture = CreateDottedCircleTexture(diameter, 5);
            var midDiameter = Mathf.RoundToInt(
                RadarMfdBezelRenderer.DisplayDiameter * (RadarContactScanner.MidRangeBandMiles / RadarContactScanner.RangeMiles));
            midRingTexture = CreateDottedCircleTexture(midDiameter, 4);
            var innerDiameter = Mathf.RoundToInt(
                RadarMfdBezelRenderer.DisplayDiameter * (RadarContactScanner.HostileDetectionMiles / RadarContactScanner.RangeMiles));
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
