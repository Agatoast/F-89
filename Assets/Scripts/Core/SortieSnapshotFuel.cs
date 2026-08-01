using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    public static class SortieSnapshotFuel
    {
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
        }
    }
}
