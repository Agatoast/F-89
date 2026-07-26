using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    public static class LandResearchService
    {
        public const float ChancePerItemPercent = 0.5f;
        public const float MaxChancePercent = 30f;
        public const int MinTechLevel = 1;
        public const int MaxTechLevel = 10;
        public const int SlotCount = 4;

        public static void EnsureSlotTechLevels(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.ResearchHelmetTechLevel = ClampTechLevel(save.ResearchHelmetTechLevel);
            save.ResearchVestTechLevel = ClampTechLevel(save.ResearchVestTechLevel);
            save.ResearchWeaponTechLevel = ClampTechLevel(save.ResearchWeaponTechLevel);
            save.ResearchBootsTechLevel = ClampTechLevel(save.ResearchBootsTechLevel);
            save.ResearchHelmetChancePercent = ClampChance(save.ResearchHelmetChancePercent);
            save.ResearchVestChancePercent = ClampChance(save.ResearchVestChancePercent);
            save.ResearchWeaponChancePercent = ClampChance(save.ResearchWeaponChancePercent);
            save.ResearchBootsChancePercent = ClampChance(save.ResearchBootsChancePercent);
        }

        public static int GetSlotTechLevel(CharacterSaveData save, int researchSlotIndex)
        {
            if (save == null)
            {
                return MinTechLevel;
            }

            EnsureSlotTechLevels(save);
            return researchSlotIndex switch
            {
                0 => save.ResearchHelmetTechLevel,
                1 => save.ResearchVestTechLevel,
                2 => save.ResearchWeaponTechLevel,
                3 => save.ResearchBootsTechLevel,
                _ => MinTechLevel
            };
        }

        public static float GetChancePercent(CharacterSaveData save, int researchSlotIndex)
        {
            if (save == null)
            {
                return 0f;
            }

            EnsureSlotTechLevels(save);
            return researchSlotIndex switch
            {
                0 => save.ResearchHelmetChancePercent,
                1 => save.ResearchVestChancePercent,
                2 => save.ResearchWeaponChancePercent,
                3 => save.ResearchBootsChancePercent,
                _ => 0f
            };
        }

        public static float AddResearchProgress(CharacterSaveData save, int researchSlotIndex)
        {
            if (save == null || researchSlotIndex < 0 || researchSlotIndex >= SlotCount)
            {
                return 0f;
            }

            EnsureSlotTechLevels(save);
            var next = Mathf.Min(MaxChancePercent, GetChancePercent(save, researchSlotIndex) + ChancePerItemPercent);
            switch (researchSlotIndex)
            {
                case 0: save.ResearchHelmetChancePercent = next; break;
                case 1: save.ResearchVestChancePercent = next; break;
                case 2: save.ResearchWeaponChancePercent = next; break;
                case 3: save.ResearchBootsChancePercent = next; break;
            }

            return next;
        }

        public static bool AcceptsItem(CharacterSaveData save, int researchSlotIndex, LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            return LandTechLevelRules.GetTechLevel(item) >= GetSlotTechLevel(save, researchSlotIndex);
        }

        public static bool IsValidResearchCandidate(
            CharacterSaveData save,
            int researchSlotIndex,
            LandGearInstance item,
            LandItemCatalog catalog,
            out bool wrongCategory,
            out bool techTooLow)
        {
            wrongCategory = false;
            techTooLow = false;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (!LandItemCategoryRules.CanUseForResearch(item, catalog))
            {
                wrongCategory = true;
                return false;
            }

            if (!AcceptsItem(save, researchSlotIndex, item))
            {
                techTooLow = true;
                return false;
            }

            return true;
        }

        private static int ClampTechLevel(int level)
        {
            if (level < MinTechLevel)
            {
                return MinTechLevel;
            }

            return level > MaxTechLevel ? MaxTechLevel : level;
        }

        private static float ClampChance(float chance) =>
            Mathf.Clamp(chance, 0f, MaxChancePercent);
    }
}
