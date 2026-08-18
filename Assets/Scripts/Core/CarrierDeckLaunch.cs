using F89.Audio;
using F89.Controls;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Catapult launch from the CV deck — no VTOL scale-up takeoff.</summary>
    public static class CarrierDeckLaunch
    {
        public static bool BeginTakeoff(AircraftController aircraft)
        {
            if (aircraft == null)
            {
                return false;
            }

            GamePauseController.ClearPauseOnSceneLoad();

            var landing = aircraft.GetComponent<AircraftLandingController>()
                ?? aircraft.gameObject.AddComponent<AircraftLandingController>();
            landing.PrepareForCarrierDeckTakeoff();

            aircraft.SetLandingLocked(false);
            aircraft.ApplyCarrierTakeoffLaunch(FlightMissionLaunchState.CarrierTakeoffSpeedMph);

            var input = aircraft.GetComponent<PlayerAircraftInput>();
            if (input != null)
            {
                input.enabled = true;
            }

            FlightAudio.SetInFlight(true);
            CombatThreatRange.InvalidateCaches();
            FlightMissionLaunchState.ClearForceCarrierDeckLaunch();
            return true;
        }

        public static bool BeginTakeoff(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return false;
            }

            return BeginTakeoff(playerObject.GetComponent<AircraftController>());
        }
    }
}
