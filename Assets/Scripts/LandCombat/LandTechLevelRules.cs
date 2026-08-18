using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    public static class LandTechLevelRules
    {
        public const string TechZeroDefinitionId = "TechZeroScrap";

        /// <summary>Tech level = rarity tier (White = 1 … Gold = 10), matching MTAU. Test junk is TL 0.</summary>
        public static int GetTechLevel(LandItemRarity rarity) => (int)rarity + 1;

        public static int GetTechLevel(LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return 0;
            }

            if (item.DefinitionId == TechZeroDefinitionId)
            {
                return 0;
            }

            // US Basic Loadout / R&D ladders are authoritative for M- and X- gear.
            if (LandUsWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var usWeapon))
            {
                return usWeapon.TechLevel;
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var usGear))
            {
                return usGear.TechLevel;
            }

            if (LandUrWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var urWeapon))
            {
                return urWeapon.TechLevel;
            }

            if (LandUrGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var urGear))
            {
                return urGear.TechLevel;
            }

            if (IsBasicLoadoutItem(item))
            {
                return 0;
            }

            return GetTechLevel(item.Rarity);
        }

        /// <summary>Basic Loadout tray gear — tech level 0, distinct from TL 1 UR loot.</summary>
        public static bool IsBasicLoadoutItem(LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            var id = item.DefinitionId;
            return id == LandUsWeaponCatalog.BasicLoadoutDefinitionId
                || id == LandUsGearCatalog.BasicHelmetId
                || id == LandUsGearCatalog.BasicVestId
                || id == LandUsGearCatalog.BasicBootsId;
        }
    }
}
