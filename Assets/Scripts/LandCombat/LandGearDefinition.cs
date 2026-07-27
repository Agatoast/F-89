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
        public LandItemCategory Category = LandItemCategory.Military;

        [Header("Combat Stats")]
        [Tooltip("Damage Resistance contributed while equipped. Stacks across helmet/vest/boots.")]
        public int DamageResistance;

        [Tooltip("Boots only: while worn, sets character Move to this value.")]
        public int Move;
    }
}
