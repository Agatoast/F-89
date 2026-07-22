using F89.Core;
using UnityEngine;

namespace F89.Flight
{
    [CreateAssetMenu(fileName = "FlightProfile", menuName = "F-89/Flight Profile")]
    public class FlightProfile : ScriptableObject
    {
        [Header("Throttle (MPH)")]
        [Tooltip("Minimum speed when S is held to idle.")]
        public float minThrottleMph = 150f;

        [Tooltip("Maximum speed when W is held to full military power.")]
        public float maxThrottleMph = 1200f;

        [Tooltip("Maximum speed while Shift afterburner is active.")]
        public float afterburnerMaxThrottleMph = 1440f;

        [Tooltip("Speed on spawn and when refueling.")]
        public float startThrottleMph = 900f;

        [Tooltip("Autopilot cruise speed.")]
        public float autopilotCruiseMph = 900f;

        [Tooltip("Speed change while W or S is held, in mph per second.")]
        public float throttleChangeMphPerSecond = 150f;

        [Header("World Scale")]
        [Tooltip("World units equal to one aircraft sprite length (1 tic).")]
        public float ticSizeWorldUnits = 1f;

        [Header("Afterburner")]
        [Tooltip("Throttle change multiplier while Shift afterburner is active.")]
        public float afterburnerThrottleChangeMultiplier = 2f;

        [Tooltip("Separate afterburner tank in seconds. Does not affect main fuel range.")]
        public float afterburnerFuelCapacity = 50f;

        [Tooltip("Fuel consumed per second while Shift is held.")]
        public float afterburnerFuelConsumption = 1f;

        [Header("Main Fuel Range")]
        [Tooltip("Main fuel burn multiplier at max throttle. Scales linearly with speed.")]
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
        [Tooltip("Roll strength at full turn. Multiplied by 120 to get bank angle in degrees (0.25 = 30°).")]
        public float turnForeshortenAtFullInput = 0.25f;

        public float bankSmoothTime = 0.12f;
        public float pitchDuringAfterburner = 10f;

        public float AutopilotCruiseMph =>
            autopilotCruiseMph > 0f ? autopilotCruiseMph : startThrottleMph;

        public float MphToWorldSpeed(float mph, WorldMapConfig worldMap)
        {
            return WorldMapConfig.MilesPerSecondToWorldUnits(mph / 3600f, worldMap, ticSizeWorldUnits);
        }

        public float MinSpeedWorld(WorldMapConfig worldMap) => MphToWorldSpeed(minThrottleMph, worldMap);

        public float MaxSpeedWorld(WorldMapConfig worldMap) => MphToWorldSpeed(maxThrottleMph, worldMap);

        public float AfterburnerMaxSpeedWorld(WorldMapConfig worldMap) =>
            MphToWorldSpeed(afterburnerMaxThrottleMph, worldMap);

        public float GetEffectiveMaxThrottleMph(bool afterburning) =>
            afterburning ? afterburnerMaxThrottleMph : maxThrottleMph;

        public float AutopilotCruiseSpeedWorld(WorldMapConfig worldMap) =>
            MphToWorldSpeed(AutopilotCruiseMph, worldMap);
    }
}
