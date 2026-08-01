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

        [Header("Flight Pace")]
        [Tooltip("World-movement multiplier for displayed mph. At max throttle (~1200 mph) the aircraft crosses about two 1-mile grid squares per second.")]
        public float travelSpeedScale = 6f;

        [Header("Antarctica")]
        public float antarcticaSizeMiles = 3000f;

        [Header("F-89 Fuel")]
        [Tooltip("Maximum flight range on a full tank at cruise power.")]
        public float maxFuelRangeMiles = 1350f;

        [Tooltip("Capacity of each wing tank in US gallons.")]
        public float fuelGallonsPerTank = 1350f;

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

        public float MphToMilesPerSecond(float mph)
        {
            var scale = travelSpeedScale > 0f ? travelSpeedScale : 1f;
            return mph / 3600f * scale;
        }

        public float MphToWorldUnits(float mph, float ticSizeWorldUnits)
        {
            return MilesPerSecondToWorldUnits(mph / 3600f, this, ticSizeWorldUnits);
        }

        public float WorldUnitsToMph(float worldSpeed, float ticSizeWorldUnits)
        {
            if (ticSizeWorldUnits <= 0f)
            {
                return 0f;
            }

            return TicsPerSecondToMph((worldSpeed / ticSizeWorldUnits) / GetTravelSpeedScale());
        }

        private float GetTravelSpeedScale()
        {
            return travelSpeedScale > 0f ? travelSpeedScale : 1f;
        }

        public static float MilesPerSecondToWorldUnits(
            float milesPerSecond,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (milesPerSecond <= 0f)
            {
                return 0f;
            }

            if (worldMap == null)
            {
                return milesPerSecond * 20f * ticSizeWorldUnits * 6f;
            }

            return worldMap.MilesToTics(milesPerSecond * worldMap.GetTravelSpeedScale()) * ticSizeWorldUnits;
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

        /// <summary>World units represented by one map mile for the supplied flight tic size.</summary>
        public float GetWorldUnitsPerMile(float ticSizeWorldUnits)
        {
            if (ticSizeWorldUnits <= 0f || milesPerGrid <= 0f)
            {
                return 0f;
            }

            return GridSpacingTics * ticSizeWorldUnits / milesPerGrid;
        }

        /// <summary>
        /// Number of one-grid-square columns and rows in Antarctica. Coordinates are 1-based:
        /// (1,1) is the southwest (bottom-left) square, then right/east first and up/north second.
        /// </summary>
        public Vector2Int GetMapGridDimensions()
        {
            var cellMiles = Mathf.Max(0.0001f, milesPerGrid);
            var width = Mathf.CeilToInt(antarcticaSizeMiles / cellMiles);
            var height = Mathf.CeilToInt(AntarcticaLandMask.GetMapHeightMiles(antarcticaSizeMiles) / cellMiles);
            return new Vector2Int(width, height);
        }

        /// <summary>
        /// Converts a world position to its canonical 1-based Antarctica map square.
        /// Column is right/east; row is up/north; (1,1) is the southwest map square.
        /// </summary>
        public bool TryWorldPositionToGridCell(
            Vector3 worldPosition,
            float ticSizeWorldUnits,
            out Vector2Int gridCell)
        {
            gridCell = Vector2Int.zero;
            var cellSizeWorld = GridSpacingTics * ticSizeWorldUnits;
            if (cellSizeWorld <= 0f)
            {
                return false;
            }

            var dimensions = GetMapGridDimensions();
            var worldUnitsPerMile = GetWorldUnitsPerMile(ticSizeWorldUnits);
            var mapWidthWorld = antarcticaSizeMiles * worldUnitsPerMile;
            var mapHeightWorld = AntarcticaLandMask.GetMapHeightMiles(antarcticaSizeMiles) * worldUnitsPerMile;
            var minX = -mapWidthWorld * 0.5f;
            var minZ = -mapHeightWorld * 0.5f;
            if (worldPosition.x < minX || worldPosition.x > minX + mapWidthWorld
                || worldPosition.z < minZ || worldPosition.z > minZ + mapHeightWorld)
            {
                return false;
            }

            var column = Mathf.FloorToInt((worldPosition.x - minX) / cellSizeWorld) + 1;
            var row = Mathf.FloorToInt((worldPosition.z - minZ) / cellSizeWorld) + 1;
            gridCell = new Vector2Int(
                Mathf.Clamp(column, 1, dimensions.x),
                Mathf.Clamp(row, 1, dimensions.y));
            return true;
        }

        /// <summary>Returns the world-space center of a canonical 1-based map square.</summary>
        public Vector3 GridCellToWorldCenter(Vector2Int gridCell, float ticSizeWorldUnits, float worldY = 0f)
        {
            var dimensions = GetMapGridDimensions();
            var column = Mathf.Clamp(gridCell.x, 1, dimensions.x);
            var row = Mathf.Clamp(gridCell.y, 1, dimensions.y);
            var cellSizeWorld = GridSpacingTics * ticSizeWorldUnits;
            var worldUnitsPerMile = GetWorldUnitsPerMile(ticSizeWorldUnits);
            var mapWidthWorld = antarcticaSizeMiles * worldUnitsPerMile;
            var mapHeightWorld = AntarcticaLandMask.GetMapHeightMiles(antarcticaSizeMiles) * worldUnitsPerMile;
            return new Vector3(
                -mapWidthWorld * 0.5f + (column - 0.5f) * cellSizeWorld,
                worldY,
                -mapHeightWorld * 0.5f + (row - 0.5f) * cellSizeWorld);
        }

        /// <summary>
        /// Canonical 1-based map square. Use <see cref="TryWorldPositionToGridCell"/> when
        /// an out-of-bounds result must be distinguished from the southwest square.
        /// </summary>
        public Vector2Int WorldPositionToGridCell(Vector3 worldPosition, float ticSizeWorldUnits)
        {
            return TryWorldPositionToGridCell(worldPosition, ticSizeWorldUnits, out var gridCell)
                ? gridCell
                : Vector2Int.zero;
        }

        public bool IsWithinAntarcticaBounds(Vector3 worldPosition, float ticSizeWorldUnits)
        {
            var halfWidthWorld = MilesToTics(antarcticaSizeMiles * 0.5f) * ticSizeWorldUnits;
            var halfHeightWorld = halfWidthWorld / AntarcticaLandMask.GetMapWidthOverHeight();
            return Mathf.Abs(worldPosition.x) <= halfWidthWorld
                && Mathf.Abs(worldPosition.z) <= halfHeightWorld;
        }
    }
}
