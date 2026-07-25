namespace F89.Core
{
    /// <summary>
    /// Summary of a completed (or failed) ground session, written when returning to flight.
    /// </summary>
    public struct LandGroundSessionResult
    {
        public bool CompletedVoluntarily;
        public int TroopsKilled;
        public int ScoreEarned;

        public static LandGroundSessionResult Empty => default;
    }
}
