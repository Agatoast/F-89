using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "Aim9zWeaponConfig", menuName = "F-89/Weapons/AIM-9z Config")]
    public class Aim9zWeaponConfig : ScriptableObject, IMissileWeaponConfig
    {
        [Header("Performance")]
        public float rangeMiles = 20f;
        [Range(0f, 1f)] public float lockHitChance = 0.62f;
        public float speedMilesPerSecond = 0.52f;
        public float missileLifetimeSeconds = 30f;

        [Header("Lock-On")]
        public float lockTimeSeconds = 2.5f;
        public float maxBeepInterval = 0.55f;
        public float minBeepInterval = 0.12f;
        [Tooltip("180° front arc — half-angle from aircraft nose (90° each side).")]
        public float forwardLockHalfAngleDegrees = 90f;

        [Header("Unlocked Shot")]
        public float unlockedHitConeHalfAngleDegrees = 1.5f;
        public float collisionRadiusTics = 0.5f;

        [Header("Loadout")]
        public int startingMissileCount = 4;

        float ILockCapableWeapon.RangeMiles => rangeMiles;
        float ILockCapableWeapon.LockTimeSeconds => lockTimeSeconds;
        float ILockCapableWeapon.MaxBeepInterval => maxBeepInterval;
        float ILockCapableWeapon.MinBeepInterval => minBeepInterval;
        LockableTargetKind ILockCapableWeapon.ValidTargetKind => LockableTargetKind.Air;
        WeaponAimMode ILockCapableWeapon.AimMode => WeaponAimMode.ForwardLock;
        float ILockCapableWeapon.ForwardLockHalfAngleDegrees => forwardLockHalfAngleDegrees;
        WeaponEngagementType ILockCapableWeapon.EngagementType => WeaponEngagementType.AirToAirMissile;
        float IMissileWeaponConfig.LockHitChance => lockHitChance;
        float IMissileWeaponConfig.SpeedMilesPerSecond => speedMilesPerSecond;
        float IMissileWeaponConfig.MissileLifetimeSeconds => missileLifetimeSeconds;
        float IMissileWeaponConfig.UnlockedHitConeHalfAngleDegrees => unlockedHitConeHalfAngleDegrees;
        float IMissileWeaponConfig.CollisionRadiusTics => collisionRadiusTics;
        string IMissileWeaponConfig.WeaponName => "AIM-9z";
        WeaponAimMode IMissileWeaponConfig.AimMode => WeaponAimMode.ForwardLock;
    }
}
