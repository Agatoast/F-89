namespace F89.Core
{
    public static class FlightMissionLaunchState
    {
        public const float CarrierTakeoffSpeedMph = 280f;

        public static bool LaunchFromCarrier { get; private set; }
        public static string LaunchFromOutpostName { get; private set; } = string.Empty;
        public static bool LaunchOutpostUsesVtolTakeoff { get; private set; }

        public static void BeginCarrierLaunch()
        {
            LaunchFromCarrier = true;
            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
        }

        public static void BeginOutpostLaunch(string outpostName, bool vtolTakeoff = false)
        {
            LaunchFromCarrier = false;
            LaunchFromOutpostName = outpostName ?? string.Empty;
            LaunchOutpostUsesVtolTakeoff = vtolTakeoff;
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

        public static bool TryConsumeOutpostLaunch(out string outpostName, out bool vtolTakeoff)
        {
            outpostName = LaunchFromOutpostName;
            vtolTakeoff = LaunchOutpostUsesVtolTakeoff;
            if (string.IsNullOrEmpty(outpostName))
            {
                return false;
            }

            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
            return true;
        }
    }
}
