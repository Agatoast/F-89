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

        /// <summary>
        /// Older snapshots may only store FuelNormalized; hydrate tank gallons for restore paths.
        /// </summary>
        public static void ResolveTankGallons(in LandSortieSnapshot snapshot, out float leftGallons, out float rightGallons)
        {
            leftGallons = snapshot.LeftTankGallons;
            rightGallons = snapshot.RightTankGallons;
            if (leftGallons + rightGallons > UsableFuelGallonsEpsilon)
            {
                return;
            }

            if (snapshot.FuelNormalized <= 0.001f)
            {
                leftGallons = 0f;
                rightGallons = 0f;
                return;
            }

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var gallonsPerTank = worldMap != null ? worldMap.fuelGallonsPerTank : 1350f;
            var filledPerTank = gallonsPerTank * Mathf.Clamp01(snapshot.FuelNormalized);
            leftGallons = filledPerTank;
            rightGallons = filledPerTank;
        }
    }
}
