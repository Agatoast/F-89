namespace F89.LandCombat
{
    /// <summary>Temporary combat test settings for validating boss encounters.</summary>
    public static class LandCombatTestCheats
    {
        public const bool UnlimitedGrenades = false;
        public const bool PlayerInvulnerable = false;
        // FINAL BUILD: set false so boss and guard progress remains permanent.
        public const bool ResetAllBossProgressOnDevEntry = true;
        public const bool ResetDestroyedOutpostsOnDevEntry = true;
    }
}
