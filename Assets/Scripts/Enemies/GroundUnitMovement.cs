using F89.Core;
using UnityEngine;

namespace F89.Enemies
{
    public static class GroundUnitMovement
    {
        public static bool TryMoveOnLand(
            Transform unitTransform,
            Vector3 deltaWorld,
            WorldMapConfig worldMap,
            float worldUnitsPerMile)
        {
            if (unitTransform == null || worldMap == null || deltaWorld.sqrMagnitude < 0.000001f)
            {
                return false;
            }

            var mapSizeMiles = worldMap.antarcticaSizeMiles;
            var fullDelta = unitTransform.position + deltaWorld;
            if (IsLandWorldPosition(fullDelta, worldMap, worldUnitsPerMile, mapSizeMiles))
            {
                unitTransform.position = fullDelta;
                return true;
            }

            var deltaX = new Vector3(deltaWorld.x, 0f, 0f);
            var deltaZ = new Vector3(0f, 0f, deltaWorld.z);
            if (deltaX.sqrMagnitude > 0.000001f)
            {
                var xOnly = unitTransform.position + deltaX;
                if (IsLandWorldPosition(xOnly, worldMap, worldUnitsPerMile, mapSizeMiles))
                {
                    unitTransform.position = xOnly;
                    return true;
                }
            }

            if (deltaZ.sqrMagnitude > 0.000001f)
            {
                var zOnly = unitTransform.position + deltaZ;
                if (IsLandWorldPosition(zOnly, worldMap, worldUnitsPerMile, mapSizeMiles))
                {
                    unitTransform.position = zOnly;
                    return true;
                }
            }

            return false;
        }

        private static bool IsLandWorldPosition(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float worldUnitsPerMile,
            float mapSizeMiles)
        {
            var miles = WorldMapConfig.WorldToMileOffset(worldPosition, worldUnitsPerMile);
            return AntarcticaLandMask.IsDisplayLandMiles(miles, mapSizeMiles);
        }
    }
}
