namespace F89.Core
{
    /// <summary>Handoff from a friendly outpost runway into loadout before takeoff.</summary>
    public static class FriendlyOutpostTakeoffState
    {
        public static string OutpostName { get; private set; } = string.Empty;

        public static bool HasPending => !string.IsNullOrEmpty(OutpostName);

        public static void Begin(string outpostName)
        {
            OutpostName = outpostName ?? string.Empty;
        }

        public static bool TryConsume(out string outpostName)
        {
            outpostName = OutpostName;
            if (string.IsNullOrEmpty(outpostName))
            {
                return false;
            }

            OutpostName = string.Empty;
            return true;
        }

        public static void Clear() => OutpostName = string.Empty;
    }
}
