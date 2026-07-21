using System.Collections.Generic;
using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Flight
{
    [RequireComponent(typeof(AircraftController))]
    [RequireComponent(typeof(Rigidbody))]
    public class AutopilotController : MonoBehaviour
    {
        public readonly struct RouteLeg
        {
            public RouteLeg(Vector3 world, string label)
            {
                world.y = 0f;
                World = world;
                Label = label;
            }

            public Vector3 World { get; }
            public string Label { get; }
        }
        public const float HostileBaseContactMiles = 50f;
        public const float HostileUnitContactMiles = 40f;
        public const float MinTimeWarpScale = 10f;
        public const float MaxTimeWarpScale = 50f;
        public const float TimeWarpStep = 10f;
        public const float DefaultTimeWarpScale = 20f;
        public const float ArrivalThresholdMiles = 2f;

        private AircraftController aircraft;
        private Rigidbody body;
        private AntarcticaMapOverlay mapOverlay;

        private Vector3 destinationWorld;
        private bool hasDestination;
        private string destinationLabel = string.Empty;
        private float currentTimeWarpScale = DefaultTimeWarpScale;
        private readonly List<RouteLeg> routeQueue = new List<RouteLeg>(8);
        private int suspendInputFrame = -1;

        public bool IsFlying { get; private set; }
        public bool IsSelectingDestination { get; private set; }
        public bool HasDestination => hasDestination;
        public bool HasPendingRouteLegs => routeQueue.Count > 0;
        public int PendingRouteLegCount => routeQueue.Count;
        public Vector3 DestinationWorld => destinationWorld;
        public string DestinationLabel => destinationLabel;
        public float DestinationDistanceMiles { get; private set; }
        public float TimeWarpScale => currentTimeWarpScale;
        public string StatusToast { get; private set; } = string.Empty;
        public float StatusToastUntil { get; private set; }
        public bool CanResume => hasDestination && !IsFlying;

        public static AutopilotController Instance { get; private set; }

        public void CancelAutopilot()
        {
            if (!IsFlying)
            {
                return;
            }

            SuspendAutopilot(closeMap: true);
            suspendInputFrame = Time.frameCount;
            ShowToast("Autopilot paused — P to resume.");
        }

        public void DisengageAutopilot(string reason = "Autopilot canceled.")
        {
            if (IsFlying)
            {
                Disengage(reason, closeMap: false);
                ShowToast("Autopilot canceled.");
                return;
            }

            if (!CanResume)
            {
                return;
            }

            AbandonSuspendedRoute();
            mapOverlay?.EndAutopilotFlight();
            ShowToast("Autopilot canceled.");
            Debug.Log($"F-89: {reason}");
        }

        private void HandleAutopilotRightClickCancel()
        {
            if (GamePauseController.IsPaused || (!IsFlying && !CanResume))
            {
                return;
            }

            if (mapOverlay == null)
            {
                mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            }

            if (mapOverlay != null && mapOverlay.CancelAutopilotIfActive())
            {
                return;
            }

            DisengageAutopilot("Autopilot canceled.");
        }

        public void ResumeAutopilot()
        {
            if (IsFlying || !hasDestination)
            {
                return;
            }

            if (HasHostileContact())
            {
                Debug.Log(
                    "F-89: Autopilot blocked — hostile base within 50 MI or unit within 40 MI.");
                return;
            }

            if (mapOverlay == null)
            {
                mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            }

            IsSelectingDestination = false;
            IsFlying = true;
            aircraft?.ApplyAutopilotState(
                aircraft.Profile != null ? aircraft.Profile.ThrottleSpeedWorld : 0f,
                true);
            mapOverlay?.SetAutopilotHudBearing(destinationWorld, destinationLabel);
            if (mapOverlay != null && AntarcticaMapOverlay.IsOpen)
            {
                mapOverlay.BeginAutopilotFlight();
            }

            Time.timeScale = currentTimeWarpScale;
            ShowToast(destinationLabel);
            Debug.Log(
                $"F-89: Autopilot resumed — {destinationLabel} at {currentTimeWarpScale:0}x speed.");
        }

        public void Configure(AntarcticaMapOverlay map)
        {
            mapOverlay = map;
        }

        private void Awake()
        {
            aircraft = GetComponent<AircraftController>();
            body = GetComponent<Rigidbody>();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (IsFlying)
            {
                Time.timeScale = 1f;
            }
        }

        private void Update()
        {
            if (GamePauseController.IsPaused)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                HandleAutopilotKey();
            }

            if (Input.GetMouseButtonDown(1))
            {
                HandleAutopilotRightClickCancel();
            }

            if (IsFlying && HasManualSteeringInput())
            {
                Disengage("Manual steering — autopilot off.");
                return;
            }

            HandleTimeWarpInput();

            if (IsFlying && !GamePauseController.IsPaused)
            {
                Time.timeScale = currentTimeWarpScale;
            }
        }

        private void HandleTimeWarpInput()
        {
            if (!IsFlying && !IsSelectingDestination)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                ChangeTimeWarp(-TimeWarpStep);
            }
            else if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadEquals)
                || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                ChangeTimeWarp(TimeWarpStep);
            }
        }

        private void ChangeTimeWarp(float delta)
        {
            var next = Mathf.Clamp(currentTimeWarpScale + delta, MinTimeWarpScale, MaxTimeWarpScale);
            if (Mathf.Approximately(next, currentTimeWarpScale))
            {
                return;
            }

            currentTimeWarpScale = next;
            if (IsFlying && !GamePauseController.IsPaused)
            {
                Time.timeScale = currentTimeWarpScale;
            }

            ShowToast($"Autopilot {currentTimeWarpScale:0}X");
            Debug.Log($"F-89: Autopilot speed set to {currentTimeWarpScale:0}x.");
        }

        private void FixedUpdate()
        {
            if (!IsFlying || aircraft == null || body == null || !hasDestination)
            {
                return;
            }

            if (HasHostileContact())
            {
                Disengage("Hostile contact — autopilot off.", closeMap: true);
                return;
            }

            FlyTowardDestination();

            DestinationDistanceMiles = CombatThreatRange.DistanceMiles(
                transform.position,
                destinationWorld,
                aircraft.WorldMap,
                aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f);

            if (DestinationDistanceMiles <= ArrivalThresholdMiles)
            {
                if (HasPendingRouteLegs)
                {
                    AdvanceToNextRouteLeg();
                }
                else
                {
                    mapOverlay?.ClearMapRouteOnArrival();
                    Disengage($"Arrived at {destinationLabel}.", closeMap: true);
                }
            }
        }

        private void AdvanceToNextRouteLeg()
        {
            mapOverlay?.AdvanceMapRouteAfterLeg();

            var next = routeQueue[0];
            routeQueue.RemoveAt(0);
            ApplyDestination(next.World, next.Label);
            mapOverlay?.SetAutopilotHudBearing(next.World, next.Label);
            ShowToast(next.Label);
            Debug.Log($"F-89: Autopilot — next leg {next.Label} ({routeQueue.Count} remaining).");
        }

        private void HandleAutopilotKey()
        {
            if (IsFlying)
            {
                CancelAutopilot();
                return;
            }

            if (hasDestination && Time.frameCount != suspendInputFrame)
            {
                if (mapOverlay == null)
                {
                    mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
                }

                if (mapOverlay != null
                    && mapOverlay.HasAutopilotMapTarget
                    && mapOverlay.TryEngageAutopilotToMapTarget())
                {
                    return;
                }

                ResumeAutopilot();
                return;
            }

            if (IsSelectingDestination)
            {
                if (mapOverlay != null && mapOverlay.TryEngageAutopilotToMapTarget())
                {
                    return;
                }

                CancelDestinationSelection();
                return;
            }

            if (HasHostileContact())
            {
                Debug.Log(
                    "F-89: Autopilot blocked — hostile base within 50 MI or unit within 40 MI.");
                return;
            }

            if (mapOverlay == null)
            {
                mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            }

            var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!shiftHeld && mapOverlay != null && mapOverlay.TryEngageAutopilotToMapTarget())
            {
                return;
            }

            BeginDestinationSelection();
        }

        public void BeginDestinationSelection()
        {
            if (mapOverlay == null)
            {
                mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            }

            IsSelectingDestination = true;
            mapOverlay?.OpenAutopilotSelection();
        }

        public void CancelDestinationSelection()
        {
            IsSelectingDestination = false;
            mapOverlay?.CloseMap();
        }

        public void CommitDestination(Vector3 worldPosition, string label)
        {
            CommitRoute(new[] { new RouteLeg(worldPosition, label) });
        }

        public void CommitRoute(IReadOnlyList<RouteLeg> legs)
        {
            if (legs == null || legs.Count == 0)
            {
                return;
            }

            routeQueue.Clear();
            for (var i = 1; i < legs.Count; i++)
            {
                routeQueue.Add(legs[i]);
            }

            ApplyDestination(legs[0].World, legs[0].Label);
            IsSelectingDestination = false;
            IsFlying = true;
            aircraft?.ApplyAutopilotState(
                aircraft.Profile != null ? aircraft.Profile.ThrottleSpeedWorld : 0f,
                true);
            mapOverlay?.BeginAutopilotFlight();
            Time.timeScale = currentTimeWarpScale;
            var routeHint = routeQueue.Count > 0 ? $" ({legs.Count} legs)" : string.Empty;
            ShowToast(destinationLabel);
            Debug.Log(
                $"F-89: Autopilot engaged — {destinationLabel}{routeHint} at {currentTimeWarpScale:0}x speed.");
        }

        public void AppendRouteLeg(Vector3 worldPosition, string label)
        {
            if (!IsFlying)
            {
                return;
            }

            worldPosition.y = 0f;
            routeQueue.Add(new RouteLeg(worldPosition, label));
            Debug.Log(
                $"F-89: Route extended — {label} ({routeQueue.Count} leg(s) after current).");
        }

        private void ApplyDestination(Vector3 worldPosition, string label)
        {
            destinationWorld = worldPosition;
            destinationWorld.y = 0f;
            destinationLabel = string.IsNullOrWhiteSpace(label)
                ? $"({FormatMiles(worldPosition)})"
                : label;
            hasDestination = true;
        }

        private void ShowToast(string message)
        {
            StatusToast = message;
            StatusToastUntil = Time.unscaledTime + 2.5f;
        }


        private void FlyTowardDestination()
        {
            var profile = aircraft.Profile;
            if (profile == null)
            {
                return;
            }

            var position = transform.position;
            position.y = 0f;
            var toTarget = destinationWorld - position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var targetDirection = toTarget.normalized;
            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var signedAngle = Vector3.SignedAngle(forward, targetDirection, Vector3.up);
            var turnInput = Mathf.Clamp(signedAngle / 45f, -1f, 1f);
            transform.Rotate(0f, turnInput * profile.turnRate * Time.fixedDeltaTime, 0f, Space.World);

            forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            var speed = profile.ThrottleSpeedWorld;
            body.linearVelocity = forward * speed;
            aircraft.ApplyAutopilotState(speed, true);
        }

        private static bool HasManualSteeringInput()
        {
            return Input.GetKey(KeyCode.A)
                || Input.GetKey(KeyCode.D)
                || Input.GetKey(KeyCode.LeftArrow)
                || Input.GetKey(KeyCode.RightArrow);
        }

        private void SuspendAutopilot(bool closeMap = false)
        {
            IsFlying = false;
            IsSelectingDestination = false;
            Time.timeScale = 1f;
            mapOverlay?.SetAutopilotHudBearing(destinationWorld, destinationLabel);
            if (closeMap)
            {
                mapOverlay?.CloseMap();
            }
            else
            {
                mapOverlay?.PauseAutopilotFlight();
            }

            aircraft?.ApplyAutopilotState(0f, false);
            Debug.Log($"F-89: Autopilot paused — {destinationLabel}.");
        }

        public void AbandonSuspendedRoute()
        {
            if (!CanResume)
            {
                return;
            }

            IsFlying = false;
            IsSelectingDestination = false;
            hasDestination = false;
            destinationLabel = string.Empty;
            DestinationDistanceMiles = 0f;
            routeQueue.Clear();
            aircraft?.ApplyAutopilotState(0f, false);
        }

        private void Disengage(string reason, bool closeMap = false)
        {
            IsFlying = false;
            IsSelectingDestination = false;
            hasDestination = false;
            destinationLabel = string.Empty;
            DestinationDistanceMiles = 0f;
            routeQueue.Clear();
            Time.timeScale = 1f;
            mapOverlay?.EndAutopilotFlight();
            if (closeMap)
            {
                mapOverlay?.CloseMap();
            }

            aircraft?.ApplyAutopilotState(0f, false);
            Debug.Log($"F-89: {reason}");
        }

        private bool HasHostileContact()
        {
            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return false;
            }

            return CombatThreatRange.HasHostileContact(
                transform.position,
                aircraft.WorldMap,
                aircraft.Profile.ticSizeWorldUnits);
        }

        private string FormatMiles(Vector3 worldPosition)
        {
            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return "destination";
            }

            var worldUnitsPerMile = aircraft.WorldMap.GridSpacingTics
                * aircraft.Profile.ticSizeWorldUnits
                / aircraft.WorldMap.milesPerGrid;
            var miles = WorldMapConfig.WorldToMileOffset(worldPosition, worldUnitsPerMile);
            return $"{miles.x:0}, {miles.y:0} MI";
        }
    }
}
