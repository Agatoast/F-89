using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    public static class BasicTankSpawner
    {
        public const string OutpostSouthBaseName = "Outpost South";
        public const int OutpostSouthTankCount = 40;
        private const float SpawnRadiusMiles = 1f;

        public static void RemoveAllTanks()
        {
            var tanks = Object.FindObjectsByType<BasicTankController>(FindObjectsSortMode.None);
            var removed = 0;
            for (var i = 0; i < tanks.Length; i++)
            {
                if (tanks[i] == null)
                {
                    continue;
                }

                Object.Destroy(tanks[i].gameObject);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"F-89: Removed {removed} Basic Tank(s) from the map.");
            }
        }

        public static void EnsureOutpostSouthTank(AircraftController player)
        {
            // Tank platoon spawning is disabled until outpost targeting by SiteCode is locked in.
            RemoveAllTanks();
        }

        public static string FormatTankLabel(int index)
        {
            return $"{BasicTankConfig.DefaultUnitName} {index:D2}";
        }

        private static BasicTankController FindTankByLabel(AntarcticaBase outpost, string label)
        {
            if (outpost == null)
            {
                return null;
            }

            var tanks = outpost.GetComponentsInChildren<BasicTankController>(true);
            for (var i = 0; i < tanks.Length; i++)
            {
                var tank = tanks[i];
                if (tank != null && tank.UnitName == label)
                {
                    return tank;
                }
            }

            return null;
        }

        private static Vector3 ResolveSpawnWorldPosition(
            Vector3 outpostWorld,
            int index,
            float worldUnitsPerMile,
            WorldMapConfig worldMap)
        {
            outpostWorld.y = 0f;
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;

            // Deterministic ring scatter so tanks don't stack on the outpost center.
            var goldenAngle = 2.399963f;
            var angle = index * goldenAngle;
            var radiusMiles = Mathf.Lerp(0.1f, SpawnRadiusMiles, index / (float)OutpostSouthTankCount);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var attemptAngle = angle + (attempt * 0.35f);
                var attemptRadius = radiusMiles * (1f - (attempt * 0.08f));
                var offset = new Vector3(
                    Mathf.Cos(attemptAngle) * attemptRadius * worldUnitsPerMile,
                    0f,
                    Mathf.Sin(attemptAngle) * attemptRadius * worldUnitsPerMile);
                var candidate = outpostWorld + offset;
                var miles = WorldMapConfig.WorldToMileOffset(candidate, worldUnitsPerMile);
                if (AntarcticaLandMask.IsDisplayLandMiles(miles, mapSizeMiles))
                {
                    return candidate;
                }
            }

            return outpostWorld;
        }

        private static BasicTankConfig LoadConfig()
        {
            var config = Resources.Load<BasicTankConfig>("F89_BasicTankConfig");
            return config != null ? config : ScriptableObject.CreateInstance<BasicTankConfig>();
        }

        private static AntarcticaBase FindBaseByName(string baseName)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite != null && baseSite.BaseName == baseName)
                {
                    return baseSite;
                }
            }

            return null;
        }

        private static float ResolveWorldUnitsPerMile(WorldMapConfig worldMap, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null)
            {
                return 20f * ticSize;
            }

            return worldMap.GridSpacingTics * ticSize / worldMap.milesPerGrid;
        }
    }
}
