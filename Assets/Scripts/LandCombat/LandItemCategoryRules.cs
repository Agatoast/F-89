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
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (catalog != null && catalog.TryGetWeapon(item.DefinitionId, out var weapon))
            {
                category = weapon.Category;
                return true;
            }

            if (catalog != null && catalog.TryGetGear(item.DefinitionId, out var gear))
            {
                category = gear.Category;
                return true;
            }

            if (LandUrWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var urWeapon))
            {
                category = urWeapon.Category;
                return true;
            }

            if (LandUrGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var urGear))
            {
                category = urGear.Category;
                return true;
            }

            if (LandUsWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var usWeapon))
            {
                category = usWeapon.Category;
                return true;
            }

            if (LandUsGearCatalog.TryGetByDefinitionId(item.DefinitionId, out var usGear))
            {
                category = usGear.Category;
                return true;
            }

            return false;
        }

        public static bool CanUseForResearch(LandGearInstance item, LandItemCatalog catalog) =>
            TryResolve(item, catalog, out var category) && category == LandItemCategory.UltimateReich;
    }
}
