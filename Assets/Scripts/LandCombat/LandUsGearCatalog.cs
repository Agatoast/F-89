namespace F89.LandCombat
{
    /// <summary>
    /// US Basic Loadout / R&amp;D gear ladders for Helmet, Vest, and Boots.
    /// Level 1 is Basic Loadout; levels 2–10 are earned through R&amp;D.
    /// </summary>
    public static class LandUsGearCatalog
    {
        public readonly struct Entry
        {
            public readonly int TechLevel;
            public readonly string DefinitionId;
            public readonly string DisplayName;
            public readonly LandEquipmentSlot Slot;
            public readonly int DamageResistance;
            public readonly int Move;
            public readonly LandItemRarity Rarity;
            public readonly LandItemCategory Category;

            public Entry(
                int techLevel,
                string definitionId,
                string displayName,
                LandEquipmentSlot slot,
                int damageResistance,
                int move,
                LandItemRarity rarity,
                LandItemCategory category)
            {
                TechLevel = techLevel;
                DefinitionId = definitionId;
                DisplayName = displayName;
                Slot = slot;
                DamageResistance = damageResistance;
                Move = move;
                Rarity = rarity;
                Category = category;
            }
        }

        public const string BasicHelmetId = "IHPS";
        public const string BasicVestId = "MSV";
        public const string BasicBootsId = "ECWB";

        public static readonly Entry[] Helmets =
        {
            new(1, "IHPS", "IHPS", LandEquipmentSlot.Helmet, 4, 0, LandItemRarity.White, LandItemCategory.Military),
            new(2, "X1H", "X-1H", LandEquipmentSlot.Helmet, 7, 0, LandItemRarity.Green, LandItemCategory.Experimental),
            new(3, "X2H", "X-2H", LandEquipmentSlot.Helmet, 8, 0, LandItemRarity.Blue, LandItemCategory.Experimental),
            new(4, "X3H", "X-3H", LandEquipmentSlot.Helmet, 9, 0, LandItemRarity.Purple, LandItemCategory.Experimental),
            new(5, "X4H", "X-4H", LandEquipmentSlot.Helmet, 10, 0, LandItemRarity.Yellow, LandItemCategory.Experimental),
            new(6, "X5H", "X-5H", LandEquipmentSlot.Helmet, 11, 0, LandItemRarity.Orange, LandItemCategory.Experimental),
            new(7, "X6H", "X-6H", LandEquipmentSlot.Helmet, 12, 0, LandItemRarity.Red, LandItemCategory.Experimental),
            new(8, "X7H", "X-7H", LandEquipmentSlot.Helmet, 13, 0, LandItemRarity.Crimson, LandItemCategory.Experimental),
            new(9, "X8H", "X-8H", LandEquipmentSlot.Helmet, 14, 0, LandItemRarity.Black, LandItemCategory.Experimental),
            new(10, "X9H", "X-9H", LandEquipmentSlot.Helmet, 15, 0, LandItemRarity.Gold, LandItemCategory.Experimental)
        };

        public static readonly Entry[] Vests =
        {
            new(1, "MSV", "MSV", LandEquipmentSlot.Core, 1, 0, LandItemRarity.White, LandItemCategory.Military),
            new(2, "X1V", "X-1V", LandEquipmentSlot.Core, 4, 0, LandItemRarity.Green, LandItemCategory.Experimental),
            new(3, "X2V", "X-2V", LandEquipmentSlot.Core, 5, 0, LandItemRarity.Blue, LandItemCategory.Experimental),
            new(4, "X3V", "X-3V", LandEquipmentSlot.Core, 6, 0, LandItemRarity.Purple, LandItemCategory.Experimental),
            new(5, "X4V", "X-4V", LandEquipmentSlot.Core, 7, 0, LandItemRarity.Yellow, LandItemCategory.Experimental),
            new(6, "X5V", "X-5V", LandEquipmentSlot.Core, 8, 0, LandItemRarity.Orange, LandItemCategory.Experimental),
            new(7, "X6V", "X-6V", LandEquipmentSlot.Core, 9, 0, LandItemRarity.Red, LandItemCategory.Experimental),
            new(8, "X7V", "X-7V", LandEquipmentSlot.Core, 10, 0, LandItemRarity.Crimson, LandItemCategory.Experimental),
            new(9, "X8V", "X-8V", LandEquipmentSlot.Core, 11, 0, LandItemRarity.Black, LandItemCategory.Experimental),
            new(10, "X9V", "X-9V", LandEquipmentSlot.Core, 12, 0, LandItemRarity.Gold, LandItemCategory.Experimental)
        };

        public static readonly Entry[] Boots =
        {
            new(1, "ECWB", "ECWB", LandEquipmentSlot.Boots, 0, 4, LandItemRarity.White, LandItemCategory.Military),
            new(2, "X1B", "X-1B", LandEquipmentSlot.Boots, 1, 7, LandItemRarity.Green, LandItemCategory.Experimental),
            new(3, "X2B", "X-2B", LandEquipmentSlot.Boots, 2, 8, LandItemRarity.Blue, LandItemCategory.Experimental),
            new(4, "X3B", "X-3B", LandEquipmentSlot.Boots, 2, 9, LandItemRarity.Purple, LandItemCategory.Experimental),
            new(5, "X4B", "X-4B", LandEquipmentSlot.Boots, 2, 10, LandItemRarity.Yellow, LandItemCategory.Experimental),
            new(6, "X5B", "X-5B", LandEquipmentSlot.Boots, 3, 11, LandItemRarity.Orange, LandItemCategory.Experimental),
            new(7, "X6B", "X-6B", LandEquipmentSlot.Boots, 3, 12, LandItemRarity.Red, LandItemCategory.Experimental),
            new(8, "X7B", "X-7B", LandEquipmentSlot.Boots, 3, 13, LandItemRarity.Crimson, LandItemCategory.Experimental),
            new(9, "X8B", "X-8B", LandEquipmentSlot.Boots, 4, 14, LandItemRarity.Black, LandItemCategory.Experimental),
            new(10, "X9B", "X-9B", LandEquipmentSlot.Boots, 5, 15, LandItemRarity.Gold, LandItemCategory.Experimental)
        };

        public static bool TryGetByDefinitionId(string definitionId, out Entry entry)
        {
            if (TryFind(Helmets, definitionId, out entry)
                || TryFind(Vests, definitionId, out entry)
                || TryFind(Boots, definitionId, out entry))
            {
                return true;
            }

            entry = default;
            return false;
        }

        public static bool TryGetByTechLevel(LandEquipmentSlot slot, int techLevel, out Entry entry)
        {
            var table = slot switch
            {
                LandEquipmentSlot.Helmet => Helmets,
                LandEquipmentSlot.Core => Vests,
                LandEquipmentSlot.Boots => Boots,
                _ => null
            };

            if (table == null)
            {
                entry = default;
                return false;
            }

            for (var i = 0; i < table.Length; i++)
            {
                if (table[i].TechLevel == techLevel)
                {
                    entry = table[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private static bool TryFind(Entry[] table, string definitionId, out Entry entry)
        {
            for (var i = 0; i < table.Length; i++)
            {
                if (table[i].DefinitionId == definitionId)
                {
                    entry = table[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
