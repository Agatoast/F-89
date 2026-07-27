namespace F89.Core
{
    public static class AutoFireState
    {
        public static bool Enabled { get; private set; }

        public static void Toggle()
        {
            Enabled = !Enabled;
        }

        public static string StatusLabel => Enabled ? "AUTO ON" : "AUTO OFF";
    }
}
