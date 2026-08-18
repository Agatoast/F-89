namespace F89.Core
{
    /// <summary>Clears Editor-session handoff that breaks carrier spawn, terrain, and weapons in release builds.</summary>
    public static class GameplaySessionBootstrap
    {
        /// <summary>Clears ground-return / deck handoff only — never clears explicit loadout launch intent.</summary>
        public static void ClearSessionHandoffOnly()
        {
            LandMissionHandoffState.Clear();
            LandingMileFlagState.Clear();
            OutpostRunwayDeckState.Clear();
            CarrierResupplyState.Clear();
            FriendlyOutpostTakeoffState.Clear();
            DeckLandingServiceState.Clear();
            F89.LandCombat.OpenFieldLandingState.Clear();
        }

        public static void ClearStalePersistedSession()
        {
            ClearSessionHandoffOnly();
            FlightMissionLaunchState.Clear();
        }
    }
}
