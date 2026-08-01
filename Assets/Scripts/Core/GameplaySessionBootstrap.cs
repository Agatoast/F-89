namespace F89.Core
{
    /// <summary>Clears Editor-session handoff that breaks carrier spawn, terrain, and weapons in release builds.</summary>
    public static class GameplaySessionBootstrap
    {
        public static void ClearStalePersistedSession()
        {
            LandMissionHandoffState.Clear();
            LandingMileFlagState.Clear();
            OutpostRunwayDeckState.Clear();
            CarrierResupplyState.Clear();
            FriendlyOutpostTakeoffState.Clear();
            DeckLandingServiceState.Clear();
            F89.LandCombat.OpenFieldLandingState.Clear();
        }
    }
}
