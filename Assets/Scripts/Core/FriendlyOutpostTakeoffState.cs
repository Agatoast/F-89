namespace F89.Core
{
    /// <summary>Handoff from a friendly runway into Aircraft Loadout.</summary>
    public static class FriendlyOutpostTakeoffState
    {
        public static string OutpostName { get; private set; } = string.Empty;
        public static bool ReturnToDeckMenu { get; private set; }

        /// <summary>Loadout should offer return to the parked friendly runway.</summary>
        public static bool HasPending => ReturnToDeckMenu;

        public static void BeginDeckRearm(string outpostName)
        {
            CarrierResupplyState.Clear();
            OutpostName = outpostName ?? string.Empty;
            ReturnToDeckMenu = true;
        }

        public static bool TryConsume(out string outpostName, out bool returnToDeckMenu)
        {
            outpostName = OutpostName;
            returnToDeckMenu = ReturnToDeckMenu;
            if (!ReturnToDeckMenu)
            {
                return false;
            }

            OutpostName = string.Empty;
            ReturnToDeckMenu = false;
            return true;
        }

        public static void Clear()
        {
            OutpostName = string.Empty;
            ReturnToDeckMenu = false;
        }
    }
}
