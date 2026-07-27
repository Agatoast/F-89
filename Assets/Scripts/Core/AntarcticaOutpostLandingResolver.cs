using UnityEngine;

namespace F89.Core
{
    /// <summary>Resolves whether an aircraft landed in the same one-mile square as an active land outpost.</summary>
    public static class AntarcticaOutpostLandingResolver
    {
        public static bool TryResolveOutpost(
            Vector3 landingWorldPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out string outpostName)
        {
            outpostName = string.Empty;
            if (worldMap == null)
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

                if (!worldMap.TryWorldPositionToGridCell(
                        candidate.transform.position,
                        ticSizeWorldUnits,
                        out var outpostCell)
                    || outpostCell != landingCell)
                {
                    continue;
                }

                outpostName = candidate.BaseName;
                return true;
            }

            return false;
        }
    }
}
