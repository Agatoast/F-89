using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Canonical tactical-map coordinates for the entire game.
    /// Origin (0,0) is the southwest corner of the theater. +X is east (columns read left-to-right),
    /// +Y is north (rows read bottom-to-top). Each 1×1 MI grid square is labeled by its 1-based cell
    /// index — block 103 miles east and 100 miles north of the origin is (103, 100).
    /// </summary>
    public static class CampaignMapCoordinates
    {
        public static float GetMapWidthMiles(WorldMapConfig worldMap) =>
            worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;

        public static float GetMapHeightMiles(WorldMapConfig worldMap) =>
            AntarcticaLandMask.GetMapHeightMiles(GetMapWidthMiles(worldMap));

        public static Vector2 GetMapCenterMiles(WorldMapConfig worldMap) =>
            new Vector2(GetMapWidthMiles(worldMap) * 0.5f, GetMapHeightMiles(worldMap) * 0.5f);

        public static Vector2 WorldToMiles(Vector3 worldPosition, WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, ticSizeWorldUnits);
            if (worldMap == null || worldUnitsPerMile <= 0f)
            {
                return Vector2.zero;
            }

            var minCorner = GetSouthwestWorldCorner(worldMap, ticSizeWorldUnits);
            return new Vector2(
                (worldPosition.x - minCorner.x) / worldUnitsPerMile,
                (worldPosition.z - minCorner.z) / worldUnitsPerMile);
        }

        public static Vector3 MilesToWorld(Vector2 miles, WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, ticSizeWorldUnits);
            if (worldMap == null || worldUnitsPerMile <= 0f)
            {
                return Vector3.zero;
            }

            var minCorner = GetSouthwestWorldCorner(worldMap, ticSizeWorldUnits);
            return new Vector3(
                minCorner.x + miles.x * worldUnitsPerMile,
                0f,
                minCorner.z + miles.y * worldUnitsPerMile);
        }

        public static Vector3 GridCellToWorld(Vector2Int gridCell, WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return Vector3.zero;
            }

            return worldMap.GridCellToWorldCenter(gridCell, ticSizeWorldUnits);
        }

        public static bool TryWorldToGridCell(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out Vector2Int gridCell)
        {
            gridCell = Vector2Int.zero;
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            return worldMap.TryWorldPositionToGridCell(worldPosition, ticSizeWorldUnits, out gridCell);
        }

        public static bool TryMilesToGridCell(Vector2 miles, out Vector2Int gridCell)
        {
            gridCell = Vector2Int.zero;
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            if (worldMap == null || ticSize <= 0f)
            {
                return false;
            }

            return TryWorldToGridCell(MilesToWorld(miles, worldMap, ticSize), worldMap, ticSize, out gridCell);
        }

        /// <summary>1-based grid index for internal map layout only — not shown to the player.</summary>
        public static Vector2Int MilesToGridCellLabel(Vector2 miles)
        {
            if (!TryMilesToGridCell(miles, out var gridCell))
            {
                return Vector2Int.zero;
            }

            return gridCell;
        }

        /// <summary>Grid cell label as tactical miles — e.g. cell (103,100) → (103,100).</summary>
        public static Vector2 GridCellToMiles(Vector2Int gridCell) =>
            new Vector2(gridCell.x, gridCell.y);

        public static Vector2 WorldToMiles(Vector3 worldPosition)
        {
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            return WorldToMiles(worldPosition, worldMap, ticSize);
        }

        public static Vector3 MilesToWorld(Vector2 miles)
        {
            var worldMap = LoadWorldMapConfig();
            var ticSize = ResolveTicSizeWorldUnits();
            return MilesToWorld(miles, worldMap, ticSize);
        }

        public static string FormatMilesLabel(Vector2 miles) =>
            $"({miles.x:0}, {miles.y:0}) MI";

        private static Vector3 GetSouthwestWorldCorner(WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, ticSizeWorldUnits);
            var mapWidthWorld = GetMapWidthMiles(worldMap) * worldUnitsPerMile;
            var mapHeightWorld = GetMapHeightMiles(worldMap) * worldUnitsPerMile;
            return new Vector3(-mapWidthWorld * 0.5f, 0f, -mapHeightWorld * 0.5f);
        }

        private static float ResolveWorldUnitsPerMile(WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return 0f;
            }

            return worldMap.GetWorldUnitsPerMile(ticSizeWorldUnits);
        }

        /// <summary>Locked tactical miles for map markers, waypoints, and autopilot (never runtime drift).</summary>
        public static bool TryGetLockedBaseMiles(AntarcticaBase baseSite, out Vector2 miles)
        {
            miles = Vector2.zero;
            if (baseSite == null)
            {
                return false;
            }

            if (baseSite.SiteKind == BaseSiteKind.Carrier)
            {
                miles = AntarcticaWorldLocations.LockedCarrierPositionMiles;
                return true;
            }

            if (CampaignMapLayoutState.TryGetSite(baseSite.BaseName, out var site)
                || CampaignMapLayoutState.TryGetSiteByCode(baseSite.SiteCode, out site))
            {
                var locked = CampaignMapLayoutState.GetLockedMiles(site);
                if (locked.sqrMagnitude > 0.01f)
                {
                    miles = locked;
                    return true;
                }
            }

            miles = baseSite.PositionMiles;
            return true;
        }

        public static Vector3 GetLockedBaseWorldPosition(
            AntarcticaBase baseSite,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            TryGetLockedBaseMiles(baseSite, out var miles);
            return MilesToWorld(miles, worldMap, ticSizeWorldUnits);
        }

        private static WorldMapConfig LoadWorldMapConfig() =>
            Resources.Load<WorldMapConfig>("F89_WorldMapConfig");

        private static float ResolveTicSizeWorldUnits()
        {
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            return profile != null ? profile.ticSizeWorldUnits : 0f;
        }
    }
}
