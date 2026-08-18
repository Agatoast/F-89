using System;

namespace F89.Core
{
    /// <summary>
    /// Campaign catalog mission objectives — keyed by CampaignMissionNumber / SiteCode.
    /// Outpost/station missions permanently occupy their SiteCode for the character after END MISSION.
    /// </summary>
    public static class CampaignMissionObjectiveState
    {
        /// <summary>
        /// Mission numbers whose SiteCode becomes permanently friendly after that mission's END MISSION.
        /// </summary>
        private static readonly int[] PermanentOccupationMissions =
        {
            1,  // OP-SOUTH
            5,  // OP-01
            8,  // OP-13
            10, // OP-13-SE
            13, // OP-43
            15, // OP-21
            18, // STN-ROTHERA
            21, // STN-PALMER
            24, // STN-MARAMBIO
            27, // OP-18
            31, // OP-05
            34, // OP-33
            38, // OP-27
            42, // OP-41
            44, // STN-CONCORDIA
            46, // STN-NEUMAYER-III
            47, // STN-HALLEY-VI
            49, // OP-44
            53  // STN-AMUNDSEN-SCOTT
        };

        public static bool TryGetCurrentSiteCode(CharacterSaveData save, out string siteCode) =>
            CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out siteCode);

        public static bool IsPermanentOccupationMission(int missionNumber)
        {
            for (var i = 0; i < PermanentOccupationMissions.Length; i++)
            {
                if (PermanentOccupationMissions[i] == missionNumber)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveOutpostBaseName(string siteCode, out string baseName)
        {
            baseName = string.Empty;
            if (!CampaignMissionSiteCatalog.IsOutpostOrStationSite(siteCode))
            {
                return false;
            }

            if (!CampaignMapLayoutState.TryGetSiteByCode(siteCode, out var site)
                || site == null
                || string.IsNullOrWhiteSpace(site.Label))
            {
                return false;
            }

            baseName = CampaignMapLayoutState.NormalizeSiteName(site.Label);
            return !string.IsNullOrWhiteSpace(baseName);
        }

        public static bool IsActiveMissionOutpost(CharacterSaveData save, AntarcticaBase baseSite)
        {
            if (save == null
                || baseSite == null
                || !GamePlayModeState.IsCampaign
                || CampaignMissionProgress.IsCampaignFinished(save))
            {
                return false;
            }

            if (!TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignMissionSiteCatalog.IsOutpostOrStationSite(siteCode))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(baseSite.SiteCode)
                && string.Equals(baseSite.SiteCode, siteCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return TryResolveOutpostBaseName(siteCode, out var baseName)
                && string.Equals(baseSite.BaseName, baseName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsActiveMissionWaypoint(CharacterSaveData save, CampaignWaypointRecord waypoint)
        {
            return CampaignWaypointLayoutState.IsActiveMissionWaypoint(waypoint, save);
        }

        public static bool IsPrimaryMissionComplete(CharacterSaveData save)
        {
            if (save == null
                || !GamePlayModeState.IsCampaign
                || CampaignMissionProgress.IsCampaignFinished(save))
            {
                return false;
            }

            if (!TryGetCurrentSiteCode(save, out var siteCode))
            {
                return false;
            }

            if (CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                CampaignWaypointPlatoonState.TrySyncActiveMissionBeforeEnd(save);
                return IsWaypointPrimaryComplete(siteCode);
            }

            if (!TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return false;
            }

            return OutpostPrimaryObjective.AreAirObjectivesDestroyed(baseName)
                || OutpostPrimaryObjective.AreAirObjectivesDestroyed(siteCode);
        }

        public static bool IsSecondaryMissionComplete(CharacterSaveData save)
        {
            if (save == null
                || !GamePlayModeState.IsCampaign
                || CampaignMissionProgress.IsCampaignFinished(save))
            {
                return false;
            }

            if (!TryGetCurrentSiteCode(save, out var siteCode))
            {
                return false;
            }

            if (CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                var guardCount = CampaignWaypointSecondaryCatalog.GetSecondaryInfantryCount(siteCode);
                return guardCount > 0
                    && OutpostGroundGuardState.AreSurfaceGuardsCleared(siteCode, guardCount);
            }

            if (!TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return false;
            }

            return AntarcticaOutpostState.IsBunkerCleared(baseName)
                || AntarcticaOutpostState.IsBunkerCleared(siteCode);
        }

        /// <summary>
        /// Permanently marks the current outpost/station mission site friendly for this character.
        /// Called only after a primary-complete END MISSION.
        /// </summary>
        public static void TryApplyVictoryOccupation(CharacterSaveData save)
        {
            if (save == null || !GamePlayModeState.IsCampaign)
            {
                return;
            }

            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);
            if (!IsPermanentOccupationMission(missionNumber))
            {
                return;
            }

            if (!CampaignMissionSiteCatalog.TryGetSiteCode(missionNumber, out var siteCode)
                || !TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return;
            }

            AntarcticaOutpostState.MarkFriendlyOccupied(baseName);
        }

        /// <summary>
        /// Any outpost/station mission already completed (advanced past) stays permanently friendly.
        /// </summary>
        public static void RecoverCompletedOccupationSites(CharacterSaveData save)
        {
            if (save == null || !GamePlayModeState.IsCampaign)
            {
                return;
            }

            var current = CampaignMissionProgress.GetCurrentMissionNumber(save);
            for (var i = 0; i < PermanentOccupationMissions.Length; i++)
            {
                var missionNumber = PermanentOccupationMissions[i];
                if (missionNumber >= current)
                {
                    continue;
                }

                if (!CampaignMissionSiteCatalog.TryGetSiteCode(missionNumber, out var siteCode)
                    || !TryResolveOutpostBaseName(siteCode, out var baseName))
                {
                    continue;
                }

                if (AntarcticaOutpostState.IsFriendlyOccupied(baseName))
                {
                    continue;
                }

                AntarcticaOutpostState.MarkFriendlyOccupied(baseName);
            }
        }

        private static bool IsWaypointPrimaryComplete(string siteCode) =>
            CampaignWaypointPlatoonState.IsPrimaryAirObjectivesComplete(siteCode);
    }
}
