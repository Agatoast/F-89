using F89.Core;
using F89.Weapons;
using UnityEngine;

namespace F89.Flight
{
    /// <summary>
    /// Restores the aircraft at the landing site after ground combat and starts VTOL takeoff.
    /// </summary>
    public static class FlightGroundReturnService
    {
        public static bool TryGetPendingReturnSpawn(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot))
            {
                return false;
            }

            position = snapshot.HasLandingGridCell
                ? snapshot.LandingGridWorldCenter
                : snapshot.AircraftWorldPosition;
            rotation = snapshot.AircraftWorldRotation;
            return true;
        }

        public static bool TryApplyPendingReturn(GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            if (LandMissionHandoffState.TryConsumeReturnToFlight(out var returnSnapshot, out _))
            {
                if (ShouldReturnToRunwayDeck(returnSnapshot))
                {
                    return ApplyReturnToRunwayDeck(player, returnSnapshot);
                }

                return ApplyReturnTakeoff(player, returnSnapshot);
            }

            if (LandMissionHandoffState.ShouldSuppressCarrierRespawn
                && LandMissionHandoffState.GetStoredFlightSnapshot().IsValid)
            {
                // Scene bootstrap and AircraftController.Start both run during the same
                // return. The first restores the grid square and starts VTOL; the second
                // must not reset that sequence or re-position the aircraft.
                if (AircraftLandingController.IsTakeoffActive
                    || AircraftLandingController.IsParkedAtRunway)
                {
                    return true;
                }

                var stored = LandMissionHandoffState.GetStoredFlightSnapshot();
                if (ShouldReturnToRunwayDeck(stored))
                {
                    return ApplyReturnToRunwayDeck(player, stored);
                }

                return ApplyReturnTakeoff(player, stored);
            }

            return false;
        }

        private static bool ShouldReturnToRunwayDeck(LandSortieSnapshot snapshot)
        {
            return snapshot.ReturnToRunwayDeck;
        }

        public static bool ApplyReturnTakeoff(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                Debug.LogWarning("[LandCombat] Ground return skipped — landing snapshot was invalid.");
                return false;
            }

            NormalizeLegacyGridLabel(player, ref snapshot);
            var returnPosition = snapshot.HasLandingGridCell
                ? snapshot.LandingGridWorldCenter
                : snapshot.AircraftWorldPosition;
            player.transform.SetPositionAndRotation(returnPosition, snapshot.AircraftWorldRotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            RestoreAircraftState(player, snapshot);

            var landing = player.GetComponent<AircraftLandingController>();
            if (landing == null)
            {
                landing = player.AddComponent<AircraftLandingController>();
            }

            landing.PrepareForGroundReturn(returnPosition);
            landing.BeginTakeoff();

            // Keep carrier spawn suppressed until takeoff finishes (ConfirmReturnApplied).
            EnsureApplier(player);
            // The saved landing square has been consumed by this active flight scene.
            // Do not carry it into a later game launch after this takeoff.
            LandMissionHandoffState.ClearPersistedReturnAfterApplication();

            var locationLabel = snapshot.HasLandingGridCell
                ? $"landing grid {snapshot.LandingGridCellX},{snapshot.LandingGridCellZ}"
                : $"landing spot {snapshot.AircraftWorldPosition}";
            Debug.Log($"[LandCombat] Restored flight at {locationLabel} (fuel {snapshot.FuelNormalized:P0}).");
            return true;
        }

        public static bool ApplyReturnToRunwayDeck(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                Debug.LogWarning("[LandCombat] Runway deck return skipped — landing snapshot was invalid.");
                return false;
            }

            NormalizeLegacyGridLabel(player, ref snapshot);
            var returnPosition = snapshot.HasLandingGridCell
                ? snapshot.LandingGridWorldCenter
                : snapshot.AircraftWorldPosition;
            player.transform.SetPositionAndRotation(returnPosition, snapshot.AircraftWorldRotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            RestoreAircraftState(player, snapshot);

            var landing = player.GetComponent<AircraftLandingController>();
            if (landing == null)
            {
                landing = player.AddComponent<AircraftLandingController>();
            }

            landing.RestoreRunwayDeckParked(snapshot);
            EnsureApplier(player);
            LandMissionHandoffState.ClearPersistedReturnAfterApplication();

            Debug.Log(
                $"[LandCombat] Restored parked runway deck at {snapshot.OutpostName} "
                + $"(fuel {snapshot.FuelNormalized:P0}).");
            return true;
        }

        private static void NormalizeLegacyGridLabel(GameObject player, ref LandSortieSnapshot snapshot)
        {
            if (!snapshot.HasLandingGridCell || snapshot.GridCoordinateVersion >= 1)
            {
                return;
            }

            var aircraft = player.GetComponent<AircraftController>();
            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            if (aircraft?.WorldMap == null
                || !aircraft.WorldMap.TryWorldPositionToGridCell(
                    snapshot.LandingGridWorldCenter,
                    ticSize,
                    out var canonicalCell))
            {
                return;
            }

            snapshot.LandingGridCellX = canonicalCell.x;
            snapshot.LandingGridCellZ = canonicalCell.y;
            snapshot.GridCoordinateVersion = 1;
        }

        public static bool ShouldSkipCarrierSpawn()
        {
            return LandMissionHandoffState.ShouldSuppressCarrierRespawn
                || LandMissionHandoffState.HasPendingGroundReturn;
        }

        private static void RestoreAircraftState(GameObject player, LandSortieSnapshot snapshot)
        {
            var aircraft = player.GetComponent<AircraftController>();
            if (aircraft != null)
            {
                aircraft.ApplyFuelState(
                    snapshot.LeftTankGallons,
                    snapshot.RightTankGallons,
                    snapshot.AfterburnerFuelRemaining);
            }

            if (snapshot.HasStoresInventory)
            {
                var weapons = player.GetComponent<PlayerWeaponController>();
                weapons?.ApplySortieLoadout(
                    snapshot.Aim9zRemaining,
                    snapshot.Agm88jRemaining,
                    snapshot.Gbu12Remaining,
                    snapshot.Agm114Remaining,
                    snapshot.GauRoundsRemaining);
            }

            var flares = player.GetComponent<FlareCountermeasureController>();
            flares?.SetFlaresRemaining(snapshot.FlaresRemaining);
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
