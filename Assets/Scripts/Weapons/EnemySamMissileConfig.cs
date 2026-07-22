using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "EnemySamMissileConfig", menuName = "F-89/Weapons/Enemy SAM Missile Config")]
    public class EnemySamMissileConfig : ScriptableObject, IMissileWeaponConfig
    {
        [Header("Engagement")]
        public float acquisitionRangeMiles = 25f;
        public float launchRangeMiles = 20f;

        [Header("Performance")]
        public float rangeMiles = 20f;
        [Range(0f, 1f)] public float lockHitChance = 0.85f;
        [Range(0f, 1f)] public float playerDestroyChanceOnHit = 0.5f;
        public float speedMilesPerSecond = 0.45f;
        public float missileLifetimeSeconds = 60f;

        [Header("Seeker / Flares")]
        public float flareRetargetRangeMiles = 10f;
        [Range(0f, 1f)] public float flareRetargetChance = 1f;
        [Range(0f, 1f)] public float flareBurnoutReacquireChance = 0.1f;
        [Tooltip("180 degree front arc — half-angle from missile nose.")]
        public float seekerForwardHalfAngleDegrees = 90f;

        [Header("Unlocked Shot")]
        public float unlockedHitConeHalfAngleDegrees = 1.5f;
        public float collisionRadiusTics = 0.5f;

        float ILockCapableWeapon.RangeMiles => rangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => 0f;
        float ILockCapableWeapon.MaxBeepInterval => 0.5f;
        float ILockCapableWeapon.MinBeepInterval => 0.5f;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => LockableTargetKind.Air;
        WeaponAimMode ILockCapableWeapon.AimMode => WeaponAimMode.ForwardLock;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => seekerForwardHalfAngleDegrees;
        WeaponEngagementType ILockCapableWeapon.EngagementType => WeaponEngagementType.AirToAirMissile;
        float IMissileWeaponConfig.LockHitChance => lockHitChance;
        float IMissileWeaponConfig.SpeedMilesPerSecond => speedMilesPerSecond;
        float IMissileWeaponConfig.MissileLifetimeSeconds => missileLifetimeSeconds;
        float IMissileWeaponConfig.UnlockedHitConeHalfAngleDegrees => unlockedHitConeHalfAngleDegrees;
        float IMissileWeaponConfig.CollisionRadiusTics => collisionRadiusTics;
        string IMissileWeaponConfig.WeaponName => "Basic Enemy Missile";
        WeaponAimMode IMissileWeaponConfig.AimMode => WeaponAimMode.ForwardLock;
    }
}
