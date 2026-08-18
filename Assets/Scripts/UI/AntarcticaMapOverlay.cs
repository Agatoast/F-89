using System.Collections;
using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public class AntarcticaMapOverlay : MonoBehaviour
    {
        private const float MinVisibleWidthMiles = 20f;
        private const float FullViewOceanPadding = 1.06f;
        private static readonly Color MapOceanDeepColor = new Color(0.239f, 0.486f, 0.800f, 1f);
        private static readonly Color MapOceanMidColor = new Color(0.28f, 0.54f, 0.84f, 1f);
        private static readonly Color MapOceanShallowColor = new Color(0.34f, 0.62f, 0.88f, 1f);
        private static readonly Color MapOceanHighlightColor = new Color(0.42f, 0.70f, 0.92f, 1f);
        private const float ScrollSensitivity = 0.1f;
        private const float HeaderHeight = 20f;
        private const float MapMargin = 24f;

        [SerializeField] private AircraftController aircraft;
        [SerializeField] private WorldMapConfig worldMap;

        private float zoomLevel;
        private Vector2 panOffsetMiles;
        private MapGeoProjection? repaintMapProjection;
        private Rect repaintGeoRect;
        private bool isDraggingPan;
        private Vector2 lastDragMouse;
        private GUIStyle headerStyle;
        private GUIStyle carrierMapLabelStyle;
        private GUIStyle baseRangeLabelStyle;
        private GUIStyle mapRangeLabelStyle;
        private GUIStyle baseMapLabelStyle;
        private GUIStyle autopilotPlaneLabelStyle;
        private bool isMenuPreviewMode;
        private Texture2D lineTexture;
        private Texture2D satelliteTexture;

        public static bool IsOpen { get; private set; }
        public static bool IsAutopilotFlightMode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsOpen = false;
            IsAutopilotFlightMode = false;
        }

        private void OnDestroy()
        {
            IsOpen = false;
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
        private int mapPanControlId;
        private bool mapPanHotActive;

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
                var activeAutopilot = AutopilotController.Instance;
                useAutopilotTimeWarp = activeMapBearingUseAutopilotWarp
                    || (activeAutopilot != null && activeAutopilot.IsFlying);
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

            var baseMiles = GetBaseMapMiles(selectedMapBase);
            world = GetBaseWorldPosition(selectedMapBase);
            label = FormatRouteWaypointLabel(baseMiles, 0, selectedMapBase.SiteCode);
            return true;
        }

        private static Vector2 GetBaseMapMiles(AntarcticaBase baseSite)
        {
            CampaignMapCoordinates.TryGetLockedBaseMiles(baseSite, out var miles);
            return miles;
        }

        private Vector3 GetBaseWorldPosition(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return Vector3.zero;
            }

            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            return CampaignMapCoordinates.GetLockedBaseWorldPosition(baseSite, worldMap, ticSize);
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

            if (activeAutopilot != null && activeAutopilot.IsFlying)
            {
                if (mapRoute.Count > 0)
                {
                    var next = mapRoute[0];
                    SetMapHudBearing(next.World, next.Label, useAutopilotTimeWarp: true);
                    return;
                }

                if (TryGetSelectedBaseDestination(out var world, out var label))
                {
                    SetMapHudBearing(world, label, useAutopilotTimeWarp: true);
                    return;
                }
            }

            if (mapRoute.Count > 0)
            {
                var next = mapRoute[0];
                SetMapHudBearing(next.World, next.Label, useAutopilotTimeWarp: false);
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
            isMenuPreviewMode = false;
        }

        public void ConfigureForMenuPreview(WorldMapConfig mapConfig)
        {
            aircraft = null;
            autopilot = null;
            worldMap = mapConfig;
            isMenuPreviewMode = true;
            IsAutopilotFlightMode = false;
            ClearMapRoute();
            ClearMapPointerState();
        }

        public void OpenMenuPreview()
        {
            if (worldMap == null)
            {
                worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            }

            zoomLevel = 0f;
            panOffsetMiles = CampaignMapCoordinates.GetMapCenterMiles(worldMap);
            SetOpen(true);
        }

        public void CloseMenuPreview()
        {
            isMenuPreviewMode = false;
            CloseMap();
        }

        public void BeginAutopilotFlight()
        {
            IsAutopilotFlightMode = true;
            ClearMapPointerState();
        }

        public void StageAutopilotTarget(Vector3 worldTarget, string label)
        {
            ClearMapRouteAndSetSingleWaypoint(worldTarget, label);
            SetAutopilotHudBearing(worldTarget, label);
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
        }

        public void ClearMapRouteOnArrival()
        {
            mapRoute.Clear();
            selectedMapBase = null;
            ClearHudBearing();
        }

        public void CloseMap()
        {
            if (pendingBaseCommit != null)
            {
                StopCoroutine(pendingBaseCommit);
                pendingBaseCommit = null;
            }

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

            if (Input.GetKeyDown(KeyCode.M) && !isMenuPreviewMode)
            {
                if (IsAutopilotFlightMode && IsOpen)
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

            if (IsOpen && worldMap != null)
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

            if (IsOpen && zoomLevel > 0.001f && worldMap != null)
            {
                ClampPanOffset();
            }
        }

        private void SetOpen(bool open)
        {
            var wasOpen = IsOpen;
            IsOpen = open;
            if (!open)
            {
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
                panOffsetMiles = CampaignMapCoordinates.GetMapCenterMiles(worldMap);
            }
        }

        private void OnGUI()
        {
            if (Event.current == null
                || !IsOpen
                || GamePauseController.IsPaused
                || worldMap == null
                || (!isMenuPreviewMode && aircraft == null))
            {
                return;
            }

            var mapRect = GetMapRect();
            var geoRect = GetGeoMapRect(mapRect);
            HandleMapPointerInput(geoRect);
            if (TryHandleMenuPreviewBackButton())
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            repaintGeoRect = geoRect;
            repaintMapProjection = BuildMapGeoProjection();

            EnsureStyles();
            DrawBackdrop();
            DrawMapContents(geoRect);
            if (!isMenuPreviewMode)
            {
                DrawMapPlayerLayer(geoRect);
            }
            DrawMapWaypointLayer(geoRect);
            DrawHeader();
        }

        private bool TryHandleMenuPreviewBackButton()
        {
            if (!isMenuPreviewMode)
            {
                return false;
            }

            var mapPanel = GetMapRect();
            const float buttonWidth = 120f;
            const float buttonHeight = 44f;
            var backRect = new Rect(
                mapPanel.x + 12f,
                mapPanel.yMax - buttonHeight - 12f,
                buttonWidth,
                buttonHeight);
            if (!StartPageMenuStyles.DrawMenuButton(backRect, "BACK", fontSize: 16))
            {
                return false;
            }

            CampaignMapPageController.CloseActive();
            return true;
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

            GUI.Label(new Rect(mapPanel.x, 0f, mapPanel.width, HeaderHeight), "ANTARCTICA MAP", headerStyle);
            GUI.color = Color.white;
        }

        private void DrawMapContents(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawSatelliteImagery(mapRect);

            DrawBaseMarkers(mapRect);
            DrawCampaignWaypointMarkers(mapRect);
            ClearPlacedMapLabels();
            DrawCarrierMapLabels(mapRect);
            DrawMapBaseNameLabels(mapRect);
            DrawCampaignWaypointMapLabels(mapRect);
            GUI.color = Color.white;
        }

        private void DrawMapWaypointLayer(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

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
            if (mapRoute.Count == 0 || worldMap == null)
            {
                return;
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var activeAutopilot = ResolveAutopilot();
            var autopilotFlying = activeAutopilot != null && activeAutopilot.IsFlying;
            var hudColor = FlightHudColorPalette.Waypoint;
            var routeAlpha = autopilotFlying ? 0.72f : 0.92f;
            var routeColor = new Color(hudColor.r, hudColor.g, hudColor.b, routeAlpha);

            for (var i = 0; i < mapRoute.Count; i++)
            {
                var waypointMiles = WorldToMiles(mapRoute[i].World);
                var waypointGui = WorldMilesToGui(mapRect, waypointMiles);
                if (mapRect.Contains(waypointGui))
                {
                    var radius = i == 0 && autopilotFlying
                        ? WaypointMarkerRadius * 1.08f
                        : WaypointMarkerRadius;
                    DrawMapOutlinedDot(waypointGui, radius, routeColor, 2f);
                }
            }
        }

        private void DrawAutopilotRemainingRoute(Rect mapRect)
        {
            if (autopilot == null || !autopilot.IsFlying || mapRoute.Count <= 1 || aircraft?.Profile == null)
            {
                return;
            }

            var hudColor = FlightHudColorPalette.Waypoint;
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
                }
            }
        }

        private void DrawAutopilotRoute(Rect mapRect)
        {
            if (autopilot == null || !autopilot.IsFlying || !autopilot.HasDestination)
            {
                return;
            }

            var playerMiles = WorldToMiles(aircraft.transform.position);
            var destinationMiles = WorldToMiles(autopilot.DestinationWorld);
            DrawAutopilotRouteLine(mapRect, playerMiles, destinationMiles, isPreview: false);
        }

        private float GetTravelTimeSeconds(float rangeMiles, bool autopilotTimeWarp)
        {
            if (aircraft?.Profile == null || worldMap == null || rangeMiles <= 0f)
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

            var milesPerSecond = worldMap.MphToMilesPerSecond(mph);
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
            var hudColor = FlightHudColorPalette.Waypoint;
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
                DrawOceanUnderlay(mapRect);
                return;
            }

            var projection = ResolveMapGeoProjection(mapRect);
            DrawOceanUnderlay(mapRect);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(mapRect, texture, projection.GetSatelliteTextureCoords());
        }

        private static void DrawOceanUnderlay(Rect mapRect)
        {
            var ocean = GetOceanUnderlayTexture();
            GUI.color = Color.white;
            // Tile slightly so waves keep some scale across zoom levels.
            var tile = new Rect(0f, 0f, 2.4f, 2.4f);
            GUI.DrawTextureWithTexCoords(mapRect, ocean, tile);
        }

        private static Texture2D oceanUnderlayTexture;

        private static Texture2D GetOceanUnderlayTexture()
        {
            if (oceanUnderlayTexture != null)
            {
                return oceanUnderlayTexture;
            }

            const int size = 256;
            oceanUnderlayTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MapOceanUnderlay",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                var v = y / (float)(size - 1);
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)(size - 1);

                    // Soft long swells + finer ripples (Southern Ocean look).
                    var swell = Mathf.Sin((u * 7.2f + v * 1.4f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    var cross = Mathf.Sin((u * 1.1f + v * 5.8f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    var ripple = Mathf.Sin((u * 18f - v * 14f) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    var depth = Mathf.Clamp01(swell * 0.45f + cross * 0.35f + ripple * 0.20f);

                    var ocean = Color.Lerp(MapOceanDeepColor, MapOceanMidColor, depth);
                    ocean = Color.Lerp(ocean, MapOceanShallowColor, Mathf.SmoothStep(0.55f, 1f, depth) * 0.55f);
                    var highlight = Mathf.SmoothStep(0.72f, 1f, ripple) * 0.22f;
                    ocean = Color.Lerp(ocean, MapOceanHighlightColor, highlight);
                    pixels[y * size + x] = ocean;
                }
            }

            oceanUnderlayTexture.SetPixels(pixels);
            oceanUnderlayTexture.Apply(false, true);
            return oceanUnderlayTexture;
        }

        private static readonly Color LandOutpostColor = new Color(0.9f, 0.18f, 0.12f);
        private static readonly Color ClearedOutpostFillColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        private static readonly Color MissionObjectiveFillColor = new Color(0.18f, 0.48f, 0.95f, 1f);
        private static readonly Color WaypointMissionFillColor = MissionObjectiveFillColor;
        private static readonly Color FriendlyOutpostFillColor = new Color(0.18f, 0.72f, 0.22f, 1f);
        private static readonly Color FriendlyOutpostBorderColor = Color.black;
        private static readonly Color CarrierLabelColor = new Color(0.95f, 0.85f, 0.1f);
        private const float LandOutpostBorderPixels = 1f;
        private const float WaypointMissionMarkerPixels = 9f;
        private const float WaypointMissionBorderPixels = 1f;
        private const float CarrierMarkerScale = 1f;

        private void DrawBaseMarkers(Rect mapRect)
        {
            var bases = GetMapBases();
            var dotSize = GetLandOutpostMarkerSize();

            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive)
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, GetBaseMapMiles(baseSite));
                var isCarrier = baseSite.SiteKind == BaseSiteKind.Carrier;
                if (!IsPointVisibleOnMap(mapRect, guiPoint, isCarrier))
                {
                    continue;
                }

                if (baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    if (baseSite.IsDestroyed)
                    {
                        var destroyedSize = dotSize * 4f * CarrierMarkerScale;
                        DrawMapFrame(guiPoint, destroyedSize);
                    }
                    else
                    {
                        var carrierHeight = GetCarrierMarkerHeight(dotSize);
                        CarrierMarkerArt.DrawNorthUpMarker(guiPoint, carrierHeight);
                    }

                    continue;
                }

                if (ShouldDrawFriendlyOutpostMapMarker(baseSite))
                {
                    DrawMapBorderedDot(
                        guiPoint,
                        dotSize,
                        FriendlyOutpostFillColor,
                        LandOutpostBorderPixels,
                        FriendlyOutpostBorderColor);
                    continue;
                }

                if (ShouldDrawMissionObjectiveMapMarker(baseSite))
                {
                    DrawMapBorderedDot(guiPoint, dotSize, MissionObjectiveFillColor, LandOutpostBorderPixels);
                    continue;
                }

                if (ShouldDrawClearedOutpostMapMarker(baseSite))
                {
                    DrawMapBorderedDot(guiPoint, dotSize, ClearedOutpostFillColor, LandOutpostBorderPixels);
                    continue;
                }

                DrawMapBorderedDot(guiPoint, dotSize, LandOutpostColor, LandOutpostBorderPixels);
            }
        }

        private void DrawCampaignWaypointMarkers(Rect mapRect)
        {
            CampaignWaypointLayoutState.EnsureLoaded();
            var waypointList = CampaignWaypointLayoutState.Waypoints;
            if (waypointList.Count == 0)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;

            for (var i = 0; i < waypointList.Count; i++)
            {
                var waypoint = waypointList[i];
                if (!CampaignWaypointLayoutState.ShouldShowOnMap(waypoint, save))
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, CampaignWaypointLayoutState.GetMiles(waypoint));
                if (!mapRect.Contains(guiPoint))
                {
                    continue;
                }

                DrawMapBorderedDot(
                    guiPoint,
                    WaypointMissionMarkerPixels,
                    WaypointMissionFillColor,
                    WaypointMissionBorderPixels);
            }
        }

        private void DrawCampaignWaypointMapLabels(Rect mapRect)
        {
            CampaignWaypointLayoutState.EnsureLoaded();
            var waypointList = CampaignWaypointLayoutState.Waypoints;
            if (waypointList.Count == 0)
            {
                return;
            }

            EnsureBaseMapLabelStyle(GetLandOutpostMarkerSize());
            var save = CharacterSessionState.ActiveSave;
            var dotSize = WaypointMissionMarkerPixels;

            for (var i = 0; i < waypointList.Count; i++)
            {
                var waypoint = waypointList[i];
                if (!CampaignWaypointLayoutState.ShouldShowOnMap(waypoint, save))
                {
                    continue;
                }

                var labelText = waypoint.SiteCode;
                if (string.IsNullOrWhiteSpace(labelText))
                {
                    continue;
                }

                var guiPoint = WorldMilesToGui(mapRect, CampaignWaypointLayoutState.GetMiles(waypoint));
                if (!mapRect.Contains(guiPoint))
                {
                    continue;
                }

                var emphasize = CampaignWaypointLayoutState.IsActiveMissionWaypoint(waypoint, save);
                var size = baseMapLabelStyle.CalcSize(new GUIContent(labelText));
                if (TryPlaceMapLabelRect(
                        mapRect,
                        guiPoint,
                        dotSize,
                        size,
                        emphasize,
                        out var labelRect))
                {
                    DrawBaseMapNameLabelAtRect(labelRect, labelText, emphasize);
                }
            }
        }

        private static void DrawMapLabelBlackText(Rect rect, string text, GUIStyle style)
        {
            var previousContentColor = GUI.contentColor;
            GUI.contentColor = Color.black;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

        private static bool ShouldDrawFriendlyOutpostMapMarker(AntarcticaBase baseSite)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land || baseSite.IsDestroyed)
            {
                return false;
            }

            return AntarcticaOutpostState.IsFriendlyBaseSite(baseSite);
        }

        private static bool ShouldDrawMissionObjectiveMapMarker(AntarcticaBase baseSite)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land || baseSite.IsDestroyed)
            {
                return false;
            }

            if (AntarcticaOutpostState.IsFriendlyBaseSite(baseSite))
            {
                return false;
            }

            return CampaignMissionObjectiveState.IsActiveMissionOutpost(
                CharacterSessionState.ActiveSave,
                baseSite);
        }

        private static bool ShouldDrawClearedOutpostMapMarker(AntarcticaBase baseSite)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
            {
                return false;
            }

            if (AntarcticaOutpostState.IsFriendlyBaseSite(baseSite))
            {
                return false;
            }

            if (baseSite.IsDestroyed || AntarcticaOutpostState.IsBunkerCleared(baseSite.BaseName))
            {
                return true;
            }

            if (OutpostSurfaceBunkerPad.IsRevealedAtOutpost(baseSite))
            {
                return true;
            }

            var save = CharacterSessionState.ActiveSave;
            return LandBossMissionAssignment.IsBunkerRevealedAtOutpost(save, baseSite.BaseName);
        }

        private void DrawMapBaseNameLabels(Rect mapRect)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            var bases = GetMapBases();
            var dotSize = GetLandOutpostMarkerSize();
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

                var guiPoint = WorldMilesToGui(mapRect, GetBaseMapMiles(baseSite));
                if (!mapRect.Contains(guiPoint))
                {
                    continue;
                }

                var emphasize = baseSite == selectedMapBase || baseSite == hoveredMapBase;
                var priority = emphasize ? 100 : 0;
                if (CampaignMissionObjectiveState.IsActiveMissionOutpost(
                        CharacterSessionState.ActiveSave,
                        baseSite))
                {
                    priority += 10;
                }

                priority += baseSite.BaseName.Length;
                labelCandidates.Add((baseSite, guiPoint, emphasize, priority));
            }

            labelCandidates.Sort((a, b) => b.priority.CompareTo(a.priority));

            foreach (var candidate in labelCandidates)
            {
                if (candidate.baseSite != hoveredMapBase && candidate.baseSite != selectedMapBase)
                {
                    continue;
                }

                var labelText = GetOutpostMapLabel(candidate.baseSite);
                if (string.IsNullOrWhiteSpace(labelText))
                {
                    continue;
                }

                var size = baseMapLabelStyle.CalcSize(new GUIContent(labelText));
                if (TryPlaceMapLabelRect(
                        mapRect,
                        candidate.guiPoint,
                        dotSize,
                        size,
                        candidate.emphasize,
                        out var labelRect))
                {
                    DrawBaseMapNameLabelAtRect(labelRect, labelText, candidate.emphasize);
                }
            }
        }

        private static string GetOutpostMapLabel(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return string.Empty;
            }

            var code = baseSite.SiteCode;
            if (string.IsNullOrWhiteSpace(code))
            {
                return baseSite.BaseName;
            }

            // Site codes are the shared ID (OP-01, OP-SOUTH, STN-PALMER).
            return code;
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

            DrawMapLabelYellowText(labelRect, baseName, baseMapLabelStyle);
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

                var guiPoint = WorldMilesToGui(mapRect, GetBaseMapMiles(baseSite));
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

        private static void DrawMapBorderedDot(
            Vector2 center,
            float size,
            Color fillColor,
            float borderPixels = 1f,
            Color? borderColor = null)
        {
            var outerRect = BuildSquareMarkerRect(center, size);
            var inset = borderPixels;
            var innerRect = new Rect(
                outerRect.x + inset,
                outerRect.y + inset,
                outerRect.width - inset * 2f,
                outerRect.height - inset * 2f);

            GUI.color = borderColor ?? Color.black;
            GUI.DrawTexture(outerRect, Texture2D.whiteTexture);
            if (innerRect.width > 0f && innerRect.height > 0f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(innerRect, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        private static Rect BuildSquareMarkerRect(Vector2 center, float size) =>
            new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);

        private float GetLandOutpostMarkerSize() =>
            Mathf.Clamp(GetVisibleWidthMiles() * 0.014f, 6f, 14f);

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

            DrawMapOutlinedDot(guiPoint, 7f * AircraftLandingController.VisualScaleMultiplier, Color.white, outlineThickness);
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
            var markerScale = AircraftLandingController.VisualScaleMultiplier;
            const float sizeScale = 0.5f;
            var noseDistWorld = Mathf.Max(0.35f, GetVisibleWidthMiles() * 0.015f) * sizeScale * worldUnitsPerMile * markerScale;
            var tailDistWorld = Mathf.Max(0.25f, GetVisibleWidthMiles() * 0.01f) * sizeScale * worldUnitsPerMile * markerScale;
            var wingSpanWorld = Mathf.Max(0.2f, GetVisibleWidthMiles() * 0.008f) * sizeScale * worldUnitsPerMile * markerScale;
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
            if (mapPanHotActive)
            {
                GUIUtility.hotControl = 0;
                mapPanHotActive = false;
            }
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
            if (evt != null)
            {
                // OnGUI Event.mousePosition is top-left origin (Unity 6+).
                return evt.mousePosition;
            }

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

            mapPanControlId = GUIUtility.GetControlID(FocusType.Passive);
            var mouse = GetMapGuiMouse(currentEvent);
            var allowPan = !IsAutopilotFlightMode && !isMenuPreviewMode;
            var ownsPan = mapPanHotActive && GUIUtility.hotControl == mapPanControlId;

            if (currentEvent.type == EventType.ScrollWheel && mapRect.Contains(mouse))
            {
                if (isMenuPreviewMode)
                {
                    currentEvent.Use();
                    return;
                }

                if (allowPan && worldMap != null)
                {
                    HandleMapScrollZoom(mapRect, mouse, currentEvent.delta.y);
                    currentEvent.Use();
                    return;
                }
            }

            if (currentEvent.type == EventType.MouseDown
                && currentEvent.button == 1
                && mapRect.Contains(mouse))
            {
                if (isMenuPreviewMode)
                {
                    return;
                }

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

                if (allowPan && zoomLevel > 0.001f)
                {
                    GUIUtility.hotControl = mapPanControlId;
                    mapPanHotActive = true;
                }

                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag
                     && currentEvent.button == 0
                     && (mapPointerDownOnMap || ownsPan))
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
                        ClampPanOffset();
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
                if (mapPanHotActive && GUIUtility.hotControl == mapPanControlId)
                {
                    GUIUtility.hotControl = 0;
                }

                mapPanHotActive = false;
                var releaseGui = GetMapGuiMouse(currentEvent);
                if (!isMenuPreviewMode
                    && !mapPointerDragged
                    && mapRect.Contains(releaseGui))
                {
                    if (IsControlHeld(currentEvent))
                    {
                        AppendRouteWaypointAtMapPoint(mapRect, releaseGui);
                        currentEvent.Use();
                    }
                    else if (TryCommitMapClickTarget(mapRect, releaseGui))
                    {
                        currentEvent.Use();
                    }
                    else
                    {
                        // Waypoints can be set anytime — including during an active autopilot leg.
                        selectedMapBase = null;
                        PlaceMapWaypoint(mapRect, releaseGui);
                        if (ResolveAutopilot() is { IsFlying: true })
                        {
                            CommitAutopilotRoute();
                        }

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

            // Only remove when the click hits a waypoint marker — never wipe the whole route.
            if (!TryPickRouteWaypointAtGuiPoint(mapRect, guiPoint, out var routeIndex))
            {
                return false;
            }

            RemoveMapRouteWaypointAt(routeIndex);
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

            if (activeAutopilot.CommitRoute(legs))
            {
                SyncHudBearingToCurrentTarget();
                BeginAutopilotFlight();
            }
        }

        private bool TryCommitMapClickTarget(Rect mapRect, Vector2 guiPoint)
        {
            // Waypoint markers are drawn above outpost dots — exact hit on overlapping boxes uses that layer.
            if (TryPickCampaignWaypointAtGuiPoint(mapRect, guiPoint, out var pickedWaypoint))
            {
                CommitMapCampaignWaypoint(pickedWaypoint);
                return true;
            }

            if (TryPickBaseAtGuiPoint(mapRect, guiPoint, out var pickedBase))
            {
                CommitMapBaseWaypoint(pickedBase);
                return true;
            }

            return false;
        }

        private void CommitMapCampaignWaypoint(CampaignWaypointRecord pickedWaypoint)
        {
            selectedMapBase = null;
            AbandonSuspendedAutopilotIfNeeded();
            var waypointMiles = CampaignWaypointLayoutState.GetMiles(pickedWaypoint);
            ClearMapRouteAndSetSingleWaypoint(
                MilesToWorld(waypointMiles),
                FormatRouteWaypointLabel(
                    waypointMiles,
                    0,
                    string.IsNullOrWhiteSpace(pickedWaypoint.SiteCode)
                        ? $"WP-{pickedWaypoint.MissionNumber:00}"
                        : pickedWaypoint.SiteCode));
            SyncHudBearingToCurrentTarget();
            if (ResolveAutopilot() is { IsFlying: true })
            {
                CommitAutopilotRoute();
            }
        }

        private void CommitMapBaseWaypoint(AntarcticaBase pickedBase)
        {
            selectedMapBase = pickedBase;
            AbandonSuspendedAutopilotIfNeeded();
            ClearMapRouteAndSetSingleWaypoint(
                GetBaseWorldPosition(pickedBase),
                FormatRouteWaypointLabel(
                    GetBaseMapMiles(pickedBase),
                    0,
                    pickedBase.SiteCode));
            SyncHudBearingToCurrentTarget();
            if (ResolveAutopilot() is { IsFlying: true })
            {
                CommitAutopilotRoute();
            }
        }

        private bool TryPickBaseAtGuiPoint(Rect mapRect, Vector2 guiPoint, out AntarcticaBase baseSite)
        {
            baseSite = null;
            var dotSize = GetLandOutpostMarkerSize();

            foreach (var candidate in GetMapBases())
            {
                if (candidate == null || !candidate.IsActive || candidate.IsDestroyed)
                {
                    continue;
                }

                if (!IsGuiPointOnBaseMarker(mapRect, guiPoint, candidate, dotSize))
                {
                    continue;
                }

                baseSite = candidate;
                return true;
            }

            return false;
        }

        private bool TryPickCampaignWaypointAtGuiPoint(
            Rect mapRect,
            Vector2 guiPoint,
            out CampaignWaypointRecord waypoint)
        {
            waypoint = null;
            CampaignWaypointLayoutState.EnsureLoaded();
            var waypointList = CampaignWaypointLayoutState.Waypoints;
            if (waypointList.Count == 0)
            {
                return false;
            }

            var save = CharacterSessionState.ActiveSave;

            for (var i = 0; i < waypointList.Count; i++)
            {
                var candidate = waypointList[i];
                if (!CampaignWaypointLayoutState.ShouldShowOnMap(candidate, save))
                {
                    continue;
                }

                var guiBase = WorldMilesToGui(mapRect, CampaignWaypointLayoutState.GetMiles(candidate));
                if (!mapRect.Contains(guiBase))
                {
                    continue;
                }

                if (!BuildSquareMarkerRect(guiBase, WaypointMissionMarkerPixels).Contains(guiPoint))
                {
                    continue;
                }

                waypoint = candidate;
                return true;
            }

            return false;
        }

        private bool IsGuiPointOnBaseMarker(
            Rect mapRect,
            Vector2 guiPoint,
            AntarcticaBase candidate,
            float dotSize)
        {
            var guiBase = WorldMilesToGui(mapRect, GetBaseMapMiles(candidate));

            if (candidate.SiteKind == BaseSiteKind.Carrier)
            {
                if (candidate.IsDestroyed)
                {
                    var destroyedSize = dotSize * 4f * CarrierMarkerScale;
                    return BuildSquareMarkerRect(guiBase, destroyedSize).Contains(guiPoint);
                }

                var carrierHeight = GetCarrierMarkerHeight(dotSize);
                var carrierWidth = CarrierMarkerArt.GetWidthForHeight(carrierHeight);
                return new Rect(
                    guiBase.x - carrierWidth * 0.5f,
                    guiBase.y - carrierHeight * 0.5f,
                    carrierWidth,
                    carrierHeight).Contains(guiPoint);
            }

            return BuildSquareMarkerRect(guiBase, dotSize).Contains(guiPoint);
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

            var dotSize = GetLandOutpostMarkerSize();
            EnsureBaseRangeLabelStyle(dotSize);
            var guiPoint = WorldMilesToGui(mapRect, GetBaseMapMiles(selectedMapBase));
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

        private MapGeoProjection BuildMapGeoProjection()
        {
            GetVisibleHalfExtents(out var halfXMiles, out var halfYMiles);
            return new MapGeoProjection(
                GetViewCenterMiles(),
                halfXMiles,
                halfYMiles,
                worldMap.antarcticaSizeMiles);
        }

        private MapGeoProjection GetMapGeoProjection() => BuildMapGeoProjection();

        private MapGeoProjection ResolveMapGeoProjection(Rect mapRect)
        {
            if (repaintMapProjection.HasValue && repaintGeoRect == mapRect)
            {
                return repaintMapProjection.Value;
            }

            return BuildMapGeoProjection();
        }

        private void HandleMapScrollZoom(Rect mapRect, Vector2 mouse, float scrollDelta)
        {
            if (Mathf.Abs(scrollDelta) < 0.01f)
            {
                return;
            }

            var projection = BuildMapGeoProjection();
            var zoomAnchorMiles = projection.GuiToWorldMiles(mapRect, mouse);

            zoomLevel = Mathf.Clamp01(zoomLevel + scrollDelta * ScrollSensitivity);
            if (zoomLevel <= 0.001f)
            {
                zoomLevel = 0f;
                panOffsetMiles = CampaignMapCoordinates.GetMapCenterMiles(worldMap);
            }
            else
            {
                panOffsetMiles = BuildMapGeoProjection()
                    .ComputePanForAnchorAtGui(mapRect, zoomAnchorMiles, mouse);
            }

            ClampPanOffset();
        }

        private Vector2 GuiToWorldMiles(Rect mapRect, Vector2 guiPoint)
        {
            return GetMapGeoProjection().GuiToWorldMiles(mapRect, guiPoint);
        }

        private Vector3 MilesToWorld(Vector2 miles)
        {
            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            return CampaignMapCoordinates.MilesToWorld(miles, worldMap, ticSize);
        }

        private void ClampPanOffset()
        {
            var mapWidthMiles = worldMap.antarcticaSizeMiles;
            var mapHeightMiles = AntarcticaLandMask.GetMapHeightMiles(mapWidthMiles);
            GetVisibleHalfExtents(out var visibleHalfXMiles, out var visibleHalfYMiles);

            if (visibleHalfXMiles >= mapWidthMiles * 0.5f
                && visibleHalfYMiles >= mapHeightMiles * 0.5f)
            {
                if (zoomLevel <= 0.001f)
                {
                    panOffsetMiles = CampaignMapCoordinates.GetMapCenterMiles(worldMap);
                }

                return;
            }

            var panMinX = visibleHalfXMiles;
            var panMaxX = mapWidthMiles - visibleHalfXMiles;
            var panMinY = visibleHalfYMiles;
            var panMaxY = mapHeightMiles - visibleHalfYMiles;
            panOffsetMiles = new Vector2(
                Mathf.Clamp(panOffsetMiles.x, panMinX, panMaxX),
                Mathf.Clamp(panOffsetMiles.y, panMinY, panMaxY));
        }

        private Rect GetGeoMapRect(Rect mapRect)
        {
            var texture = GetSatelliteTexture();
            var aspect = GetMapTextureAspect();

            var geoHeight = mapRect.height;
            var geoWidth = geoHeight * aspect;
            if (geoWidth > mapRect.width)
            {
                geoWidth = mapRect.width;
                geoHeight = geoWidth / aspect;
            }

            var y = mapRect.y + (mapRect.height - geoHeight) * 0.5f;
            return new Rect(mapRect.center.x - geoWidth * 0.5f, y, geoWidth, geoHeight);
        }

        private float GetMapPanelEastWestStretch()
        {
            return GetMapTextureAspect();
        }

        private float GetMapTextureAspect()
        {
            var texture = GetSatelliteTexture();
            return texture != null ? (float)texture.width / texture.height : 1024f / 837f;
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

        private void GetVisibleHalfExtents(out float halfXMiles, out float halfYMiles)
        {
            var mapAspect = AntarcticaLandMask.GetMapWidthOverHeight();
            var antarcticaHalfWidthMiles = worldMap.antarcticaSizeMiles * 0.5f;
            var fullViewHalfXMiles = antarcticaHalfWidthMiles * FullViewOceanPadding;
            var minHalfXMiles = MinVisibleWidthMiles * 0.5f;
            var t = zoomLevel <= 0.001f ? 0f : zoomLevel;
            halfXMiles = Mathf.Lerp(fullViewHalfXMiles, minHalfXMiles, t);
            halfYMiles = halfXMiles / mapAspect;
        }

        private float GetVisibleHalfMiles()
        {
            GetVisibleHalfExtents(out var halfXMiles, out _);
            return halfXMiles;
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
            // All overlay markers (outposts, CV, player, routes) must go through this path.
            return ResolveMapGeoProjection(mapRect).WorldMilesToGui(mapRect, worldMiles);
        }

        private Vector2 ComputePanForAnchorAtGui(Rect mapRect, Vector2 anchorMiles, Vector2 guiPoint)
        {
            return ResolveMapGeoProjection(mapRect).ComputePanForAnchorAtGui(mapRect, anchorMiles, guiPoint);
        }

        /// <summary>
        /// Locked map display (do not change without explicit map-orientation work):
        /// Satellite — top=north, bottom=south, left=west, right=east (<see cref="GetSatelliteTextureCoords"/>).
        /// Overlays (outposts, CV, player, routes) use the same texture-space mapping as the satellite layer.
        /// Mile/world/spawn data is never modified here.
        /// </summary>
        private readonly struct MapGeoProjection
        {
            public readonly float UMin;
            public readonly float UMax;
            public readonly float MileVMin;
            public readonly float MileVMax;
            public readonly float MapWidthMiles;
            public readonly float MapHeightMiles;
            public readonly float VisibleHalfMilesX;
            public readonly float VisibleHalfMilesY;

            public MapGeoProjection(Vector2 centerMiles, float visibleHalfMilesX, float visibleHalfMilesY, float mapWidthMiles)
            {
                VisibleHalfMilesX = visibleHalfMilesX;
                VisibleHalfMilesY = visibleHalfMilesY;
                MapWidthMiles = mapWidthMiles;
                MapHeightMiles = AntarcticaLandMask.GetMapHeightMiles(mapWidthMiles);
                UMin = (centerMiles.x - visibleHalfMilesX) / MapWidthMiles;
                UMax = (centerMiles.x + visibleHalfMilesX) / MapWidthMiles;
                MileVMin = (centerMiles.y - visibleHalfMilesY) / MapHeightMiles;
                MileVMax = (centerMiles.y + visibleHalfMilesY) / MapHeightMiles;
            }

            public Rect GetSatelliteTextureCoords()
            {
                var vMin = 1f - MileVMax;
                var vMax = 1f - MileVMin;
                return new Rect(UMin, vMin, UMax - UMin, vMax - vMin);
            }

            public Vector2 WorldMilesToGui(Rect mapRect, Vector2 worldMiles)
            {
                MileFractionsToGui(mapRect, worldMiles, out var guiX, out var guiY);
                return new Vector2(guiX, guiY);
            }

            public Vector2 GuiToWorldMiles(Rect mapRect, Vector2 guiPoint)
            {
                GuiToMileFractions(mapRect, guiPoint, out var fu, out var fy);
                return FractionsToWorldMiles(fu, fy);
            }

            public Vector2 ComputePanForAnchorAtGui(Rect mapRect, Vector2 anchorMiles, Vector2 guiPoint)
            {
                GuiToMileFractions(mapRect, guiPoint, out var fu, out var fy);
                var visibleWidthMiles = VisibleHalfMilesX * 2f;
                var visibleHeightMiles = VisibleHalfMilesY * 2f;
                return new Vector2(
                    anchorMiles.x + VisibleHalfMilesX - fu * visibleWidthMiles,
                    anchorMiles.y + VisibleHalfMilesY - fy * visibleHeightMiles);
            }

            private void MileFractionsToGui(Rect mapRect, Vector2 worldMiles, out float guiX, out float guiY)
            {
                WorldMilesToFractions(worldMiles, out var fu, out var fy);
                var vMin = 1f - MileVMax;
                var vMax = 1f - MileVMin;
                var textureV = Mathf.Lerp(vMin, vMax, fy);
                var fvFromBottom = Mathf.InverseLerp(vMin, vMax, textureV);
                guiX = mapRect.xMin + fu * mapRect.width;
                guiY = mapRect.yMax - fvFromBottom * mapRect.height;
            }

            private void GuiToMileFractions(Rect mapRect, Vector2 guiPoint, out float fu, out float fy)
            {
                fu = (guiPoint.x - mapRect.xMin) / mapRect.width;
                fy = (mapRect.yMax - guiPoint.y) / mapRect.height;
            }

            private void WorldMilesToFractions(Vector2 worldMiles, out float fu, out float fy)
            {
                var u = worldMiles.x / MapWidthMiles;
                var mileV = worldMiles.y / MapHeightMiles;
                fu = Mathf.InverseLerp(UMin, UMax, u);
                fy = Mathf.InverseLerp(MileVMin, MileVMax, mileV);
            }

            private Vector2 FractionsToWorldMiles(float fu, float fy)
            {
                var u = Mathf.Lerp(UMin, UMax, fu);
                var mileV = Mathf.Lerp(MileVMin, MileVMax, fy);
                return new Vector2(
                    u * MapWidthMiles,
                    mileV * MapHeightMiles);
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
            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            return CampaignMapCoordinates.WorldToMiles(worldPosition, worldMap, ticSize);
        }

        private float GetWorldUnitsPerMile()
        {
            if (aircraft?.Profile == null)
            {
                return 20f;
            }

            return worldMap.GridSpacingTics * aircraft.Profile.ticSizeWorldUnits / worldMap.milesPerGrid;
        }

        private Texture2D GetSatelliteTexture()
        {
            if (satelliteTexture == null)
            {
                satelliteTexture = AntarcticaLandMask.GetReadableMap();
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
            if (headerStyle != null)
            {
                headerStyle.normal.textColor = hudColor;
                headerStyle.alignment = TextAnchor.MiddleCenter;
                return;
            }

            headerStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.MiddleCenter, hudColor);
        }
    }
}
