namespace F89.Core
{
    /// <summary>
    /// Thin flight ↔ land handoff contract. Flight code should only call BeginEnterFromFlight
    /// and consume-return helpers here — never land module types directly.
    /// </summary>
    public static class LandMissionHandoffState
    {
        private static LandSortieSnapshot pendingEnterSnapshot;
        private static LandSortieSnapshot pendingReturnSnapshot;
        private static LandSortieSnapshot storedFlightSnapshot;
        private static LandGroundSessionResult lastGroundResult;
        private static bool suppressCarrierRespawn;

        public static bool HasPendingEnter => pendingEnterSnapshot.IsValid;
        public static bool HasPendingReturn => pendingReturnSnapshot.IsValid;
        public static bool HasPendingGroundReturn => pendingReturnSnapshot.IsValid;
        public static bool ShouldSuppressCarrierRespawn => suppressCarrierRespawn;
        public static bool ShouldBlockCarrierSpawn =>
            suppressCarrierRespawn || pendingReturnSnapshot.IsValid;
        public static LandGroundSessionResult LastGroundResult => lastGroundResult;

        public static string PendingReturnSceneName =>
            pendingReturnSnapshot.IsValid && !string.IsNullOrEmpty(pendingReturnSnapshot.ReturnSceneName)
                ? pendingReturnSnapshot.ReturnSceneName
                : GameScenes.FlightTest;

        public static LandSortieSnapshot GetStoredFlightSnapshot() => storedFlightSnapshot;

        public static void BeginEnterFromFlight(LandSortieSnapshot snapshot)
        {
            storedFlightSnapshot = snapshot;
            storedFlightSnapshot.IsValid = true;
            pendingEnterSnapshot = snapshot;
            pendingEnterSnapshot.IsValid = true;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
            suppressCarrierRespawn = false;
        }

        public static bool TryConsumeEnterFromFlight(out LandSortieSnapshot snapshot)
        {
            snapshot = pendingEnterSnapshot;
            if (!pendingEnterSnapshot.IsValid)
            {
                return false;
            }

            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            return true;
        }

        public static void BeginReturnToFlight(LandSortieSnapshot snapshot, LandGroundSessionResult groundResult)
        {
            pendingReturnSnapshot = snapshot;
            pendingReturnSnapshot.IsValid = true;
            lastGroundResult = groundResult;
            suppressCarrierRespawn = true;
        }

        public static bool TryConsumeReturnToFlight(out LandSortieSnapshot snapshot, out LandGroundSessionResult groundResult)
        {
            snapshot = pendingReturnSnapshot;
            groundResult = lastGroundResult;
            if (!pendingReturnSnapshot.IsValid)
            {
                return false;
            }

            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            return true;
        }

        public static void ConfirmReturnApplied()
        {
            suppressCarrierRespawn = false;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
        }

        public static void Clear()
        {
            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            storedFlightSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
            suppressCarrierRespawn = false;
        }
    }
}
