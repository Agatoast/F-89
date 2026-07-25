using System.Collections.Generic;

namespace F89.LandCombat
{
    public readonly struct LandInventoryAddress
    {
        public LandInventoryAddress(int index) => Index = index;
        public int Index { get; }
    }

    public static class LandLoadoutEquipService
    {
        public static LandEquipResult TryEquipFromInventory(
            LandRunLoadout loadout,
            LandInventoryAddress from,
            LandEquipmentSlot paperdollSlot,
            LandItemCatalog catalog)
        {
            if (loadout == null || catalog == null || !TryGetInventoryItem(loadout, from, out var incoming))
            {
                return LandEquipResult.InvalidItem;
            }

            if (!TryResolveItemSlot(incoming, catalog, out var itemSlot))
            {
                return LandEquipResult.InvalidItem;
            }

            if (!LandGearEquipRules.CanEquip(itemSlot, paperdollSlot))
            {
                return LandEquipResult.WrongSlot;
            }

            var previous = LandLoadoutSlots.GetEquipped(loadout, paperdollSlot);
            LandLoadoutSlots.SetEquipped(loadout, paperdollSlot, CloneItem(incoming));
            SetInventoryItem(loadout, from, previous);
            return LandEquipResult.Success;
        }

        public static LandEquipResult TrySwapInventorySlots(
            LandRunLoadout loadout,
            LandInventoryAddress from,
            LandInventoryAddress to)
        {
            if (loadout == null || from.Index < 0 || to.Index < 0)
            {
                return LandEquipResult.InvalidItem;
            }

            if (!TryGetInventoryItem(loadout, from, out var fromItem) || !LandLoadoutSlots.IsValidItem(fromItem))
            {
                return LandEquipResult.InventoryEmpty;
            }

            TryGetInventoryItem(loadout, to, out var toItem);
            SetInventoryItem(loadout, from, CloneItem(toItem));
            SetInventoryItem(loadout, to, CloneItem(fromItem));
            return LandEquipResult.Success;
        }

        public static bool TryResolveItemSlot(LandGearInstance item, LandItemCatalog catalog, out LandEquipmentSlot itemSlot)
        {
            itemSlot = LandEquipmentSlot.Core;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (catalog.TryGetWeapon(item.DefinitionId, out _))
            {
                itemSlot = LandEquipmentSlot.Weapon;
                return true;
            }

            if (!catalog.TryGetGear(item.DefinitionId, out var gear))
            {
                return false;
            }

            itemSlot = gear.Slot;
            return true;
        }

        public static bool TryAddToFirstEmptyInventory(LandRunLoadout loadout, LandGearInstance item)
        {
            if (loadout == null || !LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            var accessible = LandInventoryRules.GetAccessibleSlotCount(loadout.DuffleBag);
            for (var i = 0; i < accessible; i++)
            {
                if (!LandLoadoutSlots.IsValidItem(loadout.Inventory[i]))
                {
                    loadout.Inventory[i] = CloneItem(item);
                    return true;
                }
            }

            return false;
        }

        public static LandGearInstance CloneItem(LandGearInstance item)
        {
            if (item == null)
            {
                return null;
            }

            return new LandGearInstance
            {
                DefinitionId = item.DefinitionId,
                Rarity = item.Rarity,
                Affixes = item.Affixes != null ? new List<LandRolledAffix>(item.Affixes) : new List<LandRolledAffix>()
            };
        }

        private static bool TryGetInventoryItem(LandRunLoadout loadout, LandInventoryAddress from, out LandGearInstance item)
        {
            item = null;
            LandInventoryRules.EnsureInventoryCapacity(loadout);
            if (from.Index < 0 || from.Index >= loadout.Inventory.Count)
            {
                return false;
            }

            item = loadout.Inventory[from.Index];
            return true;
        }

        private static void SetInventoryItem(LandRunLoadout loadout, LandInventoryAddress to, LandGearInstance item)
        {
            LandInventoryRules.EnsureInventoryCapacity(loadout);
            while (loadout.Inventory.Count <= to.Index)
            {
                loadout.Inventory.Add(null);
            }

            loadout.Inventory[to.Index] = item;
        }
    }
}
