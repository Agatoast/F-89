using System.Collections.Generic;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class FlightHud : MonoBehaviour
    {
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private PlayerWeaponController weapons;

        private GUIStyle labelStyle;
        private GUIStyle bearingLabelStyle;
        private GUIStyle compassLabelStyle;
        private Color lastHudColor = Color.clear;
        private Color lastBearingHudColor = Color.clear;
        private Color lastCompassHudColor = Color.clear;
        private Texture2D lineTexture;

        private const float MapBearingArrowDistancePixels = 400f;
        private const float MissileIndicatorPerpendicularSpacing = 72f;
        private const float MissileIndicatorRadialSpacing = 44f;
        private const float MissileDirectionGroupDegrees = 14f;
        private const float CompassPixelsPerDegree = 4f;
        private const float CompassTickHeightPixels = 10f;
        private const float CompassLineThickness = 1.5f;
        private const float CompassTopMarginPixels = 50f;
        private const float CompassVisibleArcDegrees = 190f;
        private const float CompassWaypointMarkerDiameter = 10f;
        private const int CompassLabelFontSize = 12;

        private Texture2D compassWaypointMarkerTexture;
        private readonly List<HomingMissile> incomingMissileThreats = new List<HomingMissile>();

        private struct MissileIndicatorLayout
        {
            public HomingMissile Missile;
            public Vector2 Direction;
            public int SpreadIndex;
            public int SpreadCount;
        }

        private readonly List<MissileIndicatorLayout> missileIndicatorLayouts = new List<MissileIndicatorLayout>();
        private readonly List<(HomingMissile Missile, float Angle, Vector2 Direction)> missileDirectionScratch =
            new List<(HomingMissile, float, Vector2)>();

        private static readonly Color IncomingMissileIndicatorColor = new Color(0.92f, 0.15f, 0.1f);

        public void Configure(AircraftController aircraftController, PlayerWeaponController weaponController)
        {
            aircraft = aircraftController;
            weapons = weaponController;
        }

        private void Update()
        {
            if (GamePauseController.IsPaused)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                FlightHudColorPalette.CycleNext();
            }
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();

            DrawAutopilotAboveRadar();
            DrawMissileAcquisitionWarning();
            DrawTopRightWeapons();
            DrawHeadingCompass();
            DrawMapBearingIndicator();
            DrawIncomingMissileThreatIndicators();
        }

        private void DrawHeadingCompass()
        {
            var hudColor = FlightHudColorPalette.Current;
            var centerX = Screen.width * 0.5f;
            const float lubberHalfHeight = 15f;
            var centerY = CompassTopMarginPixels + lubberHalfHeight;
            var heading = GetHeadingDegrees();
            var visibleHalfDegrees = CompassVisibleArcDegrees * 0.5f;
            var tickHalfHeight = CompassTickHeightPixels * 0.5f;

            var tickTop = centerY - tickHalfHeight;
            var tickBottom = centerY + tickHalfHeight;
            DrawHudLine(
                new Vector2(centerX, centerY - lubberHalfHeight),
                new Vector2(centerX, centerY + lubberHalfHeight),
                hudColor,
                CompassLineThickness + 0.5f);

            var startMark = Mathf.FloorToInt((heading - visibleHalfDegrees) / 10f) * 10;
            var endMark = Mathf.CeilToInt((heading + visibleHalfDegrees) / 10f) * 10;

            EnsureCompassLabelStyle(hudColor);

            for (var mark = startMark; mark <= endMark; mark += 10)
            {
                var delta = Mathf.DeltaAngle(heading, mark);
                var x = centerX + delta * CompassPixelsPerDegree;
                if (x < -24f || x > Screen.width + 24f)
                {
                    continue;
                }

                DrawHudLine(
                    new Vector2(x, tickTop),
                    new Vector2(x, tickBottom),
                    hudColor,
                    CompassLineThickness);

                var normalized = ((mark % 360) + 360) % 360;
                if (normalized != 0 && normalized != 90 && normalized != 180 && normalized != 270)
                {
                    continue;
                }

                var label = normalized.ToString();
                var labelSize = compassLabelStyle.CalcSize(new GUIContent(label));
                var labelRect = new Rect(
                    x - labelSize.x * 0.5f,
                    tickTop - labelSize.y - 3f,
                    labelSize.x,
                    labelSize.y);
                GUI.color = hudColor;
                GUI.Label(labelRect, label, compassLabelStyle);
                GUI.color = Color.white;
            }

            DrawCompassWaypointMarker(centerX, centerY, heading, visibleHalfDegrees, hudColor);
            DrawCompassSelectedTargetMarker(centerX, centerY, heading, visibleHalfDegrees);
        }

        private void DrawCompassSelectedTargetMarker(
            float centerX,
            float centerY,
            float heading,
            float visibleHalfDegrees)
        {
            if (weapons == null)
            {
                return;
            }

            var target = weapons.GetActiveHudTarget();
            if (target == null || !target.IsAlive)
            {
                return;
            }

            DrawCompassCircleMarker(
                centerX,
                centerY,
                heading,
                visibleHalfDegrees,
                target.transform.position,
                PlaneRadarOverlay.GetBlipColor(target));
        }

        private void DrawCompassWaypointMarker(
            float centerX,
            float centerY,
            float heading,
            float visibleHalfDegrees,
            Color hudColor)
        {
            if (!AntarcticaMapOverlay.TryGetHudBearing(out var targetWorld, out _, out _)
                || aircraft == null)
            {
                return;
            }

            DrawCompassCircleMarker(
                centerX,
                centerY,
                heading,
                visibleHalfDegrees,
                targetWorld,
                hudColor);
        }

        private void DrawCompassCircleMarker(
            float centerX,
            float centerY,
            float heading,
            float visibleHalfDegrees,
            Vector3 targetWorld,
            Color markerColor)
        {
            if (aircraft == null)
            {
                return;
            }

            targetWorld.y = 0f;
            var planePos = aircraft.transform.position;
            planePos.y = 0f;
            var toTarget = targetWorld - planePos;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            var bearing = (Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg + 360f) % 360f;
            var delta = Mathf.DeltaAngle(heading, bearing);
            if (Mathf.Abs(delta) > visibleHalfDegrees)
            {
                return;
            }

            var markerX = centerX + delta * CompassPixelsPerDegree;
            EnsureCompassWaypointMarkerTexture();
            var radius = CompassWaypointMarkerDiameter * 0.5f;
            var previous = GUI.color;
            GUI.color = markerColor;
            GUI.DrawTexture(
                new Rect(markerX - radius, centerY - radius, CompassWaypointMarkerDiameter, CompassWaypointMarkerDiameter),
                compassWaypointMarkerTexture);
            GUI.color = previous;
        }

        private void EnsureCompassWaypointMarkerTexture()
        {
            if (compassWaypointMarkerTexture != null)
            {
                return;
            }

            const int diameter = 16;
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
                    pixels[y * diameter + x] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            compassWaypointMarkerTexture = texture;
        }

        private float GetHeadingDegrees()
        {
            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return (Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + 360f) % 360f;
        }

        private void EnsureCompassLabelStyle(Color hudColor)
        {
            if (compassLabelStyle != null && hudColor == lastCompassHudColor)
            {
                return;
            }

            lastCompassHudColor = hudColor;
            compassLabelStyle = HudStyleFactory.CreateLabel(
                CompassLabelFontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                hudColor);
        }

        private void DrawMapBearingIndicator()
        {
            if (!AntarcticaMapOverlay.TryGetHudBearing(
                    out var targetWorld,
                    out _,
                    out var useAutopilotTimeWarp))
            {
                return;
            }

            var etaSpeedMilesPerSecond = GetAircraftTravelSpeedMilesPerSecond(useAutopilotTimeWarp);
            DrawWorldBearingIndicator(
                targetWorld,
                FlightHudColorPalette.Default,
                etaSpeedMilesPerSecond,
                stackIndex: 0,
                stackCount: 1);
        }

        private void DrawIncomingMissileThreatIndicators()
        {
            if (aircraft?.Profile == null || aircraft.WorldMap == null)
            {
                return;
            }

            HomingMissile.CollectIncomingEnemyThreats(incomingMissileThreats);
            BuildMissileIndicatorLayouts();
            if (missileIndicatorLayouts.Count == 0)
            {
                return;
            }

            for (var i = 0; i < missileIndicatorLayouts.Count; i++)
            {
                var layout = missileIndicatorLayouts[i];
                var missile = layout.Missile;
                if (missile == null)
                {
                    continue;
                }

                DrawWorldBearingIndicator(
                    missile.WorldPosition,
                    IncomingMissileIndicatorColor,
                    missile.SpeedMilesPerSecond,
                    layout.SpreadIndex,
                    layout.SpreadCount,
                    layout.Direction,
                    MissileIndicatorPerpendicularSpacing,
                    MissileIndicatorRadialSpacing);
            }
        }

        private void BuildMissileIndicatorLayouts()
        {
            missileIndicatorLayouts.Clear();
            missileDirectionScratch.Clear();
            if (incomingMissileThreats.Count == 0)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            var planePos = aircraft.transform.position;
            planePos.y = 0f;
            var planeScreen = cam.WorldToScreenPoint(planePos);
            if (planeScreen.z <= 0f)
            {
                return;
            }

            var planeGui = HudTargetMarkerLayout.ScreenToGui(planeScreen);

            for (var i = 0; i < incomingMissileThreats.Count; i++)
            {
                var missile = incomingMissileThreats[i];
                if (missile == null)
                {
                    continue;
                }

                if (!TryGetThreatIndicatorDirection(cam, planeGui, missile.WorldPosition, out var direction))
                {
                    continue;
                }

                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                missileDirectionScratch.Add((missile, angle, direction));
            }

            if (missileDirectionScratch.Count == 0)
            {
                return;
            }

            missileDirectionScratch.Sort((a, b) => a.Angle.CompareTo(b.Angle));

            var groupStart = 0;
            for (var i = 0; i <= missileDirectionScratch.Count; i++)
            {
                var endGroup = i == missileDirectionScratch.Count
                    || Mathf.Abs(Mathf.DeltaAngle(
                        missileDirectionScratch[groupStart].Angle,
                        missileDirectionScratch[i].Angle)) > MissileDirectionGroupDegrees;

                if (!endGroup)
                {
                    continue;
                }

                var groupCount = i - groupStart;
                for (var j = groupStart; j < i; j++)
                {
                    var entry = missileDirectionScratch[j];
                    missileIndicatorLayouts.Add(new MissileIndicatorLayout
                    {
                        Missile = entry.Missile,
                        Direction = entry.Direction,
                        SpreadIndex = j - groupStart,
                        SpreadCount = groupCount
                    });
                }

                groupStart = i;
            }
        }

        private bool TryGetThreatIndicatorDirection(
            Camera cam,
            Vector2 planeGui,
            Vector3 targetWorld,
            out Vector2 direction)
        {
            direction = default;
            targetWorld.y = 0f;
            var planePos = aircraft.transform.position;
            planePos.y = 0f;
            var toTarget = targetWorld - planePos;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return false;
            }

            var targetScreen = cam.WorldToScreenPoint(targetWorld);
            if (targetScreen.z > 0f)
            {
                var targetGui = HudTargetMarkerLayout.ScreenToGui(targetScreen);
                direction = targetGui - planeGui;
            }
            else
            {
                direction = GetCameraFallbackDirection(cam, toTarget.normalized);
            }

            if (direction.sqrMagnitude < 4f)
            {
                return false;
            }

            direction.Normalize();
            return true;
        }

        private void DrawWorldBearingIndicator(
            Vector3 targetWorld,
            Color indicatorColor,
            float etaSpeedMilesPerSecond,
            int stackIndex,
            int stackCount)
        {
            DrawWorldBearingIndicator(
                targetWorld,
                indicatorColor,
                etaSpeedMilesPerSecond,
                stackIndex,
                stackCount,
                fixedDirection: default,
                hasFixedDirection: false,
                perpendicularSpacing: 28f,
                radialSpacing: 0f);
        }

        private void DrawWorldBearingIndicator(
            Vector3 targetWorld,
            Color indicatorColor,
            float etaSpeedMilesPerSecond,
            int stackIndex,
            int stackCount,
            Vector2 fixedDirection,
            float perpendicularSpacing,
            float radialSpacing)
        {
            DrawWorldBearingIndicator(
                targetWorld,
                indicatorColor,
                etaSpeedMilesPerSecond,
                stackIndex,
                stackCount,
                fixedDirection,
                hasFixedDirection: true,
                perpendicularSpacing,
                radialSpacing);
        }

        private void DrawWorldBearingIndicator(
            Vector3 targetWorld,
            Color indicatorColor,
            float etaSpeedMilesPerSecond,
            int stackIndex,
            int stackCount,
            Vector2 fixedDirection,
            bool hasFixedDirection,
            float perpendicularSpacing,
            float radialSpacing)
        {
            var cam = Camera.main;
            if (cam == null || aircraft?.Profile == null || aircraft.WorldMap == null)
            {
                return;
            }

            targetWorld.y = 0f;
            var planePos = aircraft.transform.position;
            planePos.y = 0f;
            var toTarget = targetWorld - planePos;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            var rangeMiles = CombatThreatRange.DistanceMiles(
                planePos,
                targetWorld,
                aircraft.WorldMap,
                aircraft.Profile.ticSizeWorldUnits);
            var etaSeconds = etaSpeedMilesPerSecond > 0f
                ? rangeMiles / etaSpeedMilesPerSecond
                : 0f;
            var etaText = AntarcticaMapOverlay.FormatTravelEta(etaSeconds);

            var planeScreen = cam.WorldToScreenPoint(planePos);
            if (planeScreen.z <= 0f)
            {
                return;
            }

            var planeGui = HudTargetMarkerLayout.ScreenToGui(planeScreen);
            Vector2 direction;
            if (hasFixedDirection)
            {
                direction = fixedDirection;
            }
            else
            {
                var targetScreen = cam.WorldToScreenPoint(targetWorld);
                if (targetScreen.z > 0f)
                {
                    var targetGui = HudTargetMarkerLayout.ScreenToGui(targetScreen);
                    direction = targetGui - planeGui;
                }
                else
                {
                    direction = GetCameraFallbackDirection(cam, toTarget.normalized);
                }

                if (direction.sqrMagnitude < 4f)
                {
                    return;
                }

                direction.Normalize();
            }

            var perpendicular = new Vector2(-direction.y, direction.x);
            var stackCenter = (stackCount - 1) * 0.5f;
            var stackOffset = stackCount > 1
                ? perpendicular * (stackIndex - stackCenter) * perpendicularSpacing
                    + direction * (stackIndex - stackCenter) * radialSpacing
                : Vector2.zero;
            var arrowTip = planeGui + direction * MapBearingArrowDistancePixels + stackOffset;
            DrawHudDirectionArrow(arrowTip, direction, indicatorColor);
            DrawHudBearingLabel(arrowTip, direction, rangeMiles, etaText, indicatorColor);
        }

        private float GetAircraftTravelSpeedMilesPerSecond(bool autopilotTimeWarp)
        {
            if (aircraft?.Profile == null || aircraft.WorldMap == null)
            {
                return 0f;
            }

            var mph = autopilotTimeWarp
                ? aircraft.Profile.AutopilotCruiseMph
                : aircraft.CurrentSpeedMph;
            if (mph <= 0f)
            {
                return 0f;
            }

            var milesPerSecond = mph / 3600f;
            if (autopilotTimeWarp)
            {
                var warp = AutopilotController.Instance != null
                    ? AutopilotController.Instance.TimeWarpScale
                    : AutopilotController.DefaultTimeWarpScale;
                milesPerSecond *= warp;
            }

            return milesPerSecond;
        }

        private static Vector2 GetCameraFallbackDirection(Camera cam, Vector3 worldDirection)
        {
            var forward = cam.transform.forward;
            forward.y = 0f;
            var right = cam.transform.right;
            right.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            forward.Normalize();
            right.Normalize();
            return new Vector2(Vector3.Dot(worldDirection, right), -Vector3.Dot(worldDirection, forward));
        }

        private void DrawHudBearingLabel(
            Vector2 arrowTip,
            Vector2 direction,
            float rangeMiles,
            string etaText,
            Color hudColor)
        {
            EnsureBearingLabelStyle(hudColor);
            var text = $"{rangeMiles:0} MI\n{etaText}";
            var content = new GUIContent(text);
            var size = bearingLabelStyle.CalcSize(content);
            const float padX = 6f;
            const float padY = 3f;
            const float arrowLength = 26f;
            const float labelInsetFromArrowBase = 14f;

            direction.Normalize();
            var arrowBaseCenter = arrowTip - direction * arrowLength;
            var labelCenter = arrowBaseCenter - direction * labelInsetFromArrowBase;
            var labelWidth = size.x + padX * 2f;
            var labelHeight = size.y + padY * 2f;
            var labelRect = new Rect(
                labelCenter.x - labelWidth * 0.5f,
                labelCenter.y - labelHeight * 0.5f,
                labelWidth,
                labelHeight);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.color = hudColor;
            GUI.Label(labelRect, text, bearingLabelStyle);
            GUI.color = Color.white;
        }

        private void EnsureBearingLabelStyle(Color hudColor)
        {
            if (bearingLabelStyle != null && hudColor == lastBearingHudColor)
            {
                return;
            }

            lastBearingHudColor = hudColor;
            bearingLabelStyle = HudStyleFactory.CreateLabel(
                13,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                hudColor);
        }

        private void DrawHudDirectionArrow(Vector2 tip, Vector2 direction, Color color)
        {
            direction.Normalize();
            var perpendicular = new Vector2(-direction.y, direction.x);
            const float arrowLength = 26f;
            const float arrowWidth = 18f;
            const float outlineThickness = 2f;
            var baseCenter = tip - direction * arrowLength;
            var baseLeft = baseCenter - perpendicular * (arrowWidth * 0.5f);
            var baseRight = baseCenter + perpendicular * (arrowWidth * 0.5f);

            DrawFilledHudTriangle(tip, baseLeft, baseRight, color);
            DrawHudLine(tip, baseLeft, Color.black, outlineThickness);
            DrawHudLine(tip, baseRight, Color.black, outlineThickness);
            DrawHudLine(baseLeft, baseRight, Color.black, outlineThickness);
        }

        private static void DrawFilledHudTriangle(Vector2 a, Vector2 b, Vector2 c, Color fillColor)
        {
            var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y));
            var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y));

            GUI.color = fillColor;
            for (var y = minY; y <= maxY; y++)
            {
                var scanY = y + 0.5f;
                var intersections = new System.Collections.Generic.List<float>(3);
                AddTriangleScanIntersection(a, b, scanY, intersections);
                AddTriangleScanIntersection(b, c, scanY, intersections);
                AddTriangleScanIntersection(c, a, scanY, intersections);
                if (intersections.Count < 2)
                {
                    continue;
                }

                intersections.Sort();
                var xStart = intersections[0];
                var xEnd = intersections[intersections.Count - 1];
                if (xEnd <= xStart)
                {
                    continue;
                }

                GUI.DrawTexture(new Rect(xStart, y, xEnd - xStart + 1f, 1f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private static void AddTriangleScanIntersection(
            Vector2 start,
            Vector2 end,
            float scanY,
            System.Collections.Generic.List<float> intersections)
        {
            if (Mathf.Approximately(start.y, end.y))
            {
                if (Mathf.Approximately(start.y, scanY))
                {
                    intersections.Add(start.x);
                    intersections.Add(end.x);
                }

                return;
            }

            if ((scanY >= start.y && scanY < end.y) || (scanY >= end.y && scanY < start.y))
            {
                var t = (scanY - start.y) / (end.y - start.y);
                intersections.Add(start.x + t * (end.x - start.x));
            }
        }

        private void DrawHudLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            if (lineTexture == null)
            {
                lineTexture = Texture2D.whiteTexture;
            }

            HudGuiUtility.DrawScreenLine(start, end, color, thickness, lineTexture);
        }

        private void DrawAutopilotAboveRadar()
        {
            var autopilot = AutopilotController.Instance;
            if (autopilot == null)
            {
                return;
            }

            var scale = RadarMfdBezelRenderer.LayoutScale;
            var hasToast = Time.unscaledTime <= autopilot.StatusToastUntil
                && !string.IsNullOrEmpty(autopilot.StatusToast);
            var toastPrefix = hasToast ? autopilot.StatusToast + "\n\n" : string.Empty;

            if (autopilot.IsFlying)
            {
                DrawPanel(
                    PlaneRadarOverlay.GetRectAboveRadar(68f * scale),
                    toastPrefix +
                    $"AUTOPILOT {autopilot.TimeWarpScale:0}X\n" +
                    $"DEST {autopilot.DestinationLabel}\n" +
                    $"ETA  {autopilot.DestinationDistanceMiles:0} MI\n" +
                    $"- / = — SPEED   P — CANCEL");
                return;
            }

            if (autopilot.CanResume)
            {
                DrawPanel(
                    PlaneRadarOverlay.GetRectAboveRadar(56f * scale),
                    toastPrefix +
                    $"AUTOPILOT PAUSED\n" +
                    $"DEST {autopilot.DestinationLabel}\n" +
                    $"P — RESUME");
                return;
            }

            if (hasToast)
            {
                DrawPanel(
                    PlaneRadarOverlay.GetRectAboveRadar(36f * scale),
                    autopilot.StatusToast);
            }
        }

        private void DrawMissileAcquisitionWarning()
        {
            if (!MissileThreatNotifier.HasMissilesTargetingPlayer()
                || !MissileThreatNotifier.ShouldBlinkVisible())
            {
                return;
            }

            EnsureStyles();
            var warningColor = new Color(1f, 0.35f, 0.1f);
            var warningStyle = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                warningColor);

            const string text = "MISSILE ACQUISITION";
            var size = warningStyle.CalcSize(new GUIContent(text));
            var rect = new Rect(
                (Screen.width - size.x) * 0.5f - 20f,
                88f,
                size.x + 40f,
                size.y + 14f);

            GUI.color = new Color(warningColor.r, warningColor.g, warningColor.b, 0.28f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = warningColor;
            GUI.Label(rect, text, warningStyle);
            GUI.color = Color.white;
        }

        private void DrawTopRightWeapons()
        {
            if (weapons == null)
            {
                return;
            }

            var selected = weapons.ActiveWeapon switch
            {
                SelectedWeapon.Gau27a => "GAU-27A",
                SelectedWeapon.Agm88jSiaw => "AGM-88J SiAW",
                SelectedWeapon.Agm114Hellfire => "AGM-114 HELLFIRE",
                SelectedWeapon.Gbu12Paveway => "GBU-12 PAVEWAY",
                SelectedWeapon.Aim9z => "AIM-9Z",
                _ => "NONE"
            };

            var status = GetWeaponStatusLine();

            DrawPanel(
                new Rect(Screen.width - 296f, 16f, 280f, 108f),
                "WEAPONS\n" +
                $"SEL  {selected}\n" +
                $"TYPE {weapons.ActiveWeaponEngagementLabel}\n" +
                $"STS  {status}");
        }

        private string GetWeaponStatusLine()
        {
            if (weapons == null)
            {
                return "NONE";
            }

            return weapons.ActiveWeapon switch
            {
                SelectedWeapon.Gau27a when weapons.Gau27aGun != null =>
                    $"RNG {weapons.Gau27aGun.CrosshairDistanceMiles:0.0} MI" +
                    (weapons.Gau27aGun.HasTargetUnderCrosshair ? " TGT" : ""),
                _ when weapons.LockController != null
                    && weapons.LockController.SelectedTarget != null
                    && weapons.LockController.SelectedTarget.RespondsWithIff
                    && weapons.LockController.SelectedFriendlyBlocksLock =>
                    "IFF FRIEND",
                _ when weapons.LockController != null
                    && weapons.LockController.SelectedFriendlyBlocksLock =>
                    "FRIENDLY",
                _ when weapons.LockController != null && weapons.LockController.IffFriendActive =>
                    "IFF FRIEND",
                _ when weapons.LockController != null
                    && weapons.LockController.SelectedTargetKindMismatch =>
                    "WRONG TGT",
                _ when weapons.LockController != null
                    && weapons.ActiveWeapon != SelectedWeapon.Gau27a
                    && weapons.ActiveWeapon != SelectedWeapon.None
                    && weapons.LockController.SelectedTarget == null =>
                    "SELECT TGT",
                _ when weapons.LockController != null =>
                    weapons.LockController.TargetOutOfRange
                        ? "OUT OF RANGE"
                        : weapons.LockController.LockState.ToString().ToUpperInvariant(),
                _ => "READY"
            };
        }

        private void DrawPanel(Rect rect, string text)
        {
            var hudColor = FlightHudColorPalette.Current;
            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.12f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = hudColor;
            GUI.Label(rect, text, labelStyle);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            var hudColor = FlightHudColorPalette.Current;
            if (labelStyle != null && hudColor == lastHudColor)
            {
                return;
            }

            lastHudColor = hudColor;
            labelStyle = HudStyleFactory.CreateLabel(
                14,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                hudColor,
                wordWrap: true);
        }
    }
}
