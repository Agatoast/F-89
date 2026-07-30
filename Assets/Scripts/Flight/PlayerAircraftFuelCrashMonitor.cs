using UnityEngine;

namespace F89.Flight
{
    /// <summary>Starts a forced crash landing when main tanks run dry during flight.</summary>
    public sealed class PlayerAircraftFuelCrashMonitor : MonoBehaviour
    {
        private bool crashTriggered;

        private void Update()
        {
            if (crashTriggered)
            {
                return;
            }

            var aircraft = GetComponent<AircraftController>();
            if (aircraft == null || !aircraft.IsOutOfFuel)
            {
                return;
            }

            if (AircraftLandingController.IsLandingComplete
                || AircraftLandingController.IsCrashLandingActive
                || PlayerAircraftCrashController.IsCrashActive)
            {
                return;
            }

            crashTriggered = true;
            var crash = GetComponent<PlayerAircraftCrashController>()
                ?? gameObject.AddComponent<PlayerAircraftCrashController>();
            crash.BeginCrash("fuel exhaustion");
        }
    }
}
