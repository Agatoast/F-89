using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Data for a ground vehicle, troop, or air unit. Populate from the master vehicle list.
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleUnitDefinition", menuName = "F-89/Enemies/Vehicle Unit Definition")]
    public class VehicleUnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Abbreviation shown on the unit sprite.")]
        public string abbreviation;

        [Tooltip("UR = hostile, US = friendly.")]
        public VehicleUnitDesignation designation = VehicleUnitDesignation.UR;

        [Tooltip("Relative level compared to other vehicles.")]
        public int vehicleLevel = 1;

        [Tooltip("Helicopter or other aerial unit — LockableTargetKind.Air, air damage rules.")]
        public bool isFlier;

        [Tooltip("May share horizontal space with ground units/troops and fly above them (e.g. AH-64, TDP).")]
        public bool fliesOverGroundUnits;

        [Tooltip("Infantry troop — 1 GHP, destroyed without explosion, same movement/combat as vehicles.")]
        public bool isTroop;

        [Header("Movement")]
        [Tooltip("Travel speed in MPH (real time) relative to map miles.")]
        public float speedMph = 30f;

        [Header("Survivability")]
        [Tooltip("Ground Hit Points before the unit is destroyed.")]
        public int ghp = 1;

        [Header("Ground Combat")]
        [Tooltip("Max targeting range against ground units, in tics.")]
        public float groundRangeTics;

        [Range(0f, 1f)]
        [Tooltip("Each shot at a selected ground target rolls this — ground vehicles never auto-hit.")]
        public float chanceToHitGround;

        [Range(0f, 1f)]
        [Tooltip("Straight % chance to kill a vehicle or troop when a shot hits.")]
        public float chanceToKill;

        [Tooltip("Ground GHP applied on a successful ground hit. 0 = use chanceToKill destroy roll.")]
        public int groundHitDamage;

        [Tooltip("Minimum seconds between shots (each shot randomizes inside min–max).")]
        public float fireRateMinSeconds = 2f;

        [Tooltip("Maximum seconds between shots.")]
        public float fireRateMaxSeconds = 4f;

        [Header("Air Combat")]
        [Tooltip("Max targeting range against air units, in miles.")]
        public float airRangeMiles;

        [Range(0f, 1f)]
        [Tooltip("Chance to hit a selected air target (includes the player aircraft).")]
        public float chanceToHitAir;

        [Tooltip("Air GHP applied to planes and other aerial targets on hit.")]
        public int airDamage;

        [Range(0f, 1f)]
        [Tooltip("For UR units: chance this unit prioritizes the player aircraft over other targets.")]
        public float planeVsOtherTargetChance;

        [Tooltip("Mission-score points by unit level when destroyed (enemy +, friendly -).")]
        public int pointValue;

        public bool IsHostile => designation == VehicleUnitDesignation.UR;

        public TargetAffiliation ToTargetAffiliation()
        {
            return IsHostile ? TargetAffiliation.Hostile : TargetAffiliation.Friendly;
        }

        public LockableTargetKind ToLockableTargetKind()
        {
            return isFlier ? LockableTargetKind.Air : LockableTargetKind.Ground;
        }

        public TargetUnitClass ToTargetUnitClass()
        {
            if (isTroop)
            {
                return TargetUnitClass.Infantry;
            }

            return isFlier ? TargetUnitClass.Flier : TargetUnitClass.GroundVehicle;
        }
    }
}
