using UnityEngine;

namespace F89.LandCombat
{
    public static class LandRuntimeContent
    {
        private static LandWeaponDefinition fallbackWeapon;

        public static LandWeaponDefinition GetFallbackBlaster() => GetFallbackWeapon();

        public static LandWeaponDefinition GetFallbackWeapon()
        {
            if (fallbackWeapon != null)
            {
                return fallbackWeapon;
            }

            var loaded = Resources.Load<LandWeaponDefinition>(
                "LandCombat/Content/" + LandUsWeaponCatalog.BasicLoadoutDefinitionId);
            if (loaded != null)
            {
                fallbackWeapon = loaded;
                return fallbackWeapon;
            }

            // Last resort if assets are missing.
            fallbackWeapon = ScriptableObject.CreateInstance<LandWeaponDefinition>();
            fallbackWeapon.name = LandUsWeaponCatalog.BasicLoadoutDefinitionId;
            fallbackWeapon.DisplayName = "M-4";
            fallbackWeapon.Kind = LandWeaponKind.Bullet;
            fallbackWeapon.Category = LandItemCategory.Military;
            fallbackWeapon.Rarity = LandItemRarity.White;
            fallbackWeapon.Damage = 40f;
            fallbackWeapon.Range = 6f;
            fallbackWeapon.RateOfFire = 4f;
            fallbackWeapon.ProjectileSpeed = 22f;
            return fallbackWeapon;
        }
    }
}
