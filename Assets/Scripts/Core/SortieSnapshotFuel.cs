using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    public static class SortieSnapshotFuel
    {
        private const float UsableFuelGallonsEpsilon = 0.01f;

        public static void ApplyMaxFuel(ref LandSortieSnapshot snapshot)
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var gallonsPerTank = worldMap != null ? worldMap.fuelGallonsPerTank : 1350f;
            snapshot.LeftTankGallons = gallonsPerTank;
            snapshot.RightTankGallons = gallonsPerTank;
            snapshot.AfterburnerFuelRemaining = profile != null
                ? profile.afterburnerFuelCapacity
                : snapshot.AfterburnerFuelRemaining;
            snapshot.FuelNormalized = 1f;
            snapshot.IsValid = true;
        }

        public static bool HasUsableFuel(in LandSortieSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                return false;
            }

            if (snapshot.LeftTankGallons + snapshot.RightTankGallons > UsableFuelGallonsEpsilon)
            {
                return true;
            }

            return snapshot.FuelNormalized > 0.001f;
        }
    }
}
