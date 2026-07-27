using System.Collections.Generic;
using F89.Core;

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

            // Always place in the item's correct equipment slot; previous item returns to the inventory cell.
            paperdollSlot = itemSlot;

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

            if (catalog != null && catalog.TryGetWeapon(item.DefinitionId, out _))
            {
                itemSlot = LandEquipmentSlot.Weapon;
                return true;
            }

            if (catalog != null && catalog.TryGetGear(item.DefinitionId, out var gear))
            {
                itemSlot = gear.Slot;
                return true;
            }

            // Fallback when ScriptableObjects fail to load (dev assets / import issues).
            if (LandUsWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out _)
                || LandUrWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out _))
            {
                itemSlot = LandEquipmentSlot.Weapon;
                return true;
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var usGear))
            {
                itemSlot = usGear.Slot;
                return true;
            }

            if (LandUrGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var urGear))
            {
                itemSlot = urGear.Slot;
                return true;
            }

            return false;
        }

        public static bool TryAddToFirstEmptyInventory(LandRunLoadout loadout, LandGearInstance item)
        {
            if (loadout == null
                || !LandLoadoutSlots.IsValidItem(item)
                || LandConsumableIds.IsConsumable(item))
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

        public static LandEquipResult TryEquipItemCopy(
            LandRunLoadout loadout,
            LandGearInstance source,
            LandEquipmentSlot paperdollSlot,
            LandItemCatalog catalog,
            CharacterVaultSaveData vault = null)
        {
            if (loadout == null || catalog == null || !LandLoadoutSlots.IsValidItem(source))
            {
                return LandEquipResult.InvalidItem;
            }

            if (LandConsumableIds.IsConsumable(source))
            {
                return LandEquipResult.InvalidItem;
            }

            if (!TryResolveItemSlot(source, catalog, out var itemSlot))
            {
                return LandEquipResult.InvalidItem;
            }

            if (!LandGearEquipRules.CanEquip(itemSlot, paperdollSlot))
            {
                return LandEquipResult.WrongSlot;
            }

            var previous = LandLoadoutSlots.GetEquipped(loadout, paperdollSlot);
            LandLoadoutSlots.SetEquipped(loadout, paperdollSlot, CloneItem(source));
            if (LandLoadoutSlots.IsValidItem(previous))
            {
                if (!TryAddToFirstEmptyInventory(loadout, previous)
                    && !TryAddToFirstEmptyVault(vault, previous))
                {
                    // Inventory and vault are full — previous item is replaced.
                }
            }

            return LandEquipResult.Success;
        }

        public static LandEquipResult TryPlaceItemCopyInInventory(
            LandRunLoadout loadout,
            LandGearInstance source,
            LandInventoryAddress to)
        {
            if (loadout == null || !LandLoadoutSlots.IsValidItem(source))
            {
                return LandEquipResult.InvalidItem;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            if (!LandInventoryRules.IsInventoryIndexAccessible(to.Index, loadout.DuffleBag))
            {
                return LandEquipResult.InvalidItem;
            }

            TryGetInventoryItem(loadout, to, out var existing);
            if (LandLoadoutSlots.IsValidItem(existing))
            {
                return LandEquipResult.VaultFull;
            }

            SetInventoryItem(loadout, to, CloneItem(source));
            return LandEquipResult.Success;
        }

        public static LandEquipResult TryMoveOrSwapEquipmentWithInventory(
            LandRunLoadout loadout,
            LandEquipmentSlot equipmentSlot,
            LandInventoryAddress inventory,
            LandItemCatalog catalog)
        {
            if (loadout == null || catalog == null)
            {
                return LandEquipResult.InvalidItem;
            }

            var equipped = LandLoadoutSlots.GetEquipped(loadout, equipmentSlot);
            if (!LandLoadoutSlots.IsValidItem(equipped))
            {
                return LandEquipResult.InventoryEmpty;
            }

            LandInventoryRules.EnsureInventoryCapacity(loadout);
            if (!LandInventoryRules.IsInventoryIndexAccessible(inventory.Index, loadout.DuffleBag))
            {
                return LandEquipResult.InvalidItem;
            }

            TryGetInventoryItem(loadout, inventory, out var inventoryItem);
            if (!LandLoadoutSlots.IsValidItem(inventoryItem))
            {
                SetInventoryItem(loadout, inventory, CloneItem(equipped));
                LandLoadoutSlots.SetEquipped(loadout, equipmentSlot, null);
                return LandEquipResult.Success;
            }

            if (!TryResolveItemSlot(inventoryItem, catalog, out var incomingSlot)
                || !LandGearEquipRules.CanEquip(incomingSlot, equipmentSlot))
            {
                return LandEquipResult.WrongSlot;
            }

            LandLoadoutSlots.SetEquipped(loadout, equipmentSlot, CloneItem(inventoryItem));
            SetInventoryItem(loadout, inventory, CloneItem(equipped));
            return LandEquipResult.Success;
        }

        public static LandEquipResult TrySwapPaperdollSlots(
            LandRunLoadout loadout,
            LandEquipmentSlot fromSlot,
            LandEquipmentSlot toSlot,
            LandItemCatalog catalog)
        {
            if (loadout == null || catalog == null)
            {
                return LandEquipResult.InvalidItem;
            }

            var fromItem = LandLoadoutSlots.GetEquipped(loadout, fromSlot);
            if (!LandLoadoutSlots.IsValidItem(fromItem))
            {
                return LandEquipResult.InventoryEmpty;
            }

            if (!TryResolveItemSlot(fromItem, catalog, out var fromItemSlot)
                || !LandGearEquipRules.CanEquip(fromItemSlot, toSlot))
            {
                return LandEquipResult.WrongSlot;
            }

            var toItem = LandLoadoutSlots.GetEquipped(loadout, toSlot);
            if (LandLoadoutSlots.IsValidItem(toItem)
                && (!TryResolveItemSlot(toItem, catalog, out var toItemSlot)
                    || !LandGearEquipRules.CanEquip(toItemSlot, fromSlot)))
            {
                return LandEquipResult.WrongSlot;
            }

            LandLoadoutSlots.SetEquipped(loadout, fromSlot, toItem);
            LandLoadoutSlots.SetEquipped(loadout, toSlot, fromItem);
            return LandEquipResult.Success;
        }

        public static bool TryAddToFirstEmptyVault(CharacterVaultSaveData vault, LandGearInstance item)
        {
            if (vault == null || !LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            var index = LandVaultStorageService.FindFirstEmptyVaultIndex(vault);
            if (index < 0)
            {
                return false;
            }

            LandVaultStorageService.EnsureVaultSize(vault);
            vault.Items[index] = LandGearSaveMapper.ToSaveInstance(CloneItem(item));
            return true;
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
