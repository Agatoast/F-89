namespace F89.LandCombat
{
    /// <summary>
    /// US Basic Loadout / R&amp;D weapon ladder.
    /// Level 1 (M-4) is Basic Loadout; levels 2–10 (X-4…X-12) are earned through R&amp;D.
    /// </summary>
    public static class LandUsWeaponCatalog
    {
        public readonly struct Entry
        {
            public readonly int TechLevel;
            public readonly string DefinitionId;
            public readonly string DisplayName;
            public readonly float Damage;
            public readonly float Range;
            public readonly LandItemRarity Rarity;
            public readonly LandItemCategory Category;

            public Entry(
                int techLevel,
                string definitionId,
                string displayName,
                float damage,
                float range,
                LandItemRarity rarity,
                LandItemCategory category)
            {
                TechLevel = techLevel;
                DefinitionId = definitionId;
                DisplayName = displayName;
                Damage = damage;
                Range = range;
                Rarity = rarity;
                Category = category;
            }
        }

        public static readonly Entry[] Weapons =
        {
            new(1, "M4", "M-4", 40f, 6f, LandItemRarity.White, LandItemCategory.Military),
            new(2, "X4", "X-4", 47f, 6f, LandItemRarity.Green, LandItemCategory.Experimental),
            new(3, "X5", "X-5", 50f, 7f, LandItemRarity.Blue, LandItemCategory.Experimental),
            new(4, "X6", "X-6", 52f, 7f, LandItemRarity.Purple, LandItemCategory.Experimental),
            new(5, "X7", "X-7", 54f, 7f, LandItemRarity.Yellow, LandItemCategory.Experimental),
            new(6, "X8", "X-8", 57f, 8f, LandItemRarity.Orange, LandItemCategory.Experimental),
            new(7, "X9", "X-9", 59f, 8f, LandItemRarity.Red, LandItemCategory.Experimental),
            new(8, "X10", "X-10", 61f, 9f, LandItemRarity.Crimson, LandItemCategory.Experimental),
            new(9, "X11", "X-11", 64f, 9f, LandItemRarity.Black, LandItemCategory.Experimental),
            new(10, "X12", "X-12", 67f, 10f, LandItemRarity.Gold, LandItemCategory.Experimental)
        };

        public const string BasicLoadoutDefinitionId = "M4";

        public static bool TryGetByTechLevel(int techLevel, out Entry entry)
        {
            for (var i = 0; i < Weapons.Length; i++)
            {
                if (Weapons[i].TechLevel == techLevel)
                {
                    entry = Weapons[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public static bool TryGetByDefinitionId(string definitionId, out Entry entry)
        {
            for (var i = 0; i < Weapons.Length; i++)
            {
                if (Weapons[i].DefinitionId == definitionId)
                {
                    entry = Weapons[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
