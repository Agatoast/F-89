using F89.Audio;
using F89.Flight;
using F89.LandCombat;
using F89.Testing;
using F89.UI;
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

        /// <summary>CV catapult already fired this scene load — ground-return applier must not override it.</summary>
        public static bool CarrierCatapultLaunchComplete { get; private set; }

        public static void ResetForSceneLoad()
        {
            SortieStartupComplete = false;
            CarrierCatapultLaunchComplete = false;
        }

        public static void Apply(AircraftController aircraft, GameObject playerObject)
        {
            if (aircraft == null || playerObject == null)
            {
                return;
            }

            if (AircraftLandingController.IsTakeoffActive)
            {
                return;
            }

            if (SortieStartupComplete
                && !FlightGroundReturnService.ShouldApplySortieReturn()
                && !FlightGroundReturnService.ShouldApplyMidAirRefuelReturn()
                && !FlightGroundReturnService.BlocksCarrierDeckTakeoff()
                && !LandingMileFlagState.HasActiveFlag
                && !LandMissionHandoffState.HasPendingGroundReturn
                && !LandMissionHandoffState.ShouldSuppressCarrierRespawn
                && !FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                && !FlightMissionLaunchState.HasPersistedFreshCarrierLaunch()
                && !FlightMissionLaunchState.HasPendingCarrierLaunch
                && !FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                return;
            }

            var startupSucceeded = false;
            try
            {
                GamePauseController.ClearPauseOnSceneLoad();
                MidAirRefuelHandoffState.ForceReloadFromPrefs();
                LandMissionHandoffState.ForceReloadFromPrefs();
                LandingMileFlagState.ForceReloadFromPrefs();
                FlightMissionLaunchState.ClearStaleCarrierWhenSortieReturnPending();
                FlightMissionLaunchState.ClearStaleCarrierLaunchIntentFromPrefs();
                FlightMissionLaunchState.TryRestorePendingLaunchFromPrefs();
                FlightMissionLaunchState.EnsureOutpostLaunchRestoredFromPrefs();
                FlightMissionLaunchState.ClearStaleCarrierOverrideWhenOutpostPending();
                FlightMissionLaunchState.ClearStaleCarrierLaunchIntentFromPrefs();

                var explicitCarrierSortie = FlightMissionLaunchState.IsExplicitCarrierSortiePending();
                if (explicitCarrierSortie && !CarrierDeckTakeoffState.IsPending)
                {
                    LandingMileFlagState.Clear();
                    OpenFieldLandingState.Clear();
                    LandMissionHandoffState.Clear();
                }

                var save = CharacterSessionState.ActiveSave;
                var hasLastLandedBase = MissionLaunchOrigin.HasSavedLaunchOutpost(save);
                var sortieReturnPending = !explicitCarrierSortie
                    && FlightGroundReturnService.ShouldApplySortieReturn();
                var forceCarrierDeck = !sortieReturnPending
                    && !FlightMissionLaunchState.ShouldHonorOutpostLaunch()
                    && (FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                        || FlightMissionLaunchState.HasPersistedFreshCarrierLaunch()
                        || (FlightMissionLaunchState.HasPendingCarrierLaunch && !hasLastLandedBase));
                if (forceCarrierDeck)
                {
                    if (!FlightGroundReturnService.BlocksCarrierDeckTakeoff()
                        && !FlightGroundReturnService.ShouldApplySortieReturn())
                    {
                        GameplaySessionBootstrap.ClearSessionHandoffOnly();
                    }

                    if (!FlightMissionLaunchState.HasPendingCarrierLaunch)
                    {
                        FlightMissionLaunchState.BeginFreshSortieLaunch();
                    }
                }

                MissionScoreState.HydrateFromSave(CharacterSessionState.ActiveSave);
                MissionLaunchOrigin.EnsureDefaultLaunchOriginIfMissing(CharacterSessionState.ActiveSave);
                FlightTestRuntimeBuilder.EnsurePlayerLockableTarget(playerObject);
                AntarcticaBaseSpawner.SpawnIfNeeded();
                CampaignWaypointSecondaryState.RestorePadsInScene();

                if (FlightGroundReturnService.ShouldApplyMidAirRefuelReturn()
                    && MidAirRefuelHandoffState.TryConsumeReturnToFlight(out var refuelReturnSnapshot)
                    && FlightGroundReturnService.TryApplyMidAirRefuelReturn(playerObject, refuelReturnSnapshot))
                {
                    MidAirRefuelHandoffState.ClearPersistedAfterApplication();
                    FlightAudioBootstrap.SyncFromAircraft(aircraft);
                    startupSucceeded = true;
                    return;
                }

                var applied = false;
                var outpostLaunchApplied = false;
                var carrierLaunchApplied = false;
                var explicitLaunchPending = !sortieReturnPending
                    && (forceCarrierDeck || FlightMissionLaunchState.HasPendingLaunch);

                // Ground return / landing-mile flag always beats stale loadout launch prefs.
                if (sortieReturnPending)
                {
                    var returnApplied = FlightGroundReturnService.TryBeginGroundReturnTakeoff(playerObject);
                    if (!returnApplied)
                    {
                        returnApplied = FlightGroundReturnService.TryApplyLandingFlagTakeoff(playerObject);
                    }

                    if (!returnApplied)
                    {
                        Debug.LogWarning(
                            "[F-89] Sortie return was queued but could not be applied — "
                            + "skipping default carrier deck spawn.");
                    }

                    applied = true;
                }
                else if (explicitLaunchPending)
                {
                    if (FlightMissionLaunchState.ShouldHonorOutpostLaunch())
                    {
                        outpostLaunchApplied = ApplyPendingLaunch(aircraft, playerObject);
                        applied = true;
                    }
                    else if (forceCarrierDeck || FlightMissionLaunchState.HasPendingCarrierLaunch)
                    {
                        if (!hasLastLandedBase || explicitCarrierSortie)
                        {
                            carrierLaunchApplied = ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
                        }
                        else
                        {
                            outpostLaunchApplied = TryApplySavedOutpostLaunch(aircraft, playerObject, out _);
                        }

                        applied = true;
                    }
                    else
                    {
                        outpostLaunchApplied = ApplyPendingLaunch(aircraft, playerObject);
                        applied = true;
                    }
                }
                else if (!applied && !FlightGroundReturnService.ShouldApplySortieReturn())
                {
                    if (TryApplySavedOutpostLaunch(aircraft, playerObject, out outpostLaunchApplied))
                    {
                        applied = true;
                    }
                    else
                    {
                        Debug.LogWarning(
                            "F-89: Scene startup skipped ocean CV spawn — launch from loadout or a land outpost.");
                        applied = true;
                    }
                }

                if (!AircraftLandingController.IsTakeoffActive
                    && !AircraftLandingController.IsRunwayDeckMenuVisible
                    && FlightMissionLaunchState.HasPendingCarrierLaunch
                    && !hasLastLandedBase
                    && !FlightMissionLaunchState.ShouldHonorOutpostLaunch()
                    && !FlightGroundReturnService.BlocksCarrierDeckTakeoff())
                {
                    carrierLaunchApplied |= ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
                }

                if (!carrierLaunchApplied)
                {
                    EnsureFreshSortieTakeoffStarted(aircraft);
                }

                if (applied && !sortieReturnPending && !outpostLaunchApplied
                    && !FlightMissionLaunchState.ShouldHonorOutpostLaunch()
                    && !FlightGroundReturnService.BlocksCarrierDeckTakeoff())
                {
                    FlightSpawnValidation.EnsurePlausibleOrCarrier(aircraft);
                }

                if (AircraftLandingController.IsTakeoffActive
                    || AircraftLandingController.IsRunwayDeckMenuVisible
                    || AircraftLandingController.IsParkedAtRunway
                    || carrierLaunchApplied)
                {
                    FlightMissionLaunchState.ClearPendingLaunchPrefs();
                    FlightMissionLaunchState.ClearForceCarrierDeckLaunch();
                }

                FlightAudioBootstrap.SyncFromAircraft(aircraft);
                if (sortieReturnPending && !AircraftLandingController.IsTakeoffActive)
                {
                    startupSucceeded = false;
                }
                else
                {
                    startupSucceeded = AircraftLandingController.IsTakeoffActive
                        || AircraftLandingController.IsRunwayDeckMenuVisible
                        || AircraftLandingController.IsParkedAtRunway
                        || carrierLaunchApplied
                        || (!FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                            && !FlightMissionLaunchState.HasPersistedFreshCarrierLaunch()
                            && !FlightMissionLaunchState.HasPendingLaunch);
                }
            }
            finally
            {
                if (startupSucceeded)
                {
                    SortieStartupComplete = true;
                }
            }
        }

        private static bool ApplyPendingLaunch(AircraftController aircraft, GameObject playerObject)
        {
            if (FlightGroundReturnService.ShouldApplySortieReturn())
            {
                return false;
            }

            if (FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                FlightMissionLaunchState.ClearForceCarrierDeckLaunch();
            }
            else if ((FlightMissionLaunchState.HasPendingCarrierLaunch
                || FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                || FlightMissionLaunchState.HasPersistedFreshCarrierLaunch())
                && !MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave))
            {
                if (FlightGroundReturnService.BlocksCarrierDeckTakeoff())
                {
                    return false;
                }

                if (!FlightMissionLaunchState.HasPendingCarrierLaunch)
                {
                    FlightMissionLaunchState.BeginFreshSortieLaunch();
                }

                ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
                return false;
            }

            var intendedOutpost = FlightMissionLaunchState.LaunchFromOutpostName;
            if (string.IsNullOrWhiteSpace(intendedOutpost)
                && FlightMissionLaunchState.TryGetPersistedOutpostLaunch(out var persistedOutpost))
            {
                intendedOutpost = persistedOutpost;
                FlightMissionLaunchState.BeginOutpostLaunch(intendedOutpost, vtolTakeoff: true);
            }

            if (aircraft.TryApplyMissionOutpostLaunch())
            {
                FlightMissionLaunchState.ConsumeFreshSortieLaunch();
                MissionLaunchOrigin.MaybePersistLaunchOutpostAfterTakeoff(
                    CharacterSessionState.ActiveSave,
                    intendedOutpost);
                aircraft.Refuel();
                return true;
            }

            // Never send a parked friendly-outpost launch to the ocean CV. Use layout miles.
            if (!string.IsNullOrWhiteSpace(intendedOutpost)
                && TryApplyOutpostLayoutLaunch(aircraft, playerObject, intendedOutpost))
            {
                FlightMissionLaunchState.ConsumeFreshSortieLaunch();
                MissionLaunchOrigin.MaybePersistLaunchOutpostAfterTakeoff(
                    CharacterSessionState.ActiveSave,
                    intendedOutpost);
                aircraft.Refuel();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(intendedOutpost))
            {
                Debug.LogError(
                    "F-89: Outpost launch failed for '"
                    + intendedOutpost
                    + "' — refusing ocean CV fallback while a parked launch base is saved.");
                return false;
            }

            if (MissionLaunchOrigin.TryGetSavedLastLandedBase(CharacterSessionState.ActiveSave, out var savedOutpost))
            {
                FlightMissionLaunchState.BeginOutpostLaunch(savedOutpost, vtolTakeoff: true);
                if (aircraft.TryApplyMissionOutpostLaunch()
                    || TryApplyOutpostLayoutLaunch(aircraft, playerObject, savedOutpost))
                {
                    FlightMissionLaunchState.ConsumeFreshSortieLaunch();
                    MissionLaunchOrigin.MaybePersistLaunchOutpostAfterTakeoff(
                        CharacterSessionState.ActiveSave,
                        savedOutpost);
                    aircraft.Refuel();
                    return true;
                }

                Debug.LogError(
                    "F-89: Saved launch outpost '"
                    + savedOutpost
                    + "' could not spawn — refusing carrier fallback.");
                return false;
            }

            Debug.LogWarning(
                "F-89: No outpost launch resolved — refusing implicit carrier deck spawn.");
            return false;
        }

        private static bool TryApplySavedOutpostLaunch(
            AircraftController aircraft,
            GameObject playerObject,
            out bool outpostLaunchApplied)
        {
            outpostLaunchApplied = false;
            if (FlightGroundReturnService.ShouldApplySortieReturn())
            {
                return false;
            }

            if (!MissionLaunchOrigin.TryGetSavedLastLandedBase(CharacterSessionState.ActiveSave, out var outpostName))
            {
                return false;
            }

            FlightMissionLaunchState.BeginFreshSortieLaunch(outpostName, vtolTakeoff: true);
            outpostLaunchApplied = ApplyPendingLaunch(aircraft, playerObject);
            return true;
        }

        private static bool TryApplyOutpostLayoutLaunch(
            AircraftController aircraft,
            GameObject playerObject,
            string outpostName)
        {
            if (aircraft == null
                || playerObject == null
                || !OutpostRunwayLanding.TryGetLayoutMilesSpawn(
                    outpostName,
                    out var spawnPosition,
                    out var spawnRotation))
            {
                return false;
            }

            if (!FlightMissionLaunchState.TryConsumeOutpostLaunch(out _, out _))
            {
                // Launch flag may already be partially consumed; still place at outpost miles.
                FlightMissionLaunchState.Clear();
            }

            playerObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var body = playerObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            VtolTakeoffLaunch.BeginAt(aircraft, spawnPosition);
            Debug.LogWarning(
                $"F-89: Recovered outpost launch at layout miles for '{outpostName}' "
                + "(avoided carrier ocean fallback).");
            return true;
        }

        private static bool ApplyCarrierDeckStart(AircraftController aircraft, bool consumeLaunchFlag)
        {
            var restoreDeckTakeoff = CarrierDeckTakeoffState.IsPending;
            var explicitCarrierSortie = FlightMissionLaunchState.HasPendingCarrierLaunch
                || restoreDeckTakeoff
                || FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                || FlightMissionLaunchState.HasPersistedFreshCarrierLaunch();

            if (!restoreDeckTakeoff && explicitCarrierSortie)
            {
                LandingMileFlagState.Clear();
                OpenFieldLandingState.Clear();
            }

            if (FlightGroundReturnService.BlocksCarrierDeckTakeoff()
                || FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                Debug.LogError(
                    "F-89: Blocked carrier deck spawn — ground landing return or outpost launch is active.");
                return false;
            }

            if (MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave)
                && !FlightMissionLaunchState.IsExplicitCarrierSortiePending())
            {
                Debug.LogError(
                    "F-89: Blocked carrier deck spawn — last-landed base is saved on the character.");
                return false;
            }

            if (!explicitCarrierSortie)
            {
                Debug.LogError("F-89: Blocked ocean CV deck spawn — no explicit carrier sortie.");
                return false;
            }

            if (!FlightMissionLaunchState.HasPendingCarrierLaunch)
            {
                FlightMissionLaunchState.BeginFreshSortieLaunch();
            }

            var freshSortie = FlightMissionLaunchState.ConsumeFreshSortieLaunch();
            restoreDeckTakeoff = CarrierDeckTakeoffState.Consume();
            var deckSnapshot = restoreDeckTakeoff
                ? LandMissionHandoffState.GetStoredFlightSnapshot()
                : LandSortieSnapshot.Empty;

            if (!restoreDeckTakeoff)
            {
                LandMissionHandoffState.Clear();
            }

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

            if (!freshSortie && restoreDeckTakeoff && deckSnapshot.IsValid)
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

            if (!CarrierDeckLaunch.BeginTakeoff(aircraft))
            {
                Debug.LogError(
                    "[F-89] Carrier deck placement succeeded but catapult launch failed to start.");
                return false;
            }

            GamePauseController.ClearPauseOnSceneLoad();
            FlightMissionLaunchState.ClearForceCarrierDeckLaunch();
            FlightMissionLaunchState.ClearPendingLaunchPrefs();
            CarrierCatapultLaunchComplete = true;
            return true;
        }

        /// <summary>Last-resort carrier deck placement when bootstrap ran but takeoff never started.</summary>
        public static void TryForceCarrierDeckTakeoff(AircraftController aircraft, GameObject playerObject)
        {
            if (aircraft == null
                || playerObject == null
                || AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsRunwayDeckMenuVisible
                || FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return;
            }

            if (!FlightMissionLaunchState.HasForceCarrierDeckLaunch()
                && !FlightMissionLaunchState.HasPersistedFreshCarrierLaunch()
                && !FlightMissionLaunchState.HasPendingCarrierLaunch
                && !FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                return;
            }

            if (MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave)
                && !FlightMissionLaunchState.IsExplicitCarrierSortiePending())
            {
                return;
            }

            SortieStartupComplete = false;
            Apply(aircraft, playerObject);
        }

        private static void EnsureFreshSortieTakeoffStarted(AircraftController aircraft)
        {
            if (aircraft == null
                || AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsRunwayDeckMenuVisible
                || AircraftLandingController.IsParkedAtRunway
                || FlightGroundReturnService.ShouldApplySortieReturn()
                || FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                return;
            }

            FlightMissionLaunchState.TryRestorePendingLaunchFromPrefs();

            if (FlightMissionLaunchState.ShouldHonorOutpostLaunch())
            {
                if (aircraft.TryApplyMissionOutpostLaunch())
                {
                    return;
                }

                var outpostName = FlightMissionLaunchState.LaunchFromOutpostName;
                if (string.IsNullOrWhiteSpace(outpostName)
                    && !FlightMissionLaunchState.TryGetPersistedOutpostLaunch(out outpostName))
                {
                    outpostName = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(outpostName)
                    && TryApplyOutpostLayoutLaunch(aircraft, aircraft.gameObject, outpostName))
                {
                    return;
                }

                Debug.LogError(
                    "[F-89] Outpost launch was requested but takeoff never started for '"
                    + outpostName
                    + "'.");
                return;
            }

            if (FlightMissionLaunchState.IsExplicitCarrierSortiePending()
                && !FlightGroundReturnService.BlocksCarrierDeckTakeoff())
            {
                Debug.LogWarning("[F-89] Recovering missed carrier deck launch — starting catapult takeoff.");
                ApplyCarrierDeckStart(aircraft, consumeLaunchFlag: true);
                return;
            }

            if (MissionLaunchOrigin.TryGetSavedLastLandedBase(CharacterSessionState.ActiveSave, out var savedOutpost)
                && TryApplyOutpostLayoutLaunch(aircraft, aircraft.gameObject, savedOutpost))
            {
                return;
            }

            if (MissionLaunchOrigin.HasSavedLaunchOutpost(CharacterSessionState.ActiveSave))
            {
                Debug.LogError(
                    "[F-89] Takeoff never started — saved last-land base could not spawn and carrier was not queued.");
                return;
            }

            Debug.LogError("[F-89] Takeoff never started — no launch origin was resolved.");
        }

        private static void RestoreDeckTakeoffAircraftState(
            AircraftController aircraft,
            LandSortieSnapshot snapshot)
        {
            if (aircraft == null)
            {
                return;
            }

            SortieSnapshotFuel.ResolveTankGallons(snapshot, out var leftGallons, out var rightGallons);
            aircraft.ApplyFuelState(
                leftGallons,
                rightGallons,
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
