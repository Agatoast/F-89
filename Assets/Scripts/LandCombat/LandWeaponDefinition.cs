using UnityEngine;

namespace F89.LandCombat
{
    [CreateAssetMenu(fileName = "LandWeapon", menuName = "F89/Land Combat/Weapon Definition")]
    public sealed class LandWeaponDefinition : ScriptableObject
    {
        public string DisplayName = "Blaster";
        [TextArea(2, 5)]
        public string Description = string.Empty;
        public LandWeaponKind Kind = LandWeaponKind.Bullet;
        public LandItemRarity Rarity = LandItemRarity.White;
        public float Damage = 12f;
        public float FireRate = 1f;
        public float RangeTiles = 8f;
        public float MinRangeTiles;
        public float ProjectileSpeed = 16f;
        public int SpreadCount = 5;
        public float SpreadAngle = 32f;
        public float HomingTurnRate = 240f;
        public Color ProjectileColor = new(0.3f, 0.95f, 1f);
        public float ProjectileScale = 0.18f;
    }
}
