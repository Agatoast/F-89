using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Fixed world-map locations for the full campaign. The carrier has exactly one valid deck site.
    /// </summary>
    public static class AntarcticaWorldLocations
    {
        public const string CarrierName = "USS Martin Van Buren";

        /// <summary>
        /// Sole valid USS Martin Van Buren deck position (tactical miles: southwest origin, +X east, +Y north).
        /// Snapped to the center of the 441–442 × 2052–2053 MI grid square.
        /// </summary>
        public static readonly Vector2 LockedCarrierPositionMiles = new Vector2(441.5f, 2052.5f);

        /// <summary>Alias for <see cref="LockedCarrierPositionMiles"/>.</summary>
        public static Vector2 DefaultCarrierPositionMiles => LockedCarrierPositionMiles;

        public static Vector2 CarrierPositionMiles => LockedCarrierPositionMiles;

        public static void SetCarrierPositionMiles(Vector2 miles)
        {
            if (Vector2.Distance(miles, LockedCarrierPositionMiles) > 0.01f)
            {
                Debug.LogWarning(
                    $"F-89: Ignored carrier override ({miles.x:0.0}, {miles.y:0.0}) MI — "
                    + $"CV is locked at ({LockedCarrierPositionMiles.x:0.0}, {LockedCarrierPositionMiles.y:0.0}) MI.");
            }
        }

        public static Vector3 GetLockedCarrierWorldPosition(WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            return CampaignMapCoordinates.MilesToWorld(LockedCarrierPositionMiles, worldMap, ticSizeWorldUnits);
        }

        public static bool IsWithinCarrierDeckRange(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            float rangeMiles)
        {
            if (worldMap == null || ticSizeWorldUnits <= 0f || rangeMiles <= 0f)
            {
                return false;
            }

            var carrierWorld = GetLockedCarrierWorldPosition(worldMap, ticSizeWorldUnits);
            worldPosition.y = 0f;
            carrierWorld.y = 0f;
            var rangeWorld = WorldMapConfig.RangeMilesToWorldUnits(rangeMiles, worldMap, ticSizeWorldUnits);
            return Vector3.Distance(worldPosition, carrierWorld) <= rangeWorld;
        }
    }
}
