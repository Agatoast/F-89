using F89.Audio;
using F89.Controls;
using F89.Core;
using F89.LandCombat;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Flight
{
    public class AircraftLandingController : MonoBehaviour
    {
        public const float ShrinkDurationSeconds = 5f;
        public const float TargetVisualScale = 0.1f;

        private static AircraftLandingController activeInstance;

        private AircraftController aircraft;
        private PlayerAircraftInput input;
        private Rigidbody body;
        private Transform visualPivot;
        private Vector3 initialVisualScale = Vector3.one;
        private float sequenceStartTime;
        private float currentVisualScale = 1f;
        private bool sequenceActive;
        private bool carrierLanding;
        private bool friendlyBaseLanding;
        private bool outpostGroundLanding;
        private AntarcticaBase outpostBaseSite;
        private bool runwayDeckMenuVisible;
        private RunwayDeckConfirmDialog.Action pendingRunwayConfirm;
        private bool endMissionFailureConfirmVisible;
        private bool takeoffActive;
        private bool landingComplete;
        private bool hasGroundReturnPosition;
        private Vector3 groundReturnPosition;
        private Vector3 carrierLandingStart;
        private Vector3 carrierLandingTarget;

        private bool carrierApproachPromptVisible;
        private bool carrierApproachDeclined;
        private bool wasInCarrierApproachGrid;
        private bool carrierApproachInitialized;
        private bool crashLanding;

        public static bool IsLandingActive => activeInstance != null && activeInstance.sequenceActive;
        public static bool IsCrashLandingActive => activeInstance != null && activeInstance.crashLanding && activeInstance.sequenceActive;
        public static bool IsTakeoffActive => activeInstance != null && activeInstance.takeoffActive;
        public static bool IsLandingComplete => activeInstance != null && activeInstance.landingComplete;
        public static bool IsCarrierApproachPromptVisible =>
            activeInstance != null && activeInstance.carrierApproachPromptVisible;
        public static bool IsRunwayDeckMenuVisible =>
            activeInstance != null && activeInstance.runwayDeckMenuVisible;
        public static bool IsRunwayRefuelPromptVisible => IsRunwayDeckMenuVisible;
        public static bool IsParkedAtRunway =>
            activeInstance != null && activeInstance.landingComplete && activeInstance.friendlyBaseLanding;
        public static float VisualScaleMultiplier => activeInstance?.currentVisualScale ?? 1f;

        private void Awake()
        {
            aircraft = GetComponent<AircraftController>();
            input = GetComponent<PlayerAircraftInput>();
            body = GetComponent<Rigidbody>();
            visualPivot = transform.Find("VisualPivot");
            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }
        }

        private void OnDestroy()
        {
            if (activeInstance == this)
            {
                activeInstance = null;
            }
        }

        public void BeginLanding()
        {
            if (sequenceActive || landingComplete)
            {
                return;
            }

            sequenceActive = true;
            carrierLanding = false;
            activeInstance = this;
            sequenceStartTime = Time.time;
            FlightAudio.SetInFlight(false);

            if (visualPivot != null)
            {
                initialVisualScale = visualPivot.localScale;
            }

            var autopilot = GetComponent<AutopilotController>();
            autopilot?.DisengageAutopilot("Landing.");
            // L initiates a VTOL landing at the exact current point. Stop horizontal
            // travel immediately so the landing animation cannot carry the aircraft
            // into a neighboring map square.
            aircraft?.SetLandingLocked(true);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (input != null)
            {
                input.enabled = false;
            }
        }

        /// <summary>Forced VTOL descent after structural failure — routes to crash landing pages.</summary>
        public void BeginCrashLanding()
        {
            if (sequenceActive || landingComplete)
            {
                return;
            }

            crashLanding = true;
            BeginLanding();
        }

        public void BeginFriendlyBaseLanding(AntarcticaBase baseSite)
        {
            if (sequenceActive || landingComplete || baseSite == null)
            {
                return;
            }

            outpostBaseSite = baseSite;
            friendlyBaseLanding = true;
            BeginLanding();
        }

        public void BeginOutpostGroundLanding(AntarcticaBase baseSite)
        {
            if (sequenceActive || landingComplete || baseSite == null)
            {
                return;
            }

            outpostBaseSite = baseSite;
            outpostGroundLanding = true;
            BeginLanding();
        }

        public void BeginCarrierLanding()
        {
            if (sequenceActive || landingComplete)
            {
                return;
            }

            ClearCarrierApproachPrompt(unlockFlight: false);
            carrierLandingStart = transform.position;
            carrierLandingStart.y = 0f;
            if (aircraft?.WorldMap != null && aircraft.Profile != null)
            {
                carrierLandingTarget = AntarcticaWorldLocations.GetLockedCarrierWorldPosition(
                    aircraft.WorldMap,
                    aircraft.Profile.ticSizeWorldUnits);
            }
            else if (AircraftLanding.TryGetCarrierBase(out var carrier))
            {
                carrierLandingTarget = carrier.transform.position;
            }
            else
            {
                carrierLandingTarget = carrierLandingStart;
            }

            carrierLandingTarget.y = 0f;

            carrierLanding = true;
            BeginLanding();
            carrierLanding = true;
        }

        public void PrepareForGroundReturn()
        {
            PrepareForGroundReturn(Vector3.zero);
            hasGroundReturnPosition = false;
        }

        /// <summary>Prepares VTOL takeoff from a specific flight-map grid square.</summary>
        public void PrepareForGroundReturn(Vector3 returnPosition)
        {
            activeInstance = this;
            sequenceActive = false;
            takeoffActive = false;
            landingComplete = false;
            hasGroundReturnPosition = true;
            groundReturnPosition = returnPosition;
            currentVisualScale = TargetVisualScale;

            if (visualPivot != null)
            {
                if (initialVisualScale.sqrMagnitude < 0.0001f)
                {
                    initialVisualScale = Vector3.one;
                }

                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            aircraft?.SetLandingLocked(true);

            if (input != null)
            {
                input.enabled = false;
            }
        }

        public void BeginTakeoff()
        {
            if (takeoffActive || sequenceActive)
            {
                return;
            }

            takeoffActive = true;
            activeInstance = this;
            sequenceStartTime = Time.time;
            currentVisualScale = TargetVisualScale;
            FlightAudio.SetInFlight(true);

            if (input != null)
            {
                input.enabled = false;
            }
        }

        private void Update()
        {
            if (takeoffActive)
            {
                UpdateTakeoffVisual();
                return;
            }

            if (sequenceActive)
            {
                UpdateLandingSequence();
                return;
            }

            UpdateCarrierApproachPrompt();
        }

        private void OnGUI()
        {
            if (carrierApproachPromptVisible)
            {
                var carrierResult = CarrierLandConfirmDialog.Draw(true);
                if (carrierResult == CarrierLandConfirmDialog.Result.Confirmed)
                {
                    AcceptCarrierApproachLanding();
                }
                else if (carrierResult == CarrierLandConfirmDialog.Result.Cancelled)
                {
                    DeclineCarrierApproachLanding();
                }

                return;
            }

            if (runwayDeckMenuVisible)
            {
                if (endMissionFailureConfirmVisible)
                {
                    var failureResult = EndMissionFailureConfirmDialog.Draw(true);
                    if (failureResult == EndMissionFailureConfirmDialog.Result.Confirmed)
                    {
                        ExecuteRunwayEndMission(applyCampaignFailurePenalty: true);
                    }
                    else if (failureResult == EndMissionFailureConfirmDialog.Result.Cancelled)
                    {
                        endMissionFailureConfirmVisible = false;
                    }

                    return;
                }

                if (pendingRunwayConfirm != RunwayDeckConfirmDialog.Action.None)
                {
                    var confirmResult = RunwayDeckConfirmDialog.Draw(pendingRunwayConfirm);
                    if (confirmResult == RunwayDeckConfirmDialog.Result.Confirmed)
                    {
                        ExecuteRunwayDeckAction(pendingRunwayConfirm);
                    }
                    else if (confirmResult == RunwayDeckConfirmDialog.Result.Cancelled)
                    {
                        pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.None;
                    }

                    return;
                }

                var deckResult = RunwayDeckMenuDialog.Draw(true, RunwayDeckMenuOptions.BuildFlightDeckOptions());
                if (deckResult == RunwayDeckMenuDialog.Result.Rearm)
                {
                    pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.Rearm;
                }
                else if (deckResult == RunwayDeckMenuDialog.Result.Refuel)
                {
                    pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.Refuel;
                }
                else if (deckResult == RunwayDeckMenuDialog.Result.TakeOff)
                {
                    pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.TakeOff;
                }
                else if (deckResult == RunwayDeckMenuDialog.Result.EndMission)
                {
                    RequestRunwayEndMission();
                }

                return;
            }
        }

        private void RequestRunwayEndMission()
        {
            if (CampaignMissionEndFlow.RequiresFailureConfirm)
            {
                endMissionFailureConfirmVisible = true;
                return;
            }

            ExecuteRunwayEndMission(applyCampaignFailurePenalty: false);
        }

        private void ExecuteRunwayEndMission(bool applyCampaignFailurePenalty)
        {
            endMissionFailureConfirmVisible = false;
            runwayDeckMenuVisible = false;
            pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.None;

            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName) && outpostBaseSite != null)
            {
                outpostName = outpostBaseSite.BaseName;
            }

            var result = CampaignMissionEndFlow.FinishEndMission(outpostName, applyCampaignFailurePenalty);
            if (result == CampaignMissionEndFlow.FinishResult.ShowDemotion)
            {
                CampaignMissionEndFlow.LoadDemotionScene();
            }
        }

        private void ExecuteRunwayDeckAction(RunwayDeckConfirmDialog.Action action)
        {
            pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.None;
            switch (action)
            {
                case RunwayDeckConfirmDialog.Action.Rearm:
                    ExecuteRunwayRearm();
                    break;
                case RunwayDeckConfirmDialog.Action.Refuel:
                    aircraft?.Refuel();
                    DeckLandingServiceState.MarkRefuelUsed();
                    break;
                case RunwayDeckConfirmDialog.Action.TakeOff:
                    LandMissionHandoffState.ClearRunwayDeckReturnIntent();
                    runwayDeckMenuVisible = false;
                    BeginTakeoff();
                    break;
            }
        }

        private void UpdateCarrierApproachPrompt()
        {
            if (landingComplete
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null)
            {
                return;
            }

            var inApproach = AircraftLanding.IsInCarrierApproachGrid(aircraft);
            if (!carrierApproachInitialized)
            {
                // Spawn/start on or near the CV must not open the prompt until a re-entry.
                carrierApproachInitialized = true;
                wasInCarrierApproachGrid = inApproach;
                return;
            }

            if (!inApproach)
            {
                wasInCarrierApproachGrid = false;
                carrierApproachDeclined = false;
                return;
            }

            var justEntered = !wasInCarrierApproachGrid;
            wasInCarrierApproachGrid = true;

            if (carrierApproachPromptVisible || carrierApproachDeclined || !justEntered)
            {
                return;
            }

            ShowCarrierApproachPrompt();
        }

        private void ShowCarrierApproachPrompt()
        {
            carrierApproachPromptVisible = true;
            activeInstance = this;
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            var autopilot = GetComponent<AutopilotController>();
            autopilot?.DisengageAutopilot("Carrier approach.");

            aircraft?.SetLandingLocked(true);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (input != null)
            {
                input.enabled = false;
            }
        }

        private void AcceptCarrierApproachLanding()
        {
            carrierApproachPromptVisible = false;
            BeginCarrierLanding();
        }

        private void DeclineCarrierApproachLanding()
        {
            carrierApproachDeclined = true;
            ClearCarrierApproachPrompt(unlockFlight: true);
        }

        private void ClearCarrierApproachPrompt(bool unlockFlight)
        {
            carrierApproachPromptVisible = false;
            if (!unlockFlight || sequenceActive || takeoffActive || landingComplete)
            {
                return;
            }

            aircraft?.SetLandingLocked(false);
            if (input != null)
            {
                input.enabled = true;
            }

            if (activeInstance == this && !sequenceActive && !takeoffActive)
            {
                activeInstance = null;
            }
        }

        private void UpdateLandingSequence()
        {
            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            currentVisualScale = Mathf.Lerp(1f, TargetVisualScale, progress);

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * currentVisualScale;
            }

            if (carrierLanding)
            {
                var position = Vector3.Lerp(carrierLandingStart, carrierLandingTarget, progress);
                position.y = transform.position.y;
                transform.position = position;
                if (body != null)
                {
                    body.position = position;
                }

                var toCarrier = carrierLandingTarget - carrierLandingStart;
                toCarrier.y = 0f;
                if (toCarrier.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCarrier.normalized, Vector3.up);
                    if (body != null)
                    {
                        body.rotation = transform.rotation;
                    }
                }
            }
            else if (outpostGroundLanding || friendlyBaseLanding)
            {
                // VTOL descent in place at outpost.
            }

            if (progress >= 1f)
            {
                CompleteLanding();
            }
        }

        private void FixedUpdate()
        {
            if (takeoffActive)
            {
                UpdateTakeoffMotion();
                return;
            }

            if (carrierApproachPromptVisible && body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                return;
            }

            if (!sequenceActive || aircraft == null || body == null)
            {
                return;
            }

            if (carrierLanding)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                return;
            }

            if (outpostGroundLanding || friendlyBaseLanding)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                return;
            }

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            body.linearVelocity = forward.normalized * aircraft.CurrentSpeed;
        }

        private void CompleteLanding()
        {
            sequenceActive = false;
            landingComplete = true;
            currentVisualScale = TargetVisualScale;

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            if (aircraft != null)
            {
                aircraft.SetLandingLocked(true);
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }

            if (carrierLanding)
            {
                var snapshot = CaptureSortieSnapshot(gameObject);
                LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
                DeckLandingServiceState.ResetForNewLanding();
                LandMissionCompleteState.BeginCarrierLanding();
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.MissionComplete);
                return;
            }

            if (friendlyBaseLanding)
            {
                CompleteFriendlyBaseDeck();
                return;
            }

            if (outpostGroundLanding)
            {
                CompleteOutpostGroundLanding();
                return;
            }

            if (crashLanding)
            {
                Time.timeScale = 1f;
                var outcome = CrashLandingResolver.RollOutcome();
                CrashLandingOutcomeState.Begin(outcome);
                SceneManager.LoadScene(GameScenes.CrashLandingOutcome);
                return;
            }

            OutpostRunwayDeckState.Clear();
            var openFieldSnapshot = CaptureSortieSnapshot(gameObject);
            openFieldSnapshot.IsOpenFieldLanding = true;
            openFieldSnapshot.ReturnToRunwayDeck = false;
            LandingMileFlagState.ApplyToSnapshot(ref openFieldSnapshot);
            LandMissionHandoffState.BeginEnterFromFlight(openFieldSnapshot);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }

        private void CompleteFriendlyBaseDeck()
        {
            var outpostName = outpostBaseSite != null ? outpostBaseSite.BaseName : string.Empty;
            OutpostRunwayDeckState.BeginParked(outpostName, friendly: true);
            DeckLandingServiceState.ResetForNewLanding();
            if (!string.IsNullOrWhiteSpace(outpostName))
            {
                LandBossMissionAssignment.PersistLaunchOutpost(CharacterSessionState.ActiveSave, outpostName);
            }

            activeInstance = this;
            Time.timeScale = 1f;
            runwayDeckMenuVisible = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void CompleteOutpostGroundLanding()
        {
            var outpostName = outpostBaseSite != null ? outpostBaseSite.BaseName : string.Empty;
            OutpostRunwayDeckState.Clear();
            var snapshot = CaptureSortieSnapshot(gameObject);
            snapshot.IsOpenFieldLanding = false;
            snapshot.ReturnToRunwayDeck = false;
            snapshot.OutpostName = outpostName;
            LandingMileFlagState.ApplyToSnapshot(ref snapshot);
            LandMissionHandoffState.BeginEnterFromFlight(snapshot);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }

        private void ExecuteRunwayRearm()
        {
            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            var snapshot = CaptureSortieSnapshot(gameObject);
            snapshot.ReturnToRunwayDeck = true;
            LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
            runwayDeckMenuVisible = false;
            CharacterGearSession.PersistActive();
            FriendlyOutpostTakeoffState.BeginDeckRearm(outpostName);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }

        /// <summary>Restores parked friendly-base deck after loadout rearm.</summary>
        public void RestoreRunwayDeckParked(LandSortieSnapshot snapshot)
        {
            activeInstance = this;
            sequenceActive = false;
            takeoffActive = false;
            landingComplete = true;
            friendlyBaseLanding = true;
            runwayDeckMenuVisible = true;
            pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.None;
            currentVisualScale = TargetVisualScale;
            hasGroundReturnPosition = false;

            if (visualPivot != null)
            {
                if (initialVisualScale.sqrMagnitude < 0.0001f)
                {
                    initialVisualScale = Vector3.one;
                }

                visualPivot.localScale = initialVisualScale * TargetVisualScale;
            }

            outpostBaseSite = ResolveOutpostBaseSite(snapshot.OutpostName);
            var outpostName = snapshot.OutpostName;
            OutpostRunwayDeckState.BeginParked(outpostName, friendly: true);

            if (OutpostRunwayDeckState.ConsumePendingDeckRefuelOnRestore())
            {
                pendingRunwayConfirm = RunwayDeckConfirmDialog.Action.Refuel;
            }

            aircraft?.SetLandingLocked(true);
            if (input != null)
            {
                input.enabled = false;
            }

            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private static AntarcticaBase ResolveOutpostBaseSite(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return null;
            }

            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var candidate = bases[i];
                if (candidate != null
                    && candidate.SiteKind == BaseSiteKind.Land
                    && string.Equals(candidate.BaseName, outpostName, System.StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static LandSortieSnapshot CaptureSortieSnapshot(GameObject player)
        {
            var snapshot = new LandSortieSnapshot
            {
                IsValid = true,
                AircraftWorldPosition = player != null ? player.transform.position : Vector3.zero,
                AircraftWorldRotation = player != null ? player.transform.rotation : Quaternion.identity,
                ReturnSceneName = GameScenes.FlightTest
            };

            if (player == null)
            {
                snapshot.FuelNormalized = 1f;
                return snapshot;
            }

            var aircraftController = player.GetComponent<AircraftController>();
            if (aircraftController != null)
            {
                snapshot.LeftTankGallons = aircraftController.LeftTankGallons;
                snapshot.RightTankGallons = aircraftController.RightTankGallons;
                snapshot.AfterburnerFuelRemaining = aircraftController.AfterburnerFuelRemaining;
                snapshot.FuelNormalized = aircraftController.TotalFuelCapacityGallons > 0f
                    ? aircraftController.TotalFuelGallons / aircraftController.TotalFuelCapacityGallons
                    : 1f;
                ResolveLandingOutpost(ref snapshot, aircraftController);
                snapshot.HasOutpostBunker = !string.IsNullOrWhiteSpace(snapshot.OutpostName)
                    && OutpostSurfaceAccess.HasBunkerGroundAccess(snapshot.OutpostName);
            }
            else
            {
                snapshot.FuelNormalized = 1f;
            }

            var weapons = player.GetComponent<F89.Weapons.PlayerWeaponController>();
            if (weapons != null && weapons.HasSortieInventory)
            {
                snapshot.HasStoresInventory = true;
                snapshot.Aim9zRemaining = weapons.Aim9zRemaining;
                snapshot.Agm88jRemaining = weapons.Agm88jRemaining;
                snapshot.Gbu12Remaining = weapons.Gbu12Remaining;
                snapshot.Agm114Remaining = weapons.Agm114Remaining;
                snapshot.GauRoundsRemaining = weapons.Gau27aGun != null
                    ? weapons.Gau27aGun.RoundsRemaining
                    : 0;
            }

            var flares = player.GetComponent<F89.Weapons.FlareCountermeasureController>();
            if (flares != null)
            {
                snapshot.FlaresRemaining = flares.FlaresRemaining;
            }

            LandingMileFlagState.SetFromWorldPosition(
                snapshot.AircraftWorldPosition,
                snapshot.AircraftWorldRotation);
            LandingMileFlagState.ApplyToSnapshot(ref snapshot);
            return snapshot;
        }

        private static void ResolveLandingOutpost(ref LandSortieSnapshot snapshot, AircraftController aircraftController)
        {
            snapshot.OutpostName = string.Empty;
            if (aircraftController?.WorldMap == null)
            {
                return;
            }

            var ticSize = aircraftController.Profile != null
                ? aircraftController.Profile.ticSizeWorldUnits
                : 1f;

            if (!string.IsNullOrWhiteSpace(OutpostRunwayDeckState.ParkedOutpostName))
            {
                snapshot.OutpostName = OutpostRunwayDeckState.ParkedOutpostName;
                return;
            }

            if (activeInstance?.outpostBaseSite != null)
            {
                snapshot.OutpostName = activeInstance.outpostBaseSite.BaseName;
                return;
            }

            AntarcticaOutpostLandingResolver.TryResolveOutpost(
                snapshot.AircraftWorldPosition,
                aircraftController.WorldMap,
                ticSize,
                out snapshot.OutpostName);
        }

        private void UpdateTakeoffVisual()
        {
            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            currentVisualScale = Mathf.Lerp(TargetVisualScale, 1f, progress);

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale * currentVisualScale;
            }

            if (progress >= 1f)
            {
                CompleteTakeoff();
            }
        }

        private void UpdateTakeoffMotion()
        {
            if (aircraft == null || body == null)
            {
                return;
            }

            var progress = Mathf.Clamp01((Time.time - sequenceStartTime) / ShrinkDurationSeconds);
            var targetSpeedMph = Mathf.Lerp(0f, FlightMissionLaunchState.VtolTakeoffSpeedMph, progress);
            aircraft.ApplyTakeoffSpeed(targetSpeedMph);
        }

        private void CompleteTakeoff()
        {
            takeoffActive = false;
            landingComplete = false;
            friendlyBaseLanding = false;
            outpostGroundLanding = false;
            runwayDeckMenuVisible = false;
            carrierApproachPromptVisible = false;
            carrierApproachDeclined = false;
            currentVisualScale = 1f;

            if (visualPivot != null)
            {
                visualPivot.localScale = initialVisualScale;
            }

            if (aircraft != null)
            {
                aircraft.SetLandingLocked(false);
                aircraft.ApplyTakeoffSpeed(FlightMissionLaunchState.VtolTakeoffSpeedMph);
            }

            FlightAudio.SetInFlight(true);
            CombatThreatRange.InvalidateCaches();

            if (input != null)
            {
                input.enabled = true;
            }

            activeInstance = null;
            var parkedOutpost = OutpostRunwayDeckState.ParkedOutpostName;
            if (!string.IsNullOrWhiteSpace(parkedOutpost))
            {
                OutpostFlightPlatoonState.SyncOutpostFromScene(parkedOutpost);
            }

            LandMissionHandoffState.ConfirmReturnApplied();
            OutpostRunwayDeckState.Clear();
            OpenFieldLandingState.Clear();
            LandingMileFlagState.ClearAfterTakeoff();
        }
    }
}
