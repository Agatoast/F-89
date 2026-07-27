using F89.Core;

namespace F89.LandCombat
{
    /// <summary>
    /// Temporary dev helper: clears inventory / footlocker, equips Basic Loadout L1,
    /// and resets R&amp;D chance percents.
    /// </summary>
    public static class LandDevWeaponFill
    {
        private const int FillVersion = 4;
        private static int filledVersion;

        public static bool FillInventoryAndFootlockerOnce(CharacterSaveData save)
        {
            if (filledVersion == FillVersion || save == null)
            {
                return false;
            }

            filledVersion = FillVersion;
            FillInventoryAndFootlocker(save);
            return true;
        }

        public static void FillInventoryAndFootlocker(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var loadout = LandGearSaveMapper.ToRuntime(save.Loadout);
            ClearEquipment(loadout);
            ClearInventory(loadout);
            ClearVault(save.Vault);
            EquipBasicLoadoutLevel1(loadout);
            ResetResearchChances(save);

            LandGearSaveMapper.ToSave(loadout, save.Loadout);
            CharacterSaveRepository.WriteGear(save);
        }

        private static void ClearEquipment(LandRunLoadout loadout)
        {
            for (var i = 0; i < LandLoadoutSlots.PaperdollSlots.Length; i++)
            {
                LandLoadoutSlots.SetEquipped(loadout, LandLoadoutSlots.PaperdollSlots[i], null);
            }
        }

        private static void ClearInventory(LandRunLoadout loadout)
        {
            LandInventoryRules.EnsureInventoryCapacity(loadout);
            for (var i = 0; i < loadout.Inventory.Count; i++)
            {
                loadout.Inventory[i] = null;
            }
        }

        private static void ClearVault(CharacterVaultSaveData vault)
        {
            LandVaultStorageService.EnsureVaultSize(vault);
            for (var i = 0; i < vault.Items.Length; i++)
            {
                vault.Items[i] = null;
            }
        }

        private static void EquipBasicLoadoutLevel1(LandRunLoadout loadout)
        {
            if (LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Helmet, 1, out var helmet))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Helmet, FromGear(helmet));
            }

            if (LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Core, 1, out var vest))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Core, FromGear(vest));
            }

            if (LandUsWeaponCatalog.TryGetByTechLevel(1, out var weapon))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Weapon, FromWeapon(weapon));
            }

            if (LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Boots, 1, out var boots))
            {
                LandLoadoutSlots.SetEquipped(loadout, LandEquipmentSlot.Boots, FromGear(boots));
            }
        }

        private static void ResetResearchChances(CharacterSaveData save)
        {
            save.ResearchHelmetChancePercent = 0f;
            save.ResearchVestChancePercent = 0f;
            save.ResearchWeaponChancePercent = 0f;
            save.ResearchBootsChancePercent = 0f;
            LandResearchService.EnsureSlotTechLevels(save);
        }

        private static LandGearInstance FromGear(LandUsGearCatalog.Entry entry) =>
            new()
            {
                DefinitionId = entry.DefinitionId,
                Rarity = entry.Rarity
            };

        private static LandGearInstance FromWeapon(LandUsWeaponCatalog.Entry entry) =>
            new()
            {
                DefinitionId = entry.DefinitionId,
                Rarity = entry.Rarity
            };
    }
}
