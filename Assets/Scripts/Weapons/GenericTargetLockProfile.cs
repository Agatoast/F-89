namespace F89.Weapons
{
    /// <summary>
    /// Omni lock profile used for target selection / lock audio when no firing weapon is active.
    /// </summary>
    public sealed class GenericTargetLockProfile : ILockCapableWeapon
    {
        public static readonly GenericTargetLockProfile Instance = new GenericTargetLockProfile();

        private GenericTargetLockProfile()
        {
        }

        public float RangeMiles => 999f;
        public float LockTimeSeconds => OmniLockGroundWeaponProfile.LockTimeSeconds;
        public float MaxBeepInterval => OmniLockGroundWeaponProfile.MaxBeepInterval;
        public float MinBeepInterval => OmniLockGroundWeaponProfile.MinBeepInterval;
        public LockableTargetKind ValidTargetKind => LockableTargetKind.Ground;
        public WeaponAimMode AimMode => WeaponAimMode.OmniLock;
        public float ForwardLockHalfAngleDegrees => 0f;
        public WeaponEngagementType EngagementType => WeaponEngagementType.AirToGroundMissile;
    }
}
