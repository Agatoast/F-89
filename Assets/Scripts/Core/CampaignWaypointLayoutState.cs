using F89.Flight;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace F89.Core
{
    [Serializable]
    public sealed class CampaignWaypointRecord
    {
        public string SiteCode = string.Empty;
        public int MissionNumber;
        public int GridCellX;
        public int GridCellZ;
    }

    [Serializable]
    public sealed class CampaignWaypointLayoutData
    {
        public CampaignWaypointRecord[] Waypoints = Array.Empty<CampaignWaypointRecord>();
    }

    /// <summary>
    /// Campaign mission waypoints (non-outpost objectives) from Resources/CampaignWaypointLayout.json.
    /// </summary>
    public static class CampaignWaypointLayoutState
    {
        private const string ResourcePath = "CampaignWaypointLayout";

        private static readonly List<CampaignWaypointRecord> waypoints = new();
        private static readonly Dictionary<string, CampaignWaypointRecord> waypointsByCode =
            new(StringComparer.OrdinalIgnoreCase);
        private static bool loaded;

        public static IReadOnlyList<CampaignWaypointRecord> Waypoints
        {
            get
            {
                EnsureLoaded();
                return waypoints;
            }
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            waypoints.Clear();
            waypointsByCode.Clear();

            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("F-89: Missing Resources/CampaignWaypointLayout.json.");
                return;
            }

            var data = JsonUtility.FromJson<CampaignWaypointLayoutData>(asset.text);
            if (data?.Waypoints == null)
            {
                return;
            }

            for (var i = 0; i < data.Waypoints.Length; i++)
            {
                var waypoint = data.Waypoints[i];
                if (waypoint == null || waypoint.MissionNumber <= 0)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(waypoint.SiteCode))
                {
                    waypoint.SiteCode = waypoint.SiteCode.Trim().ToUpperInvariant();
                    waypointsByCode[waypoint.SiteCode] = waypoint;
                }

                waypoints.Add(waypoint);
            }
        }

        public static bool TryGetByCode(string siteCode, out CampaignWaypointRecord waypoint)
        {
            waypoint = null;
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return false;
            }

            return waypointsByCode.TryGetValue(siteCode.Trim(), out waypoint);
        }

        public static bool TryGetByMissionNumber(int missionNumber, out CampaignWaypointRecord waypoint)
        {
            waypoint = null;
            EnsureLoaded();
            if (missionNumber <= 0)
            {
                return false;
            }

            for (var i = 0; i < waypoints.Count; i++)
            {
                var candidate = waypoints[i];
                if (candidate != null && candidate.MissionNumber == missionNumber)
                {
                    waypoint = candidate;
                    return true;
                }
            }

            return false;
        }

        public static Vector2 GetMiles(CampaignWaypointRecord waypoint)
        {
            if (waypoint == null || waypoint.GridCellX <= 0 || waypoint.GridCellZ <= 0)
            {
                return Vector2.zero;
            }

            return CampaignMapCoordinates.GridCellToMiles(
                new Vector2Int(waypoint.GridCellX, waypoint.GridCellZ));
        }

        public static bool TryGetWorldPosition(string siteCode, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            if (!TryGetByCode(siteCode, out var waypoint))
            {
                return false;
            }

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            worldPosition = CampaignMapCoordinates.MilesToWorld(GetMiles(waypoint), worldMap, ticSize);
            worldPosition.y = 0f;
            return true;
        }

        /// <summary>
        /// True when this WP is the current campaign mission objective (non-outpost mission).
        /// </summary>
        public static bool IsActiveMissionWaypoint(CampaignWaypointRecord waypoint, CharacterSaveData save)
        {
            if (waypoint == null || save == null || !GamePlayModeState.IsCampaign)
            {
                return false;
            }

            if (CampaignMissionProgress.IsCampaignFinished(save))
            {
                return false;
            }

            if (!CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(waypoint.SiteCode)
                && string.Equals(waypoint.SiteCode, siteCode, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return waypoint.MissionNumber == CampaignMissionProgress.GetCurrentMissionNumber(save);
        }

        /// <summary>
        /// Only the active campaign mission waypoint is shown on the tactical map.
        /// </summary>
        public static bool ShouldShowOnMap(CampaignWaypointRecord waypoint, CharacterSaveData save)
        {
            return IsActiveMissionWaypoint(waypoint, save);
        }
    }
}
