using F89.Flight;
using F89.LandCombat;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Restores a campaign mission site when that mission becomes active so early kills on future
    /// sites do not carry over (vehicles, buildings, surface guards, waypoint platoons).
    /// </summary>
    public static class CampaignMissionSiteRestore
    {
        private const int MaxSurfaceGuardSlots = 32;

        public static void EnsureActiveMissionSiteRestored(CharacterSaveData save)
        {
            if (save == null || !GamePlayModeState.IsCampaign)
            {
                return;
            }

            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);
            if (missionNumber <= 0 || save.CampaignMissionSiteRestoredForMission >= missionNumber)
            {
                return;
            }

            RestoreActiveMissionSite(save, missionNumber);
            save.CampaignMissionSiteRestoredForMission = missionNumber;
            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static void RestoreActiveMissionSite(CharacterSaveData save, int missionNumber)
        {
            if (save == null
                || !CampaignMissionSiteCatalog.TryGetSiteCode(missionNumber, out var siteCode)
                || string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            if (AntarcticaOutpostState.IsFriendlyOccupied(siteCode))
            {
                return;
            }

            if (CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                RestoreWaypointSite(siteCode);
                return;
            }

            if (!CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return;
            }

            RestoreOutpostSite(save, baseName, siteCode, missionNumber);
        }

        private static void RestoreOutpostSite(
            CharacterSaveData save,
            string baseName,
            string siteCode,
            int missionNumber)
        {
            AntarcticaOutpostState.ClearOutpostDestroyed(baseName);
            if (!string.IsNullOrWhiteSpace(siteCode))
            {
                AntarcticaOutpostState.ClearOutpostDestroyed(siteCode);
            }

            AntarcticaOutpostState.ClearAllSiteTargets(baseName);
            if (!string.IsNullOrWhiteSpace(siteCode)
                && !string.Equals(siteCode, baseName, System.StringComparison.OrdinalIgnoreCase))
            {
                AntarcticaOutpostState.ClearAllSiteTargets(siteCode);
            }

            ClearSurfaceGuardFlags(save, baseName, siteCode);
            TryRebuildOutpostInScene(baseName, siteCode);
            Debug.Log($"F-89: Restored mission site {siteCode} ({baseName}) for mission {missionNumber}.");
        }

        private static void RestoreWaypointSite(string siteCode)
        {
            AntarcticaOutpostState.ClearAllSiteTargets(siteCode);
            CampaignWaypointSecondaryState.ClearReveal(siteCode);
            TryRebuildWaypointInScene(siteCode);
            Debug.Log($"F-89: Restored waypoint mission site {siteCode}.");
        }

        private static void ClearSurfaceGuardFlags(CharacterSaveData save, string baseName, string siteCode)
        {
            for (var i = 0; i < MaxSurfaceGuardSlots; i++)
            {
                var label = OutpostGroundGuardState.GetGuardLabel(i);
                AntarcticaOutpostState.ClearTargetDestroyed(baseName, label);
                if (!string.IsNullOrWhiteSpace(siteCode))
                {
                    AntarcticaOutpostState.ClearTargetDestroyed(siteCode, label);
                }
            }

            if (save != null
                && LandBossMissionAssignment.TryGetBossForOutpost(save, baseName, out var bossNumber)
                && bossNumber > 0)
            {
                LandBossEncounter.ClearGuardsCleared(bossNumber);
            }
        }

        private static void TryRebuildOutpostInScene(string baseName, string siteCode)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            AntarcticaBase match = null;
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
                {
                    continue;
                }

                if (string.Equals(baseSite.BaseName, baseName, System.StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(siteCode)
                        && string.Equals(baseSite.SiteCode, siteCode, System.StringComparison.OrdinalIgnoreCase)))
                {
                    match = baseSite;
                    break;
                }
            }

            if (match == null)
            {
                return;
            }

            ResolveWorldScale(out var worldUnitsPerMile, out var ticSize);
            match.RefreshPersistedWorldState(worldUnitsPerMile);

            var platoon = match.transform.Find("VehiclePlatoon");
            if (platoon != null)
            {
                Object.Destroy(platoon.gameObject);
            }

            OutpostBuildingClusterSpawner.ForceRebuildCluster(match, worldUnitsPerMile, ticSize);
        }

        private static void TryRebuildWaypointInScene(string siteCode)
        {
            var sites = Object.FindObjectsByType<CampaignWaypointMissionSite>(FindObjectsSortMode.None);
            CampaignWaypointMissionSite match = null;
            for (var i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                if (site != null
                    && string.Equals(site.SiteCode, siteCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    match = site;
                    break;
                }
            }

            if (match != null)
            {
                var platoon = match.transform.Find("VehiclePlatoon");
                if (platoon != null)
                {
                    Object.Destroy(platoon.gameObject);
                }
            }

            CampaignWaypointSecondaryState.ClearReveal(siteCode, destroyScenePad: true);
        }

        private static void ResolveWorldScale(out float worldUnitsPerMile, out float ticSize)
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            worldUnitsPerMile = worldMap == null
                ? 20f * ticSize
                : worldMap.GridSpacingTics * ticSize / worldMap.milesPerGrid;
        }
    }
}
