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
        public float blastRadiusTics = 1.5f;
        [Tooltip("Max random impact scatter at 0% firing accuracy, in tics.")]
        public float maxScatterTicsAtZeroAccuracy = 3f;

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
