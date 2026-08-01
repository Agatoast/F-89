using F89.Audio;
using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Single VTOL takeoff entry point for carrier, outpost, and ground-return launches.</summary>
    public static class VtolTakeoffLaunch
    {
        public static void BeginAt(AircraftController aircraft, Vector3 worldPosition)
        {
            if (aircraft == null)
            {
                return;
            }

            worldPosition.y = 0f;
            aircraft.SetLandingLocked(true);
            if (aircraft.TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var landing = aircraft.GetComponent<AircraftLandingController>()
                ?? aircraft.gameObject.AddComponent<AircraftLandingController>();
            landing.PrepareForGroundReturn(worldPosition);
            landing.BeginTakeoff();
            FlightAudio.SetInFlight(true);
        }

        public static void BeginAt(GameObject playerObject, Vector3 worldPosition)
        {
            if (playerObject == null)
            {
                return;
            }

            BeginAt(playerObject.GetComponent<AircraftController>(), worldPosition);
        }
    }
}
