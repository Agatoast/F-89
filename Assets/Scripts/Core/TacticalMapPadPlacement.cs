using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Keeps bunker and secondary landing pads in grid-square interiors — never on mile grid lines.
    /// </summary>
    public static class TacticalMapPadPlacement
    {
        private const float MinFractionFromLine = 0.12f;
        private const float MaxFractionFromLine = 0.88f;
        private const float StableInteriorOffsetMiles = 0.28f;

        public static Vector3 ResolveInteriorWorld(Vector3 candidateWorld, string stableKey = null)
        {
            candidateWorld.y = 0f;
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null
                || ticSize <= 0f
                || !CampaignMapCoordinates.TryWorldToGridCell(
                    candidateWorld,
                    worldMap,
                    ticSize,
                    out var gridCell))
            {
                return candidateWorld;
            }

            var center = CampaignMapCoordinates.GridCellToWorld(gridCell, worldMap, ticSize);
            var centerMiles = CampaignMapCoordinates.WorldToMiles(center, worldMap, ticSize);
            var seed = (stableKey ?? candidateWorld.ToString()).GetHashCode() ^ gridCell.GetHashCode();
            var angle = (Mathf.Abs(seed) % 360) * Mathf.Deg2Rad;
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * StableInteriorOffsetMiles;
            var padMiles = new Vector2(
                NudgeOffGridLine(centerMiles.x + offset.x),
                NudgeOffGridLine(centerMiles.y + offset.y));
            ClampMilesInsideCell(ref padMiles, gridCell);
            return CampaignMapCoordinates.MilesToWorld(padMiles, worldMap, ticSize);
        }

        private static float NudgeOffGridLine(float mileCoordinate)
        {
            var whole = Mathf.Floor(mileCoordinate);
            var fraction = mileCoordinate - whole;
            if (fraction <= MinFractionFromLine)
            {
                return whole + MinFractionFromLine;
            }

            if (fraction >= MaxFractionFromLine)
            {
                return whole + MaxFractionFromLine;
            }

            return mileCoordinate;
        }

        private static void ClampMilesInsideCell(ref Vector2 miles, Vector2Int gridCell)
        {
            var cellMinX = gridCell.x - 1 + MinFractionFromLine;
            var cellMaxX = gridCell.x - MaxFractionFromLine;
            var cellMinY = gridCell.y - 1 + MinFractionFromLine;
            var cellMaxY = gridCell.y - MaxFractionFromLine;
            miles.x = Mathf.Clamp(miles.x, cellMinX, cellMaxX);
            miles.y = Mathf.Clamp(miles.y, cellMinY, cellMaxY);
        }
    }
}
