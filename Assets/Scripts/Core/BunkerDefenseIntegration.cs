using F89.UI;
using F89.Weapons;
using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Core
{
    public static class BunkerDefenseIntegration
    {
        public const string PlaySceneName = "BunkerPlayScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterHostResume()
        {
            BunkerDefenseHostResumeRegistry.Handler = BunkerDefenseHostResumeBridge.Instance;
            FlightMissionData.OnConsumedMissionsChanged = PersistConsumedMissionsToActiveSave;
        }

        public static void OnActiveSaveChanged(CharacterSaveData save)
        {
            var data = FlightMissionData.Instance;
            if (data == null)
            {
                return;
            }

            if (save == null)
            {
                data.ImportConsumedMissionIds(null);
                data.BindPersistenceScope(string.Empty);
                return;
            }

            CharacterSaveRepository.EnsureBunkerDefenseProgress(save);
            data.BindPersistenceScope(save.Id);
            if (save.ConsumedBunkerDefenseMissionIds != null && save.ConsumedBunkerDefenseMissionIds.Length > 0)
            {
                data.ImportConsumedMissionIds(save.ConsumedBunkerDefenseMissionIds);
            }
            else
            {
                data.LoadPersistedConsumedMissions();
                if (data.ExportConsumedMissionIds().Length > 0)
                {
                    PersistConsumedMissionsToActiveSave();
                }
            }
        }

        public static void PersistConsumedMissionsToActiveSave()
        {
            var save = CharacterSessionState.ActiveSave;
            var data = FlightMissionData.Instance;
            if (save == null || data == null)
            {
                return;
            }

            save.ConsumedBunkerDefenseMissionIds = data.ExportConsumedMissionIds();
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        /// <summary>Clears once-per-mission bunker defense consumption so triggers can fire again.</summary>
        public static void ResetConsumedMissions(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.ConsumedBunkerDefenseMissionIds = System.Array.Empty<string>();
            save.CompletedBunkerDefenseMissionIds = System.Array.Empty<string>();
            CharacterSaveRepository.WriteWorldProgress(save);

            var data = FlightMissionData.Instance;
            if (data == null)
            {
                return;
            }

            data.BindPersistenceScope(save.Id);
            data.ClearConsumedMissions();
        }

        public static void LaunchDevMinigun(int mapNumber)
        {
            mapNumber = Mathf.Clamp(mapNumber, 1, DefenseSiteCatalog.MissionCount);
            Time.timeScale = 1f;
            GamePlayModeState.EnterCampaign();

            var data = FlightMissionData.Instance;
            if (data == null)
            {
                return;
            }

            data.SetStandaloneMap(mapNumber);
            data.SetReturnSceneName(GameScenes.StartPage);
            SceneManager.LoadScene(PlaySceneName);
        }

        public static bool TryLaunchBeforeFlight(
            bool carrierResupply,
            bool returnToRunwayDeck,
            bool friendlyReturnToDeck)
        {
            if (carrierResupply || returnToRunwayDeck || friendlyReturnToDeck)
            {
                return false;
            }

            if (GamePlayModeState.IsFreeFlight)
            {
                return false;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return false;
            }

            if (CampaignMissionSiteCatalog.CurrentMissionRequiresCarrierLaunch(save))
            {
                return false;
            }

            OnActiveSaveChanged(save);

            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);
            var launchOutpost = ResolveLaunchOutpostName(save);
            var launchSiteCode = ResolveLaunchSiteCodeFromOutpost(launchOutpost);
            if (string.IsNullOrWhiteSpace(launchSiteCode)
                || !IsFriendlyOccupiedLaunchSite(launchOutpost, launchSiteCode))
            {
                return false;
            }

            var missionId = ResolveDefenseMissionId(missionNumber, launchSiteCode);
            if (string.IsNullOrWhiteSpace(missionId))
            {
                return false;
            }

            var data = FlightMissionData.Instance;
            if (data == null)
            {
                Debug.LogError("F-89: Bunker defense blocked — FlightMissionData missing.");
                return false;
            }

            data.SetPlaneHitPoints(GameConstants.DefaultPlaneHitPoints);
            TryRecoverStaleConsumedDefense(save, missionId);
            if (!data.TryAuthorizeMissionStartDefense(
                    missionId,
                    GameScenes.AircraftLoadout,
                    launchOutpost ?? string.Empty,
                    launchSiteCode))
            {
                Debug.LogWarning(
                    $"F-89: Bunker defense already consumed for mission id '{missionId}' (campaign mission {missionNumber}).");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(PlaySceneName))
            {
                Debug.LogError(
                    $"F-89: Bunker defense scene '{PlaySceneName}' is missing from Build Settings.");
                return false;
            }

            AircraftLoadoutState.MarkConfigured();
            GamePauseController.ClearPauseOnSceneLoad();
            Time.timeScale = 1f;
            SaveAntarctica.BunkerDefense.Combat.CombatAudio.PlayBriefingClaxon();
            SceneManager.LoadScene(PlaySceneName);
            return true;
        }

        private static string ResolveLaunchOutpostName(CharacterSaveData save)
        {
            if (MissionLaunchOrigin.TryResolveLaunchOutpost(save, out var launchOutpost)
                && !string.IsNullOrWhiteSpace(launchOutpost))
            {
                return launchOutpost;
            }

            return save?.MissionLaunchOutpostName?.Trim() ?? string.Empty;
        }

        private static string ResolveLaunchSiteCodeFromOutpost(string launchOutpost)
        {
            if (string.IsNullOrWhiteSpace(launchOutpost))
            {
                return string.Empty;
            }

            if (CampaignMapLayoutState.TryGetSite(launchOutpost, out var site)
                && site != null
                && !string.IsNullOrWhiteSpace(site.SiteCode))
            {
                return site.SiteCode.Trim();
            }

            var trimmed = launchOutpost.Trim();
            if (DefenseSiteCatalog.GetMapNumber(trimmed) > 0)
            {
                return trimmed;
            }

            return string.Empty;
        }

        private static bool IsFriendlyOccupiedLaunchSite(string launchOutpost, string launchSiteCode)
        {
            if (!string.IsNullOrWhiteSpace(launchOutpost)
                && AntarcticaOutpostState.IsFriendlyOccupied(launchOutpost.Trim()))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(launchSiteCode)
                && AntarcticaOutpostState.IsFriendlyOccupied(launchSiteCode);
        }

        /// <summary>
        /// Map 01: mission 03 intro from OP-SOUTH while on campaign mission 3 (id "3").
        /// Maps 02–18: after the site's clear mission is complete, next VTOL from that site code (id = site code).
        /// Mission 01/02 never trigger except the map-01 mission-03 intro path.
        /// </summary>
        private static string ResolveDefenseMissionId(int campaignMissionNumber, string launchSiteCode)
        {
            if (string.IsNullOrWhiteSpace(launchSiteCode)
                || !BunkerDefenseMissionTriggerCatalog.IsDefenseMission(launchSiteCode))
            {
                return string.Empty;
            }

            var mapNumber = DefenseSiteCatalog.GetMapNumber(launchSiteCode);
            if (mapNumber <= 0)
            {
                return string.Empty;
            }

            if (campaignMissionNumber == 3)
            {
                var introMap = DefenseSiteCatalog.GetIntroMapForMissionNumber(3);
                if (introMap > 0
                    && mapNumber == introMap
                    && string.Equals(
                        launchSiteCode,
                        DefenseSiteCatalog.GetSiteCode(introMap),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return "3";
                }

                return string.Empty;
            }

            if (campaignMissionNumber <= 2)
            {
                return string.Empty;
            }

            var clearMission = DefenseSiteCatalog.GetF89MissionNumber(mapNumber);
            if (clearMission <= 0 || campaignMissionNumber <= clearMission)
            {
                return string.Empty;
            }

            return launchSiteCode;
        }

        public static void ApplySortiePlaneHits(LockableTarget target)
        {
            if (target == null)
            {
                return;
            }

            var data = FlightMissionData.Instance;
            if (data == null)
            {
                target.ConfigureSortieHitBudget(PlayerAircraftGhp.SortieHitsToCrash);
                return;
            }

            var hits = data.GetPlaneHitsForNextFlight();
            target.ConfigureSortieHitBudget(hits);
        }

        public static void ConsumeSortieHitModifierIfNeeded()
        {
            FlightMissionData.Instance?.ConsumeNextFlightHitModifier();
        }

        public static void MarkDefenseMissionCompleted(string missionId)
        {
            var save = CharacterSessionState.ActiveSave;
            if (save == null || string.IsNullOrWhiteSpace(missionId))
            {
                return;
            }

            CharacterSaveRepository.EnsureBunkerDefenseProgress(save);
            var normalized = missionId.Trim();
            if (ContainsMissionId(save.CompletedBunkerDefenseMissionIds, normalized))
            {
                return;
            }

            var completed = save.CompletedBunkerDefenseMissionIds ?? System.Array.Empty<string>();
            var next = new string[completed.Length + 1];
            for (var i = 0; i < completed.Length; i++)
            {
                next[i] = completed[i];
            }

            next[next.Length - 1] = normalized;
            save.CompletedBunkerDefenseMissionIds = next;
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        private static void TryRecoverStaleConsumedDefense(CharacterSaveData save, string missionId)
        {
            if (save == null || string.IsNullOrWhiteSpace(missionId))
            {
                return;
            }

            CharacterSaveRepository.EnsureBunkerDefenseProgress(save);
            var normalized = missionId.Trim();
            if (!ContainsMissionId(save.ConsumedBunkerDefenseMissionIds, normalized)
                || ContainsMissionId(save.CompletedBunkerDefenseMissionIds, normalized))
            {
                return;
            }

            save.ConsumedBunkerDefenseMissionIds = RemoveMissionId(
                save.ConsumedBunkerDefenseMissionIds,
                normalized);
            CharacterSaveRepository.WriteWorldProgress(save);

            var data = FlightMissionData.Instance;
            if (data != null)
            {
                data.BindPersistenceScope(save.Id);
                data.ImportConsumedMissionIds(save.ConsumedBunkerDefenseMissionIds);
            }

            Debug.LogWarning(
                $"F-89: Cleared stale bunker defense consume for '{normalized}' — intro was never finished.");
        }

        private static bool ContainsMissionId(string[] missionIds, string missionId)
        {
            if (missionIds == null || string.IsNullOrWhiteSpace(missionId))
            {
                return false;
            }

            for (var i = 0; i < missionIds.Length; i++)
            {
                if (string.Equals(missionIds[i]?.Trim(), missionId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] RemoveMissionId(string[] missionIds, string missionId)
        {
            if (missionIds == null || missionIds.Length == 0)
            {
                return System.Array.Empty<string>();
            }

            var remaining = new System.Collections.Generic.List<string>(missionIds.Length);
            for (var i = 0; i < missionIds.Length; i++)
            {
                var candidate = missionIds[i];
                if (string.IsNullOrWhiteSpace(candidate)
                    || string.Equals(candidate.Trim(), missionId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                remaining.Add(candidate.Trim());
            }

            return remaining.Count == 0 ? System.Array.Empty<string>() : remaining.ToArray();
        }
    }
}
