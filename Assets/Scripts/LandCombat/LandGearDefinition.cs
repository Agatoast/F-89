using UnityEngine;

namespace F89.LandCombat
{
    [CreateAssetMenu(fileName = "LandGear", menuName = "F89/Land Combat/Gear Definition")]
    public sealed class LandGearDefinition : ScriptableObject
    {
        public string DisplayName = "Gear";
        [TextArea(2, 5)]
        public string Description = string.Empty;
        public LandEquipmentSlot Slot = LandEquipmentSlot.Core;
        public LandItemRarity Rarity = LandItemRarity.White;
        // Non-weapon gear has no MTAU attribute stats. Weapon items use LandWeaponDefinition
        // for Damage and RateOfFire (ROF).
    }
}
