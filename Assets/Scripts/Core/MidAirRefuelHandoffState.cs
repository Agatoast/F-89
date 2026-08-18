using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Flight ↔ mid-air refuel minigame handoff. Separate from ground sortie return paths.
    /// </summary>
    public static class MidAirRefuelHandoffState
    {
        private const string SnapshotPrefsKey = "F89.MidAirRefuelHandoff.Snapshot";
        private const string PendingEnterPrefsKey = "F89.MidAirRefuelHandoff.PendingEnter";
        private const string PendingReturnPrefsKey = "F89.MidAirRefuelHandoff.PendingReturn";

        private static LandSortieSnapshot pendingEnterSnapshot;
        private static LandSortieSnapshot pendingReturnSnapshot;
        private static LandSortieSnapshot storedFlightSnapshot;
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

        public static void ForceReloadFromPrefs()
        {
            restoredFromPrefs = false;
            EnsureRestoredAfterScriptReload();
        }

        public static void BeginEnterFromFlight(LandSortieSnapshot snapshot)
        {
            storedFlightSnapshot = snapshot;
            storedFlightSnapshot.IsValid = true;
            pendingEnterSnapshot = snapshot;
            pendingEnterSnapshot.IsValid = true;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
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

        public static void BeginReturnToFlight(LandSortieSnapshot snapshot)
        {
            storedFlightSnapshot = snapshot;
            storedFlightSnapshot.IsValid = true;
            pendingReturnSnapshot = snapshot;
            pendingReturnSnapshot.IsValid = true;
            SaveToPrefs();
        }

        public static bool TryConsumeReturnToFlight(out LandSortieSnapshot snapshot)
        {
            EnsureRestoredAfterScriptReload();
            snapshot = pendingReturnSnapshot;
            if (!pendingReturnSnapshot.IsValid)
            {
                return false;
            }

            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            SaveToPrefs();
            return true;
        }

        public static void ClearPersistedAfterApplication()
        {
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            storedFlightSnapshot = LandSortieSnapshot.Empty;
            ClearPrefs();
        }

        public static void Clear()
        {
            pendingEnterSnapshot = LandSortieSnapshot.Empty;
            pendingReturnSnapshot = LandSortieSnapshot.Empty;
            storedFlightSnapshot = LandSortieSnapshot.Empty;
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
            PlayerPrefs.Save();
        }

        private static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(SnapshotPrefsKey);
            PlayerPrefs.DeleteKey(PendingEnterPrefsKey);
            PlayerPrefs.DeleteKey(PendingReturnPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
