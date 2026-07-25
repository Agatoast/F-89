namespace F89.Core
{
    public static class FlightMissionLaunchState
    {
        public const float CarrierTakeoffSpeedMph = 280f;

        public static bool LaunchFromCarrier { get; private set; }

        public static void BeginCarrierLaunch()
        {
            LaunchFromCarrier = true;
        }

        public static bool TryConsumeCarrierLaunch()
        {
            if (!LaunchFromCarrier)
            {
                return false;
            }

            LaunchFromCarrier = false;
            return true;
        }
    }
}
