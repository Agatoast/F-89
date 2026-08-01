namespace F89.Core
{
    /// <summary>Handoff from a runway deck into Aircraft Loadout.</summary>
    public static class FriendlyOutpostTakeoffState
    {
        public static string OutpostName { get; private set; } = string.Empty;
        public static bool ReturnToDeckMenu { get; private set; }

        public static bool HasPending => !string.IsNullOrEmpty(OutpostName);

        public static void BeginDeckRearm(string outpostName)
        {
            OutpostName = outpostName ?? string.Empty;
            ReturnToDeckMenu = true;
        }

        public static bool TryConsume(out string outpostName, out bool returnToDeckMenu)
        {
            outpostName = OutpostName;
            returnToDeckMenu = ReturnToDeckMenu;
            if (string.IsNullOrEmpty(outpostName))
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
