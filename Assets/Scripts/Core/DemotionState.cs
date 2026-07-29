namespace F89.Core
{
    public static class DemotionState
    {
        public static bool HasPending { get; private set; }
        public static string PreviousRank { get; private set; } = string.Empty;
        public static string NewRank { get; private set; } = string.Empty;

        public static void Begin(string previousRank, string newRank)
        {
            PreviousRank = previousRank ?? string.Empty;
            NewRank = newRank ?? string.Empty;
            HasPending = true;
        }

        public static void Clear()
        {
            HasPending = false;
            PreviousRank = string.Empty;
            NewRank = string.Empty;
        }
    }
}
