using F89.LandCombat;
using UnityEngine;

namespace F89.Core
{

    /// <summary>

    /// Resolves where a fresh sortie may start: carrier deck or a friendly occupied runway.

    /// </summary>

    public static class MissionLaunchOrigin

    {

        public static bool TryResolveFromSave(CharacterSaveData save, out string friendlyOutpostName)

        {

            friendlyOutpostName = string.Empty;

            if (!TryNormalizeSavedLaunchOutpost(save, out var outpostName, out var site))

            {

                return false;

            }



            var friendly = AntarcticaOutpostState.IsFriendlyBase(outpostName)

                || (site != null && AntarcticaOutpostState.IsFriendlyBase(site.SiteCode));

            if (!friendly)

            {

                return false;

            }



            friendlyOutpostName = outpostName;

            return true;

        }



        /// <summary>

        /// Resolves a parked launch outpost for the next sortie. Accepts friendly bases and

        /// any saved origin that still has a layout/runway spawn (crash recovery must not

        /// fall back to the ocean CV when the last launch base is still valid).

        /// </summary>

        public static bool TryResolveLaunchOutpost(CharacterSaveData save, out string outpostName)
        {
            if (TryNormalizeSavedLaunchOutpost(save, out outpostName, out var site)
                && !string.IsNullOrWhiteSpace(outpostName))
            {
                // Last friendly runway landing — always honor across cold starts and VTOL.
                return true;
            }

            var recovered = RecoverLaunchOutpostName(save);
            if (string.IsNullOrWhiteSpace(recovered))
            {
                outpostName = string.Empty;
                return false;
            }

            if (CampaignMapLayoutState.TryGetSite(recovered, out site)
                && site != null
                && !string.IsNullOrWhiteSpace(site.Label))
            {
                outpostName = CampaignMapLayoutState.NormalizeSiteName(site.Label);
            }
            else
            {
                outpostName = recovered;
            }

            return IsTrustedLaunchSite(outpostName, site)
                || OutpostRunwayLanding.TryGetLayoutMilesSpawn(outpostName, out _, out _)
                || OutpostRunwayLanding.TryGetRunwaySpawn(outpostName, out _, out _);
        }

        /// <summary>
        /// Catalog outposts with locked grid cells, or any saved friendly occupied runway.
        /// Does not require runtime spawn probes (loadout has no scene bases yet).
        /// </summary>
        private static bool IsTrustedLaunchSite(string outpostName, CampaignMapMarkerRecord site)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (AntarcticaOutpostState.IsFriendlyBase(outpostName)
                || (site != null && AntarcticaOutpostState.IsFriendlyBase(site.SiteCode)))
            {
                return true;
            }

            if (site == null
                && !CampaignMapLayoutState.TryGetSite(outpostName, out site))
            {
                return false;
            }

            return site != null
                && site.GridCellX > 0
                && site.GridCellZ > 0
                && CampaignMissionSiteCatalog.IsOutpostOrStationSite(site.SiteCode);
        }



        public static bool HasSavedLaunchOutpost(CharacterSaveData save) =>
            save != null && !string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName);

        /// <summary>Last-landed base from character save only — never session recovery sources.</summary>
        public static bool TryGetSavedLastLandedBase(CharacterSaveData save, out string outpostName) =>
            TryNormalizeSavedLaunchOutpost(save, out outpostName, out _)
            && !string.IsNullOrWhiteSpace(outpostName);

        /// <summary>
        /// Start Mission from loadout: use the last friendly runway landing when saved;
        /// fall back to the current OP mission grid site only when no landing history exists.
        /// Mission 01 opening sortie and Mission 02 (escort from carrier) use the carrier deck.
        /// </summary>
        public static bool TryResolveFreshSortieLaunchBase(CharacterSaveData save, out string outpostName)
        {
            outpostName = string.Empty;
            if (save == null)
            {
                return false;
            }

            if (CampaignMissionSiteCatalog.CurrentMissionRequiresCarrierLaunch(save))
            {
                return false;
            }

            if (save.CampaignMissionNumber <= 1
                && save.SuccessfulEndMissionCount <= 0
                && (save.FriendlyOccupiedOutpostNames == null || save.FriendlyOccupiedOutpostNames.Length == 0))
            {
                return false;
            }

            if (TryGetSavedLastLandedBase(save, out outpostName)
                && IsFriendlyLaunchBase(outpostName))
            {
                return true;
            }

            if (TryGetMostRecentFriendlyOccupied(save, out outpostName))
            {
                return true;
            }

            if (CampaignMissionObjectiveState.TryGetCurrentSiteCode(save, out var siteCode)
                && CampaignMissionSiteCatalog.IsOutpostOrStationSite(siteCode)
                && siteCode.StartsWith("OP-", System.StringComparison.OrdinalIgnoreCase)
                && CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var missionBase)
                && !string.IsNullOrWhiteSpace(missionBase)
                && IsFriendlyLaunchBase(missionBase))
            {
                outpostName = missionBase;
                return true;
            }

            return false;
        }

        private static bool IsFriendlyLaunchBase(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var trimmed = outpostName.Trim();
            return AntarcticaOutpostState.IsFriendlyOccupied(trimmed)
                || AntarcticaOutpostState.IsFriendlyBase(trimmed);
        }

        /// <summary>Most recently END MISSION occupied friendly base — recovers stale OP-South launch saves.</summary>
        private static bool TryGetMostRecentFriendlyOccupied(CharacterSaveData save, out string outpostName)
        {
            outpostName = string.Empty;
            if (save?.FriendlyOccupiedOutpostNames == null)
            {
                return false;
            }

            for (var i = save.FriendlyOccupiedOutpostNames.Length - 1; i >= 0; i--)
            {
                var occupied = save.FriendlyOccupiedOutpostNames[i];
                if (string.IsNullOrWhiteSpace(occupied))
                {
                    continue;
                }

                var candidate = occupied.Trim();
                CampaignMapMarkerRecord site = null;
                if (CampaignMapLayoutState.TryGetSite(candidate, out site)
                    && site != null
                    && !string.IsNullOrWhiteSpace(site.Label))
                {
                    candidate = CampaignMapLayoutState.NormalizeSiteName(site.Label);
                }

                if (!AntarcticaOutpostState.IsFriendlyOccupied(candidate)
                    && !AntarcticaOutpostState.IsFriendlyBase(candidate))
                {
                    continue;
                }

                outpostName = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Only friendly occupied runways update MissionLaunchOutpostName — hostile mission-site launches do not.
        /// </summary>
        public static bool ShouldPersistLaunchOutpostAfterTakeoff(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var trimmed = outpostName.Trim();
            return AntarcticaOutpostState.IsFriendlyOccupied(trimmed)
                || AntarcticaOutpostState.IsFriendlyBase(trimmed);
        }

        public static void MaybePersistLaunchOutpostAfterTakeoff(CharacterSaveData save, string outpostName)
        {
            if (ShouldPersistLaunchOutpostAfterTakeoff(outpostName))
            {
                PersistLaunchOutpost(save, outpostName);
            }
        }

        /// <summary>Clears saved runway launch origin — next fresh sortie uses the carrier deck.</summary>
        public static void ClearLastLandedBase(CharacterSaveData save)
        {
            if (save == null || string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName))
            {
                return;
            }

            save.MissionLaunchOutpostName = string.Empty;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        /// <summary>Last friendly runway/base where the pilot landed.</summary>
        public static void EnsureDefaultLaunchOriginIfMissing(CharacterSaveData save)
        {
            if (save == null || HasSavedLaunchOutpost(save))
            {
                return;
            }

            if (CampaignMissionSiteCatalog.CurrentMissionRequiresCarrierLaunch(save))
            {
                return;
            }

            // Brand-new campaign — opening sortie still launches from the carrier deck.
            if (save.CampaignMissionNumber <= 1
                && save.SuccessfulEndMissionCount <= 0
                && (save.FriendlyOccupiedOutpostNames == null || save.FriendlyOccupiedOutpostNames.Length == 0))
            {
                return;
            }

            if (save.FriendlyOccupiedOutpostNames != null)
            {
                for (var i = 0; i < save.FriendlyOccupiedOutpostNames.Length; i++)
                {
                    var occupied = save.FriendlyOccupiedOutpostNames[i];
                    if (string.IsNullOrWhiteSpace(occupied))
                    {
                        continue;
                    }

                    PersistLaunchOutpost(save, occupied.Trim());
                    Debug.LogWarning(
                        "F-89: Backfilled last-land base '"
                        + occupied.Trim()
                        + "' from friendly occupation on save.");
                    return;
                }
            }

            PersistLaunchOutpost(save, LandBossMissionAssignment.Boss1OutpostName);
            Debug.LogWarning(
                "F-89: Backfilled default last-land base '"
                + LandBossMissionAssignment.Boss1OutpostName
                + "' on save.");
        }

        public static void PrepareFreshSortieLaunch(CharacterSaveData save)
        {
            FlightMissionLaunchState.ClearForceCarrierDeckLaunch();
            FlightMissionLaunchState.ClearStaleCarrierLaunchIntentFromPrefs();

            // Parked runway name for this session — persist to save before handoff reset runs.
            var parkedOutpost = OutpostRunwayDeckState.ParkedOutpostName;
            if (!string.IsNullOrWhiteSpace(parkedOutpost))
            {
                PersistLaunchOutpost(save, parkedOutpost);
            }

            // Explicit loadout launch — never inherit a prior session's ground-return handoff.
            GameplaySessionBootstrap.ClearSessionHandoffOnly();

            EnsureDefaultLaunchOriginIfMissing(save);

            if (CampaignMissionSiteCatalog.CurrentMissionRequiresCarrierLaunch(save))
            {
                ClearLastLandedBase(save);
            }

            if (TryResolveFreshSortieLaunchBase(save, out var outpostName))
            {
                MaybePersistLaunchOutpostAfterTakeoff(save, outpostName);
                FlightMissionLaunchState.BeginFreshSortieLaunch(outpostName, vtolTakeoff: true);
                Debug.Log($"F-89: Fresh sortie queued from '{outpostName}'.");
                return;
            }

            // No landing history yet — campaign opening sortie from the carrier deck only.
            Debug.Log("F-89: No land launch base — opening carrier deck sortie.");
            FlightMissionLaunchState.BeginFreshSortieLaunch();
        }



        /// <summary>

        /// Crash/incomplete abort: keep the last launch base and drop handoff that can force CV spawn.

        /// </summary>

        public static void PreserveAfterCrash(CharacterSaveData save)

        {

            var recovered = RecoverLaunchOutpostName(save);

            if (!string.IsNullOrWhiteSpace(recovered))

            {

                PersistLaunchOutpost(save, recovered);

            }



            LandMissionHandoffState.Clear();

            LandingMileFlagState.Clear();

            OpenFieldLandingState.Clear();

            CarrierDeckTakeoffState.Clear();

            OutpostRunwayDeckState.Clear();

            FlightMissionLaunchState.Clear();

        }



        public static void PersistLaunchOutpost(CharacterSaveData save, string outpostName)

        {

            if (save == null || string.IsNullOrWhiteSpace(outpostName))

            {

                return;

            }



            LandBossMissionAssignment.PersistLaunchOutpost(save, outpostName.Trim());

        }

        /// <summary>Last friendly runway/base landed — never cleared; only overwritten by a new base landing.</summary>
        public static void RecordLastLandedBase(CharacterSaveData save, string outpostName) =>
            PersistLaunchOutpost(save, outpostName);



        private static string RecoverLaunchOutpostName(CharacterSaveData save)

        {

            if (save != null && !string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName))

            {

                return save.MissionLaunchOutpostName.Trim();

            }



            if (!string.IsNullOrWhiteSpace(FlightMissionLaunchState.LaunchFromOutpostName))

            {

                return FlightMissionLaunchState.LaunchFromOutpostName.Trim();

            }



            if (!string.IsNullOrWhiteSpace(OutpostRunwayDeckState.ParkedOutpostName))

            {

                return OutpostRunwayDeckState.ParkedOutpostName.Trim();

            }



            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();

            if (snapshot.IsValid && !string.IsNullOrWhiteSpace(snapshot.OutpostName))

            {

                return snapshot.OutpostName.Trim();

            }



            if (!string.IsNullOrWhiteSpace(FriendlyOutpostTakeoffState.OutpostName))

            {

                return FriendlyOutpostTakeoffState.OutpostName.Trim();

            }



            return string.Empty;

        }



        private static bool TryNormalizeSavedLaunchOutpost(

            CharacterSaveData save,

            out string outpostName,

            out CampaignMapMarkerRecord site)

        {

            outpostName = string.Empty;

            site = null;

            if (save == null || string.IsNullOrWhiteSpace(save.MissionLaunchOutpostName))

            {

                return false;

            }



            outpostName = save.MissionLaunchOutpostName.Trim();

            if (CampaignMapLayoutState.TryGetSite(outpostName, out site)

                && site != null

                && !string.IsNullOrWhiteSpace(site.Label))

            {

                outpostName = CampaignMapLayoutState.NormalizeSiteName(site.Label);

            }



            return !string.IsNullOrWhiteSpace(outpostName);

        }

    }

}


