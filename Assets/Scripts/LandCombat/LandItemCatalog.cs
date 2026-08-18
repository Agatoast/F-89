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

            if (LandUsWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var usWeapon))
            {
                return usWeapon.DisplayName;
            }

            if (LandUrWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var urWeapon))
            {
                return urWeapon.DisplayName;
            }

            if (TryGetGear(item.DefinitionId, out var gear))
            {
                return gear.DisplayName;
            }

            return item.DefinitionId;
        }

        public bool TryGetWeaponSummary(LandGearInstance item, out string summary)
        {
            summary = string.Empty;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (TryGetWeapon(item.DefinitionId, out var weapon))
            {
                summary = LandItemStatFormatter.FormatWeaponSummary(weapon);
                return true;
            }

            if (LandItemCombatStatsResolver.TryGetWeaponStats(item, this, out var damage, out var range))
            {
                summary = $"{LandItemStatFormatter.FormatStatLabel(LandItemStat.Damage)} {damage:0}  |  {LandItemStatFormatter.FormatStatLabel(LandItemStat.Range)} {range:0}";
                return true;
            }

            return false;
        }

        public bool TryGetGearSummary(LandGearInstance item, out string summary)
        {
            summary = string.Empty;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (TryGetGear(item.DefinitionId, out var gear))
            {
                summary = LandItemStatFormatter.FormatGearSummary(gear);
                return !string.IsNullOrEmpty(summary);
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var usGear))
            {
                summary =
                    $"{LandItemStatFormatter.FormatStatLabel(LandItemStat.DamageResistance)} {usGear.DamageResistance}"
                    + (usGear.Move > 0
                        ? $"  |  {LandItemStatFormatter.FormatStatLabel(LandItemStat.Move)} {usGear.Move}"
                        : string.Empty);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves DR / Move for equipped gear. Falls back to US/UR catalogs when SOs fail to load.
        /// </summary>
        public bool TryGetGearCombatStats(
            LandGearInstance item,
            out LandEquipmentSlot slot,
            out int damageResistance,
            out int move)
        {
            slot = LandEquipmentSlot.Core;
            damageResistance = 0;
            move = 0;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (TryGetGear(item.DefinitionId, out var gear))
            {
                slot = gear.Slot;
                damageResistance = Mathf.Max(0, gear.DamageResistance);
                move = gear.Move;
                return true;
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var usGear))
            {
                slot = usGear.Slot;
                damageResistance = Mathf.Max(0, usGear.DamageResistance);
                move = usGear.Move;
                return true;
            }

            if (LandUrGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var urGear))
            {
                slot = urGear.Slot;
                damageResistance = Mathf.Max(0, urGear.DamageResistance);
                move = urGear.Move;
                return true;
            }

            return false;
        }
    }
}
