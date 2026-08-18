using F89.Flight;
using UnityEngine;

namespace F89.Core
{
    /// <summary>VTOL takeoff from a parked friendly runway (not CV).</summary>
    public static class FriendlyRunwayTakeoff
    {
        public static bool BeginAt(AircraftController aircraft)
        {
            if (aircraft == null)
            {
                return false;
            }

            LandMissionHandoffState.ClearRunwayDeckReturnIntent();
            return VtolTakeoffLaunch.BeginAt(aircraft, aircraft.transform.position);
        }

        public static bool BeginAt(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return false;
            }

            return BeginAt(playerObject.GetComponent<AircraftController>());
        }
    }
}
