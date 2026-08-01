using System;
using System.Collections.Generic;
using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    [Serializable]
    public sealed class CampaignMapMarkerRecord
    {
        public string Id = Guid.NewGuid().ToString("N");
        /// <summary>Stable speakable ID (OP-01, OP-SOUTH, STN-PALMER). Derived from Label if empty.</summary>
        public string SiteCode = string.Empty;
        public string Label = "Mission";
        public int GridCellX;
        public int GridCellZ;
    }

    [Serializable]
    public sealed class CampaignMapLayoutData
    {
        public CampaignMapMarkerRecord[] Markers = Array.Empty<CampaignMapMarkerRecord>();
    }

    /// <summary>
    /// Read-only outpost positions from Resources/CampaignMapLayout.json.
    /// </summary>
    public static class CampaignMapLayoutState
    {
        private const string LockedResourcePath = "CampaignMapLayout";

        private static readonly List<CampaignMapMarkerRecord> lockedMarkers = new();
        private static readonly Dictionary<string, CampaignMapMarkerRecord> lockedMarkersByName =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, CampaignMapMarkerRecord> lockedMarkersByCode =
            new(StringComparer.OrdinalIgnoreCase);
        private static bool lockedLoaded;

        public static IReadOnlyList<CampaignMapMarkerRecord> Markers
        {
            get
            {
                EnsureLockedLoaded();
                return lockedMarkers;
            }
        }

        public static void EnsureLoaded()
        {
            EnsureLockedLoaded();
        }

        public static void EnsureLockedLoaded()
        {
            if (lockedLoaded)
            {
                return;
            }

            lockedLoaded = true;
            lockedMarkers.Clear();
            lockedMarkersByName.Clear();
            lockedMarkersByCode.Clear();

            var asset = Resources.Load<TextAsset>(LockedResourcePath);
            if (asset == null)
            {
                Debug.LogError("F-89: Missing Resources/CampaignMapLayout.json.");
                return;
            }

            var loaded = JsonUtility.FromJson<CampaignMapLayoutData>(asset.text);
            if (loaded?.Markers == null)
            {
                return;
            }

            for (var i = 0; i < loaded.Markers.Length; i++)
            {
                var marker = loaded.Markers[i];
                if (marker == null || string.IsNullOrWhiteSpace(marker.Label))
                {
                    continue;
                }

                marker.Label = NormalizeSiteName(marker.Label);
                if (string.IsNullOrWhiteSpace(marker.SiteCode))
                {
                    marker.SiteCode = OutpostSiteIds.FromLabel(marker.Label);
                }
                else
                {
                    marker.SiteCode = marker.SiteCode.Trim().ToUpperInvariant();
                }

                lockedMarkers.Add(marker);
                lockedMarkersByName[marker.Label] = marker;
                lockedMarkersByCode[marker.SiteCode] = marker;
            }
        }

        public static bool TryGetSite(string siteNameOrCode, out CampaignMapMarkerRecord site)
        {
            site = null;
            EnsureLockedLoaded();
            if (string.IsNullOrWhiteSpace(siteNameOrCode))
            {
                return false;
            }

            var key = siteNameOrCode.Trim();
            if (lockedMarkersByCode.TryGetValue(key, out site))
            {
                return true;
            }

            return lockedMarkersByName.TryGetValue(NormalizeSiteName(key), out site);
        }

        public static bool TryGetSiteByCode(string siteCode, out CampaignMapMarkerRecord site)
        {
            site = null;
            EnsureLockedLoaded();
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return false;
            }

            return lockedMarkersByCode.TryGetValue(siteCode.Trim(), out site);
        }

        public static Vector2 GetLockedMiles(CampaignMapMarkerRecord site)
        {
            if (site == null)
            {
                return Vector2.zero;
            }

            if (site.GridCellX > 0 && site.GridCellZ > 0)
            {
                return CampaignMapCoordinates.GridCellToMiles(new Vector2Int(site.GridCellX, site.GridCellZ));
            }

            return Vector2.zero;
        }

        public static Vector3 GetSiteWorldPosition(CampaignMapMarkerRecord site)
        {
            if (site == null)
            {
                return Vector3.zero;
            }

            if (site.GridCellX > 0 && site.GridCellZ > 0)
            {
                var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
                var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
                var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
                return CampaignMapCoordinates.GridCellToWorld(
                    new Vector2Int(site.GridCellX, site.GridCellZ),
                    worldMap,
                    ticSize);
            }

            return Vector3.zero;
        }

        public static string NormalizeSiteName(string siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
            {
                return string.Empty;
            }

            var trimmed = siteName.Trim();
            if (!trimmed.StartsWith("Outpost ", StringComparison.Ordinal))
            {
                return trimmed;
            }

            var suffix = trimmed.Substring("Outpost ".Length).Trim();
            return int.TryParse(suffix, out var number)
                ? $"Outpost {number:00}"
                : trimmed;
        }
    }
}
