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
        public float MaxHealthBonus;
        public float VelocityBonus;
        public float StrengthMod;
        public float AgilityMod;
        public float VitalityMod;
        public float IntelligenceMod;
        public float WisdomMod;
        public float DefenseMod;
    }
}
