using F89.Core;
using F89.LandCombat;
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
            if (LandingMileFlagState.TryResolveWorldPosition(out position, out rotation))
            {
                return true;
            }

            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot))
            {
                return false;
            }

            position = FlightSpawnValidation.ResolveSortieReturnPosition(
                snapshot,
                Resources.Load<WorldMapConfig>("F89_WorldMapConfig"),
                Resources.Load<FlightProfile>("F89_DefaultFlightProfile")?.ticSizeWorldUnits ?? 1f);
            rotation = snapshot.AircraftWorldRotation;
            return true;
        }

        public static bool ShouldApplySortieReturn()
        {
            return LandingMileFlagState.HasActiveFlag
                || LandMissionHandoffState.HasPendingReturn
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn;
        }

        public static bool TryApplyPendingReturn(GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            if (AircraftLandingController.IsTakeoffActive)
            {
                return true;
            }

            if (AircraftLandingController.IsParkedAtRunway
                && !LandMissionHandoffState.HasPendingGroundReturn
                && !LandingMileFlagState.HasActiveFlag)
            {
                return true;
            }

            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot)
                || !snapshot.IsValid)
            {
                if (!LandingMileFlagState.HasActiveFlag)
                {
                    return false;
                }

                snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
                if (!snapshot.IsValid)
                {
                    snapshot = new LandSortieSnapshot
                    {
                        IsValid = true,
                        ReturnSceneName = GameScenes.FlightTest,
                        RestoreWithImmediateTakeoff = true
                    };
                }

                snapshot.RestoreWithImmediateTakeoff = true;
                LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
            }
            else
            {
                LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
            }

            if (!ApplyReturnFromSnapshot(player, snapshot))
            {
                Debug.LogWarning(
                    "[LandCombat] Ground return could not be applied — leaving aircraft at current position.");
                return false;
            }

            if (LandMissionHandoffState.HasPendingReturn)
            {
                LandMissionHandoffState.TryConsumeReturnToFlight(out _, out _);
            }

            return true;
        }

        private static bool ApplyReturnFromSnapshot(GameObject player, LandSortieSnapshot snapshot)
        {
            if (snapshot.RestoreWithImmediateTakeoff)
            {
                return ApplyReturnTakeoff(player, snapshot);
            }

            if (ShouldReturnToRunwayDeck(snapshot))
            {
                return ApplyReturnToRunwayDeck(player, snapshot);
            }

            return ApplyReturnTakeoff(player, snapshot);
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

            LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
            EnsureOpenFieldMiles(ref snapshot);

            var aircraft = player.GetComponent<AircraftController>();
            var worldMap = aircraft?.WorldMap ?? Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = aircraft?.Profile ?? Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;

            if (!TryResolveLandingTakeoffPosition(snapshot, worldMap, ticSize, out var returnPosition, out var returnRotation))
            {
                Debug.LogWarning("[LandCombat] Ground return skipped — landing miles were unavailable.");
                return false;
            }

            returnPosition.y = 0f;
            player.transform.SetPositionAndRotation(returnPosition, returnRotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = returnPosition;
                body.rotation = returnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            RestoreAircraftState(player, snapshot);
            VtolTakeoffLaunch.BeginAt(player, returnPosition);

            EnsureApplier(player);
            LandMissionHandoffState.ClearPersistedReturnAfterApplication();

            Debug.Log(
                $"[LandCombat] Restored flight at {CampaignMapCoordinates.FormatMilesLabel(CampaignMapCoordinates.WorldToMiles(returnPosition, worldMap, ticSize))} "
                + $"(fuel {snapshot.FuelNormalized:P0}).");
            return true;
        }

        private static bool TryResolveLandingTakeoffPosition(
            LandSortieSnapshot snapshot,
            WorldMapConfig worldMap,
            float ticSize,
            out Vector3 returnPosition,
            out Quaternion returnRotation)
        {
            returnPosition = Vector3.zero;
            returnRotation = snapshot.AircraftWorldRotation;

            if (LandingMileFlagState.TryResolveFromSnapshot(snapshot, out returnPosition, out returnRotation)
                || LandingMileFlagState.TryResolveWorldPosition(out returnPosition, out returnRotation))
            {
                return true;
            }

            if (snapshot.HasLandingMiles)
            {
                returnPosition = CampaignMapCoordinates.MilesToWorld(
                    new Vector2(snapshot.LandingMileX, snapshot.LandingMileY),
                    worldMap,
                    ticSize);
                returnRotation = Quaternion.Euler(0f, snapshot.LandingRotationY, 0f);
                return true;
            }

            if (snapshot.IsOpenFieldLanding && OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
            {
                returnPosition = CampaignMapCoordinates.MilesToWorld(OpenFieldLandingState.LandingMiles, worldMap, ticSize);
                returnRotation = Quaternion.Euler(0f, snapshot.LandingRotationY, 0f);
                return true;
            }

            return false;
        }

        private static void EnsureOpenFieldMiles(ref LandSortieSnapshot snapshot)
        {
            if (!snapshot.IsOpenFieldLanding)
            {
                return;
            }

            if (OpenFieldLandingState.LandingMiles.sqrMagnitude <= 0.01f)
            {
                return;
            }

            snapshot.HasLandingMiles = true;
            snapshot.LandingMileX = OpenFieldLandingState.LandingMiles.x;
            snapshot.LandingMileY = OpenFieldLandingState.LandingMiles.y;
            LandingMileFlagState.SetFromMiles(
                OpenFieldLandingState.LandingMiles,
                snapshot.LandingRotationY);
        }

        public static bool ApplyReturnToRunwayDeck(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                Debug.LogWarning("[LandCombat] Runway deck return skipped — landing snapshot was invalid.");
                return false;
            }

            var aircraft = player.GetComponent<AircraftController>();
            var worldMap = aircraft?.WorldMap;
            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var returnPosition = FlightSpawnValidation.ResolveSortieReturnPosition(snapshot, worldMap, ticSize);
            returnPosition.y = 0f;
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

        public static bool ShouldSkipCarrierSpawn()
        {
            return LandingMileFlagState.HasActiveFlag
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn
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
