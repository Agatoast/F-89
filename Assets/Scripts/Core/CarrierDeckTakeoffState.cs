namespace F89.Core
{
    /// <summary>
    /// One-shot flag: CV ON DECK takeoff should use carrier launch + restore stored fuel/stores,
    /// not the ground-return landing-mile path.
    /// </summary>
    public static class CarrierDeckTakeoffState
    {
        public static bool IsPending { get; private set; }

        public static void Begin() => IsPending = true;

        public static bool Consume()
        {
            var pending = IsPending;
            IsPending = false;
            return pending;
        }

        public static void Clear() => IsPending = false;
    }
}
