using F89.Controls;
using F89.Core;
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
        private float afterburnerFuelRemaining;
        private float fuelRangeRemainingMiles;

        public float CurrentSpeed => currentSpeed;
        public float CurrentSpeedTics => profile != null && profile.ticSizeWorldUnits > 0f
            ? currentSpeed / profile.ticSizeWorldUnits
            : 0f;
        public float CurrentSpeedMph => worldMap != null
            ? worldMap.TicsPerSecondToMph(CurrentSpeedTics)
            : CurrentSpeedTics / 20f * 3600f;
        public bool IsAfterburning { get; private set; }
        public bool CanUseAfterburner => afterburnerFuelRemaining > 0f;
        public float AfterburnerFuelRemaining => afterburnerFuelRemaining;
        public float AfterburnerFuelNormalized =>
            profile == null || profile.afterburnerFuelCapacity <= 0f
                ? 0f
                : afterburnerFuelRemaining / profile.afterburnerFuelCapacity;
        public FlightProfile Profile => profile;
        public WorldMapConfig WorldMap => worldMap;
        public float FuelRangeRemainingMiles => fuelRangeRemainingMiles;
        public float FuelRangeNormalized =>
            worldMap == null || worldMap.maxFuelRangeMiles <= 0f
                ? 0f
                : fuelRangeRemainingMiles / worldMap.maxFuelRangeMiles;
        public bool IsOutOfFuel => worldMap != null && fuelRangeRemainingMiles <= 0f;
        public bool IsAutopilotActive { get; private set; }

        public void ApplyAutopilotState(float speedWorld, bool autopilotActive)
        {
            IsAutopilotActive = autopilotActive;
            if (autopilotActive)
            {
                currentSpeed = speedWorld;
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
            currentSpeed = profile.DefaultSpeedWorld;
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
                fuelRangeRemainingMiles = worldMap.maxFuelRangeMiles;
            }
        }

        private void FixedUpdate()
        {
            if (profile == null || inputSource == null)
            {
                return;
            }

            var autopilot = AutopilotController.Instance;
            if (IsAutopilotActive && autopilot != null && autopilot.IsFlying)
            {
                var dt = Time.fixedDeltaTime;
                var cruiseInput = new AircraftControlInput { throttleHeld = true };
                UpdateFuel(cruiseInput, dt);
                return;
            }

            var input = inputSource.Current;
            var dtNormal = Time.fixedDeltaTime;

            UpdateAfterburner(input, dtNormal);
            ApplyTurn(input, dtNormal);
            UpdateFuel(input, dtNormal);

            var targetSpeed = IsOutOfFuel ? 0f : ResolveTargetSpeed(input);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, profile.SpeedChangeRateWorld * dtNormal);

            var forward = Flatten(transform.forward);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            body.linearVelocity = forward.normalized * currentSpeed;
        }

        private void UpdateFuel(AircraftControlInput input, float dt)
        {
            if (worldMap == null || worldMap.TicsPerMile <= 0f || currentSpeed <= 0f)
            {
                return;
            }

            var milesTraveled = worldMap.TicsToMiles(CurrentSpeedTics * dt);
            var burnMultiplier = input.throttleHeld ? profile.throttleFuelMultiplier : 1f;
            fuelRangeRemainingMiles = Mathf.Max(0f, fuelRangeRemainingMiles - milesTraveled * burnMultiplier);
        }

        private void UpdateAfterburner(AircraftControlInput input, float dt)
        {
            var wantsAfterburner = input.afterburnerHeld && CanUseAfterburner;
            IsAfterburning = wantsAfterburner;

            if (!IsAfterburning)
            {
                return;
            }

            afterburnerFuelRemaining -= profile.afterburnerFuelConsumption * dt;
            if (afterburnerFuelRemaining < 0f)
            {
                afterburnerFuelRemaining = 0f;
                IsAfterburning = false;
            }
        }

        private float ResolveTargetSpeed(AircraftControlInput input)
        {
            if (input.afterburnerHeld && CanUseAfterburner)
            {
                return profile.AfterburnerSpeedWorld;
            }

            if (input.throttleHeld)
            {
                return profile.ThrottleSpeedWorld;
            }

            if (input.airbrakeHeld)
            {
                return profile.AirbrakeSpeedWorld;
            }

            return profile.DefaultSpeedWorld;
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
