using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    public enum LandResearchReportPage
    {
        NoBreakPage = 0,
        HelmetRnD = 1,
        VestRnD = 2,
        WeaponRnD = 3,
        BootsRnD = 4
    }

    public readonly struct LandResearchDiscovery
    {
        public readonly LandResearchReportPage Page;
        public readonly int ResearchSlotIndex;
        public readonly int UnlockedTechLevel;
        public readonly string ItemDefinitionId;
        public readonly string ItemDisplayName;

        public LandResearchDiscovery(
            LandResearchReportPage page,
            int researchSlotIndex,
            int unlockedTechLevel,
            string itemDefinitionId,
            string itemDisplayName)
        {
            Page = page;
            ResearchSlotIndex = researchSlotIndex;
            UnlockedTechLevel = unlockedTechLevel;
            ItemDefinitionId = itemDefinitionId ?? string.Empty;
            ItemDisplayName = itemDisplayName ?? string.Empty;
        }
    }

    /// <summary>
    /// End-of-mission R&amp;D rolls. Each slot's listed chance is rolled; successes unlock the next
    /// Basic Loadout tech level and queue discovery report pages.
    /// </summary>
    public static class LandResearchBreakthroughService
    {
        public static List<LandResearchDiscovery> RollMissionBreakthroughs(CharacterSaveData save)
        {
            var discoveries = new List<LandResearchDiscovery>();
            if (save == null)
            {
                return discoveries;
            }

            LandResearchService.EnsureSlotTechLevels(save);
            TryRollSlot(save, 0, LandResearchReportPage.HelmetRnD, discoveries);
            TryRollSlot(save, 1, LandResearchReportPage.VestRnD, discoveries);
            TryRollSlot(save, 2, LandResearchReportPage.WeaponRnD, discoveries);
            TryRollSlot(save, 3, LandResearchReportPage.BootsRnD, discoveries);
            CharacterSaveRepository.WriteGear(save);
            return discoveries;
        }

        private static void TryRollSlot(
            CharacterSaveData save,
            int slotIndex,
            LandResearchReportPage page,
            List<LandResearchDiscovery> discoveries)
        {
            var currentLevel = LandResearchService.GetSlotTechLevel(save, slotIndex);
            if (currentLevel >= LandResearchService.MaxTechLevel)
            {
                return;
            }

            var chance = LandResearchService.GetChancePercent(save, slotIndex);
            if (chance <= 0f)
            {
                return;
            }

            // Chance is stored as percent (0–30).
            if (Random.value * 100f >= chance)
            {
                return;
            }

            var unlockedLevel = currentLevel + 1;
            SetSlotTechLevel(save, slotIndex, unlockedLevel);
            SetSlotChance(save, slotIndex, 0f);

            if (!TryResolveUnlockedItem(slotIndex, unlockedLevel, out var definitionId, out var displayName))
            {
                return;
            }

            discoveries.Add(new LandResearchDiscovery(page, slotIndex, unlockedLevel, definitionId, displayName));
        }

        public static bool TryResolveUnlockedItem(
            int researchSlotIndex,
            int techLevel,
            out string definitionId,
            out string displayName)
        {
            definitionId = string.Empty;
            displayName = string.Empty;

            switch (researchSlotIndex)
            {
                case 0:
                    if (!LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Helmet, techLevel, out var helmet))
                    {
                        return false;
                    }

                    definitionId = helmet.DefinitionId;
                    displayName = helmet.DisplayName;
                    return true;
                case 1:
                    if (!LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Core, techLevel, out var vest))
                    {
                        return false;
                    }

                    definitionId = vest.DefinitionId;
                    displayName = vest.DisplayName;
                    return true;
                case 2:
                    if (!LandUsWeaponCatalog.TryGetByTechLevel(techLevel, out var weapon))
                    {
                        return false;
                    }

                    definitionId = weapon.DefinitionId;
                    displayName = weapon.DisplayName;
                    return true;
                case 3:
                    if (!LandUsGearCatalog.TryGetByTechLevel(LandEquipmentSlot.Boots, techLevel, out var boots))
                    {
                        return false;
                    }

                    definitionId = boots.DefinitionId;
                    displayName = boots.DisplayName;
                    return true;
                default:
                    return false;
            }
        }

        public static string GetBasicLoadoutDefinitionId(CharacterSaveData save, LandEquipmentSlot slot)
        {
            var researchIndex = slot switch
            {
                LandEquipmentSlot.Helmet => 0,
                LandEquipmentSlot.Core => 1,
                LandEquipmentSlot.Weapon => 2,
                LandEquipmentSlot.Boots => 3,
                _ => -1
            };

            if (researchIndex < 0)
            {
                return string.Empty;
            }

            var level = LandResearchService.GetSlotTechLevel(save, researchIndex);
            if (!TryResolveUnlockedItem(researchIndex, level, out var definitionId, out _))
            {
                return string.Empty;
            }

            return definitionId;
        }

        private static void SetSlotTechLevel(CharacterSaveData save, int slotIndex, int level)
        {
            level = Mathf.Clamp(level, LandResearchService.MinTechLevel, LandResearchService.MaxTechLevel);
            switch (slotIndex)
            {
                case 0: save.ResearchHelmetTechLevel = level; break;
                case 1: save.ResearchVestTechLevel = level; break;
                case 2: save.ResearchWeaponTechLevel = level; break;
                case 3: save.ResearchBootsTechLevel = level; break;
            }
        }

        private static void SetSlotChance(CharacterSaveData save, int slotIndex, float chance)
        {
            chance = Mathf.Clamp(chance, 0f, LandResearchService.MaxChancePercent);
            switch (slotIndex)
            {
                case 0: save.ResearchHelmetChancePercent = chance; break;
                case 1: save.ResearchVestChancePercent = chance; break;
                case 2: save.ResearchWeaponChancePercent = chance; break;
                case 3: save.ResearchBootsChancePercent = chance; break;
            }
        }
    }
}
