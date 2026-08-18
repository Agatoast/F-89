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
        public static bool ShouldPreferLandingMileTakeoff(LandSortieSnapshot snapshot)
        {
            return LandingMileFlagState.HasActiveFlag
                || snapshot.HasLandingMiles
                || snapshot.IsOpenFieldLanding
                || !string.IsNullOrWhiteSpace(snapshot.WaypointSiteCode);
        }

        private static bool ShouldUseRunwayDeckReturn(LandSortieSnapshot snapshot)
        {
            return snapshot.ReturnToRunwayDeck && !snapshot.RestoreWithImmediateTakeoff;
        }

        public static bool TryGetPendingReturnSpawn(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            LandingMileFlagState.ForceReloadFromPrefs();
            LandMissionHandoffState.ForceReloadFromPrefs();

            if (LandingMileFlagState.TryResolveWorldPosition(out position, out rotation))
            {
                return true;
            }

            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot))
            {
                return false;
            }

            var map = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var tic = Resources.Load<FlightProfile>("F89_DefaultFlightProfile")?.ticSizeWorldUnits ?? 1f;

            if (TryResolveLandingMileSpawn(snapshot, map, tic, out position, out rotation))
            {
                return true;
            }

            EnsureWaypointLandingFlag(snapshot);
            if (TryResolveWaypointPadSpawn(snapshot, out position, out rotation))
            {
                return true;
            }

            if (ShouldUseRunwayDeckReturn(snapshot)
                && !string.IsNullOrWhiteSpace(snapshot.OutpostName))
            {
                if (TryResolveRunwayDeckReturnSpawn(snapshot, map, tic, out position, out rotation))
                {
                    return true;
                }

                if (snapshot.AircraftWorldPosition.sqrMagnitude > 1f
                    && IsRunwayDeckLandPosition(snapshot.AircraftWorldPosition, map, tic))
                {
                    position = snapshot.AircraftWorldPosition;
                    position.y = 0f;
                    rotation = snapshot.AircraftWorldRotation;
                    return true;
                }
            }

            position = FlightSpawnValidation.ResolveSortieReturnPosition(snapshot, map, tic);
            rotation = snapshot.AircraftWorldRotation;
            if (!ShouldPreferLandingMileTakeoff(snapshot)
                && TryRejectImplicitCarrierSpawn(snapshot, map, tic, ref position, ref rotation))
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// Never use the locked CV deck as an implicit return spawn when a land base is known.
        /// </summary>
        private static bool TryRejectImplicitCarrierSpawn(
            LandSortieSnapshot snapshot,
            WorldMapConfig worldMap,
            float ticSize,
            ref Vector3 position,
            ref Quaternion rotation)
        {
            if (ShouldPreferLandingMileTakeoff(snapshot))
            {
                return false;
            }

            if (CarrierOceanSpawnGate.AllowsOceanCarrierSpawn())
            {
                return false;
            }

            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            if (!FlightSpawnValidation.IsNearCarrierDeck(position, worldMap, profile))
            {
                return false;
            }

            if (TryResolveOutpostRunwaySpawn(snapshot.OutpostName, worldMap, ticSize, out position, out rotation))
            {
                return true;
            }

            if (MissionLaunchOrigin.TryResolveLaunchOutpost(CharacterSessionState.ActiveSave, out var outpostName)
                && TryResolveOutpostRunwaySpawn(outpostName, worldMap, ticSize, out position, out rotation))
            {
                position.y = 0f;
                Debug.LogWarning(
                    $"F-89: Blocked implicit ocean CV return spawn — recovered '{outpostName}' runway.");
                return true;
            }

            Debug.LogError(
                "F-89: Blocked implicit ocean CV return spawn — no land runway could be resolved.");
            return false;
        }

        public static bool IsPendingRunwayDeckReturn()
        {
            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot))
            {
                return false;
            }

            return ShouldUseRunwayDeckReturn(snapshot);
        }

        public static bool TryApplyRunwayDeckReturnFallback(GameObject player)
        {
            if (player == null
                || !LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot)
                || !ShouldUseRunwayDeckReturn(snapshot))
            {
                return false;
            }

            return ApplyReturnToRunwayDeck(player, snapshot);
        }

        public static bool TryBeginGroundReturnTakeoff(GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            LandingMileFlagState.ForceReloadFromPrefs();
            LandMissionHandoffState.ForceReloadFromPrefs();
            FlightMissionLaunchState.ClearStaleCarrierWhenSortieReturnPending();
            EnsureWaypointLandingFlag(LandMissionHandoffState.GetStoredFlightSnapshot());

            if (!ShouldApplySortieReturn() && !BlocksCarrierDeckTakeoff())
            {
                return false;
            }

            if (TryApplyPendingReturn(player))
            {
                return AircraftLandingController.IsTakeoffActive
                    || AircraftLandingController.IsRunwayDeckMenuVisible
                    || AircraftLandingController.IsParkedAtRunway;
            }

            if (TryApplyLandingFlagTakeoff(player))
            {
                return AircraftLandingController.IsTakeoffActive;
            }

            return false;
        }

        /// <summary>Places the player at the landing mile flag and starts VTOL — no carrier/outpost fallbacks.</summary>
        public static bool TryApplyLandingFlagTakeoff(GameObject player)
        {
            if (player == null)
            {
                return false;
            }

            LandingMileFlagState.ForceReloadFromPrefs();
            var storedSnapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            EnsureWaypointLandingFlag(storedSnapshot);

            var aircraft = player.GetComponent<AircraftController>();
            var worldMap = aircraft?.WorldMap ?? Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = aircraft?.Profile ?? Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;

            Vector3 milePosition;
            Quaternion mileRotation;
            if (LandingMileFlagState.TryResolveWorldPosition(out milePosition, out mileRotation))
            {
                // Active landing flag always wins.
            }
            else
            {
                var snapshot = storedSnapshot;
                if (!TryResolveLandingMileSpawn(snapshot, worldMap, ticSize, out milePosition, out mileRotation)
                    && !TryResolveWaypointPadSpawn(snapshot, out milePosition, out mileRotation))
                {
                    return false;
                }
            }

            milePosition.y = 0f;
            player.transform.SetPositionAndRotation(milePosition, mileRotation);
            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = milePosition;
                body.rotation = mileRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (storedSnapshot.IsValid)
            {
                RestoreAircraftStateFromSnapshot(player, storedSnapshot);
            }

            VtolTakeoffLaunch.BeginAt(player, milePosition);
            if (LandMissionHandoffState.HasPendingReturn)
            {
                LandMissionHandoffState.TryConsumeReturnToFlight(out _, out _);
            }

            LandMissionHandoffState.ClearPersistedReturnAfterApplication();
            Debug.LogWarning(
                "[F-89] Applied landing mile flag takeoff at "
                + CampaignMapCoordinates.FormatMilesLabel(
                    CampaignMapCoordinates.WorldToMiles(milePosition, worldMap, ticSize))
                + ".");
            return AircraftLandingController.IsTakeoffActive;
        }

        public static bool ShouldApplySortieReturn()
        {
            if (LandingMileFlagState.HasActiveFlag)
            {
                return true;
            }

            if (LandMissionHandoffState.HasPendingReturn)
            {
                return true;
            }

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            return snapshot.IsValid
                && (snapshot.RestoreWithImmediateTakeoff || ShouldPreferLandingMileTakeoff(snapshot));
        }

        /// <summary>
        /// Carrier deck spawn/VTOL is only for explicit loadout CV launches and CV landing —
        /// never for ground-return or landing-mile takeoff.
        /// </summary>
        public static bool BlocksCarrierDeckTakeoff()
        {
            if (LandingMileFlagState.HasActiveFlag)
            {
                return true;
            }

            if (LandMissionHandoffState.HasPendingGroundReturn
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn)
            {
                return true;
            }

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            return snapshot.IsValid
                && (snapshot.RestoreWithImmediateTakeoff || ShouldPreferLandingMileTakeoff(snapshot));
        }

        /// <summary>
        /// Skip full-tank spawn refuel when a ground return will restore fuel from the landing snapshot.
        /// </summary>
        public static bool ShouldPreserveSortieFuelOnSpawn()
        {
            if (!ShouldApplySortieReturn() && !LandMissionHandoffState.ShouldSuppressCarrierRespawn)
            {
                return false;
            }

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            return snapshot.IsValid
                && (SortieSnapshotFuel.HasUsableFuel(snapshot)
                    || snapshot.FuelNormalized > 0.001f
                    || snapshot.LeftTankGallons + snapshot.RightTankGallons > 0f);
        }

        public static void RestoreAircraftStateFromSnapshot(GameObject player, LandSortieSnapshot snapshot)
        {
            RestoreAircraftState(player, snapshot);
        }

        public static bool TryApplyMidAirRefuelReturn(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                return false;
            }

            player.transform.SetPositionAndRotation(
                snapshot.AircraftWorldPosition,
                snapshot.AircraftWorldRotation);
            if (player.TryGetComponent<Rigidbody>(out var body))
            {
                body.position = snapshot.AircraftWorldPosition;
                body.rotation = snapshot.AircraftWorldRotation;
            }

            RestoreAircraftState(player, snapshot);

            var aircraft = player.GetComponent<AircraftController>();
            if (aircraft != null && snapshot.HasInFlightSpeed)
            {
                aircraft.RestoreInFlightMotion(snapshot.InFlightSpeedMph, snapshot.InFlightAutopilotActive);
            }

            return true;
        }

        public static bool ShouldApplyMidAirRefuelReturn()
        {
            MidAirRefuelHandoffState.ForceReloadFromPrefs();
            return MidAirRefuelHandoffState.HasPendingReturn;
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

            if (AircraftLandingController.IsRunwayDeckMenuVisible)
            {
                return true;
            }

            if (AircraftLandingController.IsParkedAtRunway
                && !BlocksCarrierDeckTakeoff()
                && !LandMissionHandoffState.HasPendingGroundReturn
                && !LandingMileFlagState.HasActiveFlag)
            {
                return true;
            }

            if (LandMissionHandoffState.TryGetPendingReturnSnapshot(out var runwayDeckSnapshot)
                && runwayDeckSnapshot.IsValid
                && ShouldUseRunwayDeckReturn(runwayDeckSnapshot))
            {
                runwayDeckSnapshot.RestoreWithImmediateTakeoff = false;
                if (ApplyReturnToRunwayDeck(player, runwayDeckSnapshot)
                    || TryForceRunwayDeckMenuRestore(player))
                {
                    if (LandMissionHandoffState.HasPendingReturn)
                    {
                        LandMissionHandoffState.TryConsumeReturnToFlight(out _, out _);
                    }

                    return true;
                }

                Debug.LogWarning(
                    "[LandCombat] Runway deck return could not be restored — leaving aircraft in place.");
                return false;
            }

            if (!LandMissionHandoffState.TryGetPendingReturnSnapshot(out var snapshot)
                || !snapshot.IsValid)
            {
                if (!LandingMileFlagState.HasActiveFlag)
                {
                    return TryForceRunwayDeckMenuRestore(player);
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
            else if (ShouldUseRunwayDeckReturn(snapshot))
            {
                snapshot.RestoreWithImmediateTakeoff = false;
            }
            else
            {
                snapshot.ReturnToRunwayDeck = false;
                snapshot.RestoreWithImmediateTakeoff = true;
                LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
            }

            if (!ApplyReturnFromSnapshot(player, snapshot))
            {
                if (TryForceRunwayDeckMenuRestore(player))
                {
                    return true;
                }

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

        /// <summary>
        /// Last-resort restore for loadout REARM → RETURN TO BASE when handoff snapshot was incomplete.
        /// </summary>
        public static bool TryForceRunwayDeckMenuRestore(GameObject player)
        {
            if (player == null || !OutpostRunwayDeckState.IsParkedAtRunway)
            {
                return false;
            }

            var snapshot = new LandSortieSnapshot
            {
                IsValid = true,
                ReturnSceneName = GameScenes.FlightTest,
                ReturnToRunwayDeck = true,
                RestoreWithImmediateTakeoff = false,
                OutpostName = OutpostRunwayDeckState.ParkedOutpostName
            };

            return ApplyReturnToRunwayDeck(player, snapshot);
        }

        private static bool ApplyReturnFromSnapshot(GameObject player, LandSortieSnapshot snapshot)
        {
            if (ShouldReturnToRunwayDeck(snapshot))
            {
                return ApplyReturnToRunwayDeck(player, snapshot);
            }

            if (snapshot.RestoreWithImmediateTakeoff)
            {
                return ApplyReturnTakeoff(player, snapshot);
            }

            return ApplyReturnTakeoff(player, snapshot);
        }

        private static bool ShouldReturnToRunwayDeck(LandSortieSnapshot snapshot)
        {
            return ShouldUseRunwayDeckReturn(snapshot);
        }

        public static bool ApplyReturnTakeoff(GameObject player, LandSortieSnapshot snapshot)
        {
            if (player == null || !snapshot.IsValid)
            {
                Debug.LogWarning("[LandCombat] Ground return skipped — landing snapshot was invalid.");
                return false;
            }

            EnsureWaypointLandingFlag(snapshot);
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

            if (TryResolveWaypointPadSpawn(snapshot, out returnPosition, out returnRotation))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Re-applies the landing mile flag from a waypoint secondary pad when prefs were lost.
        /// </summary>
        public static void EnsureWaypointLandingFlag(LandSortieSnapshot snapshot)
        {
            if (LandingMileFlagState.HasActiveFlag
                || !snapshot.IsValid
                || string.IsNullOrWhiteSpace(snapshot.WaypointSiteCode))
            {
                return;
            }

            if (!CampaignWaypointSecondaryState.TryGetPadWorldPosition(
                    snapshot.WaypointSiteCode,
                    out var padPosition))
            {
                return;
            }

            var rotation = snapshot.HasLandingMiles
                ? Quaternion.Euler(0f, snapshot.LandingRotationY, 0f)
                : snapshot.AircraftWorldRotation;
            LandingMileFlagState.SetFromLandingSquare(padPosition, rotation);
        }

        private static bool TryResolveWaypointPadSpawn(
            LandSortieSnapshot snapshot,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!snapshot.IsValid || string.IsNullOrWhiteSpace(snapshot.WaypointSiteCode))
            {
                return false;
            }

            if (!CampaignWaypointSecondaryState.TryGetPadWorldPosition(snapshot.WaypointSiteCode, out position))
            {
                return false;
            }

            rotation = snapshot.HasLandingMiles
                ? Quaternion.Euler(0f, snapshot.LandingRotationY, 0f)
                : snapshot.AircraftWorldRotation;
            position.y = 0f;
            return true;
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

            snapshot.RestoreWithImmediateTakeoff = false;
            LandingMileFlagState.TryApplyToSnapshot(ref snapshot);

            var aircraft = player.GetComponent<AircraftController>();
            var worldMap = aircraft?.WorldMap ?? Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var ticSize = aircraft?.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            Vector3 returnPosition;
            Quaternion returnRotation;

            if (TryResolveLandingMileSpawn(snapshot, worldMap, ticSize, out returnPosition, out returnRotation))
            {
                // Landing square flag always wins for deck return.
            }
            else if (TryResolveRunwayDeckReturnSpawn(snapshot, worldMap, ticSize, out returnPosition, out returnRotation))
            {
                // Runway spawn fallback when no landing flag was saved.
            }
            else if (snapshot.AircraftWorldPosition.sqrMagnitude > 1f
                && IsRunwayDeckLandPosition(snapshot.AircraftWorldPosition, worldMap, ticSize))
            {
                returnPosition = snapshot.AircraftWorldPosition;
                returnRotation = snapshot.AircraftWorldRotation;
            }
            else
            {
                Debug.LogError(
                    "[LandCombat] Runway deck return failed — no landing square flag or runway position.");
                return false;
            }

            returnPosition.y = 0f;
            player.transform.SetPositionAndRotation(returnPosition, returnRotation);

            var body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            RestoreAircraftState(player, snapshot);

            var weapons = player.GetComponent<PlayerWeaponController>();
            AircraftLoadoutState.ApplyToWeaponController(weapons);

            var landing = player.GetComponent<AircraftLandingController>();
            if (landing == null)
            {
                landing = player.AddComponent<AircraftLandingController>();
            }

            landing.RestoreRunwayDeckParked(snapshot);
            EnsureApplier(player);
            LandMissionHandoffState.ClearPersistedReturnAfterApplication();

            Debug.Log(
                $"[LandCombat] Restored parked friendly runway at {snapshot.OutpostName} "
                + $"(fuel {snapshot.FuelNormalized:P0}).");
            return true;
        }

        public static bool ShouldSkipCarrierSpawn()
        {
            return BlocksCarrierDeckTakeoff();
        }

        private static void RestoreAircraftState(GameObject player, LandSortieSnapshot snapshot)
        {
            var aircraft = player.GetComponent<AircraftController>();
            if (aircraft != null)
            {
                SortieSnapshotFuel.ResolveTankGallons(snapshot, out var leftGallons, out var rightGallons);
                aircraft.ApplyFuelState(
                    leftGallons,
                    rightGallons,
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

        private static bool IsRunwayDeckLandPosition(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            float ticSize)
        {
            return worldMap != null
                && ticSize > 0f
                && AntarcticaLandMask.IsLandingLandWorld(worldPosition, worldMap, ticSize);
        }

        private static bool TryResolveOutpostRunwaySpawn(
            string outpostName,
            WorldMapConfig worldMap,
            float ticSize,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (OutpostRunwayLanding.TryGetRunwaySpawn(outpostName, out position, out rotation))
            {
                position.y = 0f;
                return true;
            }

            if (OutpostRunwayLanding.TryGetLayoutMilesSpawn(outpostName, out position, out rotation)
                && worldMap != null
                && AntarcticaLandMask.IsDisplayInlandMiles(
                    CampaignMapCoordinates.WorldToMiles(position, worldMap, ticSize),
                    worldMap.antarcticaSizeMiles,
                    AntarcticaLandMask.BasePlacementInsetMiles))
            {
                position.y = 0f;
                return true;
            }

            _ = worldMap;
            _ = ticSize;
            return false;
        }

        private static bool TryResolveLandingMileSpawn(
            LandSortieSnapshot snapshot,
            WorldMapConfig worldMap,
            float ticSize,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (LandingMileFlagState.TryResolveWorldPosition(out position, out rotation)
                || LandingMileFlagState.TryResolveFromSnapshot(snapshot, out position, out rotation))
            {
                position.y = 0f;
                return true;
            }

            if (snapshot.HasLandingMiles)
            {
                position = CampaignMapCoordinates.MilesToWorld(
                    new Vector2(snapshot.LandingMileX, snapshot.LandingMileY),
                    worldMap,
                    ticSize);
                rotation = Quaternion.Euler(0f, snapshot.LandingRotationY, 0f);
                position.y = 0f;
                return true;
            }

            if (TryResolveWaypointPadSpawn(snapshot, out position, out rotation))
            {
                return true;
            }

            _ = worldMap;
            _ = ticSize;
            return false;
        }

        private static bool TryResolveRunwayDeckReturnSpawn(
            LandSortieSnapshot snapshot,
            WorldMapConfig worldMap,
            float ticSize,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!ShouldUseRunwayDeckReturn(snapshot) || string.IsNullOrWhiteSpace(snapshot.OutpostName))
            {
                return false;
            }

            return TryResolveOutpostRunwaySpawn(snapshot.OutpostName, worldMap, ticSize, out position, out rotation);
        }
    }
}
