using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Flight
{
    public static class AircraftLanding
    {
        public const float MaxLandingSpeedMph = 150f;
        public const float WaterLandingMessageSeconds = 10f;

        public static bool CanLand(float speedMph)
        {
            return speedMph <= MaxLandingSpeedMph
                && !AircraftLandingController.IsLandingActive
                && !AircraftLandingController.IsLandingComplete;
        }

        public static void TryLand(AircraftController aircraft)
        {
            if (aircraft == null || !CanLand(aircraft.CurrentSpeedMph))
            {
                return;
            }

            if (!IsOverLand(aircraft))
            {
                FlightHudBanner.Show("Cannot land on water", WaterLandingMessageSeconds);
                return;
            }

            var landingController = aircraft.GetComponent<AircraftLandingController>();
            if (landingController == null)
            {
                landingController = aircraft.gameObject.AddComponent<AircraftLandingController>();
            }

            landingController.BeginLanding();
        }

        private static bool IsOverLand(AircraftController aircraft)
        {
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (worldMap == null || profile == null)
            {
                return false;
            }

            var worldUnitsPerMile = worldMap.GridSpacingTics * profile.ticSizeWorldUnits / worldMap.milesPerGrid;
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            var positionMiles = WorldMapConfig.WorldToMileOffset(aircraft.transform.position, worldUnitsPerMile);
            return AntarcticaLandMask.IsDisplayLandMiles(positionMiles, worldMap.antarcticaSizeMiles);
        }
    }
}
