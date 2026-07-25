using System.Collections.Generic;

namespace F89.LandCombat
{
    public static class LandLoadoutSlots
    {
        public static readonly LandEquipmentSlot[] PaperdollSlots =
        {
            LandEquipmentSlot.Utility1,
            LandEquipmentSlot.Utility2,
            LandEquipmentSlot.Module2,
            LandEquipmentSlot.Helmet,
            LandEquipmentSlot.Module1,
            LandEquipmentSlot.Core,
            LandEquipmentSlot.Weapon,
            LandEquipmentSlot.Shield,
            LandEquipmentSlot.Boots,
            LandEquipmentSlot.DuffleBag
        };

        public static LandGearInstance GetEquipped(LandRunLoadout loadout, LandEquipmentSlot slot)
        {
            if (loadout == null)
            {
                return null;
            }

            return slot switch
            {
                LandEquipmentSlot.Weapon => loadout.Weapon,
                LandEquipmentSlot.Core => loadout.Core,
                LandEquipmentSlot.Boots => loadout.Boots,
                LandEquipmentSlot.DuffleBag => loadout.DuffleBag,
                LandEquipmentSlot.Utility1 => loadout.Utility1,
                LandEquipmentSlot.Utility2 => loadout.Utility2,
                LandEquipmentSlot.Module1 => loadout.Module1,
                LandEquipmentSlot.Module2 => loadout.Module2,
                LandEquipmentSlot.Helmet => loadout.Helmet,
                LandEquipmentSlot.Shield => loadout.Shield,
                _ => null
            };
        }

        public static void SetEquipped(LandRunLoadout loadout, LandEquipmentSlot slot, LandGearInstance item)
        {
            if (loadout == null)
            {
                return;
            }

            switch (slot)
            {
                case LandEquipmentSlot.Weapon: loadout.Weapon = item; break;
                case LandEquipmentSlot.Core: loadout.Core = item; break;
                case LandEquipmentSlot.Boots: loadout.Boots = item; break;
                case LandEquipmentSlot.DuffleBag: loadout.DuffleBag = item; break;
                case LandEquipmentSlot.Utility1: loadout.Utility1 = item; break;
                case LandEquipmentSlot.Utility2: loadout.Utility2 = item; break;
                case LandEquipmentSlot.Module1: loadout.Module1 = item; break;
                case LandEquipmentSlot.Module2: loadout.Module2 = item; break;
                case LandEquipmentSlot.Helmet: loadout.Helmet = item; break;
                case LandEquipmentSlot.Shield: loadout.Shield = item; break;
            }
        }

        public static bool IsValidItem(LandGearInstance item) =>
            item != null && !string.IsNullOrEmpty(item.DefinitionId) && item.DefinitionId != "none";
    }
}
