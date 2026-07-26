namespace F89.LandCombat
{
    public enum LandEquipmentSlot
    {
        Weapon = 0,
        Core = 1,
        Boots = 2,
        DuffleBag = 3,
        Utility1 = 4,
        Utility2 = 5,
        Module1 = 6,
        Module2 = 7,
        Helmet = 8,
        Shield = 10
    }

    public enum LandItemRarity
    {
        White = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
        Yellow = 4,
        Orange = 5,
        Red = 6,
        Crimson = 7,
        Black = 8,
        Gold = 9
    }

    /// <summary>
    /// Item origin family: M (Military basic loadout), UR (Ultimate Reich loot / R&amp;D fuel),
    /// X (Experimental — created by successful R&amp;D).
    /// </summary>
    public enum LandItemCategory
    {
        Military = 0,
        UltimateReich = 1,
        Experimental = 2
    }

    public enum LandWeaponKind
    {
        Bullet,
        Laser,
        Spread,
        Homing,
        Mortar,
        Melee
    }

    public enum LandEquipResult
    {
        Success,
        InvalidItem,
        WrongSlot,
        InventoryEmpty,
        VaultFull
    }
}
