namespace F89.UI
{
    public static class MissionIncompleteState
    {
        public static bool HasPending { get; private set; }

        public static void Begin()
        {
            HasPending = true;
        }

        public static void Clear()
        {
            HasPending = false;
        }
    }
}
