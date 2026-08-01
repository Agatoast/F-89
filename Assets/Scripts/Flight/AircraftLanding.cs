using F89.Core;

using F89.Flight;

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



        public static bool CanAttemptLanding()

        {

            return !AircraftLandingController.IsLandingActive

                && !AircraftLandingController.IsCrashLandingActive

                && !AircraftLandingController.IsTakeoffActive

                && !AircraftLandingController.IsLandingComplete

                && !AircraftLandingController.IsCarrierApproachPromptVisible

                && !AircraftLandingController.IsRunwayRefuelPromptVisible

                && !PlayerAircraftCrashController.IsCrashActive;

        }



        public static bool CanLandOnLand(float speedMph) =>

            CanAttemptLanding() && speedMph <= MaxLandingSpeedMph;



        public static bool CanShowLandPrompt(AircraftController aircraft)

        {

            if (aircraft == null || !CanAttemptLanding())

            {

                return false;

            }



            if (IsInCarrierApproachGrid(aircraft))

            {

                return true;

            }



            return IsOverLand(aircraft) && aircraft.CurrentSpeedMph <= MaxLandingSpeedMph;

        }



        public static void TryLand(AircraftController aircraft)

        {

            if (aircraft == null || !CanAttemptLanding())

            {

                return;

            }



            if (IsOverCarrier(aircraft) || IsInCarrierApproachGrid(aircraft))

            {

                GetOrAddLandingController(aircraft).BeginCarrierLanding();

                return;

            }



            if (!IsOverLand(aircraft))

            {

                FlightHudBanner.Show("Cannot land on water", WaterLandingMessageSeconds);

                return;

            }



            if (aircraft.CurrentSpeedMph > MaxLandingSpeedMph)

            {

                return;

            }



            if (HasEnemiesNearLandingSite(aircraft))

            {

                FlightHudBanner.Show(LandingBlockedByEnemiesMessage, LandingBlockedMessageSeconds);

                return;

            }



            if (TryResolveOutpostAtPosition(aircraft, out var outpostBase))

            {

                var landing = GetOrAddLandingController(aircraft);

                if (AntarcticaOutpostState.IsFriendlyOccupied(outpostBase.BaseName))

                {

                    landing.BeginFriendlyBaseLanding(outpostBase);

                }

                else

                {

                    landing.BeginOutpostGroundLanding(outpostBase);

                }



                return;

            }



            GetOrAddLandingController(aircraft).BeginLanding();

        }



        public static bool TryGetCarrierBase(out AntarcticaBase carrier)

        {

            carrier = F89.Testing.AntarcticaBaseSpawner.FindCarrierBase();

            return carrier != null;

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



            var ticSize = aircraft.Profile.ticSizeWorldUnits;

            var carrierWorld = AntarcticaWorldLocations.GetLockedCarrierWorldPosition(

                aircraft.WorldMap,

                ticSize);

            if (!aircraft.WorldMap.TryWorldPositionToGridCell(

                    aircraft.transform.position,

                    ticSize,

                    out var planeCell)

                || !aircraft.WorldMap.TryWorldPositionToGridCell(

                    carrierWorld,

                    ticSize,

                    out var carrierCell))

            {

                return false;

            }



            return Mathf.Abs(planeCell.x - carrierCell.x) <= 1

                && Mathf.Abs(planeCell.y - carrierCell.y) <= 1;

        }



        private static AircraftLandingController GetOrAddLandingController(AircraftController aircraft)

        {

            var landing = aircraft.GetComponent<AircraftLandingController>();

            if (landing == null)

            {

                landing = aircraft.gameObject.AddComponent<AircraftLandingController>();

            }



            return landing;

        }



        private static bool TryResolveOutpostAtPosition(AircraftController aircraft, out AntarcticaBase outpostBase)

        {

            outpostBase = null;

            var worldMap = aircraft.WorldMap;

            var profile = aircraft.Profile;

            if (worldMap == null || profile == null)

            {

                return false;

            }



            if (!AntarcticaOutpostLandingResolver.TryResolveOutpost(

                    aircraft.transform.position,

                    worldMap,

                    profile.ticSizeWorldUnits,

                    out var outpostName))

            {

                return false;

            }



            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);

            for (var i = 0; i < bases.Length; i++)

            {

                var candidate = bases[i];

                if (candidate != null

                    && candidate.SiteKind == BaseSiteKind.Land

                    && candidate.IsActive

                    && string.Equals(candidate.BaseName, outpostName, System.StringComparison.Ordinal))

                {

                    outpostBase = candidate;

                    return true;

                }

            }



            return false;

        }



        private static bool IsOverCarrier(AircraftController aircraft)

        {

            var worldMap = aircraft.WorldMap;

            var profile = aircraft.Profile;

            if (worldMap == null || profile == null)

            {

                return false;

            }



            return AntarcticaWorldLocations.IsWithinCarrierDeckRange(

                aircraft.transform.position,

                worldMap,

                profile.ticSizeWorldUnits,

                CarrierLandingRangeMiles);

        }



        private static bool IsOverLand(AircraftController aircraft)

        {

            var worldMap = aircraft.WorldMap;

            var profile = aircraft.Profile;

            if (worldMap == null || profile == null)

            {

                return false;

            }



            return AntarcticaLandMask.IsLandingLandWorld(

                aircraft.transform.position,

                worldMap,

                profile.ticSizeWorldUnits);

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

