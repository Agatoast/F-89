using F89.Audio;
using F89.Flight;
using F89.Testing;
using F89.Weapons;
using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Single place to place the player and apply loadout launch intent when FlightTest loads.
    /// </summary>
    public static class FlightMissionStartBootstrap
    {
        public static bool SortieStartupComplete { get; private set; }

        public static void ResetForSceneLoad()
        {
            SortieStartupComplete = false;
        }

        public static void Apply(AircraftController aircraft, GameObject playerObject)
        {
            if (aircraft == null || playerObject == null)
            {
                return;
            }

            try
            {
                AntarcticaBaseSpawner.SpawnIfNeeded();

                var applied = false;
                var sortieReturnPending = FlightGroundReturnService.ShouldApplySortieReturn();

                if (sortieReturnPending)
                {
                    var returnApplied = FlightGroundReturnService.TryApplyPendingReturn(playerObject);
                    if (!returnApplied
                        && LandingMileFlagState.TryResolveWorldPosition(out var milePosition, out var mileRotation))
                    {
                        milePosition.y = 0f;
                        playerObject.transform.SetPositionAndRotation(milePosition, mileRotation);
                        var body = playerObject.GetComponent<Rigidbody>();
                        if (body != null)
                        {
                            body.position = milePosition;
                            body.rotation = mileRotation;
                            body.linearVelocity = Vector3.zero;
                            body.angularVelocity = Vector3.zero;
                        }

                        VtolTakeoffLaunch.BeginAt(aircraft, milePosition);
                        returnApplied = true;
                        Debug.LogWarning(
                            "[F-89] Sortie handoff failed — applied landing mile flag for takeoff.");
                    }

                    if (!returnApplied)
                    {
                        Debug.LogWarning(
                            "[F-89] Sortie return was queued but could not be applied — "
                            + "skipping default carrier deck spawn.");
                    }

                    applied = true;
                }
                else if (FlightMissionLaunchState.HasPendingLaunch)
                {
                    ApplyPendingLaunch(aircraft, playerObject);
                    applied = true;
                }
                else if (!applied)
                {
                    ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: false);
                    applied = true;
                }

                if (applied && !sortieReturnPending)
                {
                    FlightSpawnValidation.EnsurePlausibleOrCarrier(aircraft);
                }

                FlightAudioBootstrap.SyncFromAircraft(aircraft);
            }
            finally
            {
                SortieStartupComplete = true;
            }
        }

        private static void ApplyPendingLaunch(AircraftController aircraft, GameObject playerObject)
        {
            if (FlightMissionLaunchState.HasPendingCarrierLaunch)
            {
                ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
                return;
            }

            if (aircraft.TryApplyMissionOutpostLaunch())
            {
                // Fresh mission start from loadout — always launch topped off.
                aircraft.Refuel();
                return;
            }

            Debug.LogWarning("F-89: Outpost launch failed; falling back to carrier deck.");
            FlightMissionLaunchState.BeginCarrierLaunch();
            ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
        }

        private static void ApplyCarrierDeckStart(AircraftController aircraft, bool consumeLaunchFlag)
        {
            var restoreDeckTakeoff = CarrierDeckTakeoffState.Consume();
            var deckSnapshot = restoreDeckTakeoff
                ? LandMissionHandoffState.GetStoredFlightSnapshot()
                : LandSortieSnapshot.Empty;

            LandMissionHandoffState.Clear();
            LandingMileFlagState.Clear();

            AntarcticaBaseSpawner.EnsureMissionPlatoonsIfNeeded(aircraft);
            if (!AntarcticaBaseSpawner.TryMovePlayerToCarrier(
                    aircraft.transform,
                    aircraft.WorldMap,
                    aircraft.Profile))
            {
                AntarcticaBaseSpawner.ForcePlayerToDefaultCarrier(
                    aircraft.transform,
                    aircraft.WorldMap,
                    aircraft.Profile);
            }

            if (restoreDeckTakeoff && deckSnapshot.IsValid)
            {
                // Mid-mission CV deck takeoff — keep fuel/stores from the deck stay.
                RestoreDeckTakeoffAircraftState(aircraft, deckSnapshot);
            }
            else
            {
                // Fresh Start Mission (or default carrier spawn) — full tanks.
                aircraft.Refuel();
            }

            if (consumeLaunchFlag)
            {
                FlightMissionLaunchState.TryConsumeCarrierLaunch();
            }

            VtolTakeoffLaunch.BeginAt(aircraft, aircraft.transform.position);
        }

        private static void RestoreDeckTakeoffAircraftState(
            AircraftController aircraft,
            LandSortieSnapshot snapshot)
        {
            if (aircraft == null)
            {
                return;
            }

            aircraft.ApplyFuelState(
                snapshot.LeftTankGallons,
                snapshot.RightTankGallons,
                snapshot.AfterburnerFuelRemaining);

            if (snapshot.HasStoresInventory)
            {
                var weapons = aircraft.GetComponent<PlayerWeaponController>();
                weapons?.ApplySortieLoadout(
                    snapshot.Aim9zRemaining,
                    snapshot.Agm88jRemaining,
                    snapshot.Gbu12Remaining,
                    snapshot.Agm114Remaining,
                    snapshot.GauRoundsRemaining);
            }

            var flares = aircraft.GetComponent<FlareCountermeasureController>();
            flares?.SetFlaresRemaining(snapshot.FlaresRemaining);
        }
    }
}
