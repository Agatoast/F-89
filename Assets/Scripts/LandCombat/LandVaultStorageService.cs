using F89.Core;

namespace F89.LandCombat
{
    public static class LandVaultStorageService
    {
        public static int GetVaultSlotCount(CharacterVaultSaveData vault)
        {
            if (vault == null)
            {
                return LandGameConstants.VaultBaseSlotCount;
            }

            if (vault.SlotCount < LandGameConstants.VaultBaseSlotCount)
            {
                vault.SlotCount = LandGameConstants.VaultBaseSlotCount;
            }

            return vault.SlotCount > LandGameConstants.VaultMaxSlotCount
                ? LandGameConstants.VaultMaxSlotCount
                : vault.SlotCount;
        }

        public static void EnsureVaultSize(CharacterVaultSaveData vault)
        {
            if (vault == null)
            {
                return;
            }

            var slotCount = GetVaultSlotCount(vault);
            if (vault.Items == null)
            {
                vault.Items = new CharacterGearInstanceSaveData[slotCount];
                return;
            }

            if (vault.Items.Length == slotCount)
            {
                return;
            }

            var resized = new CharacterGearInstanceSaveData[slotCount];
            for (var i = 0; i < slotCount && i < vault.Items.Length; i++)
            {
                resized[i] = vault.Items[i];
            }

            vault.Items = resized;
        }

        public static bool IsVaultIndexValid(CharacterVaultSaveData vault, int index) =>
            index >= 0 && index < GetVaultSlotCount(vault);

        public static LandEquipResult TrySwapVaultSlots(CharacterVaultSaveData vault, int fromIndex, int toIndex)
        {
            if (!IsVaultIndexValid(vault, fromIndex) || !IsVaultIndexValid(vault, toIndex))
            {
                return LandEquipResult.InvalidItem;
            }

            EnsureVaultSize(vault);
            var fromItem = LandGearSaveMapper.ToRuntimeInstance(vault.Items[fromIndex]);
            if (!LandLoadoutSlots.IsValidItem(fromItem))
            {
                return LandEquipResult.InventoryEmpty;
            }

            var toItem = LandGearSaveMapper.ToRuntimeInstance(vault.Items[toIndex]);
            vault.Items[fromIndex] = LandGearSaveMapper.ToSaveInstance(toItem);
            vault.Items[toIndex] = LandGearSaveMapper.ToSaveInstance(fromItem);
            return LandEquipResult.Success;
        }

        public static LandEquipResult TrySwapInventoryWithVault(
            LandRunLoadout loadout,
            int inventoryIndex,
            CharacterVaultSaveData vault,
            int vaultIndex)
        {
            if (loadout == null || vault == null || !IsVaultIndexValid(vault, vaultIndex))
            {
                return LandEquipResult.InvalidItem;
            }

            EnsureVaultSize(vault);
            var address = new LandInventoryAddress(inventoryIndex);
            LandInventoryRules.EnsureInventoryCapacity(loadout);

            if (inventoryIndex < 0 || inventoryIndex >= loadout.Inventory.Count)
            {
                return LandEquipResult.InvalidItem;
            }

            var inventoryItem = loadout.Inventory[inventoryIndex];
            var vaultItem = LandGearSaveMapper.ToRuntimeInstance(vault.Items[vaultIndex]);
            var hasInventory = LandLoadoutSlots.IsValidItem(inventoryItem);
            var hasVault = LandLoadoutSlots.IsValidItem(vaultItem);
            if (!hasInventory && !hasVault)
            {
                return LandEquipResult.InventoryEmpty;
            }

            loadout.Inventory[inventoryIndex] = LandLoadoutEquipService.CloneItem(vaultItem);
            vault.Items[vaultIndex] = LandGearSaveMapper.ToSaveInstance(inventoryItem);
            return LandEquipResult.Success;
        }

        public static int FindFirstEmptyVaultIndex(CharacterVaultSaveData vault)
        {
            if (vault == null)
            {
                return -1;
            }

            EnsureVaultSize(vault);
            var slotCount = GetVaultSlotCount(vault);
            for (var i = 0; i < slotCount; i++)
            {
                if (!LandLoadoutSlots.IsValidItem(LandGearSaveMapper.ToRuntimeInstance(vault.Items[i])))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
