using UnityEngine;
using UnityEngine.Serialization;

namespace F89.LandCombat
{
    /// <summary>
    /// Weapon item definition. Primary combat stats are Damage and Range (by tech level).
    /// Damage is raw hit value before DR.
    /// Range is in ground-combat units: 10 = character to left/right screen edge.
    /// </summary>
    [CreateAssetMenu(fileName = "LandWeapon", menuName = "F89/Land Combat/Weapon Definition")]
    public sealed class LandWeaponDefinition : ScriptableObject
    {
        public string DisplayName = "M-4";
        [TextArea(2, 5)]
        public string Description = string.Empty;
        public LandWeaponKind Kind = LandWeaponKind.Bullet;
        public LandItemRarity Rarity = LandItemRarity.White;
        public LandItemCategory Category = LandItemCategory.Military;

        [Header("Combat Stats")]
        [Tooltip("Raw damage on hit before DR is applied.")]
        public float Damage = 10f;

        [Tooltip("Max bullet travel in ground-combat units. 10 units = left/right screen edge.")]
        [FormerlySerializedAs("RangeTiles")]
        public float Range = 6f;

        /// <summary>Legacy alias for <see cref="Range"/> (land units).</summary>
        public float RangeTiles
        {
            get => Range;
            set => Range = value;
        }

        [Header("Fire cadence")]
        [Tooltip("Shots per second (not a displayed weapon stat).")]
        [FormerlySerializedAs("FireRate")]
        public float RateOfFire = 4f;

        public float FireRate
        {
            get => RateOfFire;
            set => RateOfFire = value;
        }

        [Header("Behavior")]
        [Tooltip("Minimum engage distance in ground-combat units.")]
        [FormerlySerializedAs("MinRangeTiles")]
        public float MinRange;
        public float ProjectileSpeed = 22f;
        public int SpreadCount = 5;
        public float SpreadAngle = 32f;
        public float HomingTurnRate = 240f;
        public Color ProjectileColor = new(1f, 0.95f, 0.2f, 1f);
        public float ProjectileScale = 0.18f;
    }
}
