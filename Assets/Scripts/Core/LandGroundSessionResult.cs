using F89.LandCombat;

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
        /// <summary>Set when the player reached 0 HP; screens for these outcomes come later.</summary>
        public LandDownedOutcome DownedOutcome;

        public bool WasDowned => DownedOutcome != LandDownedOutcome.None;

        public static LandGroundSessionResult Empty => default;
    }
}
