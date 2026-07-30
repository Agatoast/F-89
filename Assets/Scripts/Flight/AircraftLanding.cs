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
                && !AircraftLandingController.IsCrashLandingActive
                && !AircraftLandingController.IsTakeoffActive
                && !AircraftLandingController.IsLandingComplete
                && !AircraftLandingController.IsCarrierApproachPromptVisible
                && !AircraftLandingController.IsRunwayRefuelPromptVisible
                && !PlayerAircraftCrashController.IsCrashActive;
        }

        public static bool CanShowLandPrompt(AircraftController aircraft)
        {
            if (aircraft == null || !CanLand(aircraft.CurrentSpeedMph))
            {
                return false;
            }

            return IsInCarrierApproachGrid(aircraft)
                || OutpostRunwayLanding.IsNearRunway(aircraft)
                || IsOverLand(aircraft);
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

            if (OutpostRunwayLanding.TryFindNearestRunway(
                    aircraft,
                    out var runwayBase,
                    out var runwayTransform,
                    out _))
            {
                if (HasEnemiesNearLandingSite(aircraft))
                {
                    FlightHudBanner.Show(LandingBlockedByEnemiesMessage, LandingBlockedMessageSeconds);
                    return;
                }

                var runwayLanding = aircraft.GetComponent<AircraftLandingController>()
                    ?? aircraft.gameObject.AddComponent<AircraftLandingController>();
                runwayLanding.BeginRunwayLanding(runwayBase, runwayTransform);
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

        public static bool TryGetCarrierBase(out AntarcticaBase carrier)
        {
            carrier = null;
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Carrier)
                {
                    continue;
                }

                carrier = baseSite;
                return true;
            }

            return false;
        }

        /// <summary>
        /// True when the aircraft is in the CV grid square or any of the 8 neighboring squares.
        /// </summary>
        public static bool IsInCarrierApproachGrid(AircraftController aircraft)
        {
            if (aircraft?.WorldMap == null || aircraft.Profile == null)
            {
                return false;
            }

            if (!TryGetCarrierBase(out var carrier))
            {
                return false;
            }

            var ticSize = aircraft.Profile.ticSizeWorldUnits;
            if (!aircraft.WorldMap.TryWorldPositionToGridCell(
                    aircraft.transform.position,
                    ticSize,
                    out var planeCell)
                || !aircraft.WorldMap.TryWorldPositionToGridCell(
                    carrier.transform.position,
                    ticSize,
                    out var carrierCell))
            {
                return false;
            }

            return Mathf.Abs(planeCell.x - carrierCell.x) <= 1
                && Mathf.Abs(planeCell.y - carrierCell.y) <= 1;
        }

        private static bool IsOverCarrier(AircraftController aircraft)
        {
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (worldMap == null || profile == null || !TryGetCarrierBase(out var carrier))
            {
                return false;
            }

            var rangeWorld = WorldMapConfig.RangeMilesToWorldUnits(
                CarrierLandingRangeMiles,
                worldMap,
                profile.ticSizeWorldUnits);
            return Vector3.Distance(aircraft.transform.position, carrier.transform.position) <= rangeWorld;
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
