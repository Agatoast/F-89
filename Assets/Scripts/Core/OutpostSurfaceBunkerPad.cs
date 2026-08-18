using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Flat black bunker pad left on the ground after the bunker building cube is destroyed.
    /// </summary>
    public sealed class OutpostSurfaceBunkerPad : MonoBehaviour
    {
        private const string PadObjectName = "SurfaceBunkerPad";

        public static bool IsRevealedAtOutpost(AntarcticaBase baseSite)
        {
            if (baseSite == null)
            {
                return false;
            }

            var cluster = baseSite.transform.Find(OutpostBuildingClusterSpawner.ClusterRootName);
            if (cluster != null && cluster.Find(PadObjectName) != null)
            {
                return true;
            }

            if (baseSite.transform.Find(PadObjectName) != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsRevealedAtOutpostName(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null
                    || baseSite.SiteKind != BaseSiteKind.Land
                    || !string.Equals(baseSite.BaseName, outpostName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return IsRevealedAtOutpost(baseSite);
            }

            return false;
        }

        public static void EnsureAt(Transform padParent, Vector3 groundPosition, float footprint)
        {
            if (padParent == null || footprint <= 0f)
            {
                return;
            }

            groundPosition = TacticalMapPadPlacement.ResolveInteriorWorld(
                groundPosition,
                padParent != null ? padParent.name : null);
            groundPosition.y = 0f;

            if (padParent.Find(PadObjectName) != null)
            {
                return;
            }

            var padHeight = Mathf.Max(footprint * 0.08f, 0.05f);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = PadObjectName;
            pad.transform.SetParent(padParent, true);
            pad.transform.position = new Vector3(groundPosition.x, padHeight * 0.5f, groundPosition.z);
            pad.transform.localRotation = Quaternion.identity;
            pad.transform.localScale = new Vector3(footprint, padHeight, footprint);

            var collider = pad.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = F89RenderMaterials.CreateUnlit(Color.black);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            pad.AddComponent<OutpostSurfaceBunkerPad>();

            var cluster = padParent.GetComponent<OutpostBuildingCluster>();
            cluster?.SetBunkerSurfaceRevealed(true);
        }

        /// <summary>World position of the bunker pad or runway bunker anchor for grid-cell landing checks.</summary>
        public static bool TryGetBunkerGroundPosition(AntarcticaBase baseSite, out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
            {
                return false;
            }

            var pad = FindPadTransform(baseSite);
            if (pad != null)
            {
                groundPosition = pad.position;
                groundPosition.y = 0f;
                return true;
            }

            var runwayBunker = baseSite.transform.Find(OutpostRunwayVisual.RunwayBunkerObjectName);
            if (runwayBunker != null && runwayBunker.gameObject.activeSelf)
            {
                groundPosition = runwayBunker.position;
                groundPosition.y = 0f;
                return true;
            }

            var runway = baseSite.transform.Find(OutpostRunwayVisual.RunwayObjectName);
            if (runway != null)
            {
                groundPosition = OutpostRunwayVisual.GetRunwayBunkerWorldPosition(runway);
                groundPosition.y = 0f;
                return true;
            }

            return false;
        }

        public static bool TryGetBunkerGroundPosition(string outpostName, out Vector3 groundPosition)
        {
            groundPosition = Vector3.zero;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate == null
                    || candidate.SiteKind != BaseSiteKind.Land
                    || !string.Equals(candidate.BaseName, outpostName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                return TryGetBunkerGroundPosition(candidate, out groundPosition);
            }

            return false;
        }

        private static Transform FindPadTransform(AntarcticaBase baseSite)
        {
            var cluster = baseSite.transform.Find(OutpostBuildingClusterSpawner.ClusterRootName);
            if (cluster != null)
            {
                var pad = cluster.Find(PadObjectName);
                if (pad != null)
                {
                    return pad;
                }
            }

            return baseSite.transform.Find(PadObjectName);
        }
    }
}
