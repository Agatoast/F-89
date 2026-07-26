namespace F89.LandCombat
{
    public static class LandItemStatFormatter
    {
        public static string FormatStatLabel(LandItemStat stat) =>
            stat switch
            {
                LandItemStat.Damage => "DMG",
                LandItemStat.RateOfFire => "ROF",
                _ => stat.ToString()
            };

        public static string FormatWeaponSummary(LandWeaponDefinition weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            return $"{FormatStatLabel(LandItemStat.Damage)} {weapon.Damage:0}  |  {FormatStatLabel(LandItemStat.RateOfFire)} {weapon.RateOfFire:0.##}";
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
