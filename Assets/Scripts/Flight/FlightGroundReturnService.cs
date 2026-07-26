using F89.Core;
using UnityEngine;

namespace F89.Flight
{
    /// <summary>
    /// Restores the aircraft at the landing site after ground combat and starts VTOL takeoff.
    /// </summary>
    public static class FlightGroundReturnService
    {
        public static bool TryApplyPendingReturn(GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            if (LandMissionHandoffState.TryConsumeReturnToFlight(out var returnSnapshot, out _))
            {
                return ApplyReturnTakeoff(player, returnSnapshot);
            }

            if (LandMissionHandoffState.ShouldSuppressCarrierRespawn
                && LandMissionHandoffState.GetStoredFlightSnapshot().IsValid)
            {
                return ApplyReturnTakeoff(player, LandMissionHandoffState.GetStoredFlightSnapshot());
            }

            return false;
        }

        public static bool ApplyReturnTakeoff(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                Debug.LogWarning("[LandCombat] Ground return skipped — landing snapshot was invalid.");
                return false;
            }

            player.transform.SetPositionAndRotation(snapshot.AircraftWorldPosition, snapshot.AircraftWorldRotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var landing = player.GetComponent<AircraftLandingController>();
            if (landing == null)
            {
                landing = player.AddComponent<AircraftLandingController>();
            }

            landing.PrepareForGroundReturn();
            landing.BeginTakeoff();

            LandMissionHandoffState.ConfirmReturnApplied();
            EnsureApplier(player);

            Debug.Log($"[LandCombat] Restored flight at landing spot {snapshot.AircraftWorldPosition}.");
            return true;
        }

        public static bool ShouldSkipCarrierSpawn()
        {
            return LandMissionHandoffState.ShouldSuppressCarrierRespawn
                || LandMissionHandoffState.HasPendingGroundReturn;
        }

        private static void EnsureApplier(GameObject player)
        {
            if (player.GetComponent<FlightGroundReturnApplier>() != null)
            {
                return;
            }

            player.AddComponent<FlightGroundReturnApplier>();
        }
    }
}
