namespace F89.Core
{
    /// <summary>Player aircraft sortie hit budget. Each enemy hit removes one hit; at 0 the aircraft crash-lands.</summary>
    public static class PlayerAircraftGhp
    {
        /// <summary>Default sortie hits before crash (minigame bonus can raise budget to <see cref="MaxSortieHitBudget"/>).</summary>
        public const int SortieHitsToCrash = 3;

        /// <summary>Extra hit from bunker defense victory (3 + 1 = 4 total).</summary>
        public const int MaxSortieHitBudget = 4;

        /// <summary>Every enemy weapon hit on the player aircraft deals exactly this much.</summary>
        public const int DamagePerEnemyHit = 1;
    }
}
