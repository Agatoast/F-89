using F89.Controls;
using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Flight
{
    public class AircraftLandingController : MonoBehaviour
    {
        public const float ShrinkDurationSeconds = 5f;
        public const float TargetVisualScale = 0.1f;

        private static AircraftLandingController activeInstance;

        private AircraftController aircraft;
        private PlayerAircraftInput input;
        private Rigidbody body;
        private Transform visualPivot;
        private Vector3 initialVisualScale = Vector3.one;
        private float sequenceStartTime;
        private float currentVisualScale = 1f;
        private bool sequenceActive;
        private bool takeoffActive;
        private bool landingComplete;

        public static bool IsLandingActive => activeInstance != null && activeInstance.sequenceActive;
        public static bool IsTakeoffActive => activeInstance != null && activeInstance.takeoffActive;
        public static bool IsLandingComplete => activeInstance != null && activeInstance.landingComplete;
        public static float VisualScaleMultiplier => activeInstance?.currentVisualScale ?? 1f;

        private void Awake()
        {
            aircraft = GetComponent<AircraftController>();
            input = GetComponent<PlayerAircraftInput>();
            body = GetComponent<Rigidbody>();
            visualPivot = transform.Find("VisualPivot");
            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        public void BeginLanding()
        {
            if (sequenceActive || landingComplete)
            {
                return;
            }

            sequenceActive = true;
            activeInstance = this;
            sequenceStartTime = Time.time;

            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }

            var autopilot = GetComponent<AutopilotController>();
            autopilot?.DisengageAutopilot("Landing.");

            if (input != null)
            {
                input.enabled = false;
            }
        }

        public void PrepareForGroundReturn()
        {
            activeInstance = this;
            sequenceActive = false;
            takeoffActive = false;
            landingComplete = false;
            currentVisualScale = TargetVisualScale;

            if (visualPivot != null)
            {
                if (initialVisualScale.sqrMagnitude < 0.0001f)
                {
                    initialVisualScale = Vector3.one;
                }

                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            aircraft?.SetLandingLocked(true);

            if (input != null)
            {
                input.enabled = false;
            }
        }

        public void BeginTakeoff()
        {
            if (takeoffActive || sequenceActive)
            {
                return;
            }

            takeoffActive = true;
            activeInstance = this;
            sequenceStartTime = Time.time;
            currentVisualScale = TargetVisualScale;

            if (input != null)
            {
                input.enabled = false;
            }
        }

        private void Update()
        {
            if (takeoffActive)
            {
                UpdateTakeoffVisual();
                return;
            }

            if (!sequenceActive)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            currentVisualScale = Mathf.Lerp(1f, TargetVisualScale, progress);

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * currentVisualScale;
            }

            if (progress >= 1f)
            {
                CompleteLanding();
            }
        }

        private void FixedUpdate()
        {
            if (takeoffActive)
            {
                UpdateTakeoffMotion();
                return;
            }

            if (!sequenceActive || aircraft == null || body == null)
            {
                return;
            }

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            body.linearVelocity = forward.normalized * aircraft.CurrentSpeed;
        }

        private void CompleteLanding()
        {
            sequenceActive = false;
            landingComplete = true;
            currentVisualScale = TargetVisualScale;

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            if (aircraft != null)
            {
                aircraft.SetLandingLocked(true);
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }

            var snapshot = CaptureSortieSnapshot(gameObject);
            LandMissionHandoffState.BeginEnterFromFlight(snapshot);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }

        public static LandSortieSnapshot CaptureSortieSnapshot(GameObject player)
        {
            var snapshot = new LandSortieSnapshot
            {
                IsValid = true,
                AircraftWorldPosition = player != null ? player.transform.position : Vector3.zero,
                AircraftWorldRotation = player != null ? player.transform.rotation : Quaternion.identity,
                ReturnSceneName = GameScenes.FlightTest
            };

            if (player == null)
            {
                snapshot.FuelNormalized = 1f;
                return snapshot;
            }

            var aircraftController = player.GetComponent<AircraftController>();
            if (aircraftController != null)
            {
                snapshot.LeftTankGallons = aircraftController.LeftTankGallons;
                snapshot.RightTankGallons = aircraftController.RightTankGallons;
                snapshot.AfterburnerFuelRemaining = aircraftController.AfterburnerFuelRemaining;
                snapshot.FuelNormalized = aircraftController.TotalFuelCapacityGallons > 0f
                    ? aircraftController.TotalFuelGallons / aircraftController.TotalFuelCapacityGallons
                    : 1f;
            }
            else
            {
                snapshot.FuelNormalized = 1f;
            }

            var weapons = player.GetComponent<F89.Weapons.PlayerWeaponController>();
            if (weapons != null && weapons.HasSortieInventory)
            {
                snapshot.HasStoresInventory = true;
                snapshot.Aim9zRemaining = weapons.Aim9zRemaining;
                snapshot.Agm88jRemaining = weapons.Agm88jRemaining;
                snapshot.Gbu12Remaining = weapons.Gbu12Remaining;
                snapshot.Agm114Remaining = weapons.Agm114Remaining;
                snapshot.GauRoundsRemaining = weapons.Gau27aGun != null
                    ? weapons.Gau27aGun.RoundsRemaining
                    : 0;
            }

            var flares = player.GetComponent<F89.Weapons.FlareCountermeasureController>();
            if (flares != null)
            {
                snapshot.FlaresRemaining = flares.FlaresRemaining;
            }

            return snapshot;
        }

        private void UpdateTakeoffVisual()
        {
            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            currentVisualScale = Mathf.Lerp(TargetVisualScale, 1f, progress);

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * currentVisualScale;
            }

            if (progress >= 1f)
            {
                CompleteTakeoff();
            }
        }

        private void UpdateTakeoffMotion()
        {
            if (aircraft == null || body == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            var targetSpeedMph = Mathf.Lerp(0f, AircraftLanding.MaxLandingSpeedMph, progress);
            aircraft.ApplyTakeoffSpeed(targetSpeedMph);
        }

        private void CompleteTakeoff()
        {
            takeoffActive = false;
            currentVisualScale = 1f;

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale;
            }

            if (aircraft != null)
            {
                aircraft.SetLandingLocked(false);
                aircraft.ApplyTakeoffSpeed(AircraftLanding.MaxLandingSpeedMph);
            }

            if (input != null)
            {
                input.enabled = true;
            }

            activeInstance = null;
            LandMissionHandoffState.ConfirmReturnApplied();
        }
    }
}
