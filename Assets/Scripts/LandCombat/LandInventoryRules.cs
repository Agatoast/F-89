using System.Collections.Generic;

namespace F89.LandCombat
{
    public static class LandInventoryRules
    {
        public static int GetDuffleBagStartIndex() => LandGameConstants.PackSlotCount;

        public static bool IsDuffleBagIndex(int index) =>
            index >= LandGameConstants.PackSlotCount && index < LandGameConstants.MainSlotCount;

        public static bool HasEquippedDuffleBag(LandGearInstance duffleBag) =>
            LandLoadoutSlots.IsValidItem(duffleBag);

        public static int GetAccessibleSlotCount(LandGearInstance duffleBag) =>
            HasEquippedDuffleBag(duffleBag) ? LandGameConstants.MainSlotCount : LandGameConstants.PackSlotCount;

        public static bool IsInventoryIndexAccessible(int index, LandGearInstance duffleBag)
        {
            if (index < 0 || index >= LandGameConstants.MainSlotCount)
            {
                return false;
            }

            return index < LandGameConstants.PackSlotCount || HasEquippedDuffleBag(duffleBag);
        }

        public static void EnsureInventoryCapacity(LandRunLoadout loadout)
        {
            if (loadout == null)
            {
                return;
            }

            loadout.Inventory ??= new List<LandGearInstance>();
            while (loadout.Inventory.Count < LandGameConstants.MainSlotCount)
            {
                loadout.Inventory.Add(null);
            }
        }
    }
}
