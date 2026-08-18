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
            if (FlightMissionStartBootstrap.CarrierCatapultLaunchComplete)
            {
                return;
            }

            if (!FlightGroundReturnService.ShouldApplySortieReturn())
            {
                return;
            }

            if (FlightMissionLaunchState.IsExplicitCarrierSortiePending())
            {
                return;
            }

            if (AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsParkedAtRunway
                || AircraftLandingController.IsRunwayDeckMenuVisible)
            {
                return;
            }

            var aircraft = GetComponent<AircraftController>();
            if (aircraft != null
                && aircraft.CurrentSpeedMph >= FlightMissionLaunchState.CarrierTakeoffSpeedMph * 0.5f)
            {
                return;
            }

            FlightGroundReturnService.TryApplyPendingReturn(gameObject);
        }
    }
}
