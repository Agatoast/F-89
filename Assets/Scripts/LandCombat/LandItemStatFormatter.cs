namespace F89.LandCombat
{
    public static class LandItemStatFormatter
    {
        public static string FormatStatLabel(LandItemStat stat) =>
            stat switch
            {
                LandItemStat.Damage => "DMG",
                LandItemStat.Range => "RNG",
                LandItemStat.RateOfFire => "ROF",
                LandItemStat.DamageResistance => "DR",
                LandItemStat.Move => "MOVE",
                _ => stat.ToString()
            };

        public static string FormatWeaponSummary(LandWeaponDefinition weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            return $"{FormatStatLabel(LandItemStat.Damage)} {weapon.Damage:0}  |  {FormatStatLabel(LandItemStat.Range)} {weapon.Range:0}";
        }

        public static string FormatGearSummary(LandGearDefinition gear)
        {
            if (gear == null)
            {
                return string.Empty;
            }

            if (gear.Slot == LandEquipmentSlot.Boots)
            {
                return $"{FormatStatLabel(LandItemStat.Move)} {gear.Move}  |  {FormatStatLabel(LandItemStat.DamageResistance)} {gear.DamageResistance}";
            }

            return $"{FormatStatLabel(LandItemStat.DamageResistance)} {gear.DamageResistance}";
        }

        public static string FormatAffix(LandRolledAffix affix)
        {
            if (affix == null)
            {
                return string.Empty;
            }

            return $"+{affix.Value:0.##} {FormatStatLabel(affix.Stat)}";
        }
    }
}
