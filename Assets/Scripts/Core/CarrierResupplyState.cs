namespace F89.Core
{
    /// <summary>One-shot handoff from CV deck REARM into Aircraft Loadout.</summary>
    public static class CarrierResupplyState
    {
        public static bool IsResupplyFromCarrier { get; private set; }
        public static bool ReturnToDeckMenu { get; private set; }

        public static void BeginDeckRearm()
        {
            IsResupplyFromCarrier = true;
            ReturnToDeckMenu = true;
        }

        public static void Clear()
        {
            IsResupplyFromCarrier = false;
            ReturnToDeckMenu = false;
        }
    }
}
