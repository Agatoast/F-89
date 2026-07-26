namespace F89.LandCombat
{
    public static class LandItemCategoryRules
    {
        public static string GetPrefix(LandItemCategory category) =>
            category switch
            {
                LandItemCategory.Military => "M",
                LandItemCategory.UltimateReich => "UR",
                LandItemCategory.Experimental => "X",
                _ => "?"
            };

        public static bool TryResolve(LandGearInstance item, LandItemCatalog catalog, out LandItemCategory category)
        {
            category = LandItemCategory.Military;
            if (!LandLoadoutSlots.IsValidItem(item) || catalog == null)
            {
                return false;
            }

            if (catalog.TryGetWeapon(item.DefinitionId, out var weapon))
            {
                category = weapon.Category;
                return true;
            }

            if (catalog.TryGetGear(item.DefinitionId, out var gear))
            {
                category = gear.Category;
                return true;
            }

            return false;
        }

        public static bool CanUseForResearch(LandGearInstance item, LandItemCatalog catalog) =>
            TryResolve(item, catalog, out var category) && category == LandItemCategory.UltimateReich;
    }
}
