using UnityEngine;

namespace F89.LandCombat
{
    public sealed class LandItemCatalog
    {
        private const string ContentRoot = "LandCombat/Content/";

        public bool TryGetGear(string definitionId, out LandGearDefinition gear)
        {
            gear = null;
            if (string.IsNullOrEmpty(definitionId))
            {
                return false;
            }

            gear = Resources.Load<LandGearDefinition>(ContentRoot + definitionId);
            return gear != null;
        }

        public bool TryGetWeapon(string definitionId, out LandWeaponDefinition weapon)
        {
            weapon = null;
            if (string.IsNullOrEmpty(definitionId))
            {
                return false;
            }

            weapon = Resources.Load<LandWeaponDefinition>(ContentRoot + definitionId);
            return weapon != null;
        }

        public string GetDisplayName(LandGearInstance item)
        {
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return "Empty";
            }

            if (TryGetWeapon(item.DefinitionId, out var weapon))
            {
                return weapon.DisplayName;
            }

            if (TryGetGear(item.DefinitionId, out var gear))
            {
                return gear.DisplayName;
            }

            return item.DefinitionId;
        }
    }
}
