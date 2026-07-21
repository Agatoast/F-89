using UnityEngine;

namespace F89.Flight
{
    [CreateAssetMenu(fileName = "FlightProfile", menuName = "F-89/Flight Profile")]
    public class FlightProfile : ScriptableObject
    {
        [Header("Speed (tics per second)")]
        [Tooltip("No throttle or brake input — fixed-wing cruise.")]
        public float defaultSpeed = 5f;

        [Tooltip("W held — military power.")]
        public float throttleSpeed = 6f;

        [Tooltip("S held — airbrake deployed.")]
        public float airbrakeSpeed = 4f;

        [Tooltip("Shift held — afterburner, while fuel remains.")]
        public float afterburnerSpeed = 8f;

        [Header("World Scale")]
        [Tooltip("World units equal to one aircraft sprite length (1 tic).")]
        public float ticSizeWorldUnits = 1f;

        [Header("Speed Response")]
        [Tooltip("How quickly speed ramps between tiers, in tics/s².")]
        public float speedChangeRate = 8f;

        [Header("Afterburner Fuel")]
        [Tooltip("Separate afterburner tank in seconds. Does not affect main fuel range.")]
        public float afterburnerFuelCapacity = 50f;

        [Tooltip("Fuel consumed per second while Shift is held.")]
        public float afterburnerFuelConsumption = 1f;

        [Header("Main Fuel Range")]
        [Tooltip("Main fuel burn multiplier while W throttle is held. 1.33 = 33% faster.")]
        public float throttleFuelMultiplier = 1.33f;

        [Header("Weapon Accuracy While Firing")]
        [Tooltip("Hit chance multiplier while W throttle is held. 0.8 = 20% accuracy loss.")]
        public float throttleWeaponAccuracy = 0.8f;

        [Tooltip("Hit chance multiplier while afterburner is active. 0.5 = 50% accuracy loss.")]
        public float afterburnerWeaponAccuracy = 0.5f;

        [Header("Turning")]
        [Tooltip("Yaw rate in degrees/sec. 45 = 90° turn in 2 seconds.")]
        public float turnRate = 45f;

        [Header("Visual Banking")]
        [Tooltip("Width reduction at full A/D input. 0.25 keeps 3/4 of the plane visible.")]
        public float turnForeshortenAtFullInput = 0.25f;

        [Tooltip("Small sideways shift while turning, in tics.")]
        public float turnLateralShift = 0.03f;

        public float bankSmoothTime = 0.12f;
        public float pitchDuringAfterburner = 10f;

        public float DefaultSpeedWorld => defaultSpeed * ticSizeWorldUnits;
        public float ThrottleSpeedWorld => throttleSpeed * ticSizeWorldUnits;
        public float AirbrakeSpeedWorld => airbrakeSpeed * ticSizeWorldUnits;
        public float AfterburnerSpeedWorld => afterburnerSpeed * ticSizeWorldUnits;
        public float SpeedChangeRateWorld => speedChangeRate * ticSizeWorldUnits;
    }
}
