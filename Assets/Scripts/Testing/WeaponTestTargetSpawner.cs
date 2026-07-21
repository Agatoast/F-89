using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Testing
{
    public static class WeaponTestTargetSpawner
    {
        private const string GridRootName = "TestGridTargets";
        private const int GridCellsPerSide = 20;

        public static void SpawnIfNeeded()
        {
            var existing = GameObject.Find(GridRootName);
            if (existing != null)
            {
                RefreshGridTargetIdentity(existing.transform);
                return;
            }

            SpawnGridTargets();
        }

        private static void SpawnGridTargets()
        {
            var root = new GameObject(GridRootName);
            var spacing = ResolveGridSpacingWorldUnits();
            var half = GridCellsPerSide / 2;

            for (var cellX = -half; cellX < half; cellX++)
            {
                for (var cellZ = -half; cellZ < half; cellZ++)
                {
                    SpawnCellTarget(root.transform, cellX, cellZ, spacing);
                }
            }
        }

        private static void RefreshGridTargetIdentity(Transform root)
        {
            var spacing = ResolveGridSpacingWorldUnits();
            foreach (var lockable in root.GetComponentsInChildren<LockableTarget>(true))
            {
                if (lockable == null)
                {
                    continue;
                }

                if (!TryResolveGridCell(lockable.transform.position, spacing, out var cellX, out var cellZ))
                {
                    continue;
                }

                ApplyGridIdentity(lockable, cellX, cellZ);
            }
        }

        private static void SpawnCellTarget(Transform parent, int cellX, int cellZ, float spacing)
        {
            ResolveGridIdentity(cellX, cellZ, out var kind, out var affiliation, out var unitClass);
            var center = new Vector3(
                (cellX + 0.5f) * spacing,
                0f,
                (cellZ + 0.5f) * spacing);
            var color = ResolveTargetColor(kind, affiliation, unitClass);
            var scale = ResolveTargetScale(kind, unitClass);

            CreateTarget(
                parent,
                BuildLabel(cellX, cellZ, kind, affiliation, unitClass),
                center,
                kind,
                affiliation,
                unitClass,
                color,
                scale);
        }

        private static void ApplyGridIdentity(LockableTarget lockable, int cellX, int cellZ)
        {
            ResolveGridIdentity(cellX, cellZ, out var kind, out var affiliation, out var unitClass);
            lockable.Configure(
                BuildLabel(cellX, cellZ, kind, affiliation, unitClass),
                kind,
                affiliation,
                unitClass);

            var renderer = lockable.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = ResolveTargetColor(kind, affiliation, unitClass);
            }

            lockable.gameObject.name = BuildLabel(cellX, cellZ, kind, affiliation, unitClass);
        }

        private static void ResolveGridIdentity(
            int cellX,
            int cellZ,
            out LockableTargetKind kind,
            out TargetAffiliation affiliation,
            out TargetUnitClass unitClass)
        {
            kind = (cellX + cellZ) % 2 == 0
                ? LockableTargetKind.Ground
                : LockableTargetKind.Air;
            affiliation = (cellX + cellZ) % 5 == 0
                ? TargetAffiliation.Friendly
                : TargetAffiliation.Hostile;
            unitClass = (cellX - cellZ) % 7 == 0
                ? TargetUnitClass.Infantry
                : TargetUnitClass.Standard;
        }

        private static bool TryResolveGridCell(
            Vector3 worldPosition,
            float spacing,
            out int cellX,
            out int cellZ)
        {
            cellX = 0;
            cellZ = 0;
            if (spacing <= 0f)
            {
                return false;
            }

            cellX = Mathf.FloorToInt(worldPosition.x / spacing);
            cellZ = Mathf.FloorToInt(worldPosition.z / spacing);
            return true;
        }

        private static string BuildLabel(
            int cellX,
            int cellZ,
            LockableTargetKind kind,
            TargetAffiliation affiliation,
            TargetUnitClass unitClass)
        {
            var side = affiliation == TargetAffiliation.Friendly ? "Frd" : "Hos";
            var unit = unitClass == TargetUnitClass.Infantry ? "Inf" : kind.ToString();
            return $"Grid-{cellX},{cellZ}-{side}-{unit}";
        }

        private static Color ResolveTargetColor(
            LockableTargetKind kind,
            TargetAffiliation affiliation,
            TargetUnitClass unitClass)
        {
            if (affiliation == TargetAffiliation.Friendly)
            {
                if (unitClass == TargetUnitClass.Infantry)
                {
                    return new Color(0.72f, 0.74f, 0.78f);
                }

                return kind == LockableTargetKind.Air
                    ? new Color(0.65f, 0.72f, 0.82f)
                    : new Color(0.58f, 0.62f, 0.52f);
            }

            if (unitClass == TargetUnitClass.Infantry)
            {
                return new Color(0.42f, 0.3f, 0.24f);
            }

            return kind == LockableTargetKind.Air
                ? new Color(0.55f, 0.1f, 0.1f)
                : new Color(0.35f, 0.38f, 0.32f);
        }

        private static Vector3 ResolveTargetScale(LockableTargetKind kind, TargetUnitClass unitClass)
        {
            var scale = kind == LockableTargetKind.Air
                ? new Vector3(1.6f, 0.8f, 1.6f)
                : new Vector3(2f, 1f, 2f);

            if (unitClass == TargetUnitClass.Infantry)
            {
                scale *= 0.55f;
            }

            return scale;
        }

        private static float ResolveGridSpacingWorldUnits()
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap != null)
            {
                return worldMap.GridSpacingTics * ticSize;
            }

            return 20f * ticSize;
        }

        private static void CreateTarget(
            Transform parent,
            string label,
            Vector3 cellCenter,
            LockableTargetKind kind,
            TargetAffiliation affiliation,
            TargetUnitClass unitClass,
            Color color,
            Vector3 scale)
        {
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = label;
            targetObject.transform.SetParent(parent, false);
            targetObject.transform.position = cellCenter + Vector3.up * (scale.y * 0.5f);
            targetObject.transform.localScale = scale;

            var renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = color;
            }

            var lockable = targetObject.AddComponent<LockableTarget>();
            lockable.Configure(label, kind, affiliation, unitClass);
        }
    }
}
