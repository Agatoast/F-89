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
            float ticSizeWorldUnits)
        {
            if (unitTransform == null || worldMap == null || deltaWorld.sqrMagnitude < 0.000001f)
            {
                return false;
            }

            var mapSizeMiles = worldMap.antarcticaSizeMiles;
            var fullDelta = unitTransform.position + deltaWorld;
            if (IsAllowedWorldPosition(fullDelta, worldMap, ticSizeWorldUnits, mapSizeMiles))
            {
                unitTransform.position = fullDelta;
                return true;
            }

            // Soft push out of a building keep-out if already inside.
            if (OutpostBuildingCluster.TryGetKeepOutPush(unitTransform.position, out var push, out var penetration)
                && penetration > 0.001f)
            {
                var escape = push * Mathf.Min(penetration, deltaWorld.magnitude + penetration * 0.25f);
                var escaped = unitTransform.position + escape;
                if (IsAllowedWorldPosition(escaped, worldMap, ticSizeWorldUnits, mapSizeMiles))
                {
                    unitTransform.position = escaped;
                    return true;
                }
            }

            var deltaX = new Vector3(deltaWorld.x, 0f, 0f);
            var deltaZ = new Vector3(0f, 0f, deltaWorld.z);
            if (deltaX.sqrMagnitude > 0.000001f)
            {
                var xOnly = unitTransform.position + deltaX;
                if (IsAllowedWorldPosition(xOnly, worldMap, ticSizeWorldUnits, mapSizeMiles))
                {
                    unitTransform.position = xOnly;
                    return true;
                }
            }

            if (deltaZ.sqrMagnitude > 0.000001f)
            {
                var zOnly = unitTransform.position + deltaZ;
                if (IsAllowedWorldPosition(zOnly, worldMap, ticSizeWorldUnits, mapSizeMiles))
                {
                    unitTransform.position = zOnly;
                    return true;
                }
            }

            return false;
        }

        public static bool IsAllowedWorldPosition(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            float mapSizeMiles = -1f)
        {
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            if (mapSizeMiles <= 0f)
            {
                mapSizeMiles = worldMap.antarcticaSizeMiles;
            }

            if (OutpostBuildingCluster.IsInsideAnyKeepOut(worldPosition))
            {
                return false;
            }

            return AntarcticaLandMask.GetLandBlendAtWorld(worldPosition, worldMap, ticSizeWorldUnits) >= 0.5f;
        }
    }
}
