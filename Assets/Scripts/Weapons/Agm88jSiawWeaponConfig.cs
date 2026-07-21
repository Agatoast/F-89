using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Agm88jSiawWeaponConfig", menuName = "F-89/Weapons/AGM-88J SiAW Config")]
    public class Agm88jSiawWeaponConfig : ScriptableObject, IMissileWeaponConfig
    {
        [Header("Air-to-Ground Missile")]
        public float rangeMiles = 30f;
        [Range(0f, 1f)] public float lockHitChance = 0.75f;
        public float speedMilesPerSecond = 0.27f;
        public float missileLifetimeSeconds = 40f;

        [Header("Lock-On (Omni 360°)")]
        public float lockTimeSeconds = OmniLockGroundWeaponProfile.LockTimeSeconds;
        public float maxBeepInterval = OmniLockGroundWeaponProfile.MaxBeepInterval;
        public float minBeepInterval = OmniLockGroundWeaponProfile.MinBeepInterval;

        [Header("Unlocked Shot")]
        public float unlockedHitConeHalfAngleDegrees = 1.5f;
        public float collisionRadiusTics = 0.75f;

        [Header("Loadout")]
        public int startingMissileCount = 2;

        float ILockCapableWeapon.RangeMiles => rangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => lockTimeSeconds;
        float ILockCapableWeapon.MaxBeepInterval => maxBeepInterval;
        float ILockCapableWeapon.MinBeepInterval => minBeepInterval;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => OmniLockGroundWeaponProfile.TargetKind;
        WeaponAimMode ILockCapableWeapon.AimMode => OmniLockGroundWeaponProfile.AimMode;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => 0f;
        WeaponEngagementType ILockCapableWeapon.EngagementType => OmniLockGroundWeaponProfile.EngagementType;
        float IMissileWeaponConfig.LockHitChance => lockHitChance;
        float IMissileWeaponConfig.SpeedMilesPerSecond => speedMilesPerSecond;
        float IMissileWeaponConfig.MissileLifetimeSeconds => missileLifetimeSeconds;
        float IMissileWeaponConfig.UnlockedHitConeHalfAngleDegrees => unlockedHitConeHalfAngleDegrees;
        float IMissileWeaponConfig.CollisionRadiusTics => collisionRadiusTics;
        string IMissileWeaponConfig.WeaponName => "AGM-88J SiAW";
        WeaponAimMode IMissileWeaponConfig.AimMode => OmniLockGroundWeaponProfile.AimMode;
    }
}
