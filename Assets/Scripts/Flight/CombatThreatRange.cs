using F89.Core;
using F89.Weapons;
using UnityEngine;

namespace F89.Flight
{
    public static class CombatThreatRange
    {
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

        public static bool HasHostileUnitWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null || rangeMiles <= 0f)
            {
                return false;
            }

            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.IsFriendly)
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

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive || baseSite.IsDestroyed)
                {
                    continue;
                }

                if (baseSite.Control != BaseControl.Hostile)
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
