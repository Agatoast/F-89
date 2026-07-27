namespace F89.LandCombat
{
    /// <summary>
    /// Ultimate Reich gear ladders (loot / R&amp;D fuel) for Helmet, Vest, and Boots.
    /// Same DR / Move rules as US gear.
    /// </summary>
    public static class LandUrGearCatalog
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
                LandItemRarity rarity)
            {
                TechLevel = techLevel;
                DefinitionId = definitionId;
                DisplayName = displayName;
                Slot = slot;
                DamageResistance = damageResistance;
                Move = move;
                Rarity = rarity;
                Category = LandItemCategory.UltimateReich;
            }
        }

        public static readonly Entry[] Helmets =
        {
            new(1, "M46", "M46", LandEquipmentSlot.Helmet, 5, 0, LandItemRarity.White),
            new(2, "M47", "M47", LandEquipmentSlot.Helmet, 6, 0, LandItemRarity.Green),
            new(3, "M48", "M48", LandEquipmentSlot.Helmet, 7, 0, LandItemRarity.Blue),
            new(4, "M49", "M49", LandEquipmentSlot.Helmet, 8, 0, LandItemRarity.Purple),
            new(5, "M50", "M50", LandEquipmentSlot.Helmet, 9, 0, LandItemRarity.Yellow),
            new(6, "M51", "M51", LandEquipmentSlot.Helmet, 10, 0, LandItemRarity.Orange),
            new(7, "M52", "M52", LandEquipmentSlot.Helmet, 11, 0, LandItemRarity.Red),
            new(8, "M53", "M53", LandEquipmentSlot.Helmet, 12, 0, LandItemRarity.Crimson),
            new(9, "M54", "M54", LandEquipmentSlot.Helmet, 13, 0, LandItemRarity.Black),
            new(10, "M55", "M55", LandEquipmentSlot.Helmet, 14, 0, LandItemRarity.Gold)
        };

        public static readonly Entry[] Vests =
        {
            new(1, "SK4", "SK4", LandEquipmentSlot.Core, 2, 0, LandItemRarity.White),
            new(2, "SK5", "SK5", LandEquipmentSlot.Core, 3, 0, LandItemRarity.Green),
            new(3, "SK6", "SK6", LandEquipmentSlot.Core, 4, 0, LandItemRarity.Blue),
            new(4, "SK7", "SK7", LandEquipmentSlot.Core, 5, 0, LandItemRarity.Purple),
            new(5, "SK8", "SK8", LandEquipmentSlot.Core, 6, 0, LandItemRarity.Yellow),
            new(6, "SK9", "SK9", LandEquipmentSlot.Core, 7, 0, LandItemRarity.Orange),
            new(7, "SK10", "SK10", LandEquipmentSlot.Core, 8, 0, LandItemRarity.Red),
            new(8, "SK11", "SK11", LandEquipmentSlot.Core, 9, 0, LandItemRarity.Crimson),
            new(9, "SK12", "SK12", LandEquipmentSlot.Core, 10, 0, LandItemRarity.Black),
            new(10, "SK13", "SK13", LandEquipmentSlot.Core, 11, 0, LandItemRarity.Gold)
        };

        public static readonly Entry[] Boots =
        {
            new(1, "BWKS", "BWKS", LandEquipmentSlot.Boots, 1, 5, LandItemRarity.White),
            new(2, "BW6", "BW-6", LandEquipmentSlot.Boots, 1, 6, LandItemRarity.Green),
            new(3, "BW7", "BW-7", LandEquipmentSlot.Boots, 1, 7, LandItemRarity.Blue),
            new(4, "BW8", "BW-8", LandEquipmentSlot.Boots, 2, 8, LandItemRarity.Purple),
            new(5, "BW9", "BW-9", LandEquipmentSlot.Boots, 2, 9, LandItemRarity.Yellow),
            new(6, "BW10", "BW-10", LandEquipmentSlot.Boots, 2, 10, LandItemRarity.Orange),
            new(7, "BW11", "BW-11", LandEquipmentSlot.Boots, 3, 11, LandItemRarity.Red),
            new(8, "BW12", "BW-12", LandEquipmentSlot.Boots, 3, 12, LandItemRarity.Crimson),
            new(9, "BW13", "BW-13", LandEquipmentSlot.Boots, 3, 13, LandItemRarity.Black),
            new(10, "BW14", "BW-14", LandEquipmentSlot.Boots, 4, 14, LandItemRarity.Gold)
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
