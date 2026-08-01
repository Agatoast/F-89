using UnityEngine;

namespace F89.Core
{
    /// <summary>Resolves whether an aircraft landed on an outpost bunker grid square.</summary>
    public static class AntarcticaOutpostLandingResolver
    {
        /// <summary>
        /// True when the landing position shares the same 1 MI x 1 MI map grid cell as the outpost bunker pad.
        /// </summary>
        public static bool IsLandingInBunkerGrid(
            Vector3 landingWorldPosition,
            string outpostName,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (string.IsNullOrWhiteSpace(outpostName)
                || worldMap == null
                || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    landingWorldPosition,
                    ticSizeWorldUnits,
                    out var landingCell))
            {
                return false;
            }

            if (!OutpostSurfaceBunkerPad.TryGetBunkerGroundPosition(outpostName, out var bunkerPosition))
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    bunkerPosition,
                    ticSizeWorldUnits,
                    out var bunkerCell))
            {
                return false;
            }

            return landingCell == bunkerCell;
        }

        /// <summary>
        /// Assigns an outpost only when the aircraft landed in that outpost's bunker map grid cell.
        /// </summary>
        public static bool TryResolveBunkerGridLanding(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string outpostName)
        {
            outpostName = string.Empty;
            if (worldMap == null || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            if (!worldMap.TryWorldPositionToGridCell(
                    landingWorldPosition,
                    ticSizeWorldUnits,
                    out var landingCell))
            {
                return false;
            }

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate == null
                    || candidate.SiteKind != BaseSiteKind.Land
                    || !candidate.IsActive)
                {
                    continue;
                }

                if (!OutpostSurfaceBunkerPad.TryGetBunkerGroundPosition(candidate, out var bunkerPosition))
                {
                    continue;
                }

                if (!worldMap.TryWorldPositionToGridCell(
                        bunkerPosition,
                        ticSizeWorldUnits,
                        out var bunkerCell)
                    || bunkerCell != landingCell)
                {
                    continue;
                }

                outpostName = candidate.BaseName;
                return true;
            }

            return false;
        }

        /// <summary>Legacy entry point — resolves only an exact bunker-grid landing.</summary>
        public static bool TryResolveOutpost(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string outpostName)
        {
            return TryResolveBunkerGridLanding(
                landingWorldPosition,
                worldMap,
                ticSizeWorldUnits,
                out outpostName);
        }
    }
}
