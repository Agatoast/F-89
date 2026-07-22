using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    public static class BasicTankSpawner
    {
        public const string OutpostSouthBaseName = "Outpost South";

        public static void EnsureOutpostSouthTank(AircraftController player)
        {
            if (player == null || player.Profile == null || player.WorldMap == null)
            {
                return;
            }

            var playerTarget = player.GetComponent<LockableTarget>();
            if (playerTarget == null)
            {
                return;
            }

            var outpost = FindBaseByName(OutpostSouthBaseName);
            if (outpost == null)
            {
                Debug.LogWarning($"F-89: Could not spawn Basic Tank — {OutpostSouthBaseName} not found.");
                return;
            }

            if (outpost.GetComponentInChildren<BasicTankController>() != null)
            {
                return;
            }

            var worldUnitsPerMile = ResolveWorldUnitsPerMile(player.WorldMap, player.Profile);
            var tankObject = new GameObject(BasicTankConfig.DefaultUnitName);
            tankObject.transform.SetParent(outpost.transform, false);
            tankObject.transform.position = outpost.transform.position;

            var tank = tankObject.AddComponent<BasicTankController>();
            tank.Configure(
                LoadConfig(),
                player.WorldMap,
                player.Profile,
                playerTarget,
                worldUnitsPerMile);

            Debug.Log($"F-89: {BasicTankConfig.DefaultUnitName} deployed at {OutpostSouthBaseName}.");
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
