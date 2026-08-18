using System.Collections.Generic;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Rolls random Ultimate Reich loot for enemy corpses.
    /// Per equipment item: pick tech level uniformly from 1..enemyLevel, then pick item type
    /// (weapon / helmet / vest / boots) with equal chance.
    /// </summary>
    public static class LandEnemyLootGenerator
    {
        private enum LootItemType
        {
            Weapon = 0,
            Helmet = 1,
            Vest = 2,
            Boots = 3,
            Bandage = 4,
            Grenade = 5
        }

        private const int EquipmentTypeCount = 4;

        public static void FillRandomLoot(int enemyLevel, List<LandGearInstance> into)
        {
            into?.Clear();
            if (into == null)
            {
                return;
            }

            FillInfantryDeathLoot(enemyLevel, into);
        }

        /// <summary>Guaranteed infantry corpse loot — each equipment tech level is 1..infantryLevel.</summary>
        public static void FillInfantryDeathLoot(int infantryLevel, List<LandGearInstance> into)
        {
            into?.Clear();
            if (into == null)
            {
                return;
            }

            infantryLevel = LandUrEnemyStats.ClampLevel(infantryLevel);
            var count = Random.Range(LandEnemyLootRules.LootMinItems, LandEnemyLootRules.LootMaxItems + 1);
            for (var i = 0; i < count; i++)
            {
                if (TryRollEquipment(infantryLevel, out var item))
                {
                    into.Add(item);
                }
            }

            if (into.Count == 0 && TryRollEquipment(infantryLevel, out var fallback))
            {
                into.Add(fallback);
            }
        }

        /// <summary>Guaranteed exact-level equipment plus 1–3 normal enemy-loot rolls.</summary>
        public static void FillBossLoot(int enemyLevel, List<LandGearInstance> into)
        {
            into?.Clear();
            if (into == null)
            {
                return;
            }

            enemyLevel = LandUrEnemyStats.ClampLevel(enemyLevel);
            if (TryRollEquipmentAtExactLevel(enemyLevel, out var guaranteedItem))
            {
                into.Add(guaranteedItem);
            }

            var extraCount = Random.Range(
                LandEnemyLootRules.BossLootMinExtraItems,
                LandEnemyLootRules.BossLootMaxExtraItems + 1);
            for (var i = 0; i < extraCount; i++)
            {
                if (TryRollEquipment(enemyLevel, out var item))
                {
                    into.Add(item);
                }
            }
        }

        private static int RollUniformItemLevel(int maxLevel) =>
            Random.Range(LandUrEnemyStats.MinLevel, maxLevel + 1);

        private static bool TryRollEquipment(int maxLevel, out LandGearInstance item)
        {
            maxLevel = LandUrEnemyStats.ClampLevel(maxLevel);
            for (var attempt = 0; attempt < EquipmentTypeCount; attempt++)
            {
                var itemLevel = RollUniformItemLevel(maxLevel);
                var type = (LootItemType)Random.Range(0, EquipmentTypeCount);
                if (!TryGetItem(type, itemLevel, out item))
                {
                    continue;
                }

                item = LandLoadoutEquipService.CloneItem(item);
                if (item != null && IsItemLevelAtMost(item, maxLevel))
                {
                    return true;
                }
            }

            item = null;
            return false;
        }

        private static bool TryRollEquipmentAtExactLevel(int techLevel, out LandGearInstance item)
        {
            techLevel = LandUrEnemyStats.ClampLevel(techLevel);
            var start = Random.Range(0, EquipmentTypeCount);
            for (var i = 0; i < EquipmentTypeCount; i++)
            {
                var type = (LootItemType)((start + i) % EquipmentTypeCount);
                if (!TryGetItem(type, techLevel, out item))
                {
                    continue;
                }

                item = LandLoadoutEquipService.CloneItem(item);
                if (item != null && LandTechLevelRules.GetTechLevel(item) == techLevel)
                {
                    return true;
                }
            }

            item = null;
            return false;
        }

        private static bool IsItemLevelAtMost(LandGearInstance item, int maxLevel)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            var techLevel = LandTechLevelRules.GetTechLevel(item);
            return techLevel == 0 || techLevel <= maxLevel;
        }

        private static bool TryGetItem(LootItemType type, int techLevel, out LandGearInstance item)
        {
            item = null;
            switch (type)
            {
                case LootItemType.Weapon:
                    return TryGetWeapon(techLevel, out item);
                case LootItemType.Helmet:
                    return TryGetGear(LandUrGearCatalog.Helmets, techLevel, out item);
                case LootItemType.Vest:
                    return TryGetGear(LandUrGearCatalog.Vests, techLevel, out item);
                case LootItemType.Boots:
                    return TryGetGear(LandUrGearCatalog.Boots, techLevel, out item);
                case LootItemType.Bandage:
                    item = CreateConsumable(LandConsumableIds.Bandage);
                    return true;
                case LootItemType.Grenade:
                    item = CreateConsumable(LandConsumableIds.Grenade);
                    return true;
                default:
                    return false;
            }
        }

        private static LandGearInstance CreateConsumable(string definitionId)
        {
            return new LandGearInstance
            {
                DefinitionId = definitionId,
                Rarity = LandItemRarity.White
            };
        }

        private static bool TryGetWeapon(int techLevel, out LandGearInstance item)
        {
            item = null;
            if (!LandUrWeaponCatalog.TryGetByTechLevel(techLevel, out var entry))
            {
                return false;
            }

            item = new LandGearInstance
            {
                DefinitionId = entry.DefinitionId,
                Rarity = entry.Rarity
            };
            return true;
        }

        private static bool TryGetGear(
            LandUrGearCatalog.Entry[] table,
            int techLevel,
            out LandGearInstance item)
        {
            item = null;
            for (var i = 0; i < table.Length; i++)
            {
                if (table[i].TechLevel != techLevel)
                {
                    continue;
                }

                item = new LandGearInstance
                {
                    DefinitionId = table[i].DefinitionId,
                    Rarity = table[i].Rarity
                };
                return true;
            }

            return false;
        }
    }
}
