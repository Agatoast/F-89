using F89.Core;
using F89.Weapons;
using UnityEngine;

namespace F89.Flight
{
    public static class CombatThreatRange
    {
        private const float TargetCacheRefreshSeconds = 1.5f;

        private static LockableTarget[] cachedTargets = System.Array.Empty<LockableTarget>();
        private static AntarcticaBase[] cachedBases = System.Array.Empty<AntarcticaBase>();
        private static float nextTargetCacheRefreshTime;
        private static float nextBaseCacheRefreshTime;

        public static void InvalidateCaches()
        {
            nextTargetCacheRefreshTime = 0f;
            nextBaseCacheRefreshTime = 0f;
        }

        public static LockableTarget[] GetCachedLockableTargets() => GetCachedTargets();

        public static AntarcticaBase[] GetCachedAntarcticaBases() => GetCachedBases();

        public static bool HasHostileContact(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            return HasHostileUnitWithinMiles(
                    worldPosition,
                    AutopilotController.HostileUnitContactMiles,
                    worldMap,
                    ticSizeWorldUnits)
                || HasHostileBaseWithinMiles(
                    worldPosition,
                    AutopilotController.HostileBaseContactMiles,
                    worldMap,
                    ticSizeWorldUnits);
        }

        /// <summary>
        /// Hostile units that should disengage autopilot: vehicles, troops, and bunkers — not generic buildings.
        /// </summary>
        public static bool HasHostileAutopilotContact(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (HasHostileBaseWithinMiles(
                    worldPosition,
                    AutopilotController.HostileBaseContactMiles,
                    worldMap,
                    ticSizeWorldUnits))
            {
                return true;
            }

            return HasHostileUnitWithinMiles(
                worldPosition,
                AutopilotController.HostileUnitContactMiles,
                worldMap,
                ticSizeWorldUnits,
                autopilotRelevantOnly: true);
        }

        public static bool HasHostileUnitWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            bool autopilotRelevantOnly = false)
        {
            if (worldMap == null || rangeMiles <= 0f)
            {
                return false;
            }

            var targets = GetCachedTargets();
            foreach (var target in targets)
            {
                if (target == null
                    || !target.IsAlive
                    || target.IsFriendly
                    || target.IsNeutral
                    || target.GetComponent<AntarcticaBase>() != null)
                {
                    continue;
                }

                var building = target.GetComponent<OutpostBuilding>();
                if (building != null
                    && !OutpostPrimaryObjective.IsMissionHostileBuilding(
                        building.BuildingType,
                        target.TargetLabel))
                {
                    continue;
                }

                if (autopilotRelevantOnly && !IsAutopilotRelevantHostileUnit(target))
                {
                    continue;
                }

                if (TryGetHostOutpostName(target, out var hostOutpost)
                    && IsClearedOutpostForAutopilotTransit(hostOutpost))
                {
                    continue;
                }

                if (IsTargetOnCarrier(target))
                {
                    continue;
                }

                if (IsWithinMiles(worldPosition, target.transform.position, rangeMiles, worldMap, ticSizeWorldUnits))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAutopilotRelevantHostileUnit(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFriendly
                || target.IsFlareDecoy
                || target.IsPlayerAircraft)
            {
                return false;
            }

            if (target.GetComponent<AntarcticaBase>() != null)
            {
                return false;
            }

            var building = target.GetComponent<OutpostBuilding>();
            if (building != null)
            {
                if (TryGetHostOutpostName(target, out var hostOutpost)
                    && IsClearedOutpostForAutopilotTransit(hostOutpost))
                {
                    return false;
                }

                return building.BuildingType == OutpostBuildingType.Bunker && !building.IsDestroyed;
            }

            return true;
        }

        private static bool IsTargetOnCarrier(LockableTarget target)
        {
            if (target == null)
            {
                return false;
            }

            var hostBase = target.GetComponentInParent<AntarcticaBase>();
            return hostBase != null && hostBase.SiteKind == BaseSiteKind.Carrier;
        }

        private static bool TryGetHostOutpostName(LockableTarget target, out string outpostName)
        {
            outpostName = string.Empty;
            if (target == null)
            {
                return false;
            }

            var baseSite = target.GetComponentInParent<AntarcticaBase>();
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
            {
                return false;
            }

            outpostName = baseSite.BaseName;
            return !string.IsNullOrWhiteSpace(outpostName);
        }

        /// <summary>Cleared/neutral outposts no longer block departure on autopilot.</summary>
        private static bool IsClearedOutpostForAutopilotTransit(string outpostName) =>
            AntarcticaOutpostState.IsNeutralOutpost(outpostName)
            || AntarcticaOutpostState.IsFriendlyOccupied(outpostName);

        public static bool HasHostileBaseWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null || rangeMiles <= 0f)
            {
                return false;
            }

            var bases = GetCachedBases();
            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive || baseSite.IsDestroyed)
                {
                    continue;
                }

                if (baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    continue;
                }

                if (baseSite.Control != BaseControl.Hostile)
                {
                    continue;
                }

                if (IsClearedOutpostForAutopilotTransit(baseSite.BaseName))
                {
                    continue;
                }

                if (IsWithinMiles(worldPosition, baseSite.transform.position, rangeMiles, worldMap, ticSizeWorldUnits))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasThreatWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            return HasHostileUnitWithinMiles(worldPosition, rangeMiles, worldMap, ticSizeWorldUnits)
                || HasHostileBaseWithinMiles(worldPosition, rangeMiles, worldMap, ticSizeWorldUnits);
        }

        public static float DistanceMiles(
            Vector3 a,
            Vector3 b,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null)
            {
                return Vector3.Distance(a, b) / (20f * ticSizeWorldUnits);
            }

            var worldUnitsPerMile = worldMap.GridSpacingTics * ticSizeWorldUnits / worldMap.milesPerGrid;
            if (worldUnitsPerMile <= 0f)
            {
                return 0f;
            }

            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude / worldUnitsPerMile;
        }

        private static LockableTarget[] GetCachedTargets()
        {
            if (Time.unscaledTime >= nextTargetCacheRefreshTime)
            {
                cachedTargets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
                nextTargetCacheRefreshTime = Time.unscaledTime + TargetCacheRefreshSeconds;
            }

            return cachedTargets;
        }

        private static AntarcticaBase[] GetCachedBases()
        {
            if (Time.unscaledTime >= nextBaseCacheRefreshTime)
            {
                cachedBases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
                nextBaseCacheRefreshTime = Time.unscaledTime + TargetCacheRefreshSeconds;
            }

            return cachedBases;
        }

        private static bool IsWithinMiles(
            Vector3 a,
            Vector3 b,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            return DistanceMiles(a, b, worldMap, ticSizeWorldUnits) <= rangeMiles;
        }
    }
}
