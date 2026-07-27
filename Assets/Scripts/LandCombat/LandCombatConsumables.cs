namespace F89.LandCombat
{
    /// <summary>Runtime Bandages / Grenade counts for the ground HUD consumable boxes.</summary>
    public static class LandCombatConsumables
    {
        public static int BandageCount { get; private set; }
        public static int GrenadeCount { get; private set; }

        public static void ResetForMission()
        {
            BandageCount = LandGameConstants.BandageSlotCapacity;
            GrenadeCount = LandGameConstants.GrenadeSlotCapacity;
        }

        public static bool TryAddBandage()
        {
            if (BandageCount >= LandGameConstants.BandageSlotCapacity)
            {
                return false;
            }

            BandageCount++;
            return true;
        }

        public static bool TryAddGrenade()
        {
            if (GrenadeCount >= LandGameConstants.GrenadeSlotCapacity)
            {
                return false;
            }

            GrenadeCount++;
            return true;
        }

        public static bool TryUseBandage()
        {
            if (BandageCount <= 0)
            {
                return false;
            }

            BandageCount--;
            return true;
        }

        public static bool TryUseGrenade()
        {
            if (LandCombatTestCheats.UnlimitedGrenades)
            {
                return true;
            }

            if (GrenadeCount <= 0)
            {
                return false;
            }

            GrenadeCount--;
            return true;
        }
    }
}
