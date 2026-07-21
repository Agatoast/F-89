using UnityEngine;

namespace F89.Core
{
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "F-89/World Map Config")]
    public class WorldMapConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Distance between grid lines, in tics.")]
        public float gridSpacingTics = 20f;

        [Tooltip("Each grid cell is this many miles square.")]
        public float milesPerGrid = 1f;

        [Header("Antarctica")]
        public float antarcticaSizeMiles = 3000f;

        [Header("F-89 Fuel")]
        [Tooltip("Maximum flight range on a full tank at cruise power.")]
        public float maxFuelRangeMiles = 1350f;

        public float GridSpacingTics => gridSpacingTics;

        public float TicsPerMile => gridSpacingTics / milesPerGrid;

        public float AntarcticaSizeTics => MilesToTics(antarcticaSizeMiles);

        public float TicsToMiles(float tics)
        {
            return tics / TicsPerMile;
        }

        public float MilesToTics(float miles)
        {
            return miles * TicsPerMile;
        }

        public float TicsPerSecondToMph(float ticsPerSecond)
        {
            return TicsToMiles(ticsPerSecond) * 3600f;
        }

        public static float MilesPerSecondToWorldUnits(
            float milesPerSecond,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null || milesPerSecond <= 0f)
            {
                return milesPerSecond * 20f * ticSizeWorldUnits;
            }

            return worldMap.MilesToTics(milesPerSecond) * ticSizeWorldUnits;
        }

        public static float RangeMilesToWorldUnits(
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (worldMap == null || rangeMiles <= 0f)
            {
                return rangeMiles * 20f * ticSizeWorldUnits;
            }

            return worldMap.MilesToTics(rangeMiles) * ticSizeWorldUnits;
        }

        /// <summary>
        /// World +X = east, world +Z = north (matches radar and flight forward).
        /// Tactical-map mile +Y renders toward screen bottom (visual south), so mile Y = -world Z.
        /// </summary>
        public static Vector3 MileOffsetToWorld(Vector2 miles, float worldUnitsPerMile)
        {
            return new Vector3(miles.x * worldUnitsPerMile, 0f, -miles.y * worldUnitsPerMile);
        }

        public static Vector2 WorldToMileOffset(Vector3 worldPosition, float worldUnitsPerMile)
        {
            if (worldUnitsPerMile <= 0f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                worldPosition.x / worldUnitsPerMile,
                -worldPosition.z / worldUnitsPerMile);
        }

        public Vector2Int WorldPositionToGridCell(Vector3 worldPosition, float ticSizeWorldUnits)
        {
            var spacingWorld = GridSpacingTics * ticSizeWorldUnits;
            if (spacingWorld <= 0f)
            {
                return Vector2Int.zero;
            }

            var cellX = Mathf.FloorToInt(worldPosition.x / spacingWorld);
            var cellZ = Mathf.FloorToInt(worldPosition.z / spacingWorld);
            return new Vector2Int(cellX, cellZ);
        }

        public bool IsWithinAntarcticaBounds(Vector3 worldPosition, float ticSizeWorldUnits)
        {
            var halfExtentWorld = MilesToTics(antarcticaSizeMiles * 0.5f) * ticSizeWorldUnits;
            return Mathf.Abs(worldPosition.x) <= halfExtentWorld
                && Mathf.Abs(worldPosition.z) <= halfExtentWorld;
        }
    }
}
