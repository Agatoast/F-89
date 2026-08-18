using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Loot rolled when UR infantry are killed on the flight map, spawned as corpses on ground dismount.
    /// </summary>
    public static class FlightInfantryLootState
    {
        public readonly struct PendingCorpseLoot
        {
            public PendingCorpseLoot(int level, List<LandGearInstance> items)
            {
                Level = level;
                Items = items ?? new List<LandGearInstance>();
            }

            public int Level { get; }
            public List<LandGearInstance> Items { get; }
        }

        private static readonly Dictionary<string, List<PendingCorpseLoot>> PendingBySite =
            new(System.StringComparer.OrdinalIgnoreCase);

        public static void Clear()
        {
            PendingBySite.Clear();
        }

        public static void RecordFlightKill(Transform unitTransform, int infantryLevel)
        {
            if (unitTransform == null || infantryLevel <= 0)
            {
                return;
            }

            var waypoint = unitTransform.GetComponentInParent<CampaignWaypointMissionSite>();
            if (waypoint != null && !string.IsNullOrWhiteSpace(waypoint.SiteCode))
            {
                RecordKill(waypoint.SiteCode, infantryLevel);
                return;
            }

            var baseSite = unitTransform.GetComponentInParent<AntarcticaBase>();
            if (baseSite == null)
            {
                return;
            }

            RecordKill(baseSite.SiteCode, infantryLevel);
            if (!string.IsNullOrWhiteSpace(baseSite.BaseName)
                && !string.Equals(baseSite.SiteCode, baseSite.BaseName, System.StringComparison.OrdinalIgnoreCase))
            {
                RecordKill(baseSite.BaseName, infantryLevel);
            }
        }

        public static IReadOnlyList<PendingCorpseLoot> ConsumeForLanding(string outpostOrSiteCode)
        {
            var merged = new List<PendingCorpseLoot>();
            DrainKey(outpostOrSiteCode, merged);
            return merged;
        }

        private static void RecordKill(string siteKey, int infantryLevel)
        {
            var normalized = NormalizeKey(siteKey);
            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            var items = new List<LandGearInstance>();
            LandEnemyLootGenerator.FillInfantryDeathLoot(infantryLevel, items);
            if (items.Count == 0)
            {
                return;
            }

            if (!PendingBySite.TryGetValue(normalized, out var list))
            {
                list = new List<PendingCorpseLoot>();
                PendingBySite[normalized] = list;
            }

            list.Add(new PendingCorpseLoot(infantryLevel, items));
        }

        private static void DrainKey(string siteKey, List<PendingCorpseLoot> merged)
        {
            var normalized = NormalizeKey(siteKey);
            if (string.IsNullOrEmpty(normalized)
                || !PendingBySite.TryGetValue(normalized, out var list)
                || list.Count == 0)
            {
                return;
            }

            merged.AddRange(list);
            PendingBySite.Remove(normalized);
        }

        private static string NormalizeKey(string siteKey) =>
            string.IsNullOrWhiteSpace(siteKey) ? string.Empty : siteKey.Trim();
    }
}
