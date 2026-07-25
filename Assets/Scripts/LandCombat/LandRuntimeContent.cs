using UnityEngine;

namespace F89.LandCombat
{
    public static class LandRuntimeContent
    {
        private static LandWeaponDefinition fallbackBlaster;

        public static LandWeaponDefinition GetFallbackBlaster()
        {
            if (fallbackBlaster != null)
            {
                return fallbackBlaster;
            }

            var loaded = Resources.Load<LandWeaponDefinition>("LandCombat/Content/Blaster");
            if (loaded != null)
            {
                fallbackBlaster = loaded;
                return fallbackBlaster;
            }

            fallbackBlaster = ScriptableObject.CreateInstance<LandWeaponDefinition>();
            fallbackBlaster.name = "RuntimeBlaster";
            fallbackBlaster.DisplayName = "Blaster";
            fallbackBlaster.Kind = LandWeaponKind.Bullet;
            fallbackBlaster.Damage = 12f;
            fallbackBlaster.FireRate = 1f;
            fallbackBlaster.RangeTiles = 8f;
            fallbackBlaster.ProjectileSpeed = 16f;
            return fallbackBlaster;
        }
    }
}
