using F89.Controls;
using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Flight
{
    [RequireComponent(typeof(Rigidbody))]
    public class AircraftController : MonoBehaviour
    {
        [SerializeField] private FlightProfile profile;
        [SerializeField] private WorldMapConfig worldMap;
        [SerializeField] private PlayerAircraftInput inputSource;

        private Rigidbody body;
        private float currentSpeed;
        private float currentSpeedMph;
        private float afterburnerFuelRemaining;
        private bool afterburnerSpoolDownActive;
        private float leftTankGallons;
        private float rightTankGallons;

        public float CurrentSpeed => currentSpeed;
        public float CurrentSpeedTics => profile != null && profile.ticSizeWorldUnits > 0f
            ? currentSpeed / profile.ticSizeWorldUnits
            : 0f;
        public float CurrentSpeedMph => currentSpeedMph;
        public bool IsAfterburning { get; private set; }
        public bool CanUseAfterburner => afterburnerFuelRemaining > 0f;
        public float AfterburnerFuelRemaining => afterburnerFuelRemaining;
        public float AfterburnerFuelNormalized =>
            profile == null || profile.afterburnerFuelCapacity <= 0f
                ? 0f
                : afterburnerFuelRemaining / profile.afterburnerFuelCapacity;
        public FlightProfile Profile => profile;
        public WorldMapConfig WorldMap => worldMap;
        public float FuelRangeRemainingMiles => ProjectedRangeMiles;
        public float ProjectedRangeMiles =>
            worldMap == null || worldMap.maxFuelRangeMiles <= 0f || TotalFuelCapacityGallons <= 0f
                ? 0f
                : worldMap.maxFuelRangeMiles * (TotalFuelGallons / TotalFuelCapacityGallons);
        public float FuelRangeNormalized =>
            worldMap == null || worldMap.maxFuelRangeMiles <= 0f
                ? 0f
                : ProjectedRangeMiles / worldMap.maxFuelRangeMiles;
        public float LeftTankGallons => leftTankGallons;
        public float RightTankGallons => rightTankGallons;
        public float FuelGallonsPerTank => worldMap != null ? worldMap.fuelGallonsPerTank : 1350f;
        public float TotalFuelCapacityGallons => FuelGallonsPerTank * 2f;
        public float TotalFuelGallons => leftTankGallons + rightTankGallons;
        public float LeftTankNormalized =>
            FuelGallonsPerTank > 0f ? leftTankGallons / FuelGallonsPerTank : 0f;
        public float RightTankNormalized =>
            FuelGallonsPerTank > 0f ? rightTankGallons / FuelGallonsPerTank : 0f;
        public bool IsOutOfFuel => TotalFuelGallons <= 0f;
        public bool IsAutopilotActive { get; private set; }
        public bool IsLandingLocked { get; private set; }

        public void SetLandingLocked(bool locked)
        {
            IsLandingLocked = locked;
            if (locked)
            {
                IsAutopilotActive = false;
                IsAfterburning = false;
                currentSpeed = 0f;
                currentSpeedMph = 0f;
                if (body != null)
                {
                    body.linearVelocity = Vector3.zero;
                }
            }
        }

        public void ApplyAutopilotState(float speedWorld, bool autopilotActive)
        {
            IsAutopilotActive = autopilotActive;
            if (autopilotActive)
            {
                currentSpeed = speedWorld;
                SyncSpeedMphFromWorld();
                IsAfterburning = false;
            }
        }

        public float GetWeaponAccuracyMultiplier()
        {
            if (profile == null)
            {
                return 1f;
            }

            if (IsAfterburning)
            {
                return profile.afterburnerWeaponAccuracy;
            }

            if (inputSource != null && inputSource.Current.throttleHeld)
            {
                return profile.throttleWeaponAccuracy;
            }

            return 1f;
        }

        public void Configure(FlightProfile flightProfile, PlayerAircraftInput input, WorldMapConfig mapConfig = null)
        {
            profile = flightProfile;
            worldMap = mapConfig;
            inputSource = input;
            InitializeFlightState();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ApplyRigidbodySettings();
            InitializeFlightState();
        }

        private void Start()
        {
            TryApplyMissionCarrierLaunch();
        }

        public void TryApplyMissionCarrierLaunch()
        {
            if (!FlightMissionLaunchState.TryConsumeCarrierLaunch())
            {
                return;
            }

            ApplyCarrierTakeoffLaunch(FlightMissionLaunchState.CarrierTakeoffSpeedMph);
        }

        private void InitializeFlightState()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (profile == null || body == null)
            {
                return;
            }

            Refuel();
            currentSpeedMph = profile.startThrottleMph;
            afterburnerSpoolDownActive = false;
            IsAfterburning = false;
            currentSpeed = profile.MphToWorldSpeed(currentSpeedMph, worldMap);
        }

        public void ApplyCarrierTakeoffLaunch(float speedMph)
        {
            if (profile == null)
            {
                return;
            }

            currentSpeedMph = Mathf.Clamp(
                speedMph,
                profile.minThrottleMph,
                profile.maxThrottleMph);
            afterburnerSpoolDownActive = false;
            IsAfterburning = false;
            currentSpeed = profile.MphToWorldSpeed(currentSpeedMph, worldMap);

            if (body != null)
            {
                var forward = Flatten(transform.forward);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector3.forward;
                }

                body.linearVelocity = forward.normalized * currentSpeed;
            }
        }

        private void OnValidate()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ApplyRigidbodySettings();
        }

        private void ApplyRigidbodySettings()
        {
            if (body == null)
            {
                return;
            }

            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
        }

        public void RefuelAfterburner()
        {
            if (profile == null)
            {
                return;
            }

            afterburnerFuelRemaining = profile.afterburnerFuelCapacity;
        }

        public void Refuel()
        {
            RefuelAfterburner();

            if (worldMap != null)
            {
                leftTankGallons = worldMap.fuelGallonsPerTank;
                rightTankGallons = worldMap.fuelGallonsPerTank;
            }
        }

        private void FixedUpdate()
        {
            if (profile == null || inputSource == null)
            {
                return;
            }

            if (GamePauseController.IsPaused)
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            if (IsLandingLocked)
            {
                currentSpeed = 0f;
                currentSpeedMph = 0f;
                body.linearVelocity = Vector3.zero;
                return;
            }

            if (AircraftLandingController.IsLandingActive)
            {
                return;
            }

            var input = inputSource.Current;
            var dtNormal = Time.fixedDeltaTime;

            var autopilot = AutopilotController.Instance;
            if (IsAutopilotActive && autopilot != null && autopilot.IsFlying)
            {
                UpdateFuel(dtNormal);
                return;
            }

            UpdateAfterburner(input, dtNormal);
            ApplyTurn(input, dtNormal);
            ApplyThrottle(input, dtNormal);
            UpdateFuel(dtNormal);

            var forward = Flatten(transform.forward);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            body.linearVelocity = forward.normalized * currentSpeed;
        }

        private void ApplyThrottle(AircraftControlInput input, float dt)
        {
            if (IsOutOfFuel)
            {
                currentSpeedMph = 0f;
                currentSpeed = 0f;
                return;
            }

            if (IsAfterburning)
            {
                var rampRate = profile.throttleChangeMphPerSecond * profile.afterburnerThrottleChangeMultiplier;
                currentSpeedMph = Mathf.MoveTowards(
                    currentSpeedMph,
                    profile.afterburnerMaxThrottleMph,
                    rampRate * dt);
            }
            else if (afterburnerSpoolDownActive)
            {
                var decayRate = profile.throttleChangeMphPerSecond * 0.5f;
                if (input.airbrakeHeld)
                {
                    currentSpeedMph -= profile.throttleChangeMphPerSecond * dt;
                }
                else
                {
                    currentSpeedMph -= decayRate * dt;
                }

                currentSpeedMph = Mathf.Max(currentSpeedMph, profile.minThrottleMph);

                if (currentSpeedMph <= profile.maxThrottleMph + 0.5f)
                {
                    currentSpeedMph = Mathf.Min(currentSpeedMph, profile.maxThrottleMph);
                    afterburnerSpoolDownActive = false;
                }
            }
            else
            {
                var changeRate = profile.throttleChangeMphPerSecond;
                if (input.throttleHeld)
                {
                    currentSpeedMph += changeRate * dt;
                }
                else if (input.airbrakeHeld)
                {
                    currentSpeedMph -= changeRate * dt;
                }

                currentSpeedMph = Mathf.Clamp(
                    currentSpeedMph,
                    profile.minThrottleMph,
                    profile.maxThrottleMph);
            }

            currentSpeed = profile.MphToWorldSpeed(currentSpeedMph, worldMap);
        }

        private void UpdateFuel(float dt)
        {
            if (worldMap == null || worldMap.TicsPerMile <= 0f || currentSpeed <= 0f || profile == null)
            {
                return;
            }

            var milesTraveled = worldMap.TicsToMiles(CurrentSpeedTics * dt);
            var speedRatio = Mathf.InverseLerp(
                profile.minThrottleMph,
                profile.afterburnerMaxThrottleMph,
                currentSpeedMph);
            var burnMultiplier = Mathf.Lerp(1f, profile.throttleFuelMultiplier, speedRatio);
            var totalCapacity = TotalFuelCapacityGallons;
            if (totalCapacity <= 0f || worldMap.maxFuelRangeMiles <= 0f)
            {
                return;
            }

            var gallonsConsumed = milesTraveled / worldMap.maxFuelRangeMiles * totalCapacity * burnMultiplier;
            var perTank = gallonsConsumed * 0.5f;
            leftTankGallons = Mathf.Max(0f, leftTankGallons - perTank);
            rightTankGallons = Mathf.Max(0f, rightTankGallons - perTank);
        }

        private void UpdateAfterburner(AircraftControlInput input, float dt)
        {
            var wantsAfterburner = input.afterburnerHeld && CanUseAfterburner;
            if (wantsAfterburner && !IsAfterburning)
            {
                afterburnerSpoolDownActive = false;
            }

            if (IsAfterburning && !wantsAfterburner)
            {
                afterburnerSpoolDownActive = currentSpeedMph > profile.maxThrottleMph + 0.5f;
            }

            IsAfterburning = wantsAfterburner;

            if (!IsAfterburning)
            {
                return;
            }

            afterburnerFuelRemaining -= profile.afterburnerFuelConsumption * dt;
            if (afterburnerFuelRemaining < 0f)
            {
                afterburnerFuelRemaining = 0f;
                if (IsAfterburning)
                {
                    afterburnerSpoolDownActive = currentSpeedMph > profile.maxThrottleMph + 0.5f;
                }

                IsAfterburning = false;
            }
        }

        private void SyncSpeedMphFromWorld()
        {
            if (worldMap != null && profile != null)
            {
                currentSpeedMph = worldMap.WorldUnitsToMph(currentSpeed, profile.ticSizeWorldUnits);
            }
        }

        private void ApplyTurn(AircraftControlInput input, float dt)
        {
            var yawDelta = input.turn * profile.turnRate * dt;
            transform.Rotate(0f, yawDelta, 0f, Space.World);
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
