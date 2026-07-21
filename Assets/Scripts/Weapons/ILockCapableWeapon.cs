namespace F89.Weapons
{
    public interface ILockCapableWeapon
    {
        float RangeMiles { get; }
        float LockTimeSeconds { get; }
        float MaxBeepInterval { get; }
        float MinBeepInterval { get; }
        LockableTargetKind ValidTargetKind { get; }
        WeaponAimMode AimMode { get; }
        float ForwardLockHalfAngleDegrees { get; }
        WeaponEngagementType EngagementType { get; }
    }
}
