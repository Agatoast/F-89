namespace F89.LandCombat
{
    /// <summary>One-shot handoff from a cleared boss bunker to its surface area.</summary>
    public static class LandMissionCompleteState
    {
        public static string CompletedBunkerCode { get; private set; } = string.Empty;
        public static string ReturnSurfaceCode { get; private set; } = string.Empty;
        public static bool IsCarrierLanding { get; private set; }

        public static void Begin(string bunkerCode, string surfaceCode)
        {
            CompletedBunkerCode = bunkerCode;
            ReturnSurfaceCode = surfaceCode;
            IsCarrierLanding = false;
        }

        public static void BeginCarrierLanding()
        {
            CompletedBunkerCode = "USS MARTIN VAN BUREN";
            ReturnSurfaceCode = string.Empty;
            IsCarrierLanding = true;
        }

        public static void Clear()
        {
            CompletedBunkerCode = string.Empty;
            ReturnSurfaceCode = string.Empty;
            IsCarrierLanding = false;
        }
    }
}
