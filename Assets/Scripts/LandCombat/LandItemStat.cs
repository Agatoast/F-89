namespace F89.LandCombat
{
    /// <summary>
    /// Combat stats for Land items. Land does not use MTAU attribute stats
    /// (Strength, Agility, Vitality, etc.).
    /// </summary>
    public enum LandItemStat
    {
        Damage = 0,
        Range = 1,
        /// <summary>Internal fire cadence; not a displayed Basic Loadout weapon stat.</summary>
        RateOfFire = 2,
        DamageResistance = 3,
        Move = 4
    }
}
