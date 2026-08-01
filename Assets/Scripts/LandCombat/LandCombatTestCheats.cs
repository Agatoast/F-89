namespace F89.LandCombat
{
    /// <summary>Temporary combat test settings for validating boss encounters.</summary>
    public static class LandCombatTestCheats
    {
        public const bool UnlimitedGrenades = false;
        public const bool PlayerInvulnerable = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const bool ResetAllBossProgressOnDevEntry = true;
        public const bool ResetDestroyedOutpostsOnDevEntry = true;
        public const bool ShowDevMenuButtons = true;
#else
        public const bool ResetAllBossProgressOnDevEntry = false;
        public const bool ResetDestroyedOutpostsOnDevEntry = false;
        public const bool ShowDevMenuButtons = false;
#endif
    }
}
