namespace F89.LandCombat
{
    /// <summary>Lootable consumables that fill HUD Bandages / Grenades boxes (not inventory).</summary>
    public static class LandConsumableIds
    {
        public const string Bandage = "Bandage";
        public const string Grenade = "Grenade";

        public static bool IsConsumable(string definitionId) =>
            definitionId == Bandage || definitionId == Grenade;

        public static bool IsConsumable(LandGearInstance item) =>
            item != null && IsConsumable(item.DefinitionId);

        public static bool IsBandage(LandGearInstance item) =>
            item != null && item.DefinitionId == Bandage;

        public static bool IsGrenade(LandGearInstance item) =>
            item != null && item.DefinitionId == Grenade;
    }
}
