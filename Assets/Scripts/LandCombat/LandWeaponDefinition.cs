using UnityEngine;
using UnityEngine.Serialization;

namespace F89.LandCombat
{
    /// <summary>
    /// Weapon item definition for Land. Combat stats are Damage and RateOfFire (ROF) —
    /// not MTAU attribute stats.
    /// </summary>
    [CreateAssetMenu(fileName = "LandWeapon", menuName = "F89/Land Combat/Weapon Definition")]
    public sealed class LandWeaponDefinition : ScriptableObject
    {
        public string DisplayName = "Blaster";
        [TextArea(2, 5)]
        public string Description = string.Empty;
        public LandWeaponKind Kind = LandWeaponKind.Bullet;
        public LandItemRarity Rarity = LandItemRarity.White;
        public LandItemCategory Category = LandItemCategory.Military;

        [Header("Combat Stats")]
        [Tooltip("Damage per hit.")]
        public float Damage = 12f;

        [Tooltip("Rate of fire (ROF) in shots per second.")]
        [FormerlySerializedAs("FireRate")]
        public float RateOfFire = 1f;

        public float FireRate
        {
            get => RateOfFire;
            set => RateOfFire = value;
        }

        [Header("Behavior")]
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
