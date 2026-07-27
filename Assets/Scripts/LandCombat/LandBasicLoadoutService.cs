using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Resolves the highest-tech (highest rarity) item available per equipment category
    /// for the Character Loadout Basic Loadout tray.
    /// </summary>
    public static class LandBasicLoadoutService
    {
        private const string ContentRoot = "LandCombat/Content";

        private static readonly LandEquipmentSlot[] TraySlots =
        {
            LandEquipmentSlot.Helmet,
            LandEquipmentSlot.Core,
            LandEquipmentSlot.Weapon,
            LandEquipmentSlot.Boots
        };

        public static LandEquipmentSlot GetTraySlot(int index) =>
            TraySlots[Mathf.Clamp(index, 0, TraySlots.Length - 1)];

        public static int TraySlotCount => TraySlots.Length;

        public static bool TryGetHighestAvailable(
            LandEquipmentSlot slot,
            LandItemCatalog catalog,
            LandRunLoadout loadout,
            CharacterVaultSaveData vault,
            out LandGearInstance item)
        {
            item = null;
            if (catalog == null)
            {
                return false;
            }

            LandGearInstance best = null;
            var bestRank = int.MinValue;

            ConsiderCatalogDefinitions(slot, catalog, ref best, ref bestRank);
            ConsiderOwnedItems(slot, catalog, loadout, vault, ref best, ref bestRank);

            if (best == null)
            {
                return false;
            }

            item = LandLoadoutEquipService.CloneItem(best);
            return true;
        }

        private static void ConsiderCatalogDefinitions(
            LandEquipmentSlot slot,
            LandItemCatalog catalog,
            ref LandGearInstance best,
            ref int bestRank)
        {
            // Catalog tray gear is Military Basic Loadout only.
            // Experimental R&D pieces appear when owned in loadout/vault.
            var gearDefs = Resources.LoadAll<LandGearDefinition>(ContentRoot);
            for (var i = 0; i < gearDefs.Length; i++)
            {
                var def = gearDefs[i];
                if (def == null || def.Slot != slot || def.Category != LandItemCategory.Military)
                {
                    continue;
                }

                Consider(
                    new LandGearInstance
                    {
                        DefinitionId = def.name,
                        Rarity = def.Rarity
                    },
                    slot,
                    catalog,
                    ref best,
                    ref bestRank);
            }

            if (slot != LandEquipmentSlot.Weapon)
            {
                return;
            }

            // Catalog weapons in the tray are Military Basic Loadout only (M-4).
            // Experimental R&D guns appear when owned in loadout/vault.
            var weaponDefs = Resources.LoadAll<LandWeaponDefinition>(ContentRoot);
            for (var i = 0; i < weaponDefs.Length; i++)
            {
                var def = weaponDefs[i];
                if (def == null || def.Category != LandItemCategory.Military)
                {
                    continue;
                }

                Consider(
                    new LandGearInstance
                    {
                        DefinitionId = def.name,
                        Rarity = def.Rarity
                    },
                    slot,
                    catalog,
                    ref best,
                    ref bestRank);
            }
        }

        private static void ConsiderOwnedItems(
            LandEquipmentSlot slot,
            LandItemCatalog catalog,
            LandRunLoadout loadout,
            CharacterVaultSaveData vault,
            ref LandGearInstance best,
            ref int bestRank)
        {
            if (loadout != null)
            {
                Consider(LandLoadoutSlots.GetEquipped(loadout, slot), slot, catalog, ref best, ref bestRank);
                LandInventoryRules.EnsureInventoryCapacity(loadout);
                for (var i = 0; i < loadout.Inventory.Count; i++)
                {
                    Consider(loadout.Inventory[i], slot, catalog, ref best, ref bestRank);
                }
            }

            if (vault?.Items == null)
            {
                return;
            }

            LandVaultStorageService.EnsureVaultSize(vault);
            for (var i = 0; i < vault.Items.Length; i++)
            {
                Consider(
                    LandGearSaveMapper.ToRuntimeInstance(vault.Items[i]),
                    slot,
                    catalog,
                    ref best,
                    ref bestRank);
            }
        }

        private static void Consider(
            LandGearInstance candidate,
            LandEquipmentSlot expectedSlot,
            LandItemCatalog catalog,
            ref LandGearInstance best,
            ref int bestRank)
        {
            if (!LandLoadoutSlots.IsValidItem(candidate))
            {
                return;
            }

            if (!LandLoadoutEquipService.TryResolveItemSlot(candidate, catalog, out var itemSlot)
                || itemSlot != expectedSlot)
            {
                return;
            }

            var rank = GetTechRank(candidate, catalog);
            if (rank < bestRank)
            {
                return;
            }

            if (rank == bestRank && best != null)
            {
                return;
            }

            best = candidate;
            bestRank = rank;
        }

        private static int GetTechRank(LandGearInstance item, LandItemCatalog catalog)
        {
            var rarity = (int)item.Rarity;
            if (catalog.TryGetWeapon(item.DefinitionId, out var weapon))
            {
                rarity = Mathf.Max(rarity, (int)weapon.Rarity);
            }
            else if (catalog.TryGetGear(item.DefinitionId, out var gear))
            {
                rarity = Mathf.Max(rarity, (int)gear.Rarity);
            }

            return rarity;
        }
    }
}
