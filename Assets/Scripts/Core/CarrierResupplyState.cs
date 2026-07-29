namespace F89.Core
{
    /// <summary>One-shot handoff from CV deck choice into Aircraft Loadout for refuel/rearm.</summary>
    public static class CarrierResupplyState
    {
        public static bool IsResupplyFromCarrier { get; private set; }

        public static void Begin()
        {
            IsResupplyFromCarrier = true;
        }

        public static void Clear()
        {
            IsResupplyFromCarrier = false;
        }
    }
}
