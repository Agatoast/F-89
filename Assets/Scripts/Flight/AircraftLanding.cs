using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Flight
{
    public static class AircraftLanding
    {
        public const float MaxLandingSpeedMph = 150f;
        public const float LandingClearanceMiles = 5f;
        public const float CarrierLandingRangeMiles = 0.75f;
        public const float WaterLandingMessageSeconds = 10f;
        public const float LandingBlockedMessageSeconds = 10f;
        public const string LandPromptText = "Press L to Land";
        public const string LandingBlockedByEnemiesMessage =
            "Enemies are too close. You must clear the area before attempting to land.";

        public static bool CanLand(float speedMph)
        {
            return speedMph <= MaxLandingSpeedMph
                && !AircraftLandingController.IsLandingActive
                && !AircraftLandingController.IsTakeoffActive
                && !AircraftLandingController.IsLandingComplete;
        }

        public static void TryLand(AircraftController aircraft)
        {
            if (aircraft == null || !CanLand(aircraft.CurrentSpeedMph))
            {
                return;
            }

            if (IsOverCarrier(aircraft))
            {
                var carrierLanding = aircraft.GetComponent<AircraftLandingController>()
                    ?? aircraft.gameObject.AddComponent<AircraftLandingController>();
                carrierLanding.BeginCarrierLanding();
                return;
            }

            if (!IsOverLand(aircraft))
            {
                FlightHudBanner.Show("Cannot land on water", WaterLandingMessageSeconds);
                return;
            }

            if (HasEnemiesNearLandingSite(aircraft))
            {
                FlightHudBanner.Show(LandingBlockedByEnemiesMessage, LandingBlockedMessageSeconds);
                return;
            }

            var landingController = aircraft.GetComponent<AircraftLandingController>();
            if (landingController == null)
            {
                landingController = aircraft.gameObject.AddComponent<AircraftLandingController>();
            }

            landingController.BeginLanding();
        }

        private static bool IsOverCarrier(AircraftController aircraft)
        {
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (worldMap == null || profile == null)
            {
                return false;
            }

            var rangeWorld = WorldMapConfig.RangeMilesToWorldUnits(
                CarrierLandingRangeMiles,
                worldMap,
                profile.ticSizeWorldUnits);
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite != null
                    && baseSite.SiteKind == BaseSiteKind.Carrier
                    && Vector3.Distance(aircraft.transform.position, baseSite.transform.position) <= rangeWorld)
                {
                    return true;
                }
            }

            return false;
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

        private static bool HasEnemiesNearLandingSite(AircraftController aircraft)
        {
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (worldMap == null || profile == null)
            {
                return false;
            }

            return CombatThreatRange.HasHostileUnitWithinMiles(
                aircraft.transform.position,
                LandingClearanceMiles,
                worldMap,
                profile.ticSizeWorldUnits);
        }
    }
}
