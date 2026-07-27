using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Thin flight ↔ land handoff contract. Flight code should only call BeginEnterFromFlight
    /// and consume-return helpers here — never land module types directly.
    /// </summary>
    public static class LandMissionHandoffState
    {
        private const string SnapshotPrefsKey = "F89.LandMissionHandoff.Snapshot";
        private const string PendingEnterPrefsKey = "F89.LandMissionHandoff.PendingEnter";
        private const string PendingReturnPrefsKey = "F89.LandMissionHandoff.PendingReturn";
        private const string SuppressCarrierPrefsKey = "F89.LandMissionHandoff.SuppressCarrier";

        private static LandSortieSnapshot pendingEnterSnapshot;
        private static LandSortieSnapshot pendingReturnSnapshot;
        private static LandSortieSnapshot storedFlightSnapshot;
        private static LandGroundSessionResult lastGroundResult;
        private static bool suppressCarrierRespawn;
        private static bool restoredFromPrefs;

        public static bool HasPendingEnter
        {
            get
            {
                EnsureRestoredAfterScriptReload();
                return pendingEnterSnapshot.IsValid;
            }
        }

        public static bool HasPendingReturn
        {
            get
            {
                EnsureRestoredAfterScriptReload();
                return pendingReturnSnapshot.IsValid;
            }
        }

        public static bool HasPendingGroundReturn => HasPendingReturn;
        public static bool ShouldSuppressCarrierRespawn
        {
            get
            {
                EnsureRestoredAfterScriptReload();
                return suppressCarrierRespawn;
            }
        }

        public static bool ShouldBlockCarrierSpawn =>
            ShouldSuppressCarrierRespawn || HasPendingReturn;
        public static LandGroundSessionResult LastGroundResult => lastGroundResult;

        public static string PendingReturnSceneName =>
            HasPendingReturn && !string.IsNullOrEmpty(pendingReturnSnapshot.ReturnSceneName)
                ? pendingReturnSnapshot.ReturnSceneName
                : GameScenes.FlightTest;

        public static LandSortieSnapshot GetStoredFlightSnapshot()
        {
            EnsureRestoredAfterScriptReload();
            return storedFlightSnapshot;
        }

        public static bool TryGetPendingReturnSnapshot(out LandSortieSnapshot snapshot)
        {
            EnsureRestoredAfterScriptReload();
            snapshot = pendingReturnSnapshot.IsValid
                ? pendingReturnSnapshot
                : suppressCarrierRespawn
                    ? storedFlightSnapshot
                    : LandSortieSnapshot.Empty;
            return snapshot.IsValid;
        }

        public static void BeginEnterFromFlight(LandSortieSnapshot snapshot)
        {
            storedFlightSnapshot = snapshot;
            storedFlightSnapshot.IsValid = true;
            pendingEnterSnapshot = snapshot;
            pendingEnterSnapshot.IsValid = true;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
            suppressCarrierRespawn = false;
            SaveToPrefs();
        }

        public static bool TryConsumeEnterFromFlight(out LandSortieSnapshot snapshot)
        {
            EnsureRestoredAfterScriptReload();
            snapshot = pendingEnterSnapshot;
            if (!pendingEnterSnapshot.IsValid)
            {
                return false;
            }

            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            SaveToPrefs();
            return true;
        }

        public static void BeginReturnToFlight(LandSortieSnapshot snapshot, LandGroundSessionResult groundResult)
        {
            pendingReturnSnapshot = snapshot;
            pendingReturnSnapshot.IsValid = true;
            lastGroundResult = groundResult;
            suppressCarrierRespawn = true;
            SaveToPrefs();
        }

        public static bool TryConsumeReturnToFlight(out LandSortieSnapshot snapshot, out LandGroundSessionResult groundResult)
        {
            EnsureRestoredAfterScriptReload();
            snapshot = pendingReturnSnapshot;
            groundResult = lastGroundResult;
            if (!pendingReturnSnapshot.IsValid)
            {
                return false;
            }

            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            SaveToPrefs();
            return true;
        }

        public static void ConfirmReturnApplied()
        {
            EnsureRestoredAfterScriptReload();
            suppressCarrierRespawn = false;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            ClearPrefs();
        }

        /// <summary>
        /// The return is now active in the loaded flight scene. Keep its transient in-memory
        /// state for startup sequencing, but never let it become the next game session's spawn.
        /// </summary>
        public static void ClearPersistedReturnAfterApplication()
        {
            ClearPrefs();
        }

        public static void Clear()
        {
            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            storedFlightSnapshot = LandSortieSnapshot.Empty;
            lastGroundResult = LandGroundSessionResult.Empty;
            suppressCarrierRespawn = false;
            ClearPrefs();
        }

        private static void EnsureRestoredAfterScriptReload()
        {
            if (restoredFromPrefs)
            {
                return;
            }

            restoredFromPrefs = true;
            var json = PlayerPrefs.GetString(SnapshotPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var snapshot = JsonUtility.FromJson<LandSortieSnapshot>(json);
            if (!snapshot.IsValid)
            {
                ClearPrefs();
                return;
            }

            storedFlightSnapshot = snapshot;
            if (PlayerPrefs.GetInt(PendingEnterPrefsKey, 0) != 0)
            {
                pendingEnterSnapshot = snapshot;
            }

            if (PlayerPrefs.GetInt(PendingReturnPrefsKey, 0) != 0)
            {
                pendingReturnSnapshot = snapshot;
            }

            suppressCarrierRespawn = PlayerPrefs.GetInt(SuppressCarrierPrefsKey, 0) != 0;
        }

        private static void SaveToPrefs()
        {
            if (!storedFlightSnapshot.IsValid)
            {
                ClearPrefs();
                return;
            }

            PlayerPrefs.SetString(SnapshotPrefsKey, JsonUtility.ToJson(storedFlightSnapshot));
            PlayerPrefs.SetInt(PendingEnterPrefsKey, pendingEnterSnapshot.IsValid ? 1 : 0);
            PlayerPrefs.SetInt(PendingReturnPrefsKey, pendingReturnSnapshot.IsValid ? 1 : 0);
            PlayerPrefs.SetInt(SuppressCarrierPrefsKey, suppressCarrierRespawn ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(SnapshotPrefsKey);
            PlayerPrefs.DeleteKey(PendingEnterPrefsKey);
            PlayerPrefs.DeleteKey(PendingReturnPrefsKey);
            PlayerPrefs.DeleteKey(SuppressCarrierPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
