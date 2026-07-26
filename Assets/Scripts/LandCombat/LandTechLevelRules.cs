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

            return GetTechLevel(item.Rarity);
        }
    }
}
