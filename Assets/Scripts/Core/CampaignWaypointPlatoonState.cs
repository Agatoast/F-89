using System.Collections.Generic;
using F89.Enemies;
using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Flight-map platoon clearance for waypoint site codes (WP-NN).</summary>
    public static class CampaignWaypointPlatoonState
    {
        private static readonly Dictionary<string, Vector3> LastHostileVehicleDestroyBySite =
            new(System.StringComparer.OrdinalIgnoreCase);

        public static bool IsPrimaryAirObjectivesComplete(string siteCode)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return false;
            }

            if (OutpostFlightPlatoonState.IsPlatoonClearedInSave(siteCode))
            {
                return true;
            }

            if (AntarcticaOutpostState.IsTargetDestroyed(
                    siteCode,
                    CampaignWaypointPrimaryObjective.ClearedLabel))
            {
                return true;
            }

            return AreAllHostileSlotsDestroyed(siteCode);
        }

        /// <summary>
        /// Show the destroyed-tank pad after the first hostile ground vehicle is destroyed at the WP
        /// (or once primary air objectives are complete). Landing on the pad still requires primary completion.
        /// </summary>
        public static bool ShouldShowSecondaryPad(string siteCode, CampaignWaypointMissionSite site)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return false;
            }

            if (CampaignWaypointSecondaryState.IsSecondaryRevealed(siteCode)
                || HasFirstHostileGroundVehicleDestroy(siteCode)
                || IsPrimaryAirObjectivesComplete(siteCode))
            {
                return true;
            }

            return site != null && !HasLivingHostileVehiclesUnderSite(site);
        }

        /// <summary>
        /// Records the wreck drop point. The first hostile ground vehicle destroyed at a WP reveals the secondary pad.
        /// </summary>
        public static void RecordHostileVehicleDestroy(string siteCode, Vector3 worldPosition)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return;
            }

            worldPosition.y = 0f;
            var normalized = siteCode.Trim().ToUpperInvariant();
            var isFirst = !LastHostileVehicleDestroyBySite.ContainsKey(normalized);
            LastHostileVehicleDestroyBySite[normalized] = worldPosition;

            if (!isFirst)
            {
                return;
            }

            var site = FindSiteInScene(normalized);
            ResolveSecondaryPadPosition(normalized, worldPosition, out var padPosition);
            CampaignWaypointSecondaryState.TryRevealSecondary(normalized, padPosition, site);
        }

        private static bool HasFirstHostileGroundVehicleDestroy(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return false;
            }

            return LastHostileVehicleDestroyBySite.ContainsKey(siteCode.Trim().ToUpperInvariant());
        }

        public static void TryFinalizeClearance(CampaignWaypointMissionSite site, Vector3 lastDestroyWorldPosition)
        {
            if (site == null || string.IsNullOrWhiteSpace(site.SiteCode))
            {
                return;
            }

            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(site.SiteCode))
            {
                return;
            }

            ResolveSecondaryPadPosition(site.SiteCode, lastDestroyWorldPosition, out var padPosition);

            if (!HasLivingHostileUnitsUnderSite(site))
            {
                if (!OutpostFlightPlatoonState.IsPlatoonClearedInSave(site.SiteCode))
                {
                    MarkAllHostileSlotsDestroyed(site.SiteCode);
                }
            }

            TryRevealSecondaryPad(site, padPosition);
        }

        /// <summary>
        /// Reconcile scene platoon state before END MISSION so clearance is not lost when the last kill
        /// did not run through the normal destroy handler (e.g. US friendly fire).
        /// </summary>
        public static void TrySyncActiveMissionBeforeEnd(CharacterSaveData save)
        {
            if (save == null
                || !GamePlayModeState.IsCampaign
                || !CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return;
            }

            var site = FindSiteInScene(siteCode);
            if (site == null)
            {
                if (ShouldShowSecondaryPad(siteCode, null) || IsPrimaryAirObjectivesComplete(siteCode))
                {
                    if (CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint))
                    {
                        var miles = CampaignWaypointLayoutState.GetMiles(waypoint);
                        var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
                        var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
                        var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
                        var siteWorld = CampaignMapCoordinates.MilesToWorld(miles, worldMap, ticSize);
                        ResolveSecondaryPadPosition(siteCode, siteWorld, out var padPosition);
                        EnsureSecondaryPadRevealed(siteCode, padPosition, site);
                    }
                }

                return;
            }

            ResolveSecondaryPadPosition(siteCode, site.transform.position, out var syncPadPosition);
            if (!HasLivingHostileUnitsUnderSite(site))
            {
                TryFinalizeClearance(site, syncPadPosition);
                return;
            }

            TryRevealSecondaryPad(site, syncPadPosition);
        }

        public static void EnsureSecondaryPadRevealed(
            string siteCode,
            Vector3 fallbackWorldPosition,
            CampaignWaypointMissionSite site = null)
        {
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return;
            }

            site ??= FindSiteInScene(siteCode);
            ResolveSecondaryPadPosition(siteCode, fallbackWorldPosition, out var padPosition);
            if (site == null)
            {
                if (ShouldShowSecondaryPad(siteCode, null))
                {
                    CampaignWaypointSecondaryState.TryRevealSecondary(siteCode, padPosition, null);
                }

                return;
            }

            TryRevealSecondaryPad(site, padPosition);
        }

        public static bool AreAllHostileSlotsDestroyed(string siteCode)
        {
            if (!TryCollectHostileSlotLabels(siteCode, out var labels) || labels.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                if (!AntarcticaOutpostState.IsTargetDestroyed(siteCode, labels[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void MarkAllHostileSlotsDestroyed(string siteCode)
        {
            var wasPrimaryComplete = IsPrimaryAirObjectivesComplete(siteCode);
            if (TryCollectHostileSlotLabels(siteCode, out var labels))
            {
                for (var i = 0; i < labels.Count; i++)
                {
                    AntarcticaOutpostState.MarkTargetDestroyed(siteCode, labels[i]);
                }
            }

            OutpostFlightPlatoonState.MarkPlatoonCleared(siteCode);
            if (!wasPrimaryComplete && IsPrimaryAirObjectivesComplete(siteCode))
            {
                F89.UI.MissionObjectiveFlashNotifier.FlashPrimaryEliminated();
            }
        }

        private static void TryRevealSecondaryPad(CampaignWaypointMissionSite site, Vector3 padPosition)
        {
            if (site == null || string.IsNullOrWhiteSpace(site.SiteCode))
            {
                return;
            }

            if (!ShouldShowSecondaryPad(site.SiteCode, site))
            {
                return;
            }

            ResolveSecondaryPadPosition(site.SiteCode, padPosition, out var resolved);
            CampaignWaypointSecondaryState.TryRevealSecondary(site.SiteCode, resolved, site);
        }

        private static void ResolveSecondaryPadPosition(string siteCode, Vector3 fallbackWorldPosition, out Vector3 padPosition)
        {
            fallbackWorldPosition.y = 0f;
            if (CampaignWaypointLayoutState.TryGetWorldPosition(siteCode, out padPosition))
            {
                padPosition = TacticalMapPadPlacement.ResolveInteriorWorld(padPosition, siteCode);
                return;
            }

            padPosition = TacticalMapPadPlacement.ResolveInteriorWorld(fallbackWorldPosition, siteCode);
        }

        private static bool TryCollectHostileSlotLabels(string siteCode, out List<string> labels)
        {
            labels = new List<string>();
            if (!CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint)
                || !CampaignMissionPrimarySpawnCatalog.TryGetPlan(waypoint.MissionNumber, out var plan))
            {
                return false;
            }

            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var slot = 0;
            var infantryLevel = CampaignMissionPrimarySpawnCatalog.GetInfantryLevelForMission(waypoint.MissionNumber);
            AppendSlotLabels(labels, catalog?.GetUrTroopByLevel(infantryLevel), plan.InfantryCount, ref slot);
            AppendSlotLabels(labels, catalog?.GetUrVehicleByLevel(plan.VehicleLevel), plan.VehicleCount, ref slot);
            AppendSlotLabels(labels, catalog?.GetUrVehicleByLevel(7), plan.Level7Count, ref slot);
            return labels.Count > 0;
        }

        private static void AppendSlotLabels(
            List<string> labels,
            VehicleUnitDefinition definition,
            int count,
            ref int slotIndex)
        {
            for (var i = 0; i < count; i++)
            {
                labels.Add(BuildUnitSlotLabel(definition, slotIndex));
                slotIndex++;
            }
        }

        private static string BuildUnitSlotLabel(VehicleUnitDefinition definition, int slotIndex)
        {
            var designation = definition != null ? definition.designation.ToString() : "UR";
            var abbrev = definition != null ? definition.abbreviation : "UNIT";
            return $"{designation}-{abbrev}-{slotIndex}";
        }

        private static bool HasLivingHostileVehiclesUnderSite(CampaignWaypointMissionSite site)
        {
            var units = site.GetComponentsInChildren<VehicleUnitComponent>(true);
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null
                    || unit.Definition == null
                    || !unit.Definition.IsHostile
                    || unit.Definition.isTroop)
                {
                    continue;
                }

                var target = unit.GetComponent<F89.Weapons.LockableTarget>();
                if (target != null && target.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLivingHostileUnitsUnderSite(CampaignWaypointMissionSite site)
        {
            // Primary clearance is hostile (UR) only — surviving friendly US must not block WP complete.
            var units = site.GetComponentsInChildren<VehicleUnitComponent>(true);
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null || unit.Definition == null || !unit.Definition.IsHostile)
                {
                    continue;
                }

                var target = unit.GetComponent<F89.Weapons.LockableTarget>();
                if (target != null && target.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static CampaignWaypointMissionSite FindSiteInScene(string siteCode)
        {
            var sites = Object.FindObjectsByType<CampaignWaypointMissionSite>(FindObjectsSortMode.None);
            for (var i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                if (site != null
                    && string.Equals(site.SiteCode, siteCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    return site;
                }
            }

            return null;
        }
    }
}
