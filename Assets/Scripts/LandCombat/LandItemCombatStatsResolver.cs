namespace F89.LandCombat
{
    /// <summary>
    /// Resolves combat stats from ScriptableObjects or static US/UR catalogs.
    /// </summary>
    public static class LandItemCombatStatsResolver
    {
        public static bool TryGetWeaponStats(
            LandGearInstance item,
            LandItemCatalog catalog,
            out float damage,
            out float range)
        {
            damage = 0f;
            range = 0f;
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                return false;
            }

            if (catalog != null && catalog.TryGetWeapon(item.DefinitionId, out var weapon))
            {
                damage = weapon.Damage;
                range = weapon.Range;
                return true;
            }

            if (LandUsWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var usWeapon))
            {
                damage = usWeapon.Damage;
                range = usWeapon.Range;
                return true;
            }

            if (LandUrWeaponCatalog.TryGetByDefinitionId(item.DefinitionId, out var urWeapon))
            {
                damage = urWeapon.Damage;
                range = urWeapon.Range;
                return true;
            }

            return false;
        }

        public static bool IsResearchUnlockedGear(LandGearInstance item, LandItemCatalog catalog) =>
            LandItemCategoryRules.TryResolve(item, catalog, out var category)
            && category == LandItemCategory.Experimental;
    }
}
