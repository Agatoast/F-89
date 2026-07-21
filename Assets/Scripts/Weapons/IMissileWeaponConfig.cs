namespace F89.Weapons
{
    public interface IMissileWeaponConfig : ILockCapableWeapon
    {
        float LockHitChance { get; }
        float SpeedMilesPerSecond { get; }
        float MissileLifetimeSeconds { get; }
        float UnlockedHitConeHalfAngleDegrees { get; }
        float CollisionRadiusTics { get; }
        string WeaponName { get; }
        WeaponAimMode AimMode { get; }
    }
}
