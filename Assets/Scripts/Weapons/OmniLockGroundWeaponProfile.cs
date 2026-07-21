namespace F89.Weapons
{
    /// <summary>Shared lock/aim profile for SiAW, Hellfire, and GBU-12.</summary>
    public static class OmniLockGroundWeaponProfile
    {
        public const float LockTimeSeconds = 3f;
        public const float MaxBeepInterval = 0.6f;
        public const float MinBeepInterval = 0.14f;
        public const WeaponAimMode AimMode = WeaponAimMode.OmniLock;
        public const LockableTargetKind TargetKind = LockableTargetKind.Ground;
        public const WeaponEngagementType EngagementType = WeaponEngagementType.AirToGroundMissile;
    }
}
