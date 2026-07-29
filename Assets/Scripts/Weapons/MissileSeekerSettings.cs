namespace F89.Weapons
{
    public struct MissileSeekerSettings
    {
        public bool RespondsToFlares;
        public LockableTarget PrimaryTarget;
        public float FlareRetargetRangeMiles;
        public float FlareRetargetChance;
        public float FlareBurnoutReacquireChance;
        public float SeekerForwardHalfAngleDegrees;

        public static MissileSeekerSettings PlayerWeapon => default;

        public static MissileSeekerSettings FromEnemySam(EnemySamMissileConfig config, LockableTarget primaryTarget)
        {
            return new MissileSeekerSettings
            {
                RespondsToFlares = true,
                PrimaryTarget = primaryTarget,
                FlareRetargetRangeMiles = config.flareRetargetRangeMiles,
                FlareRetargetChance = config.flareRetargetChance,
                FlareBurnoutReacquireChance = config.flareBurnoutReacquireChance,
                SeekerForwardHalfAngleDegrees = config.seekerForwardHalfAngleDegrees
            };
        }

        public static MissileSeekerSettings FromGroundUnitAir(
            GroundUnitAirMissileConfig config,
            LockableTarget primaryTarget)
        {
            return new MissileSeekerSettings
            {
                RespondsToFlares = true,
                PrimaryTarget = primaryTarget,
                FlareRetargetRangeMiles = config != null ? config.flareRetargetRangeMiles : 10f,
                FlareRetargetChance = config != null ? config.flareRetargetChance : 1f,
                FlareBurnoutReacquireChance = config != null ? config.flareBurnoutReacquireChance : 0.1f,
                SeekerForwardHalfAngleDegrees = config != null ? config.seekerForwardHalfAngleDegrees : 90f
            };
        }
    }
}
