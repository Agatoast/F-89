namespace F89.Weapons
{
    public static class MissileThreatNotifier
    {
        private static int activeAcquisitionSources;

        public static bool HasActiveAcquisition => activeAcquisitionSources > 0;

        public static void RegisterAcquisitionSource()
        {
            activeAcquisitionSources++;
        }

        public static void UnregisterAcquisitionSource()
        {
            if (activeAcquisitionSources <= 0)
            {
                return;
            }

            activeAcquisitionSources--;
        }

        public static bool HasMissilesTargetingPlayer()
        {
            return HomingMissile.HasAnyActivelyTargetingPlayer();
        }

        public static bool ShouldDisplayAcquisitionWarning()
        {
            return HasMissilesTargetingPlayer();
        }

        public static bool ShouldBlinkVisible(float blinkPeriodSeconds = 0.55f)
        {
            if (!HasMissilesTargetingPlayer())
            {
                return false;
            }

            return (UnityEngine.Mathf.FloorToInt(UnityEngine.Time.time / blinkPeriodSeconds) % 2) == 0;
        }

        public static void Clear()
        {
            activeAcquisitionSources = 0;
        }
    }
}
