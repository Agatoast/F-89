using System.Collections.Generic;
using F89.Weapons;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Spawns permanent outpost buildings (1–2 type-3, 30–40 type-1/2, optional bunker cube)
    /// and registers the vehicle/troop keep-out zone around the cluster.
    /// </summary>
    public static class OutpostBuildingClusterSpawner
    {
        private const string ClusterRootName = "BuildingCluster";

        public static void EnsureAllLandOutpostClusters(float worldUnitsPerMile, float ticSizeWorldUnits = 1f)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                EnsureCluster(bases[i], worldUnitsPerMile, ticSizeWorldUnits);
            }
        }

        public static void EnsureCluster(
            AntarcticaBase baseSite,
            float worldUnitsPerMile,
            float ticSizeWorldUnits = 1f)
        {
            if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
            {
                return;
            }

            var existing = baseSite.transform.Find(ClusterRootName);
            if (baseSite.IsDestroyed)
            {
                if (existing != null)
                {
                    Object.Destroy(existing.gameObject);
                }

                return;
            }

            if (existing != null)
            {
                EnsureBuildingLockableTargets(existing, ticSizeWorldUnits);
                return;
            }

            SpawnCluster(baseSite, worldUnitsPerMile, ticSizeWorldUnits);
        }

        private static void EnsureBuildingLockableTargets(Transform clusterRoot, float ticSizeWorldUnits)
        {
            if (clusterRoot == null)
            {
                return;
            }

            var footprint = OutpostGroundRules.FootprintWorld(ticSizeWorldUnits);
            var buildings = clusterRoot.GetComponentsInChildren<OutpostBuilding>(true);
            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (building == null || building.IsDestroyed)
                {
                    continue;
                }

                var lockable = building.GetComponent<LockableTarget>();
                if (lockable == null)
                {
                    lockable = building.gameObject.AddComponent<LockableTarget>();
                    var label = building.BuildingType == OutpostBuildingType.Bunker
                        ? "Bunker"
                        : building.BuildingType.ToString();
                    lockable.Configure(
                        label,
                        LockableTargetKind.Ground,
                        TargetAffiliation.Hostile,
                        TargetUnitClass.Building);
                    lockable.SetMaxGroundHitPoints(OutpostBuildingGhp.ForType(building.BuildingType));
                    lockable.SetHitRadiusWorld(footprint * 0.55f);
                }

                var collider = building.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }
        }

        private static void SpawnCluster(
            AntarcticaBase baseSite,
            float worldUnitsPerMile,
            float ticSizeWorldUnits)
        {
            var footprint = OutpostGroundRules.FootprintWorld(ticSizeWorldUnits);
            var minCenterSep = OutpostGroundRules.MinCenterSeparationWorld(ticSizeWorldUnits);
            var type3Sep = TacScale.TacsToWorld(OutpostGroundRules.MinType3SeparationTacs, ticSizeWorldUnits);
            var keepOut = OutpostGroundRules.KeepOutRadiusWorld(ticSizeWorldUnits);
            var packRadius = keepOut * 0.9f;

            var center = baseSite.transform.position;
            center.y = 0f;

            var hasBunker = baseSite.Control == BaseControl.Hostile;
            var rng = new System.Random(StableSeed(baseSite));

            var root = new GameObject(ClusterRootName);
            root.transform.SetParent(baseSite.transform, false);
            root.transform.position = center;

            var cluster = root.AddComponent<OutpostBuildingCluster>();
            cluster.Configure(center, keepOut, hasBunker);

            var placed = new List<Vector3>(48);
            var type3Centers = new List<Vector3>(2);

            if (hasBunker)
            {
                // Slight offset so the bunker sits near — not exactly on — the geometric middle.
                var bunkerOffset = new Vector3(
                    ((float)rng.NextDouble() * 2f - 1f) * footprint * 0.35f,
                    0f,
                    ((float)rng.NextDouble() * 2f - 1f) * footprint * 0.35f);
                var bunkerPos = center + bunkerOffset;
                CreateBuilding(root.transform, OutpostBuildingType.Bunker, bunkerPos, footprint, ticSizeWorldUnits);
                placed.Add(bunkerPos);
            }

            var type3Count = rng.Next(OutpostGroundRules.Type3CountMin, OutpostGroundRules.Type3CountMax + 1);
            for (var i = 0; i < type3Count; i++)
            {
                if (!TryPlace(
                        rng,
                        center,
                        packRadius,
                        minCenterSep,
                        placed,
                        type3Centers,
                        type3Sep,
                        requireType3Separation: true,
                        out var pos))
                {
                    break;
                }

                CreateBuilding(root.transform, OutpostBuildingType.Type3, pos, footprint, ticSizeWorldUnits);
                placed.Add(pos);
                type3Centers.Add(pos);
            }

            var otherCount = rng.Next(
                OutpostGroundRules.OtherBuildingCountMin,
                OutpostGroundRules.OtherBuildingCountMax + 1);
            var type1Count = otherCount / 2;
            var type2Count = otherCount - type1Count;

            PlaceMany(
                root.transform,
                OutpostBuildingType.Type1,
                type1Count,
                rng,
                center,
                packRadius,
                minCenterSep,
                placed,
                footprint,
                ticSizeWorldUnits);
            PlaceMany(
                root.transform,
                OutpostBuildingType.Type2,
                type2Count,
                rng,
                center,
                packRadius,
                minCenterSep,
                placed,
                footprint,
                ticSizeWorldUnits);
        }

        private static void PlaceMany(
            Transform parent,
            OutpostBuildingType type,
            int count,
            System.Random rng,
            Vector3 center,
            float packRadius,
            float minCenterSep,
            List<Vector3> placed,
            float footprint,
            float ticSizeWorldUnits)
        {
            for (var i = 0; i < count; i++)
            {
                if (!TryPlace(
                        rng,
                        center,
                        packRadius,
                        minCenterSep,
                        placed,
                        null,
                        0f,
                        requireType3Separation: false,
                        out var pos))
                {
                    continue;
                }

                CreateBuilding(parent, type, pos, footprint, ticSizeWorldUnits);
                placed.Add(pos);
            }
        }

        private static bool TryPlace(
            System.Random rng,
            Vector3 center,
            float packRadius,
            float minCenterSep,
            List<Vector3> placed,
            List<Vector3> type3Centers,
            float type3Sep,
            bool requireType3Separation,
            out Vector3 position)
        {
            for (var attempt = 0; attempt < 48; attempt++)
            {
                var angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                var radius = Mathf.Sqrt((float)rng.NextDouble()) * packRadius;
                var candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (!IsClear(candidate, placed, minCenterSep))
                {
                    continue;
                }

                if (requireType3Separation && type3Centers != null && type3Centers.Count > 0)
                {
                    if (!IsClear(candidate, type3Centers, type3Sep))
                    {
                        continue;
                    }
                }

                position = candidate;
                return true;
            }

            position = default;
            return false;
        }

        private static bool IsClear(Vector3 candidate, List<Vector3> others, float minCenterSep)
        {
            var minSqr = minCenterSep * minCenterSep;
            for (var i = 0; i < others.Count; i++)
            {
                var d = candidate - others[i];
                d.y = 0f;
                if (d.sqrMagnitude < minSqr)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CreateBuilding(
            Transform parent,
            OutpostBuildingType type,
            Vector3 worldPosition,
            float footprint,
            float ticSizeWorldUnits)
        {
            var heightTacs = type switch
            {
                OutpostBuildingType.Type2 => 0.5f,
                OutpostBuildingType.Type3 => 2f,
                _ => 1f
            };
            var height = TacScale.TacsToWorld(heightTacs, ticSizeWorldUnits);

            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = type == OutpostBuildingType.Bunker
                ? "BunkerBuilding"
                : $"Building_{type}";
            building.transform.SetParent(parent, true);
            building.transform.position = new Vector3(worldPosition.x, height * 0.5f, worldPosition.z);
            building.transform.localRotation = Quaternion.identity;
            building.transform.localScale = new Vector3(footprint, height, footprint);

            var component = building.AddComponent<OutpostBuilding>();
            component.Configure(type);

            var lockable = building.AddComponent<LockableTarget>();
            var label = type == OutpostBuildingType.Bunker ? "Bunker" : type.ToString();
            lockable.Configure(
                label,
                LockableTargetKind.Ground,
                TargetAffiliation.Hostile,
                TargetUnitClass.Building);
            lockable.SetMaxGroundHitPoints(OutpostBuildingGhp.ForType(type));
            lockable.SetHitRadiusWorld(footprint * 0.55f);

            var collider = building.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = building.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial)
                {
                    color = ResolveColor(type)
                };
            }
        }

        private static Color ResolveColor(OutpostBuildingType type)
        {
            return type switch
            {
                OutpostBuildingType.Bunker => Color.black,
                OutpostBuildingType.Type2 => new Color(0.45f, 0.42f, 0.38f),
                OutpostBuildingType.Type3 => new Color(0.38f, 0.40f, 0.44f),
                _ => new Color(0.52f, 0.50f, 0.46f)
            };
        }

        private static int StableSeed(AntarcticaBase baseSite)
        {
            unchecked
            {
                var code = baseSite.SiteCode ?? baseSite.BaseName ?? "OUTPOST";
                var hash = 17;
                for (var i = 0; i < code.Length; i++)
                {
                    hash = (hash * 31) + char.ToUpperInvariant(code[i]);
                }

                hash = (hash * 31) + Mathf.RoundToInt(baseSite.PositionMiles.x * 10f);
                hash = (hash * 31) + Mathf.RoundToInt(baseSite.PositionMiles.y * 10f);
                return hash;
            }
        }
    }
}
