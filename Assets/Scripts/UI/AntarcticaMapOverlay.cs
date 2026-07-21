using System.Collections;
using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.UI
{
    public class AntarcticaMapOverlay : MonoBehaviour
    {
        private const float MinVisibleWidthMiles = 20f;
        private const float EastWestStretch = 1.2f;
        // +44% east-west vs raw map aspect (1.2 base display width * 1.2 expansion).
        private const float GeoMapWidthScale = 1.44f;
        private const float FullViewOceanPadding = 1.06f;
        private const float ScrollSensitivity = 0.1f;
        private const float HeaderHeight = 36f;
        private const float MapMargin = 24f;

        [SerializeField] private AircraftController aircraft;
        [SerializeField] private WorldMapConfig worldMap;

        private float zoomLevel;
        private Vector2 panOffsetMiles;
        private bool isDraggingPan;
        private Vector2 lastDragMouse;
        private GUIStyle headerStyle;
        private GUIStyle infoStyle;
        private GUIStyle carrierMapLabelStyle;
        private GUIStyle baseRangeLabelStyle;
        private GUIStyle mapRangeLabelStyle;
        private GUIStyle baseMapLabelStyle;
        private GUIStyle autopilotPlaneLabelStyle;
        private Texture2D lineTexture;
        private Texture2D satelliteTexture;

        public static bool IsOpen { get; private set; }
        public static bool IsAutopilotSelectMode { get; private set; }
        public static bool IsAutopilotFlightMode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsOpen = false;
            IsAutopilotSelectMode = false;
            IsAutopilotFlightMode = false;
        }

        private AutopilotController autopilot;
        private string baseNamePopup = string.Empty;
        private float baseNamePopupUntil;
        private Vector2 baseNamePopupGui;
        private AntarcticaBase selectedMapBase;
        private AntarcticaBase hoveredMapBase;
        private AntarcticaBase[] cachedMapBases;
        private int cachedMapBasesFrame = -1;
        private readonly List<Rect> placedMapLabelRects = new List<Rect>();
        private Coroutine pendingBaseCommit;

        private bool mapPointerDownOnMap;
        private Vector2 mapPointerDownGui;
        private bool mapPointerDragged;
        private Vector2 panDragAnchorMiles;
        private bool panDragActive;

        private const float MapClickDragThreshold = 5f;
        private const float AutopilotPlaneLabelBlinkPeriodSeconds = 5f;
        private const float AutopilotPlaneLabelBlinkOnSeconds = 0.45f;
        private const float WaypointMarkerRadius = 12f;

        private struct MapRouteWaypoint
        {
            public Vector3 World;
            public string Label;
        }

        private static readonly List<MapRouteWaypoint> mapRoute = new List<MapRouteWaypoint>(8);

        private static bool activeMapBearing;
        private static Vector3 activeMapBearingWorld;
        private static string activeMapBearingLabel = string.Empty;
        private static bool activeMapBearingUseAutopilotWarp;

        public static bool HasMapWaypoint => mapRoute.Count > 0;

        public static int MapRouteWaypointCount => mapRoute.Count;

        public static bool TryGetHudBearing(
            out Vector3 worldTarget,
            out string label,
            out bool useAutopilotTimeWarp)
        {
            if (mapRoute.Count > 0)
            {
                var waypoint = mapRoute[0];
                worldTarget = waypoint.World;
                label = waypoint.Label;
                useAutopilotTimeWarp = activeMapBearingUseAutopilotWarp
                    || (AutopilotController.Instance != null && AutopilotController.Instance.IsFlying);
                return true;
            }

            if (!activeMapBearing)
            {
                worldTarget = default;
                label = string.Empty;
                useAutopilotTimeWarp = false;
                return false;
            }

            worldTarget = activeMapBearingWorld;
            label = activeMapBearingLabel;
            useAutopilotTimeWarp = activeMapBearingUseAutopilotWarp
                || (AutopilotController.Instance != null && AutopilotController.Instance.IsFlying);
            return true;
        }

        private AutopilotController ResolveAutopilot()
        {
            if (autopilot == null)
            {
                autopilot = AutopilotController.Instance;
            }

            return autopilot;
        }

        public static string FormatTravelEta(float totalSeconds)
        {
            if (totalSeconds < 0f || float.IsInfinity(totalSeconds) || float.IsNaN(totalSeconds))
            {
                return "--";
            }

            totalSeconds = Mathf.Max(0f, totalSeconds);
            if (totalSeconds < 60f)
            {
                return $"{totalSeconds:0} SEC";
            }

            if (totalSeconds < 3600f)
            {
                var minutes = Mathf.FloorToInt(totalSeconds / 60f);
                var seconds = Mathf.FloorToInt(totalSeconds % 60f);
                return $"{minutes}:{seconds:D2}";
            }

            var hours = Mathf.FloorToInt(totalSeconds / 3600f);
            var mins = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
            return $"{hours}H {mins:D2}M";
        }

        public void ClearHudBearing()
        {
            activeMapBearing = false;
            activeMapBearingLabel = string.Empty;
        }

        public void ClearMapRoute()
        {
            mapRoute.Clear();
            if (!IsAutopilotFlightMode)
            {
                AbandonSuspendedAutopilotIfNeeded();
                SyncHudBearingToCurrentTarget();
            }
        }

        public void AdvanceMapRouteAfterLeg()
        {
            if (mapRoute.Count > 0)
            {
                mapRoute.RemoveAt(0);
            }

            if (mapRoute.Count == 0)
            {
                ClearHudBearing();
                return;
            }

            var next = mapRoute[0];
            SetMapHudBearing(next.World, next.Label, useAutopilotTimeWarp: IsAutopilotFlightMode && activeMapBearingUseAutopilotWarp);
        }

        public void EngageAutopilotToWaypoint()
        {
            TryEngageAutopilotToMapTarget();
        }

        public bool TryEngageAutopilotToMapTarget()
        {
            var activeAutopilot = ResolveAutopilot();
            if (activeAutopilot == null)
            {
                return false;
            }

            if (TryGetSelectedBaseDestination(out var world, out var label))
            {
                CommitAutopilotDestination(world, label);
                return activeAutopilot.IsFlying;
            }

            if (mapRoute.Count > 0)
            {
                CommitAutopilotRoute();
                return activeAutopilot.IsFlying;
            }

            return false;
        }

        public bool HasAutopilotMapTarget =>
            mapRoute.Count > 0 || CanEngageAutopilotToSelectedBase();

        private bool CanEngageAutopilotToSelectedBase()
        {
            return TryGetSelectedBaseDestination(out _, out _);
        }

        private bool TryGetSelectedBaseDestination(out Vector3 world, out string label)
        {
            world = default;
            label = string.Empty;
            if (selectedMapBase == null
                || !selectedMapBase.IsActive
                || selectedMapBase.IsDestroyed)
            {
                return false;
            }

            world = GetBaseWorldPosition(selectedMapBase);
            label = FormatRouteWaypointLabel(
                selectedMapBase.PositionMiles,
                0,
                selectedMapBase.BaseName);
            return true;
        }

        private Vector3 GetBaseWorldPosition(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return Vector3.zero;
            }

            return MilesToWorld(baseSite.PositionMiles);
        }

        private void AbandonSuspendedAutopilotIfNeeded()
        {
            var activeAutopilot = ResolveAutopilot();
            if (activeAutopilot != null && activeAutopilot.CanResume)
            {
                activeAutopilot.AbandonSuspendedRoute();
            }
        }

        public void AppendRouteWaypointDuringFlight(Vector3 worldTarget, string label)
        {
            worldTarget.y = 0f;
            mapRoute.Add(new MapRouteWaypoint { World = worldTarget, Label = label });
            ResolveAutopilot()?.AppendRouteLeg(worldTarget, label);
        }

        private void RestoreWaypointHudBearing()
        {
            SyncHudBearingToCurrentTarget();
        }

        private void SyncHudBearingToCurrentTarget()
        {
            var activeAutopilot = ResolveAutopilot();
            if (activeAutopilot != null
                && activeAutopilot.IsFlying
                && activeAutopilot.HasDestination)
            {
                SetMapHudBearing(
                    activeAutopilot.DestinationWorld,
                    activeAutopilot.DestinationLabel,
                    useAutopilotTimeWarp: true);
                return;
            }

            var useWarp = activeAutopilot != null && activeAutopilot.IsFlying;
            if (mapRoute.Count > 0)
            {
                var next = mapRoute[0];
                SetMapHudBearing(next.World, next.Label, useAutopilotTimeWarp: useWarp);
                return;
            }

            if (TryGetSelectedBaseDestination(out var world, out var label))
            {
                SetMapHudBearing(world, label, useAutopilotTimeWarp: useWarp);
                return;
            }

            ClearHudBearing();
        }

        private void ClearMapRouteAndSetSingleWaypoint(Vector3 worldTarget, string label)
        {
            worldTarget.y = 0f;
            mapRoute.Clear();
            mapRoute.Add(new MapRouteWaypoint { World = worldTarget, Label = label });
        }

        private void AppendMapRouteWaypoint(Vector3 worldTarget, string label)
        {
            worldTarget.y = 0f;
            mapRoute.Add(new MapRouteWaypoint { World = worldTarget, Label = label });
            SyncHudBearingToCurrentTarget();
        }

        private static string FormatRouteWaypointLabel(Vector2 miles, int routeIndex, string placeName = null)
        {
            if (!string.IsNullOrWhiteSpace(placeName))
            {
                return placeName;
            }

            return $"WP {routeIndex + 1}";
        }

        private static string FormatWaypointLabel(Vector2 miles, string placeName = null)
        {
            return FormatRouteWaypointLabel(miles, 0, placeName);
        }

        private static bool IsControlHeld(Event evt = null)
        {
            if (evt != null && evt.control)
            {
                return true;
            }

            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private void SetMapHudBearing(Vector3 worldTarget, string label, bool useAutopilotTimeWarp)
        {
            worldTarget.y = 0f;
            activeMapBearingWorld = worldTarget;
            activeMapBearingLabel = label;
            activeMapBearingUseAutopilotWarp = useAutopilotTimeWarp;
            activeMapBearing = true;
        }

        public void SetAutopilotHudBearing(Vector3 worldTarget, string label)
        {
            SetMapHudBearing(worldTarget, label, useAutopilotTimeWarp: true);
        }

        public void Configure(AircraftController aircraftController, WorldMapConfig mapConfig, AutopilotController autopilotController = null)
        {
            aircraft = aircraftController;
            worldMap = mapConfig;
            autopilot = autopilotController;
        }

        public void OpenAutopilotSelection()
        {
            IsAutopilotSelectMode = true;
            IsAutopilotFlightMode = false;
            baseNamePopup = string.Empty;
            ClearMapPointerState();
            SetOpen(true);
        }

        public void BeginAutopilotFlight()
        {
            IsAutopilotSelectMode = false;
            IsAutopilotFlightMode = true;
            ClearMapPointerState();
            if (!IsOpen)
            {
                SetOpen(true);
            }
        }

        /// <summary>Close the tactical map during autopilot but keep flying (return to HUD).</summary>
        public void HideMapForAutopilotHud()
        {
            if (pendingBaseCommit != null)
            {
                StopCoroutine(pendingBaseCommit);
                pendingBaseCommit = null;
            }

            IsOpen = false;
            IsAutopilotFlightMode = false;
            IsAutopilotSelectMode = false;
            baseNamePopup = string.Empty;
            selectedMapBase = null;
            hoveredMapBase = null;
            ClearMapPointerState();
            SyncHudBearingToCurrentTarget();
        }

        public void EndAutopilotFlight()
        {
            IsAutopilotFlightMode = false;
            ClearHudBearing();
            RestoreWaypointHudBearing();
        }

        public void PauseAutopilotFlight()
        {
            IsAutopilotFlightMode = false;
            IsAutopilotSelectMode = false;
        }

        public void ClearMapRouteOnArrival()
        {
            mapRoute.Clear();
            ClearHudBearing();
        }

        public void CloseMap()
        {
            if (pendingBaseCommit != null)
            {
                StopCoroutine(pendingBaseCommit);
                pendingBaseCommit = null;
            }

            IsAutopilotSelectMode = false;
            IsAutopilotFlightMode = false;
            baseNamePopup = string.Empty;
            ClearMapPointerState();
            SetOpen(false);
        }

        private void Update()
        {
            if (GamePauseController.IsPaused)
            {
                hoveredMapBase = null;
                return;
            }

            var activeAutopilot = ResolveAutopilot();

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsAutopilotSelectMode)
                {
                    // Selection mode uses P to cancel; ignore M.
                }
                else if (IsAutopilotFlightMode && IsOpen)
                {
                    HideMapForAutopilotHud();
                }
                else if (activeAutopilot != null && activeAutopilot.IsFlying)
                {
                    BeginAutopilotFlight();
                }
                else if (!IsAutopilotFlightMode)
                {
                    SetOpen(!IsOpen);
                }
            }

            if (!IsOpen)
            {
                hoveredMapBase = null;
                return;
            }

            if (IsAutopilotFlightMode)
            {
                FollowAutopilotAircraft();
            }

            if (!IsAutopilotSelectMode
                && aircraft != null
                && worldMap != null)
            {
                var mapRect = GetGeoMapRect(GetMapRect());
                var mouse = GetGuiMousePosition();
                if (mapRect.Contains(mouse))
                {
                    TryPickBaseAtGuiPoint(mapRect, mouse, out hoveredMapBase);
                }
                else
                {
                    hoveredMapBase = null;
                }
            }
            else
            {
                hoveredMapBase = null;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !IsAutopilotFlightMode && worldMap != null)
            {
                var mapRect = GetGeoMapRect(GetMapRect());
                var mouse = GetGuiMousePosition();
                var projection = GetMapGeoProjection();
                var zoomAnchorMiles = mapRect.Contains(mouse)
                    ? projection.GuiToWorldMiles(mapRect, mouse)
                    : (Vector2?)null;

                zoomLevel = Mathf.Clamp01(zoomLevel + scroll * ScrollSensitivity);
                if (zoomLevel <= 0.001f)
                {
                    zoomLevel = 0f;
                    panOffsetMiles = Vector2.zero;
                }
                else if (zoomAnchorMiles.HasValue)
                {
                    panOffsetMiles = GetMapGeoProjection()
                        .ComputePanForAnchorAtGui(mapRect, zoomAnchorMiles.Value, mouse);
                    ClampPanOffset();
                }
            }
        }

        private void SetOpen(bool open)
        {
            var wasOpen = IsOpen;
            IsOpen = open;
            if (!open)
            {
                IsAutopilotSelectMode = false;
                IsAutopilotFlightMode = false;
                baseNamePopup = string.Empty;
                selectedMapBase = null;
                hoveredMapBase = null;
                ClearMapPointerState();
                RestoreWaypointHudBearing();
            }

            if (open && !wasOpen)
            {
                zoomLevel = 0f;
                panOffsetMiles = Vector2.zero;
            }
        }

        private void OnGUI()
        {
            if (Event.current == null
                || !IsOpen
                || GamePauseController.IsPaused
                || aircraft == null
                || worldMap == null)
            {
                return;
            }

            var mapRect = GetMapRect();
            var geoRect = GetGeoMapRect(mapRect);
            HandleCoordinatePickInput(geoRect);
            HandleMapPointerInput(geoRect);

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureStyles();
            DrawBackdrop();
            DrawMapContents(geoRect);
            DrawMapPlayerLayer(geoRect);
            DrawBaseNamePopup();
            DrawHeader();
        }

        private void DrawBackdrop()
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            GUI.color = new Color(0.04f, 0.06f, 0.08f, 0.96f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHeader()
        {
            var mapPanel = GetMapRect();
            var hudColor = FlightHudColorPalette.Current;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.18f);
            GUI.DrawTexture(new Rect(mapPanel.x, 0f, mapPanel.width, HeaderHeight), Texture2D.whiteTexture);
            GUI.color = hudColor;

            var headerText = IsAutopilotFlightMode
                ? "AUTOPILOT — EN ROUTE"
                : IsAutopilotSelectMode
                    ? "AUTOPILOT — SELECT DESTINATION"
                    : "ANTARCTICA MAP";
            var closeHint = IsAutopilotFlightMode
                ? "M — HUD   P — CANCEL"
                : IsAutopilotSelectMode
                    ? "P — CANCEL"
                    : "M — CLOSE";
            var infoText = closeHint;
            if (IsAutopilotFlightMode && autopilot != null)
            {
                infoText =
                    $"DEST {autopilot.DestinationLabel}   " +
                    $"{autopilot.TimeWarpScale:0}X   " +
                    $"- / = — SPEED   CTRL+CLICK — ADD WP   RIGHT-CLICK — CANCEL AP   {closeHint}";
            }
            else if (IsAutopilotSelectMode)
            {
                var speedHint = autopilot != null ? $"{autopilot.TimeWarpScale:0}X   - / = — SPEED   " : string.Empty;
                var routeHint = mapRoute.Count > 0 ? $"P — GO ({mapRoute.Count} WP)   " : string.Empty;
                infoText += $"   {speedHint}{routeHint}CTRL+CLICK — ADD WP   HOVER — PREVIEW   CLICK — ENGAGE";
            }
            else
            {
                var waypointHint = HasAutopilotMapTarget
                    ? "   P — GO TO ROUTE   SHIFT+P — SELECT DEST"
                    : string.Empty;
                infoText += $"   HOVER BASE — NAME   CLICK BASE — SELECT   CLICK MAP — SET WP   CTRL+CLICK — ADD ROUTE WP   RIGHT-CLICK — CANCEL AP / REMOVE WP{waypointHint}   SHIFT+CLICK — LOG";
            }

            GUI.Label(new Rect(mapPanel.x, 2f, mapPanel.width, 16f), headerText, headerStyle);
            GUI.Label(new Rect(mapPanel.x, 16f, mapPanel.width, HeaderHeight - 16f), infoText, infoStyle);
            GUI.color = Color.white;
        }

        private void DrawMapContents(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var hudColor = FlightHudColorPalette.Current;

            DrawSatelliteImagery(mapRect);

            if (GetVisibleWidthMiles() <= 600f)
            {
                DrawGrid(mapRect, hudColor);
            }

            DrawBaseMarkers(mapRect);
            ClearPlacedMapLabels();
            DrawCarrierMapLabels(mapRect);
            DrawMapBaseNameLabels(mapRect);
            DrawBaseSelectionLabels(mapRect);
            DrawMapPlannedRoute(mapRect);
            DrawAutopilotRoute(mapRect);
            DrawAutopilotRemainingRoute(mapRect);
            GUI.color = Color.white;
        }

        private void DrawMapPlayerLayer(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawPlayerMarker(mapRect);
            DrawAutopilotPlaneLabel(mapRect);
        }

        private void DrawAutopilotPlaneLabel(Rect mapRect)
        {
            if (!IsAutopilotFlightMode || autopilot == null || !autopilot.IsFlying)
            {
                return;
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (!ShouldShowAutopilotPlaneLabel())
            {
                return;
            }

            var playerMiles = WorldToMiles(aircraft.transform.position);
            var guiPoint = WorldMilesToGui(mapRect, playerMiles);
            if (!mapRect.Contains(guiPoint))
            {
                return;
            }

            GetPlayerMarkerTriangleGui(
                mapRect,
                out var noseGui,
                out var tailLeftGui,
                out var tailRightGui,
                out _);
            var markerTopY = Mathf.Min(noseGui.y, tailLeftGui.y, tailRightGui.y, guiPoint.y);

            EnsureAutopilotPlaneLabelStyle();
            var text = $"AUTOPILOT {autopilot.TimeWarpScale:0}X";
            var lineHeight = autopilotPlaneLabelStyle.fontSize + 4f;
            var labelWidth = Mathf.Max(88f, text.Length * autopilotPlaneLabelStyle.fontSize * 0.62f);
            var labelRect = new Rect(
                guiPoint.x - labelWidth * 0.5f,
                markerTopY - lineHeight - 5f,
                labelWidth,
                lineHeight);

            DrawMapLabelYellowText(labelRect, text, autopilotPlaneLabelStyle);
        }

        private static bool ShouldShowAutopilotPlaneLabel()
        {
            var phase = Time.unscaledTime % AutopilotPlaneLabelBlinkPeriodSeconds;
            return phase < AutopilotPlaneLabelBlinkOnSeconds;
        }

        private void EnsureAutopilotPlaneLabelStyle()
        {
            const int fontSize = 12;
            if (autopilotPlaneLabelStyle != null && autopilotPlaneLabelStyle.fontSize == fontSize)
            {
                return;
            }

            autopilotPlaneLabelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                CarrierLabelColor);
            autopilotPlaneLabelStyle.hover.textColor = CarrierLabelColor;
            autopilotPlaneLabelStyle.active.textColor = CarrierLabelColor;
            autopilotPlaneLabelStyle.focused.textColor = CarrierLabelColor;
        }

        private void DrawMapPlannedRoute(Rect mapRect)
        {
            if (IsAutopilotFlightMode || mapRoute.Count == 0 || worldMap == null)
            {
                return;
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var hudColor = FlightHudColorPalette.Current;
            var routeColor = new Color(hudColor.r, hudColor.g, hudColor.b, 0.92f);

            if (mapRoute.Count > 1 && aircraft != null)
            {
                var legOriginMiles = WorldToMiles(aircraft.transform.position);
                for (var i = 0; i < mapRoute.Count; i++)
                {
                    var waypointMiles = WorldToMiles(mapRoute[i].World);
                    DrawGuiLine(mapRect, legOriginMiles, waypointMiles, routeColor, 2.5f);
                    legOriginMiles = waypointMiles;
                }
            }

            for (var i = 0; i < mapRoute.Count; i++)
            {
                var waypointMiles = WorldToMiles(mapRoute[i].World);
                var waypointGui = WorldMilesToGui(mapRect, waypointMiles);
                if (mapRect.Contains(waypointGui))
                {
                    DrawMapOutlinedDot(waypointGui, WaypointMarkerRadius, routeColor, 2f);
                }
            }
        }

        private void DrawAutopilotRemainingRoute(Rect mapRect)
        {
            if (!IsAutopilotFlightMode || autopilot == null || mapRoute.Count <= 1 || aircraft?.Profile == null)
            {
                return;
            }

            var hudColor = FlightHudColorPalette.Current;
            var dimColor = new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f);
            var prevMiles = WorldToMiles(autopilot.DestinationWorld);

            for (var i = 1; i < mapRoute.Count; i++)
            {
                var wpMiles = WorldToMiles(mapRoute[i].World);
                DrawGuiLine(mapRect, prevMiles, wpMiles, dimColor, 2f);
                prevMiles = wpMiles;
            }

            for (var i = 1; i < mapRoute.Count; i++)
            {
                var wpGui = WorldMilesToGui(mapRect, WorldToMiles(mapRoute[i].World));
                if (mapRect.Contains(wpGui))
                {
                    DrawMapOutlinedDot(wpGui, WaypointMarkerRadius * 0.85f, dimColor, 1.5f);
                    if (aircraft?.Profile != null)
                    {
                        var legRangeMiles = CombatThreatRange.DistanceMiles(
                            mapRoute[i - 1].World,
                            mapRoute[i].World,
                            worldMap,
                            aircraft.Profile.ticSizeWorldUnits);
                        DrawRangeLabelAtMapPoint(
                            wpGui,
                            legRangeMiles,
                            useAutopilotTimeWarp: true,
                            markerRadius: WaypointMarkerRadius * 0.85f);
                    }
                }
            }
        }

        private void DrawAutopilotRoute(Rect mapRect)
        {
            if (!IsAutopilotFlightMode || autopilot == null || !autopilot.HasDestination)
            {
                return;
            }

            var playerMiles = WorldToMiles(aircraft.transform.position);
            var destinationMiles = WorldToMiles(autopilot.DestinationWorld);
            DrawAutopilotRouteLine(mapRect, playerMiles, destinationMiles, isPreview: false);

            var destinationGui = WorldMilesToGui(mapRect, destinationMiles);
            DrawRangeLabelAtMapPoint(
                destinationGui,
                autopilot.DestinationDistanceMiles,
                useAutopilotTimeWarp: true,
                markerRadius: 11f);
        }

        private float GetTravelTimeSeconds(float rangeMiles, bool autopilotTimeWarp)
        {
            if (aircraft?.Profile == null || worldMap == null || rangeMiles <= 0f)
            {
                return 0f;
            }

            var mph = worldMap.TicsPerSecondToMph(aircraft.Profile.throttleSpeed);
            if (mph <= 0f)
            {
                return 0f;
            }

            var milesPerSecond = mph / 3600f;
            if (autopilotTimeWarp)
            {
                var warp = autopilot != null
                    ? autopilot.TimeWarpScale
                    : AutopilotController.DefaultTimeWarpScale;
                milesPerSecond *= warp;
            }

            return rangeMiles / milesPerSecond;
        }

        private void DrawAutopilotRouteLine(Rect mapRect, Vector2 startMiles, Vector2 endMiles, bool isPreview)
        {
            var hudColor = FlightHudColorPalette.Current;
            var routeColor = isPreview
                ? new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f)
                : new Color(hudColor.r, hudColor.g, hudColor.b, 0.95f);
            var thickness = isPreview ? 2f : 2.5f;

            DrawGuiLine(mapRect, startMiles, endMiles, routeColor, thickness);

            var destinationGui = WorldMilesToGui(mapRect, endMiles);
            if (mapRect.Contains(destinationGui))
            {
                DrawMapOutlinedDot(destinationGui, isPreview ? 9f : 11f, routeColor, 2f);
            }
        }

        private void DrawRangeLabelAtMapPoint(
            Vector2 anchorGui,
            float rangeMiles,
            bool useAutopilotTimeWarp,
            float markerRadius,
            string title = null)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureMapRangeLabelStyle();
            var milesLine = $"{rangeMiles:0} MI";
            var timeLine = FormatTravelEta(GetTravelTimeSeconds(rangeMiles, useAutopilotTimeWarp));
            var text = string.IsNullOrWhiteSpace(title)
                ? $"{milesLine}\n{timeLine}"
                : $"{title}\n{milesLine}\n{timeLine}";
            var content = new GUIContent(text);
            var size = mapRangeLabelStyle.CalcSize(content);
            const float padX = 8f;
            const float padY = 4f;
            const float markerGap = 6f;
            var labelWidth = size.x + padX * 2f;
            var labelHeight = size.y + padY * 2f;
            var labelX = anchorGui.x - labelWidth * 0.5f;
            var labelY = anchorGui.y + markerRadius + markerGap;
            var labelRect = new Rect(labelX, labelY, labelWidth, labelHeight);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            DrawMapLabelYellowText(labelRect, text, mapRangeLabelStyle);
        }

        private void FollowAutopilotAircraft()
        {
            if (aircraft == null || zoomLevel <= 0.001f)
            {
                return;
            }

            panOffsetMiles = WorldToMiles(aircraft.transform.position);
            ClampPanOffset();
        }

        private void DrawSatelliteImagery(Rect mapRect)
        {
            var texture = GetSatelliteTexture();
            if (texture == null)
            {
                GUI.color = new Color(0.08f, 0.14f, 0.28f, 1f);
                GUI.DrawTexture(mapRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }

            var projection = GetMapGeoProjection();
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(mapRect, texture, projection.GetSatelliteTextureCoords());
        }

        private void DrawGrid(Rect mapRect, Color color)
        {
            var visibleHalfMiles = GetVisibleHalfMiles();
            var spacingMiles = ChooseGridSpacingMiles(visibleHalfMiles);
            var centerMiles = GetViewCenterMiles();
            var antarcticaHalfMiles = worldMap.antarcticaSizeMiles * 0.5f;

            var minMilesX = Mathf.Max(-antarcticaHalfMiles, centerMiles.x - visibleHalfMiles);
            var maxMilesX = Mathf.Min(antarcticaHalfMiles, centerMiles.x + visibleHalfMiles);
            var minMilesY = Mathf.Max(-antarcticaHalfMiles, centerMiles.y - visibleHalfMiles);
            var maxMilesY = Mathf.Min(antarcticaHalfMiles, centerMiles.y + visibleHalfMiles);

            var startX = Mathf.Floor(minMilesX / spacingMiles) * spacingMiles;
            var startY = Mathf.Floor(minMilesY / spacingMiles) * spacingMiles;

            var gridColor = new Color(color.r, color.g, color.b, 0.35f);
            for (var x = startX; x <= maxMilesX + 0.001f; x += spacingMiles)
            {
                DrawGuiLine(
                    mapRect,
                    new Vector2(x, minMilesY),
                    new Vector2(x, maxMilesY),
                    gridColor,
                    1f);
            }

            for (var y = startY; y <= maxMilesY + 0.001f; y += spacingMiles)
            {
                DrawGuiLine(
                    mapRect,
                    new Vector2(minMilesX, y),
                    new Vector2(maxMilesX, y),
                    gridColor,
                    1f);
            }
        }

        private static readonly Color FriendlyBaseColor = new Color(0.12f, 0.42f, 0.92f);
        private static readonly Color HostileBaseColor = new Color(0.9f, 0.18f, 0.12f);
        private static readonly Color CarrierMarkerColor = new Color(0.78f, 0.78f, 0.78f);
        private static readonly Color CarrierLabelColor = new Color(0.95f, 0.85f, 0.1f);
        private const float CarrierMarkerScale = 0.7f;

        private void DrawBaseMarkers(Rect mapRect)
        {
            var bases = GetMapBases();
            var dotSize = Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 6f, 14f);

            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive)
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, baseSite.PositionMiles);
                var isCarrier = baseSite.SiteKind == BaseSiteKind.Carrier;
                if (!IsPointVisibleOnMap(mapRect, guiPoint, isCarrier))
                {
                    continue;
                }

                var isSelected = baseSite == selectedMapBase;
                var isHovered = baseSite == hoveredMapBase;

                if (baseSite.IsDestroyed)
                {
                    var destroyedSize = baseSite.SiteKind == BaseSiteKind.Carrier
                        ? dotSize * 4f * CarrierMarkerScale
                        : dotSize;
                    DrawMapFrame(guiPoint, destroyedSize);
                    continue;
                }

                if (baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    var carrierHeight = GetCarrierMarkerHeight(dotSize);
                    if (CarrierMarkerArt.TryGetTexture(out _))
                    {
                        CarrierMarkerArt.DrawNorthUpMarker(guiPoint, carrierHeight);
                    }
                    else
                    {
                        DrawMapRect(
                            guiPoint,
                            CarrierMarkerArt.GetWidthForHeight(carrierHeight),
                            carrierHeight,
                            CarrierMarkerColor);
                    }

                    continue;
                }

                var color = baseSite.Control == BaseControl.Friendly
                    ? FriendlyBaseColor
                    : HostileBaseColor;

                if (isSelected || isHovered)
                {
                    DrawMapOutlinedDot(guiPoint, dotSize + 4f, color, 2f);
                }
                else
                {
                    DrawMapDot(guiPoint, dotSize, color);
                }
            }
        }

        private void DrawMapBaseNameLabels(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var bases = GetMapBases();
            var dotSize = Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 6f, 14f);
            EnsureBaseMapLabelStyle(dotSize);

            var labelCandidates = new List<(AntarcticaBase baseSite, Vector2 guiPoint, bool emphasize, int priority)>();
            foreach (var baseSite in bases)
            {
                if (baseSite == null
                    || !baseSite.IsActive
                    || baseSite.IsDestroyed
                    || baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, baseSite.PositionMiles);
                if (!mapRect.Contains(guiPoint))
                {
                    continue;
                }

                var emphasize = baseSite == selectedMapBase || baseSite == hoveredMapBase;
                var priority = emphasize ? 100 : 0;
                if (baseSite.IsMissionObjective)
                {
                    priority += 10;
                }

                priority += baseSite.BaseName.Length;
                labelCandidates.Add((baseSite, guiPoint, emphasize, priority));
            }

            labelCandidates.Sort((a, b) => b.priority.CompareTo(a.priority));

            foreach (var candidate in labelCandidates)
            {
                if (string.IsNullOrWhiteSpace(candidate.baseSite.BaseName))
                {
                    continue;
                }

                var size = baseMapLabelStyle.CalcSize(new GUIContent(candidate.baseSite.BaseName));
                if (TryPlaceMapLabelRect(
                        mapRect,
                        candidate.guiPoint,
                        dotSize,
                        size,
                        candidate.emphasize,
                        out var labelRect))
                {
                    DrawBaseMapNameLabelAtRect(labelRect, candidate.baseSite.BaseName, candidate.emphasize);
                }
            }
        }

        private void ClearPlacedMapLabels()
        {
            placedMapLabelRects.Clear();
        }

        private void RegisterPlacedLabel(Rect rect)
        {
            placedMapLabelRects.Add(rect);
        }

        private bool OverlapsPlacedLabels(Rect rect, float padding = 2f)
        {
            var padded = new Rect(
                rect.x - padding,
                rect.y - padding,
                rect.width + padding * 2f,
                rect.height + padding * 2f);
            for (var i = 0; i < placedMapLabelRects.Count; i++)
            {
                if (padded.Overlaps(placedMapLabelRects[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryPlaceMapLabelRect(
            Rect mapRect,
            Vector2 anchor,
            float dotSize,
            Vector2 labelSize,
            bool emphasize,
            out Rect placedRect)
        {
            const float labelPad = 6f;
            foreach (var topLeft in GetMapLabelPlacementOffsets(anchor, dotSize, labelSize))
            {
                placedRect = new Rect(topLeft.x, topLeft.y, labelSize.x, labelSize.y);
                if (emphasize)
                {
                    placedRect = new Rect(
                        placedRect.x - labelPad,
                        placedRect.y - labelPad * 0.5f,
                        placedRect.width + labelPad * 2f,
                        placedRect.height + labelPad);
                }

                if (!mapRect.Overlaps(placedRect) || !IsMostlyInsideMap(mapRect, placedRect))
                {
                    continue;
                }

                if (!OverlapsPlacedLabels(placedRect))
                {
                    RegisterPlacedLabel(placedRect);
                    if (emphasize)
                    {
                        placedRect = new Rect(
                            topLeft.x,
                            topLeft.y,
                            labelSize.x,
                            labelSize.y);
                    }

                    return true;
                }
            }

            placedRect = new Rect(
                anchor.x - labelSize.x * 0.5f,
                anchor.y - dotSize * 0.5f - labelSize.y - 3f,
                labelSize.x,
                labelSize.y);
            RegisterPlacedLabel(emphasize
                ? new Rect(
                    placedRect.x - labelPad,
                    placedRect.y - labelPad * 0.5f,
                    placedRect.width + labelPad * 2f,
                    placedRect.height + labelPad)
                : placedRect);
            return true;
        }

        private static bool IsMostlyInsideMap(Rect mapRect, Rect labelRect)
        {
            var intersection = RectIntersection(mapRect, labelRect);
            return intersection.width * intersection.height >= labelRect.width * labelRect.height * 0.55f;
        }

        private static Rect RectIntersection(Rect a, Rect b)
        {
            var xMin = Mathf.Max(a.xMin, b.xMin);
            var yMin = Mathf.Max(a.yMin, b.yMin);
            var xMax = Mathf.Min(a.xMax, b.xMax);
            var yMax = Mathf.Min(a.yMax, b.yMax);
            if (xMax <= xMin || yMax <= yMin)
            {
                return new Rect(xMin, yMin, 0f, 0f);
            }

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static IEnumerable<Vector2> GetMapLabelPlacementOffsets(
            Vector2 anchor,
            float dotSize,
            Vector2 labelSize)
        {
            var halfDot = dotSize * 0.5f;
            var halfW = labelSize.x * 0.5f;
            var gap = 3f;

            yield return new Vector2(anchor.x - halfW, anchor.y - halfDot - labelSize.y - gap);
            yield return new Vector2(anchor.x - halfW, anchor.y + halfDot + gap);
            yield return new Vector2(anchor.x - halfW, anchor.y - halfDot - labelSize.y - gap - 16f);
            yield return new Vector2(anchor.x - halfW, anchor.y + halfDot + gap + 16f);
            yield return new Vector2(anchor.x - labelSize.x - halfDot - 8f, anchor.y - labelSize.y * 0.5f);
            yield return new Vector2(anchor.x + halfDot + 8f, anchor.y - labelSize.y * 0.5f);
            yield return new Vector2(anchor.x - halfW, anchor.y - halfDot - labelSize.y - gap - 32f);
            yield return new Vector2(anchor.x - labelSize.x - halfDot - 16f, anchor.y - labelSize.y - gap);
            yield return new Vector2(anchor.x + halfDot + 16f, anchor.y - labelSize.y - gap);
            yield return new Vector2(anchor.x - halfW, anchor.y + halfDot + gap + 32f);
        }

        private void DrawBaseMapNameLabelAtRect(Rect labelRect, string baseName, bool emphasize)
        {
            if (string.IsNullOrWhiteSpace(baseName) || baseMapLabelStyle == null)
            {
                return;
            }

            var style = baseMapLabelStyle;
            if (style == null)
            {
                return;
            }

            if (emphasize)
            {
                var pad = 6f;
                var bgRect = new Rect(
                    labelRect.x - pad,
                    labelRect.y - pad * 0.5f,
                    labelRect.width + pad * 2f,
                    labelRect.height + pad);
                var previousColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.75f);
                GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
                GUI.color = previousColor;
                DrawMapLabelWithYellowOutline(labelRect, baseName, style);
                return;
            }

            DrawMapLabelYellowText(labelRect, baseName, style);
        }

        private static float GetCarrierMarkerHeight(float dotSize)
        {
            return dotSize * 4f * CarrierMarkerScale;
        }

        private static void DrawMapRect(Vector2 center, float width, float height, Color fillColor)
        {
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            GUI.color = fillColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawCarrierMapLabels(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var dotSize = Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 5f, 12f);
            var carrierHeight = GetCarrierMarkerHeight(dotSize);
            var carrierWidth = CarrierMarkerArt.GetWidthForHeight(carrierHeight);
            EnsureCarrierMapLabelStyle(dotSize);

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive || baseSite.IsDestroyed
                    || baseSite.SiteKind != BaseSiteKind.Carrier)
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, baseSite.PositionMiles);
                if (!IsPointVisibleOnMap(mapRect, guiPoint, isCarrier: true))
                {
                    continue;
                }

                var lineHeight = carrierMapLabelStyle.fontSize + 5f;
                GetCarrierMapLabelStackRects(
                    guiPoint,
                    carrierWidth,
                    carrierHeight,
                    lineHeight,
                    out var nameLine1,
                    out var nameLine2,
                    out var cvLine);
                RegisterPlacedLabel(nameLine1);
                RegisterPlacedLabel(nameLine2);
                RegisterPlacedLabel(cvLine);
                DrawCarrierMapLabel(nameLine1, "USS");
                DrawCarrierMapLabel(nameLine2, "Martin Van Buren");
                DrawCarrierMapLabel(cvLine, "CV");
            }
        }

        private static bool IsPointVisibleOnMap(Rect mapRect, Vector2 guiPoint, bool isCarrier)
        {
            if (mapRect.Contains(guiPoint))
            {
                return true;
            }

            if (!isCarrier)
            {
                return false;
            }

            const float carrierLabelPadding = 90f;
            var expanded = new Rect(
                mapRect.x - carrierLabelPadding,
                mapRect.y - carrierLabelPadding,
                mapRect.width + carrierLabelPadding * 2f,
                mapRect.height + carrierLabelPadding * 2f);
            return expanded.Contains(guiPoint);
        }

        private static void GetCarrierMapLabelStackRects(
            Vector2 guiPoint,
            float carrierWidth,
            float carrierHeight,
            float lineHeight,
            out Rect nameLine1,
            out Rect nameLine2,
            out Rect cvLine)
        {
            var labelWidth = Mathf.Max(carrierWidth * 5f, 140f);
            var gap = 4f;
            var iconTop = guiPoint.y - carrierHeight * 0.5f;
            var iconBottom = guiPoint.y + carrierHeight * 0.5f;
            var labelX = guiPoint.x - labelWidth * 0.5f;

            var nameLine2Y = iconTop - gap - lineHeight;
            var nameLine1Y = nameLine2Y - lineHeight;
            var cvLineY = iconBottom + gap;

            nameLine1 = new Rect(labelX, nameLine1Y, labelWidth, lineHeight);
            nameLine2 = new Rect(labelX, nameLine2Y, labelWidth, lineHeight);
            cvLine = new Rect(labelX, cvLineY, labelWidth, lineHeight);
        }

        private void DrawCarrierMapLabel(Rect rect, string text)
        {
            DrawCarrierMapLabel(rect, text, carrierMapLabelStyle);
        }

        private void DrawCarrierMapLabel(Rect rect, string text, GUIStyle style)
        {
            var shadow = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;

            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.contentColor = Color.black;
            GUI.Label(shadow, text, style);

            GUI.color = previousColor;
            GUI.contentColor = CarrierLabelColor;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

        private void EnsureCarrierMapLabelStyle(float dotSize)
        {
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(dotSize * 1.1f, 11f, 14f) * 2f);
            if (carrierMapLabelStyle != null && carrierMapLabelStyle.fontSize == fontSize)
            {
                return;
            }

            carrierMapLabelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                CarrierLabelColor);
            carrierMapLabelStyle.hover.textColor = CarrierLabelColor;
            carrierMapLabelStyle.active.textColor = CarrierLabelColor;
            carrierMapLabelStyle.focused.textColor = CarrierLabelColor;
        }

        private static void DrawMapDot(Vector2 center, float size, Color fillColor)
        {
            var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            GUI.color = fillColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawMapFrame(Vector2 center, float size)
        {
            const float border = 2f;
            var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - border, rect.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - border, rect.y, border, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawMapOutlinedDot(Vector2 center, float size, Color fillColor, float outlinePixels = 2f)
        {
            var outerRect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
            var inset = outlinePixels;
            var innerRect = new Rect(
                outerRect.x + inset,
                outerRect.y + inset,
                outerRect.width - inset * 2f,
                outerRect.height - inset * 2f);

            GUI.color = Color.black;
            GUI.DrawTexture(outerRect, Texture2D.whiteTexture);
            if (innerRect.width > 0f && innerRect.height > 0f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(innerRect, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private void DrawPlayerMarker(Rect mapRect)
        {
            var playerMiles = WorldToMiles(aircraft.transform.position);
            var guiPoint = WorldMilesToGui(mapRect, playerMiles);
            if (!mapRect.Contains(guiPoint))
            {
                return;
            }

            GetPlayerMarkerTriangleGui(
                mapRect,
                out var noseGui,
                out var tailLeftGui,
                out var tailRightGui,
                out _);

            const float outlineThickness = 2f;

            DrawFilledGuiTriangle(noseGui, tailLeftGui, tailRightGui, Color.white);
            DrawScreenLine(noseGui, tailLeftGui, Color.black, outlineThickness);
            DrawScreenLine(noseGui, tailRightGui, Color.black, outlineThickness);
            DrawScreenLine(tailLeftGui, tailRightGui, Color.black, outlineThickness);

            DrawMapOutlinedDot(guiPoint, 7f, Color.white, outlineThickness);
        }

        /// <summary>
        /// Projects nose/tail wing tips through the same mile georef as route lines so heading
        /// matches movement over the satellite map (not a distorted mile-space vector).
        /// </summary>
        private void GetPlayerMarkerTriangleGui(
            Rect mapRect,
            out Vector2 noseGui,
            out Vector2 tailLeftGui,
            out Vector2 tailRightGui,
            out Vector2 centerGui)
        {
            var position = aircraft.transform.position;
            position.y = 0f;

            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                var body = aircraft.GetComponent<Rigidbody>();
                if (body != null)
                {
                    forward = body.linearVelocity;
                    forward.y = 0f;
                }
            }

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var worldUnitsPerMile = GetWorldUnitsPerMile();
            const float sizeScale = 0.5f;
            var noseDistWorld = Mathf.Max(0.35f, GetVisibleWidthMiles() * 0.015f) * sizeScale * worldUnitsPerMile;
            var tailDistWorld = Mathf.Max(0.25f, GetVisibleWidthMiles() * 0.01f) * sizeScale * worldUnitsPerMile;
            var wingSpanWorld = Mathf.Max(0.2f, GetVisibleWidthMiles() * 0.008f) * sizeScale * worldUnitsPerMile;
            var right = new Vector3(forward.z, 0f, -forward.x);

            var noseWorld = position + forward * noseDistWorld;
            var tailWorld = position - forward * tailDistWorld;
            var tailLeftWorld = tailWorld - right * wingSpanWorld;
            var tailRightWorld = tailWorld + right * wingSpanWorld;

            centerGui = WorldMilesToGui(mapRect, WorldToMiles(position));
            noseGui = WorldMilesToGui(mapRect, WorldToMiles(noseWorld));
            tailLeftGui = WorldMilesToGui(mapRect, WorldToMiles(tailLeftWorld));
            tailRightGui = WorldMilesToGui(mapRect, WorldToMiles(tailRightWorld));
        }

        private static void DrawFilledGuiTriangle(Vector2 a, Vector2 b, Vector2 c, Color fillColor)
        {
            var minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y));
            var maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y));
            if (maxY < minY)
            {
                return;
            }

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

        private static void AddTriangleScanIntersection(Vector2 start, Vector2 end, float scanY, System.Collections.Generic.List<float> intersections)
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

        private void ClearMapPointerState()
        {
            mapPointerDownOnMap = false;
            mapPointerDragged = false;
            isDraggingPan = false;
            panDragActive = false;
        }

        private static Vector2 GetGuiMousePosition()
        {
            // Input.mousePosition is bottom-left origin; IMGUI uses top-left.
            var mouse = (Vector2)Input.mousePosition;
            mouse.y = Screen.height - mouse.y;
            return mouse;
        }

        private static Vector2 GetMapGuiMouse(Event evt)
        {
            return GetGuiMousePosition();
        }

        private AntarcticaBase[] GetMapBases()
        {
            if (cachedMapBasesFrame != Time.frameCount)
            {
                cachedMapBases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                cachedMapBasesFrame = Time.frameCount;
            }

            return cachedMapBases ?? System.Array.Empty<AntarcticaBase>();
        }

        private void HandleMapPointerInput(Rect mapRect)
        {
            var currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                return;
            }

            var mouse = GetMapGuiMouse(currentEvent);
            var allowPan = !IsAutopilotSelectMode && !IsAutopilotFlightMode;

            if (currentEvent.type == EventType.MouseDown
                && currentEvent.button == 1
                && mapRect.Contains(mouse)
                && !IsAutopilotSelectMode)
            {
                if (TryCancelAutopilotFromMap())
                {
                    currentEvent.Use();
                    return;
                }

                if (!IsAutopilotFlightMode && TryRemoveMapWaypointAtPoint(mapRect, mouse))
                {
                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDown
                && currentEvent.button == 0
                && mapRect.Contains(mouse))
            {
                mapPointerDownOnMap = true;
                mapPointerDragged = false;
                panDragActive = false;
                mapPointerDownGui = mouse;
                lastDragMouse = mouse;
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && mapPointerDownOnMap)
            {
                if (Vector2.Distance(mouse, mapPointerDownGui) >= MapClickDragThreshold)
                {
                    mapPointerDragged = true;
                }

                if (allowPan && mapPointerDragged && zoomLevel > 0.001f)
                {
                    isDraggingPan = true;
                    if (!panDragActive)
                    {
                        panDragAnchorMiles = GuiToWorldMiles(mapRect, mapPointerDownGui);
                        panDragActive = true;
                    }

                    panOffsetMiles = ComputePanForAnchorAtGui(mapRect, panDragAnchorMiles, mouse);
                    ClampPanOffset();
                    lastDragMouse = mouse;
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                var releaseGui = GetGuiMousePosition();
                if (!mapPointerDragged && mapRect.Contains(releaseGui))
                {
                    if (IsControlHeld(currentEvent))
                    {
                        AppendRouteWaypointAtMapPoint(mapRect, releaseGui);
                        currentEvent.Use();
                    }
                    else if (IsAutopilotSelectMode)
                    {
                        HandleAutopilotDestinationClick(mapRect, releaseGui);
                        currentEvent.Use();
                    }
                    else if (TryPickBaseAtGuiPoint(mapRect, releaseGui, out var pickedBase))
                    {
                        selectedMapBase = pickedBase;
                        ClearMapRoute();
                        SyncHudBearingToCurrentTarget();
                        currentEvent.Use();
                    }
                    else if (!IsAutopilotFlightMode)
                    {
                        selectedMapBase = null;
                        PlaceMapWaypoint(mapRect, releaseGui);
                        currentEvent.Use();
                    }
                }

                mapPointerDownOnMap = false;
                mapPointerDragged = false;
                isDraggingPan = false;
                panDragActive = false;
            }
        }

        public bool CancelAutopilotIfActive()
        {
            return TryCancelAutopilotFromMap();
        }

        private bool TryCancelAutopilotFromMap()
        {
            var activeAutopilot = ResolveAutopilot();
            if (activeAutopilot == null
                || (!activeAutopilot.IsFlying && !activeAutopilot.CanResume))
            {
                return false;
            }

            selectedMapBase = null;
            IsAutopilotFlightMode = false;
            IsAutopilotSelectMode = false;
            ClearMapRoute();
            activeAutopilot.DisengageAutopilot("Autopilot canceled.");
            ClearHudBearing();
            Debug.Log("F-89: Autopilot canceled — right-click.");
            return true;
        }

        private void PlaceMapWaypoint(Rect mapRect, Vector2 cursorGui)
        {
            if (aircraft?.Profile == null || !mapRect.Contains(cursorGui))
            {
                return;
            }

            var targetMiles = GuiToWorldMiles(mapRect, cursorGui);
            var targetWorld = MilesToWorld(targetMiles);
            var label = FormatWaypointLabel(targetMiles);
            AbandonSuspendedAutopilotIfNeeded();
            ClearMapRouteAndSetSingleWaypoint(targetWorld, label);
            SyncHudBearingToCurrentTarget();
            Debug.Log($"F-89: Map waypoint set — {label}.");
        }

        private void AppendRouteWaypointAtMapPoint(Rect mapRect, Vector2 cursorGui)
        {
            if (aircraft?.Profile == null || !mapRect.Contains(cursorGui))
            {
                return;
            }

            var targetMiles = GuiToWorldMiles(mapRect, cursorGui);
            var targetWorld = MilesToWorld(targetMiles);
            var label = FormatRouteWaypointLabel(targetMiles, mapRoute.Count);

            if (IsAutopilotFlightMode)
            {
                AppendRouteWaypointDuringFlight(targetWorld, label);
                return;
            }

            AbandonSuspendedAutopilotIfNeeded();
            AppendMapRouteWaypoint(targetWorld, label);
            Debug.Log($"F-89: Route waypoint added — {label} ({mapRoute.Count} total).");
        }

        private void HandleAutopilotDestinationClick(Rect mapRect, Vector2 mouse)
        {
            if (ResolveAutopilot() == null || aircraft?.Profile == null)
            {
                return;
            }

            if (mapRoute.Count > 0 && TryPickRouteWaypointAtGuiPoint(mapRect, mouse, out _))
            {
                CommitAutopilotRoute();
                return;
            }

            if (TryPickBaseAtGuiPoint(mapRect, mouse, out var baseSite))
            {
                if (pendingBaseCommit != null)
                {
                    StopCoroutine(pendingBaseCommit);
                }

                pendingBaseCommit = StartCoroutine(CommitBaseDestinationAfterPopup(baseSite, mouse));
                return;
            }

            if (pendingBaseCommit != null)
            {
                StopCoroutine(pendingBaseCommit);
                pendingBaseCommit = null;
                baseNamePopup = string.Empty;
            }

            var destinationMiles = GuiToWorldMiles(mapRect, mouse);
            var destinationWorld = MilesToWorld(destinationMiles);
            var label = FormatWaypointLabel(destinationMiles);
            CommitAutopilotDestination(destinationWorld, label);
        }

        private void EnsureMapRangeLabelStyle()
        {
            const int fontSize = 14;
            if (mapRangeLabelStyle != null && mapRangeLabelStyle.fontSize == fontSize)
            {
                return;
            }

            mapRangeLabelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                CarrierLabelColor);
        }

        private void DrawMapLabelYellowText(Rect rect, string text, GUIStyle style)
        {
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;

            GUI.contentColor = Color.black;
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, style);

            GUI.color = previousColor;
            GUI.contentColor = CarrierLabelColor;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

        private void DrawMapLabelWithYellowOutline(Rect rect, string text, GUIStyle style)
        {
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;

            GUI.contentColor = CarrierLabelColor;
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - 1f, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x - 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);

            GUI.color = previousColor;
            GUI.contentColor = Color.black;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

        private void EnsureBaseMapLabelStyle(float dotSize)
        {
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(900f / GetVisibleWidthMiles() * 10f, 9f, 12f) * 1.5f);
            if (baseMapLabelStyle != null && baseMapLabelStyle.fontSize == fontSize)
            {
                return;
            }

            baseMapLabelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                CarrierLabelColor);
        }

        private void EnsureBaseRangeLabelStyle(float dotSize)
        {
            var fontSize = Mathf.RoundToInt(Mathf.Clamp(dotSize * 1.15f, 11f, 15f));
            if (baseRangeLabelStyle != null && baseRangeLabelStyle.fontSize == fontSize)
            {
                return;
            }

            baseRangeLabelStyle = HudStyleFactory.CreateLabel(
                fontSize,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                CarrierLabelColor);
            baseRangeLabelStyle.normal.textColor = Color.black;
        }

        private void HandleCoordinatePickInput(Rect mapRect)
        {
            if (IsAutopilotSelectMode || IsAutopilotFlightMode)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent == null
                || currentEvent.type != EventType.MouseDown
                || currentEvent.button != 0)
            {
                return;
            }

            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                return;
            }

            var mouse = GetMapGuiMouse(currentEvent);
            if (!mapRect.Contains(mouse))
            {
                return;
            }

            var miles = GuiToWorldMiles(mapRect, mouse);
            Debug.Log(
                $"F-89: Picked map coordinate ({miles.x:0}, {miles.y:0}) — " +
                $"origin center (0, 0), x+ right, mile y+ toward map bottom (visual south). " +
                $"CarrierPositionMiles = new Vector2({miles.x:0}f, {miles.y:0}f);");
            currentEvent.Use();
        }

        private void RemoveMapRouteWaypointAt(int routeIndex)
        {
            if (routeIndex < 0 || routeIndex >= mapRoute.Count)
            {
                return;
            }

            mapRoute.RemoveAt(routeIndex);

            var activeAutopilot = ResolveAutopilot();
            if (activeAutopilot != null && activeAutopilot.CanResume)
            {
                activeAutopilot.AbandonSuspendedRoute();
            }

            if (mapRoute.Count == 0)
            {
                SyncHudBearingToCurrentTarget();
                Debug.Log("F-89: Map waypoint removed — route cleared.");
                return;
            }

            SyncHudBearingToCurrentTarget();
            Debug.Log($"F-89: Map waypoint removed ({mapRoute.Count} remaining).");
        }

        private bool TryRemoveMapWaypointAtPoint(Rect mapRect, Vector2 guiPoint)
        {
            if (mapRoute.Count == 0 || !mapRect.Contains(guiPoint))
            {
                return false;
            }

            if (TryPickRouteWaypointAtGuiPoint(mapRect, guiPoint, out var routeIndex))
            {
                RemoveMapRouteWaypointAt(routeIndex);
                return true;
            }

            ClearMapRoute();
            Debug.Log("F-89: Map waypoints cleared.");
            return true;
        }

        private bool TryPickRouteWaypointAtGuiPoint(Rect mapRect, Vector2 guiPoint, out int routeIndex)
        {
            routeIndex = -1;
            if (mapRoute.Count == 0)
            {
                return false;
            }

            var pickRadius = WaypointMarkerRadius + 4f;
            for (var i = 0; i < mapRoute.Count; i++)
            {
                var waypointGui = WorldMilesToGui(mapRect, WorldToMiles(mapRoute[i].World));
                if (Vector2.Distance(guiPoint, waypointGui) > pickRadius)
                {
                    continue;
                }

                routeIndex = i;
                return true;
            }

            return false;
        }

        private IEnumerator CommitBaseDestinationAfterPopup(AntarcticaBase baseSite, Vector2 mouse)
        {
            baseNamePopup = baseSite.BaseName;
            baseNamePopupGui = mouse;
            baseNamePopupUntil = Time.unscaledTime + 2f;
            yield return new WaitForSecondsRealtime(0.75f);
            pendingBaseCommit = null;
            var baseMiles = baseSite.PositionMiles;
            CommitAutopilotDestination(
                GetBaseWorldPosition(baseSite),
                FormatRouteWaypointLabel(baseMiles, 0, baseSite.BaseName));
        }

        private void CommitAutopilotDestination(Vector3 worldPosition, string label)
        {
            ClearMapRouteAndSetSingleWaypoint(worldPosition, label);
            CommitAutopilotRoute();
        }

        private void CommitAutopilotRoute()
        {
            var activeAutopilot = ResolveAutopilot();
            if (mapRoute.Count == 0 || activeAutopilot == null)
            {
                return;
            }

            var legs = new AutopilotController.RouteLeg[mapRoute.Count];
            for (var i = 0; i < mapRoute.Count; i++)
            {
                legs[i] = new AutopilotController.RouteLeg(mapRoute[i].World, mapRoute[i].Label);
            }

            activeAutopilot.CommitRoute(legs);
            SyncHudBearingToCurrentTarget();
        }

        private bool TryPickBaseAtGuiPoint(Rect mapRect, Vector2 guiPoint, out AntarcticaBase baseSite)
        {
            baseSite = null;
            var dotSize = Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 6f, 14f);
            var bestDistance = float.MaxValue;

            foreach (var candidate in GetMapBases())
            {
                if (candidate == null || !candidate.IsActive || candidate.IsDestroyed)
                {
                    continue;
                }

                if (!IsGuiPointOnBaseMarker(mapRect, guiPoint, candidate, dotSize, out var distance))
                {
                    continue;
                }

                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                baseSite = candidate;
            }

            return baseSite != null;
        }

        private bool IsGuiPointOnBaseMarker(
            Rect mapRect,
            Vector2 guiPoint,
            AntarcticaBase candidate,
            float dotSize,
            out float distance)
        {
            var guiBase = WorldMilesToGui(mapRect, candidate.PositionMiles);
            distance = Vector2.Distance(guiPoint, guiBase);

            if (candidate.SiteKind == BaseSiteKind.Carrier)
            {
                var carrierHeight = GetCarrierMarkerHeight(dotSize);
                var carrierWidth = CarrierMarkerArt.GetWidthForHeight(carrierHeight);
                const float pad = 24f;
                var lineHeight = carrierMapLabelStyle != null
                    ? carrierMapLabelStyle.fontSize + 5f
                    : 16f;
                var labelStackHeight = lineHeight * 2f + 4f;
                var rect = new Rect(
                    guiBase.x - carrierWidth * 0.5f - pad,
                    guiBase.y - carrierHeight * 0.5f - labelStackHeight - pad,
                    carrierWidth + pad * 2f,
                    carrierHeight + labelStackHeight + pad * 2f + lineHeight);
                return rect.Contains(guiPoint);
            }

            var hitRadius = Mathf.Max(dotSize * 4f, 28f);
            if (distance <= hitRadius)
            {
                return true;
            }

            EnsureBaseMapLabelStyle(dotSize);
            var labelHeight = baseMapLabelStyle.fontSize + 8f;
            var labelWidth = Mathf.Max(72f, candidate.BaseName.Length * baseMapLabelStyle.fontSize * 0.58f);
            var labelRect = new Rect(
                guiBase.x - labelWidth * 0.5f,
                guiBase.y - dotSize * 0.5f - labelHeight - 6f,
                labelWidth,
                labelHeight + dotSize + 8f);
            if (labelRect.Contains(guiPoint))
            {
                distance = 0f;
                return true;
            }

            return false;
        }

        private void DrawBaseSelectionLabels(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (selectedMapBase == null
                || !selectedMapBase.IsActive
                || selectedMapBase.IsDestroyed
                || selectedMapBase.SiteKind == BaseSiteKind.Carrier
                || aircraft == null
                || worldMap == null
                || aircraft.Profile == null)
            {
                return;
            }

            var dotSize = Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 6f, 14f);
            EnsureBaseRangeLabelStyle(dotSize);
            var guiPoint = WorldMilesToGui(mapRect, selectedMapBase.PositionMiles);
            if (!mapRect.Contains(guiPoint))
            {
                return;
            }

            var rangeMiles = CombatThreatRange.DistanceMiles(
                aircraft.transform.position,
                GetBaseWorldPosition(selectedMapBase),
                worldMap,
                aircraft.Profile.ticSizeWorldUnits);
            var text = $"{rangeMiles:0} MI";
            var style = baseRangeLabelStyle;
            var size = style.CalcSize(new GUIContent(text));
            if (TryPlaceMapLabelRect(
                    mapRect,
                    guiPoint,
                    dotSize,
                    size,
                    emphasize: false,
                    out var labelRect))
            {
                DrawMapLabelYellowText(labelRect, text, style);
            }
        }

        private void DrawBaseNamePopup()
        {
            if (string.IsNullOrEmpty(baseNamePopup) || Time.unscaledTime > baseNamePopupUntil)
            {
                return;
            }

            EnsureStyles();
            var popupStyle = HudStyleFactory.CreateLabel(
                14,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);

            var size = popupStyle.CalcSize(new GUIContent(baseNamePopup));
            var padding = 10f;
            var rect = new Rect(
                baseNamePopupGui.x - size.x * 0.5f - padding,
                baseNamePopupGui.y - size.y - 18f,
                size.x + padding * 2f,
                size.y + padding);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(rect, baseNamePopup, popupStyle);
        }

        private MapGeoProjection GetMapGeoProjection()
        {
            return new MapGeoProjection(
                GetViewCenterMiles(),
                GetVisibleHalfMiles(),
                worldMap.antarcticaSizeMiles);
        }

        private Vector2 GuiToWorldMiles(Rect mapRect, Vector2 guiPoint)
        {
            return GetMapGeoProjection().GuiToWorldMiles(mapRect, guiPoint);
        }

        private Vector3 MilesToWorld(Vector2 miles)
        {
            return WorldMapConfig.MileOffsetToWorld(miles, GetWorldUnitsPerMile());
        }

        private void ClampPanOffset()
        {
            var antarcticaHalfMiles = worldMap.antarcticaSizeMiles * 0.5f;
            var visibleHalfMiles = GetVisibleHalfMiles();
            var center = panOffsetMiles;

            center.x = Mathf.Clamp(
                center.x,
                -antarcticaHalfMiles + visibleHalfMiles,
                antarcticaHalfMiles - visibleHalfMiles);
            center.y = Mathf.Clamp(
                center.y,
                -antarcticaHalfMiles + visibleHalfMiles,
                antarcticaHalfMiles - visibleHalfMiles);

            panOffsetMiles = center;
        }

        private Rect GetGeoMapRect(Rect mapRect)
        {
            var texture = GetSatelliteTexture();
            var aspect = texture != null
                ? (float)texture.width / texture.height
                : 1024f / 788f;

            var geoHeight = mapRect.height;
            var geoWidth = geoHeight * aspect * GeoMapWidthScale;
            if (geoWidth > mapRect.width)
            {
                geoWidth = mapRect.width;
                geoHeight = geoWidth / (aspect * GeoMapWidthScale);
            }

            var y = mapRect.y + (mapRect.height - geoHeight) * 0.5f;
            return new Rect(mapRect.center.x - geoWidth * 0.5f, y, geoWidth, geoHeight);
        }

        private float GetMapPanelEastWestStretch()
        {
            var texture = GetSatelliteTexture();
            var aspect = texture != null
                ? (float)texture.width / texture.height
                : 1024f / 788f;
            return Mathf.Max(EastWestStretch, aspect * GeoMapWidthScale);
        }

        private Rect GetMapRect()
        {
            var maxHeight = Screen.height - HeaderHeight - MapMargin * 1.5f;
            var maxWidth = Screen.width - MapMargin * 2f;
            var widthStretch = GetMapPanelEastWestStretch();
            var height = Mathf.Min(maxHeight, maxWidth / widthStretch);
            height = Mathf.Max(height, 120f);
            var width = height * widthStretch;
            return new Rect(
                (Screen.width - width) * 0.5f,
                HeaderHeight + MapMargin * 0.25f,
                width,
                height);
        }

        private float GetVisibleHalfMiles()
        {
            var antarcticaHalfMiles = worldMap.antarcticaSizeMiles * 0.5f;
            var fullViewHalf = antarcticaHalfMiles * FullViewOceanPadding;
            var minHalfMiles = MinVisibleWidthMiles * 0.5f;
            return Mathf.Lerp(fullViewHalf, minHalfMiles, zoomLevel);
        }

        private float GetVisibleWidthMiles()
        {
            return GetVisibleHalfMiles() * 2f;
        }

        private Vector2 GetViewCenterMiles()
        {
            return panOffsetMiles;
        }

        private Vector2 WorldMilesToGui(Rect mapRect, Vector2 worldMiles)
        {
            return GetMapGeoProjection().WorldMilesToGui(mapRect, worldMiles);
        }

        private Vector2 ComputePanForAnchorAtGui(Rect mapRect, Vector2 anchorMiles, Vector2 guiPoint)
        {
            return GetMapGeoProjection().ComputePanForAnchorAtGui(mapRect, anchorMiles, guiPoint);
        }

        /// <summary>
        /// Single mile ↔ screen projection shared by satellite imagery and every map overlay marker.
        /// A fixed mile coordinate always maps to the same satellite-map pixel while panning/zooming.
        /// </summary>
        private readonly struct MapGeoProjection
        {
            public readonly float UMin;
            public readonly float UMax;
            public readonly float MileVMin;
            public readonly float MileVMax;
            public readonly float MapSizeMiles;
            public readonly float VisibleHalfMiles;

            public MapGeoProjection(Vector2 centerMiles, float visibleHalfMiles, float mapSizeMiles)
            {
                VisibleHalfMiles = visibleHalfMiles;
                MapSizeMiles = mapSizeMiles;
                var halfMapMiles = mapSizeMiles * 0.5f;
                UMin = (centerMiles.x - visibleHalfMiles + halfMapMiles) / mapSizeMiles;
                UMax = (centerMiles.x + visibleHalfMiles + halfMapMiles) / mapSizeMiles;
                MileVMin = (centerMiles.y - visibleHalfMiles + halfMapMiles) / mapSizeMiles;
                MileVMax = (centerMiles.y + visibleHalfMiles + halfMapMiles) / mapSizeMiles;
            }

            public Rect GetSatelliteTextureCoords()
            {
                // Match AntarcticaLandMask mile→pixel mapping (mile +Y → lower texture v).
                var vMin = 1f - MileVMax;
                var vMax = 1f - MileVMin;
                return new Rect(UMin, vMin, UMax - UMin, vMax - vMin);
            }

            public Vector2 WorldMilesToGui(Rect mapRect, Vector2 worldMiles)
            {
                WorldMilesToFractions(worldMiles, out var fu, out var fy);
                return new Vector2(
                    mapRect.xMin + fu * mapRect.width,
                    mapRect.yMin + fy * mapRect.height);
            }

            public Vector2 GuiToWorldMiles(Rect mapRect, Vector2 guiPoint)
            {
                var fu = (guiPoint.x - mapRect.xMin) / mapRect.width;
                var fy = (guiPoint.y - mapRect.yMin) / mapRect.height;
                return FractionsToWorldMiles(fu, fy);
            }

            public Vector2 ComputePanForAnchorAtGui(Rect mapRect, Vector2 anchorMiles, Vector2 guiPoint)
            {
                var visibleWidthMiles = VisibleHalfMiles * 2f;
                var fu = (guiPoint.x - mapRect.xMin) / mapRect.width;
                var fy = (guiPoint.y - mapRect.yMin) / mapRect.height;
                return new Vector2(
                    anchorMiles.x + VisibleHalfMiles - fu * visibleWidthMiles,
                    anchorMiles.y + VisibleHalfMiles - fy * visibleWidthMiles);
            }

            private void WorldMilesToFractions(Vector2 worldMiles, out float fu, out float fy)
            {
                var halfMapMiles = MapSizeMiles * 0.5f;
                var u = (worldMiles.x + halfMapMiles) / MapSizeMiles;
                var mileV = (worldMiles.y + halfMapMiles) / MapSizeMiles;
                fu = Mathf.InverseLerp(UMin, UMax, u);
                fy = Mathf.InverseLerp(MileVMin, MileVMax, mileV);
            }

            private Vector2 FractionsToWorldMiles(float fu, float fy)
            {
                var halfMapMiles = MapSizeMiles * 0.5f;
                var u = Mathf.Lerp(UMin, UMax, fu);
                var mileV = Mathf.Lerp(MileVMin, MileVMax, fy);
                return new Vector2(
                    u * MapSizeMiles - halfMapMiles,
                    mileV * MapSizeMiles - halfMapMiles);
            }
        }

        private void DrawGuiLine(Rect mapRect, Vector2 startMiles, Vector2 endMiles, Color color, float thickness)
        {
            var start = WorldMilesToGui(mapRect, startMiles);
            var end = WorldMilesToGui(mapRect, endMiles);
            DrawScreenLine(start, end, color, thickness);
        }

        private void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            HudGuiUtility.DrawScreenLine(start, end, color, thickness, lineTexture);
        }

        private Vector2 WorldToMiles(Vector3 worldPosition)
        {
            return WorldMapConfig.WorldToMileOffset(worldPosition, GetWorldUnitsPerMile());
        }

        private float GetWorldUnitsPerMile()
        {
            if (aircraft?.Profile == null)
            {
                return 20f;
            }

            return worldMap.GridSpacingTics * aircraft.Profile.ticSizeWorldUnits / worldMap.milesPerGrid;
        }

        private static float ChooseGridSpacingMiles(float visibleHalfMiles)
        {
            var visibleWidth = visibleHalfMiles * 2f;
            var candidates = new[] { 1f, 2f, 5f, 10f, 25f, 50f, 100f, 250f, 500f };
            foreach (var spacing in candidates)
            {
                if (visibleWidth / spacing <= 24f)
                {
                    return spacing;
                }
            }

            return 500f;
        }

        private Texture2D GetSatelliteTexture()
        {
            if (satelliteTexture == null)
            {
                satelliteTexture = Resources.Load<Texture2D>("F89_AntarcticaMap");
            }

            return satelliteTexture;
        }

        private void EnsureStyles()
        {
            if (lineTexture == null)
            {
                lineTexture = Texture2D.whiteTexture;
            }

            var hudColor = FlightHudColorPalette.Current;
            if (headerStyle != null && infoStyle != null)
            {
                headerStyle.normal.textColor = hudColor;
                infoStyle.normal.textColor = hudColor;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                infoStyle.alignment = TextAnchor.MiddleCenter;
                return;
            }

            headerStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.MiddleCenter, hudColor);
            infoStyle = HudStyleFactory.CreateLabel(12, FontStyle.Bold, TextAnchor.MiddleCenter, hudColor, wordWrap: true);
        }
    }
}
