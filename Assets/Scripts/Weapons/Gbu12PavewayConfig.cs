using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Gbu12PavewayConfig", menuName = "F-89/Weapons/GBU-12 Paveway II Config")]
    public class Gbu12PavewayConfig : ScriptableObject, ILockCapableWeapon
    {
        [Header("Air-to-Ground Missile")]
        [Tooltip("Guided ordnance — uses the same omni lock/aim profile as SiAW and Hellfire.")]
        public LockableTargetKind targetKind = OmniLockGroundWeaponProfile.TargetKind;

        [Header("Performance")]
        public float rangeMiles = 8f;
        public float speedMilesPerSecond = 0.28f;
        public float maxFlightTimeSeconds = 90f;

        [Header("Lock-On (Omni 360°)")]
        public float lockTimeSeconds = OmniLockGroundWeaponProfile.LockTimeSeconds;
        public float maxBeepInterval = OmniLockGroundWeaponProfile.MaxBeepInterval;
        public float minBeepInterval = OmniLockGroundWeaponProfile.MinBeepInterval;

        [Header("Terminal Guidance")]
        [Tooltip("Ordinance stays locked on target until this close — no reacquire inside this radius.")]
        public float terminalGuidanceMiles = 1f;

        [Header("Impact")]
        [Range(0f, 1f)] public float lockHitChance = 0.88f;
        [Tooltip("Legacy circular radius (unused for GHP). Blast uses chebyshev tic footprint.")]
        public float blastRadiusTics = 2f;
        [Tooltip("Max random impact scatter at 0% firing accuracy, in tics.")]
        public float maxScatterTicsAtZeroAccuracy = 3f;

        [Header("Ground Hit Points")]
        [Tooltip("GHP to buildings and troops in hit tic + 2 tics around (5×5 including diagonals).")]
        public int buildingOrTroopGroundHitPoints = PlaneWeaponGhp.Gbu12BuildingOrTroopHit;
        [Tooltip("GHP to vehicles in the same tic footprint.")]
        public int vehicleGroundHitPoints = PlaneWeaponGhp.Gbu12VehicleHit;
        public int blastChebyshevTics = PlaneWeaponGhp.Gbu12BlastChebyshevTics;

        [Header("Loadout")]
        public int startingBombCount = 4;

        public string WeaponName => "GBU-12 Paveway II";
        public LockableTargetKind ValidTargetKind => targetKind;
        public WeaponAimMode AimMode => OmniLockGroundWeaponProfile.AimMode;

        float ILockCapableWeapon.RangeMiles => rangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => lockTimeSeconds;
        float ILockCapableWeapon.MaxBeepInterval => maxBeepInterval;
        float ILockCapableWeapon.MinBeepInterval => minBeepInterval;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => targetKind;
        WeaponAimMode ILockCapableWeapon.AimMode => OmniLockGroundWeaponProfile.AimMode;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => 0f;
        WeaponEngagementType ILockCapableWeapon.EngagementType => OmniLockGroundWeaponProfile.EngagementType;
    }
}
