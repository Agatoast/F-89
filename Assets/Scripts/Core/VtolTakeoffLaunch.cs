using F89.Audio;
using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>VTOL takeoff for ground-return and friendly runway takeoff. CV uses <see cref="CarrierDeckLaunch"/>.</summary>
    public static class VtolTakeoffLaunch
    {
        public static bool BeginAt(AircraftController aircraft, Vector3 worldPosition)
        {
            if (aircraft == null)
            {
                return false;
            }

            var rotation = aircraft.transform.rotation;
            if (LandingMileFlagState.TryResolveWorldPosition(out var flagPosition, out var flagRotation))
            {
                worldPosition = flagPosition;
                rotation = flagRotation;
            }

            worldPosition.y = 0f;
            aircraft.transform.SetPositionAndRotation(worldPosition, rotation);
            aircraft.SetLandingLocked(true);
            if (aircraft.TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var landing = aircraft.GetComponent<AircraftLandingController>()
                ?? aircraft.gameObject.AddComponent<AircraftLandingController>();
            landing.enabled = true;
            landing.PrepareForGroundReturn(worldPosition);
            landing.BeginTakeoff();
            FlightAudio.SetInFlight(true);
            return AircraftLandingController.IsTakeoffActive;
        }

        public static bool BeginAt(GameObject playerObject, Vector3 worldPosition)
        {
            if (playerObject == null)
            {
                return false;
            }

            return BeginAt(playerObject.GetComponent<AircraftController>(), worldPosition);
        }
    }
}
