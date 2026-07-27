namespace F89.LandCombat
{
    /// <summary>Corpse linger and loot-check rules (MTAU-style open range).</summary>
    public static class LandEnemyLootRules
    {
        /// <summary>Empty corpses (no loot bag) linger this long, then despawn.</summary>
        public const float EmptyCorpseLifetimeSeconds = 300f;

        /// <summary>After the player empties a loot bag, corpse despawns after this delay.</summary>
        public const float CheckedEmptyDespawnSeconds = 20f;

        /// <summary>Max player↔corpse distance to take loot items, in ground-combat units.</summary>
        public const float LootTakeRangeUnits = 4f;
        /// <summary>World click/hover radius around a corpse body.</summary>
        public const float CorpseHoverRadiusUnits = 1.1f;
        public const float LootBagChance = 1f;
        public const int LootMinItems = 1;
        public const int LootMaxItems = 4;
        public const int LootBagSlotCount = 4;
        public const int BossLootMinExtraItems = 1;
        public const int BossLootMaxExtraItems = 3;
    }
}
