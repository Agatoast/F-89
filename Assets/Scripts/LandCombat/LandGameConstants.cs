namespace F89.LandCombat
{
    public static class LandGameConstants
    {
        public const float PlayerMoveSpeed = 6f;
        public const float PlayerMaxHealth = 100f;
        public const float WorldUnitsPerTile = 1f;
        public const int ProjectilePoolSize = 256;
        public const float DefaultProjectileLifetime = 4f;
        public const int PackSlotCount = 12;
        public const int DuffleBagSlotCount = 12;
        public const int MainSlotCount = PackSlotCount + DuffleBagSlotCount;
        public const int VaultBaseSlotCount = 30;
        public const int VaultMaxSlotCount = 50;
        public const int VaultGridColumns = 10;
        public const int InventoryGridColumns = 4;
        public const int InventoryGridRows = 3;
    }
}
