namespace F89.LandCombat
{
    public static class LandGearEquipRules
    {
        public static bool CanEquip(LandEquipmentSlot itemSlot, LandEquipmentSlot paperdollSlot) =>
            itemSlot == paperdollSlot;

        public static string GetSlotDisplayName(LandEquipmentSlot slot) =>
            slot switch
            {
                LandEquipmentSlot.Utility1 => "Utility 1",
                LandEquipmentSlot.Utility2 => "Utility 2",
                LandEquipmentSlot.Module2 => "Right Shoulder",
                LandEquipmentSlot.Helmet => "Helmet",
                LandEquipmentSlot.Module1 => "Left Shoulder",
                LandEquipmentSlot.Core => "Torso",
                LandEquipmentSlot.Weapon => "Weapon",
                LandEquipmentSlot.Shield => "Shield",
                LandEquipmentSlot.Boots => "Boots",
                LandEquipmentSlot.DuffleBag => "Duffle Bag",
                _ => slot.ToString()
            };
    }
}
