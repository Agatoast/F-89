using System.Collections.Generic;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Building group at a land outpost. Vehicles/troops must stay outside KeepOutRadiusWorld.
    /// </summary>
    public sealed class OutpostBuildingCluster : MonoBehaviour
    {
        private static readonly List<OutpostBuildingCluster> Active = new List<OutpostBuildingCluster>(64);

        [SerializeField] private Vector3 centerWorld;
        [SerializeField] private float keepOutRadiusWorld;
        [SerializeField] private bool hasBunker;
        [SerializeField] private bool blocksUnits = true;

        public Vector3 CenterWorld => centerWorld;
        public float KeepOutRadiusWorld => keepOutRadiusWorld;
        public bool HasBunker => hasBunker;
        public bool BlocksUnits => blocksUnits && isActiveAndEnabled && !OutpostIsDestroyed();

        public void Configure(Vector3 center, float keepOutRadius, bool bunkerPresent)
        {
            centerWorld = center;
            centerWorld.y = 0f;
            keepOutRadiusWorld = keepOutRadius;
            hasBunker = bunkerPresent;
            blocksUnits = true;
        }

        public void SetBlocksUnits(bool value)
        {
            blocksUnits = value;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private bool OutpostIsDestroyed()
        {
            var baseSite = GetComponentInParent<AntarcticaBase>();
            return baseSite != null && baseSite.IsDestroyed;
        }

        public static bool IsInsideAnyKeepOut(Vector3 worldPosition)
        {
            worldPosition.y = 0f;
            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var cluster = Active[i];
                if (cluster == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }

                if (!cluster.BlocksUnits)
                {
                    continue;
                }

                var dx = worldPosition.x - cluster.centerWorld.x;
                var dz = worldPosition.z - cluster.centerWorld.z;
                var radius = cluster.keepOutRadiusWorld;
                if ((dx * dx) + (dz * dz) < radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetKeepOutPush(
            Vector3 worldPosition,
            out Vector3 pushDirection,
            out float penetration)
        {
            pushDirection = Vector3.zero;
            penetration = 0f;
            worldPosition.y = 0f;
            var bestPenetration = 0f;
            var bestDir = Vector3.zero;
            var found = false;

            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var cluster = Active[i];
                if (cluster == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }

                if (!cluster.BlocksUnits)
                {
                    continue;
                }

                var away = worldPosition - cluster.centerWorld;
                away.y = 0f;
                var distance = away.magnitude;
                var radius = cluster.keepOutRadiusWorld;
                if (distance >= radius)
                {
                    continue;
                }

                var depth = radius - distance;
                if (!found || depth > bestPenetration)
                {
                    found = true;
                    bestPenetration = depth;
                    bestDir = distance > 0.0001f
                        ? away / distance
                        : Vector3.forward;
                }
            }

            if (!found)
            {
                return false;
            }

            pushDirection = bestDir;
            penetration = bestPenetration;
            return true;
        }
    }
}
