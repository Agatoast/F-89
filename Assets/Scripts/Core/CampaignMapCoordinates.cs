using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Tactical-map mile coordinates (origin at map center) and canonical 1-based landing grid cells.
    /// </summary>
    public static class CampaignMapCoordinates
    {
        public static bool TryMilesToGridCell(Vector2 miles, out Vector2Int gridCell)
        {
            gridCell = Vector2Int.zero;
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            if (worldMap == null || ticSize <= 0f)
            {
                return false;
            }

            var worldUnitsPerMile = worldMap.GetWorldUnitsPerMile(ticSize);
            var world = WorldMapConfig.MileOffsetToWorld(miles, worldUnitsPerMile);
            return worldMap.TryWorldPositionToGridCell(world, ticSize, out gridCell);
        }

        public static Vector3 MilesToWorld(Vector2 miles)
        {
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            if (worldMap == null || ticSize <= 0f)
            {
                return Vector3.zero;
            }

            return WorldMapConfig.MileOffsetToWorld(miles, worldMap.GetWorldUnitsPerMile(ticSize));
        }

        public static Vector2 GridCellToMiles(Vector2Int gridCell)
        {
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            if (worldMap == null || ticSize <= 0f)
            {
                return Vector2.zero;
            }

            var world = worldMap.GridCellToWorldCenter(gridCell, ticSize);
            return WorldMapConfig.WorldToMileOffset(world, worldMap.GetWorldUnitsPerMile(ticSize));
        }

        public static string FormatMilesLabel(Vector2 miles)
        {
            return $"({miles.x:0}, {miles.y:0}) MI";
        }

        public static string FormatGridCellLabel(Vector2Int gridCell)
        {
            if (gridCell.x <= 0 || gridCell.y <= 0)
            {
                return string.Empty;
            }

            return $"GRID {gridCell.x},{gridCell.y}";
        }

        public static string FormatCoordinateSummary(Vector2 miles, Vector2Int gridCell)
        {
            var gridLabel = FormatGridCellLabel(gridCell);
            var milesLabel = FormatMilesLabel(miles);
            return string.IsNullOrEmpty(gridLabel) ? milesLabel : $"{gridLabel}  {milesLabel}";
        }

        private static WorldMapConfig LoadWorldMapConfig()
        {
            return Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
        }

        private static float ResolveTicSizeWorldUnits()
        {
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            return profile != null ? profile.ticSizeWorldUnits : 0f;
        }
    }

}
