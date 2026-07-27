using System.Collections.Generic;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Rolls random Ultimate Reich loot for enemy corpses.
    /// Per item: pick tech level uniformly from 1..enemyLevel, then pick item type
    /// (weapon / helmet / vest / boots / bandage / grenade) with equal chance.
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

        private const int LootTypeCount = 6;

        public static void FillRandomLoot(int enemyLevel, List<LandGearInstance> into)
        {
            into?.Clear();
            if (into == null)
            {
                return;
            }

            enemyLevel = LandUrEnemyStats.ClampLevel(enemyLevel);
            var count = Random.Range(LandEnemyLootRules.LootMinItems, LandEnemyLootRules.LootMaxItems + 1);
            for (var i = 0; i < count; i++)
            {
                if (!TryRollItem(enemyLevel, out var item))
                {
                    continue;
                }

                into.Add(item);
            }
        }

        private static bool TryRollItem(int enemyLevel, out LandGearInstance item)
        {
            // Even chance among every level from 1 through the enemy's level.
            var itemLevel = Random.Range(LandUrEnemyStats.MinLevel, enemyLevel + 1);
            // Even chance among item types (revisited later).
            var type = (LootItemType)Random.Range(0, LootTypeCount);
            if (!TryGetItem(type, itemLevel, out item))
            {
                return false;
            }

            item = LandLoadoutEquipService.CloneItem(item);
            return item != null;
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
