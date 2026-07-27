namespace F89.LandCombat
{
    /// <summary>
    /// Ultimate Reich weapon ladder (loot / R&amp;D fuel). Same Damage + Range rules as US weapons.
    /// </summary>
    public static class LandUrWeaponCatalog
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
                LandItemRarity rarity)
            {
                TechLevel = techLevel;
                DefinitionId = definitionId;
                DisplayName = displayName;
                Damage = damage;
                Range = range;
                Rarity = rarity;
                Category = LandItemCategory.UltimateReich;
            }
        }

        public static readonly Entry[] Weapons =
        {
            new(1, "StG24", "StG-24", 13f, 6f, LandItemRarity.White),
            new(2, "StG24A", "StG-24A", 15f, 6f, LandItemRarity.Green),
            new(3, "StG24E", "StG-24E", 17f, 7f, LandItemRarity.Blue),
            new(4, "StG24R", "StG-24R", 20f, 7f, LandItemRarity.Purple),
            new(5, "VK24", "VK-24", 22f, 7f, LandItemRarity.Yellow),
            new(6, "ES1", "ES-1", 24f, 8f, LandItemRarity.Orange),
            new(7, "RG9", "RG-9", 27f, 8f, LandItemRarity.Red),
            new(8, "AVG", "AVG", 29f, 9f, LandItemRarity.Crimson),
            new(9, "AK9", "AK-9", 31f, 9f, LandItemRarity.Black),
            new(10, "Donar7", "Donar-7", 34f, 10f, LandItemRarity.Gold)
        };

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
