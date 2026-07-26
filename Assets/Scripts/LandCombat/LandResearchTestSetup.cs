using F89.Core;

namespace F89.LandCombat
{
    /// <summary>One-shot per play session: zero character scores and remove TL0 test scrap.</summary>
    public static class LandResearchTestSetup
    {
        private static bool appliedCleanupThisSession;

        public static void ApplyCleanupOnceForSession(CharacterSaveData save)
        {
            if (appliedCleanupThisSession || save == null)
            {
                return;
            }

            appliedCleanupThisSession = true;
            ResetScoresAndResearch(save);
            RemoveTechZeroItems(save);
            CharacterSaveRepository.WriteGear(save);
        }

        public static void ResetScoresAndResearch(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.EnemyVehiclesKilled = 0;
            save.EnemyTroopsKilled = 0;
            save.BestMissionScore = 0;
            save.TotalScore = 0;
            save.VehicleKillSummary = string.Empty;
            save.TroopKillSummary = string.Empty;
            save.ResearchHelmetChancePercent = 0f;
            save.ResearchVestChancePercent = 0f;
            save.ResearchWeaponChancePercent = 0f;
            save.ResearchBootsChancePercent = 0f;
            LandResearchService.EnsureSlotTechLevels(save);
        }

        public static void RemoveTechZeroItems(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            ClearTechZero(ref save.Loadout.Weapon);
            ClearTechZero(ref save.Loadout.Core);
            ClearTechZero(ref save.Loadout.Boots);
            ClearTechZero(ref save.Loadout.Helmet);
            ClearTechZero(ref save.Loadout.Shield);
            ClearTechZero(ref save.Loadout.DuffleBag);
            ClearTechZero(ref save.Loadout.Utility1);
            ClearTechZero(ref save.Loadout.Utility2);
            ClearTechZero(ref save.Loadout.Module1);
            ClearTechZero(ref save.Loadout.Module2);

            if (save.Loadout.Inventory != null)
            {
                for (var i = 0; i < save.Loadout.Inventory.Length; i++)
                {
                    ClearTechZero(ref save.Loadout.Inventory[i]);
                }
            }

            LandVaultStorageService.EnsureVaultSize(save.Vault);
            if (save.Vault?.Items == null)
            {
                return;
            }

            for (var i = 0; i < save.Vault.Items.Length; i++)
            {
                ClearTechZero(ref save.Vault.Items[i]);
            }
        }

        private static void ClearTechZero(ref CharacterGearInstanceSaveData item)
        {
            if (item == null || item.DefinitionId != LandTechLevelRules.TechZeroDefinitionId)
            {
                return;
            }

            item = null;
        }
    }
}
