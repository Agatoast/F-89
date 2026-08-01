namespace F89.Core
{
    /// <summary>
    /// Tracks refuel/rearm used during the current deck stay. Reset when the aircraft lands again.
    /// </summary>
    public static class DeckLandingServiceState
    {
        public static bool RefuelUsedThisLanding { get; private set; }
        public static bool RearmUsedThisLanding { get; private set; }

        public static void ResetForNewLanding()
        {
            RefuelUsedThisLanding = false;
            RearmUsedThisLanding = false;
        }

        public static void MarkRefuelUsed() => RefuelUsedThisLanding = true;

        public static void MarkRearmUsed() => RearmUsedThisLanding = true;

        public static void Clear()
        {
            RefuelUsedThisLanding = false;
            RearmUsedThisLanding = false;
        }
    }

}
