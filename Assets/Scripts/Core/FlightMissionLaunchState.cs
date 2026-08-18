using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    public static class FlightMissionLaunchState
    {
        private const string PrefsLaunchCarrier = "F89.FlightLaunch.Carrier";
        private const string PrefsLaunchOutpost = "F89.FlightLaunch.Outpost";
        private const string PrefsLaunchVtol = "F89.FlightLaunch.Vtol";
        private const string PrefsLaunchFresh = "F89.FlightLaunch.Fresh";
        private const string PrefsForceCarrierDeck = "F89.ForceCarrierDeckLaunch";

        public const float CarrierTakeoffSpeedMph = 280f;

        /// <summary>All VTOL takeoffs reach this speed when the scale-up animation completes.</summary>
        public const float VtolTakeoffSpeedMph = CarrierTakeoffSpeedMph;

        public static bool LaunchFromCarrier { get; private set; }
        public static string LaunchFromOutpostName { get; private set; } = string.Empty;
        public static bool LaunchOutpostUsesVtolTakeoff { get; private set; }
        public static bool FreshSortieLaunch { get; private set; }

        public static bool HasPendingCarrierLaunch => LaunchFromCarrier;
        public static bool HasPendingLaunch => LaunchFromCarrier || !string.IsNullOrEmpty(LaunchFromOutpostName);
        public static bool HasPendingOutpostLaunch => !string.IsNullOrWhiteSpace(LaunchFromOutpostName);

        public static bool TryGetPersistedOutpostLaunch(out string outpostName)
        {
            outpostName = PlayerPrefs.GetString(PrefsLaunchOutpost, string.Empty) ?? string.Empty;
            outpostName = outpostName.Trim();
            return !string.IsNullOrEmpty(outpostName);
        }

        public static void EnsureOutpostLaunchRestoredFromPrefs()
        {
            if (IsExplicitCarrierSortiePending()
                || HasPendingOutpostLaunch
                || !TryGetPersistedOutpostLaunch(out var outpostName))
            {
                return;
            }

            FreshSortieLaunch = PlayerPrefs.GetInt(PrefsLaunchFresh, 0) != 0;
            LaunchFromCarrier = false;
            LaunchFromOutpostName = outpostName;
            LaunchOutpostUsesVtolTakeoff = PlayerPrefs.GetInt(PrefsLaunchVtol, 1) != 0;
            ClearForceCarrierDeckLaunch();
        }

        public static bool HasPersistedOutpostLaunch() => TryGetPersistedOutpostLaunch(out _);

        /// <summary>Loadout or CV deck menu queued an ocean carrier catapult — beats stale ground-return handoff.</summary>
        public static bool IsExplicitCarrierSortiePending() =>
            LaunchFromCarrier
            || HasForceCarrierDeckLaunch()
            || HasPersistedFreshCarrierLaunch();

        /// <summary>Loadout set an outpost runway launch — stale carrier override prefs must not win.</summary>
        public static bool ShouldHonorOutpostLaunch()
        {
            if (FlightGroundReturnService.ShouldApplySortieReturn()
                || LandMissionHandoffState.HasPendingGroundReturn)
            {
                return false;
            }

            if (IsExplicitCarrierSortiePending())
            {
                return false;
            }

            if (HasPendingOutpostLaunch || HasPersistedOutpostLaunch())
            {
                return true;
            }

            return MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave);
        }

        /// <summary>Drop stale carrier PlayerPrefs when a last-landed base is on the save.</summary>
        public static void ClearStaleCarrierLaunchIntentFromPrefs()
        {
            if (!MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave))
            {
                return;
            }

            // Honor explicit loadout carrier resupply.
            if (LaunchFromCarrier && HasForceCarrierDeckLaunch())
            {
                return;
            }

            LaunchFromCarrier = false;
            ClearForceCarrierDeckLaunch();
            PlayerPrefs.DeleteKey(PrefsLaunchCarrier);
            PlayerPrefs.DeleteKey(PrefsLaunchFresh);
            PlayerPrefs.Save();
        }

        public static void ClearStaleCarrierOverrideWhenOutpostPending()
        {
            if (!ShouldHonorOutpostLaunch())
            {
                return;
            }

            ClearStaleCarrierLaunchIntent();
        }

        /// <summary>Ground return / landing-mile takeoff must not lose to stale carrier or outpost launch prefs.</summary>
        public static void ClearStaleCarrierWhenSortieReturnPending()
        {
            LandMissionHandoffState.ForceReloadFromPrefs();
            LandingMileFlagState.ForceReloadFromPrefs();
            if (!FlightGroundReturnService.ShouldApplySortieReturn()
                && !FlightGroundReturnService.BlocksCarrierDeckTakeoff()
                && !LandMissionHandoffState.ShouldSuppressCarrierRespawn
                && !LandMissionHandoffState.HasPendingGroundReturn)
            {
                return;
            }

            LaunchFromCarrier = false;
            FreshSortieLaunch = false;
            ClearStaleOutpostLaunchIntent();
            ClearForceCarrierDeckLaunch();
            PlayerPrefs.DeleteKey(PrefsLaunchFresh);
            PlayerPrefs.DeleteKey(PrefsLaunchCarrier);
            PlayerPrefs.Save();
        }

        private static void ClearStaleOutpostLaunchIntent()
        {
            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
            PlayerPrefs.DeleteKey(PrefsLaunchOutpost);
            PlayerPrefs.DeleteKey(PrefsLaunchVtol);
        }

        private static void ClearStaleCarrierLaunchIntent()
        {
            ClearForceCarrierDeckLaunch();
            if (!LaunchFromCarrier)
            {
                return;
            }

            LaunchFromCarrier = false;
            PersistPendingLaunch();
        }

        public static void BeginCarrierLaunch()
        {
            LaunchFromCarrier = true;
            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
            PersistPendingLaunch();
        }

        /// <summary>Loadout → Start Mission: full tanks; never restore a prior deck snapshot.</summary>
        public static void BeginFreshSortieLaunch(string outpostName = null, bool vtolTakeoff = true)
        {
            FreshSortieLaunch = true;
            if (!string.IsNullOrWhiteSpace(outpostName))
            {
                BeginOutpostLaunch(outpostName, vtolTakeoff);
                return;
            }

            BeginCarrierLaunch();
        }

        public static bool ConsumeFreshSortieLaunch()
        {
            var wasFresh = FreshSortieLaunch;
            FreshSortieLaunch = false;
            PersistPendingLaunch();
            return wasFresh;
        }

        public static void Clear()
        {
            LaunchFromCarrier = false;
            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
            FreshSortieLaunch = false;
            ClearPendingLaunchPrefs();
        }

        public static void BeginOutpostLaunch(string outpostName, bool vtolTakeoff = true)
        {
            LaunchFromCarrier = false;
            LaunchFromOutpostName = outpostName ?? string.Empty;
            LaunchOutpostUsesVtolTakeoff = vtolTakeoff;
            ClearForceCarrierDeckLaunch();
            PersistPendingLaunch();
        }

        public static bool TryConsumeCarrierLaunch()
        {
            if (!LaunchFromCarrier)
            {
                return false;
            }

            LaunchFromCarrier = false;
            SyncPendingLaunchPrefsAfterConsume();
            return true;
        }

        public static bool TryConsumeOutpostLaunch(out string outpostName, out bool vtolTakeoff)
        {
            outpostName = LaunchFromOutpostName;
            vtolTakeoff = LaunchOutpostUsesVtolTakeoff;
            if (string.IsNullOrEmpty(outpostName))
            {
                return false;
            }

            LaunchFromOutpostName = string.Empty;
            LaunchOutpostUsesVtolTakeoff = false;
            SyncPendingLaunchPrefsAfterConsume();
            return true;
        }

        /// <summary>Restores loadout launch intent if in-memory static state was lost during scene load.</summary>
        public static void TryRestorePendingLaunchFromPrefs()
        {
            if (HasPendingLaunch)
            {
                return;
            }

            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return;
            }

            // Character save beats stale carrier PlayerPrefs after a cold start.
            if (MissionLaunchOrigin.TryResolveLaunchOutpost(CharacterSessionState.ActiveSave, out var savedOutpost)
                || (MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave)
                    && !string.IsNullOrWhiteSpace(
                        CharacterSessionState.ActiveSave.MissionLaunchOutpostName)))
            {
                if (string.IsNullOrWhiteSpace(savedOutpost))
                {
                    savedOutpost = CharacterSessionState.ActiveSave.MissionLaunchOutpostName.Trim();
                }

                FreshSortieLaunch = PlayerPrefs.GetInt(PrefsLaunchFresh, 0) != 0;
                LaunchOutpostUsesVtolTakeoff = PlayerPrefs.GetInt(PrefsLaunchVtol, 1) != 0;
                LaunchFromCarrier = false;
                LaunchFromOutpostName = savedOutpost;
                ClearForceCarrierDeckLaunch();
                PlayerPrefs.DeleteKey(PrefsLaunchCarrier);
                PlayerPrefs.Save();
                Debug.LogWarning(
                    "[F-89] Restored last-landed base launch from character save: '" + savedOutpost + "'.");
                return;
            }

            var carrier = PlayerPrefs.GetInt(PrefsLaunchCarrier, 0) != 0;
            var outpost = PlayerPrefs.GetString(PrefsLaunchOutpost, string.Empty) ?? string.Empty;
            var fresh = PlayerPrefs.GetInt(PrefsLaunchFresh, 0) != 0;
            if (!carrier && string.IsNullOrWhiteSpace(outpost) && !fresh)
            {
                return;
            }

            FreshSortieLaunch = fresh;
            LaunchOutpostUsesVtolTakeoff = PlayerPrefs.GetInt(PrefsLaunchVtol, 1) != 0;
            if (!string.IsNullOrWhiteSpace(outpost))
            {
                LaunchFromCarrier = false;
                LaunchFromOutpostName = outpost.Trim();
                ClearForceCarrierDeckLaunch();
            }
            else if (carrier)
            {
                if (MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave)
                    && !HasForceCarrierDeckLaunch())
                {
                    var lastLandedBase = CharacterSessionState.ActiveSave.MissionLaunchOutpostName.Trim();
                    LaunchFromCarrier = false;
                    LaunchFromOutpostName = lastLandedBase;
                    ClearForceCarrierDeckLaunch();
                    PlayerPrefs.DeleteKey(PrefsLaunchCarrier);
                    PlayerPrefs.Save();
                    Debug.LogWarning(
                        "[F-89] Ignored stale carrier launch prefs — restored last-landed base '"
                        + lastLandedBase
                        + "'.");
                    return;
                }

                LaunchFromCarrier = true;
                LaunchFromOutpostName = string.Empty;
            }

            Debug.LogWarning("[F-89] Restored pending flight launch intent from PlayerPrefs.");
        }

        public static bool HasPersistedFreshCarrierLaunch()
        {
            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return false;
            }

            return PlayerPrefs.GetInt(PrefsLaunchFresh, 0) != 0
                && PlayerPrefs.GetInt(PrefsLaunchCarrier, 0) != 0;
        }

        /// <summary>Set by loadout Start Mission — bootstrap must honor carrier deck catapult even if handoff prefs linger.</summary>
        public static void MarkForceCarrierDeckLaunch()
        {
            PlayerPrefs.SetInt(PrefsForceCarrierDeck, 1);
            PlayerPrefs.Save();
        }

        public static bool HasForceCarrierDeckLaunch()
        {
            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return false;
            }

            return PlayerPrefs.GetInt(PrefsForceCarrierDeck, 0) != 0;
        }

        public static void ClearForceCarrierDeckLaunch()
        {
            PlayerPrefs.DeleteKey(PrefsForceCarrierDeck);
            PlayerPrefs.Save();
        }

        public static void ClearPendingLaunchPrefs()
        {
            PlayerPrefs.DeleteKey(PrefsLaunchCarrier);
            PlayerPrefs.DeleteKey(PrefsLaunchOutpost);
            PlayerPrefs.DeleteKey(PrefsLaunchVtol);
            PlayerPrefs.DeleteKey(PrefsLaunchFresh);
            PlayerPrefs.Save();
        }

        private static void PersistPendingLaunch()
        {
            PlayerPrefs.SetInt(PrefsLaunchCarrier, LaunchFromCarrier ? 1 : 0);
            PlayerPrefs.SetString(PrefsLaunchOutpost, LaunchFromOutpostName ?? string.Empty);
            PlayerPrefs.SetInt(PrefsLaunchVtol, LaunchOutpostUsesVtolTakeoff ? 1 : 0);
            PlayerPrefs.SetInt(PrefsLaunchFresh, FreshSortieLaunch ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void SyncPendingLaunchPrefsAfterConsume()
        {
            if (HasPendingLaunch || FreshSortieLaunch)
            {
                PersistPendingLaunch();
                return;
            }

            ClearPendingLaunchPrefs();
        }
    }
}
