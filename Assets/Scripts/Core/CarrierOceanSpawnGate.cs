using F89.Flight;
using F89.LandCombat;

namespace F89.Core
{
    /// <summary>
    /// The locked CV deck is the only valid ocean spawn. It may run only for explicit carrier
    /// sorties — never as a default or correction fallback when a land outpost is available.
    /// </summary>
    public static class CarrierOceanSpawnGate
    {
        public static bool AllowsOceanCarrierSpawn()
        {
            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return false;
            }

            if (FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                return false;
            }

            return FlightMissionLaunchState.HasPendingCarrierLaunch
                || CarrierDeckTakeoffState.IsPending;
        }
    }
}
