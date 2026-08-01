using F89.Core;
using UnityEngine;

namespace F89.Flight
{
    /// <summary>
    /// Safety net if scene bootstrap runs before/after other startup code: re-apply landing return on Start.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class FlightGroundReturnApplier : MonoBehaviour
    {
        private void Start()
        {
            if (FlightMissionStartBootstrap.SortieStartupComplete
                || FlightMissionLaunchState.HasPendingCarrierLaunch
                || !string.IsNullOrEmpty(FlightMissionLaunchState.LaunchFromOutpostName)
                || !FlightGroundReturnService.ShouldApplySortieReturn())
            {
                return;
            }

            if (AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsParkedAtRunway)
            {
                return;
            }

            FlightGroundReturnService.TryApplyPendingReturn(gameObject);
        }
    }
}
