using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Carrier deck choice after landing on the CV, or bunker-clear completion before returning to surface.
    /// </summary>
    public sealed class MissionCompleteController : MonoBehaviour
    {
        private enum CarrierPhase
        {
            DeckChoice = 0,
            FailureConfirm = 1,
            Demotion = 2
        }

        private CarrierPhase carrierPhase = CarrierPhase.DeckChoice;
        private bool isLeaving;
        private RunwayDeckConfirmDialog.Action pendingDeckConfirm;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 1f;

            if (DemotionState.HasPending)
            {
                carrierPhase = CarrierPhase.Demotion;
            }
        }

        private void OnGUI()
        {
            GUI.color = new Color(0.03f, 0.05f, 0.07f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // After a leave choice, keep a blank deck backdrop until the next scene loads.
            // Clearing IsCarrierLanding before LoadScene would otherwise flash "MISSION COMPLETE".
            if (isLeaving)
            {
                return;
            }

            if (LandMissionCompleteState.IsCarrierLanding || DemotionState.HasPending)
            {
                DrawCarrierFlow();
                return;
            }

            DrawBunkerComplete();
        }

        private void DrawCarrierFlow()
        {
            switch (carrierPhase)
            {
                case CarrierPhase.FailureConfirm:
                    DrawFailureConfirm();
                    break;
                case CarrierPhase.Demotion:
                    DrawDemotion();
                    break;
                default:
                    DrawDeckChoice();
                    break;
            }
        }

        private void DrawDeckChoice()
        {
            var title = HudStyleFactory.CreateLabel(36, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            GUI.Label(new Rect(0f, Screen.height * 0.22f, Screen.width, 54f), "ON DECK", title);

            if (pendingDeckConfirm != RunwayDeckConfirmDialog.Action.None)
            {
                var confirmResult = RunwayDeckConfirmDialog.Draw(pendingDeckConfirm);
                if (confirmResult == RunwayDeckConfirmDialog.Result.Confirmed)
                {
                    ExecuteDeckAction(pendingDeckConfirm);
                }
                else if (confirmResult == RunwayDeckConfirmDialog.Result.Cancelled)
                {
                    pendingDeckConfirm = RunwayDeckConfirmDialog.Action.None;
                }

                return;
            }

            var deckOptions = RunwayDeckMenuOptions.BuildCarrierDeckOptions();
            deckOptions.DialogVerticalAnchor = 0.58f;
            var deckResult = RunwayDeckMenuDialog.Draw(true, deckOptions);
            if (deckResult == RunwayDeckMenuDialog.Result.Rearm)
            {
                pendingDeckConfirm = RunwayDeckConfirmDialog.Action.Rearm;
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.Refuel)
            {
                pendingDeckConfirm = RunwayDeckConfirmDialog.Action.Refuel;
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.TakeOff)
            {
                // No second "Take Off?" confirm — TAKE OFF already is the choice.
                BeginCarrierTakeOff();
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.EndMission)
            {
                RequestEndMission();
            }
        }

        private void ExecuteDeckAction(RunwayDeckConfirmDialog.Action action)
        {
            pendingDeckConfirm = RunwayDeckConfirmDialog.Action.None;
            switch (action)
            {
                case RunwayDeckConfirmDialog.Action.Rearm:
                    BeginDeckRearm();
                    break;
                case RunwayDeckConfirmDialog.Action.Refuel:
                    ApplyCarrierRefuel();
                    break;
            }
        }

        private void ApplyCarrierRefuel()
        {
            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (!snapshot.IsValid)
            {
                snapshot = LandSortieSnapshot.Empty;
            }

            SortieSnapshotFuel.ApplyMaxFuel(ref snapshot);
            LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
            DeckLandingServiceState.MarkRefuelUsed();
        }

        private void BeginDeckRearm()
        {
            isLeaving = true;
            CharacterGearSession.PersistActive();
            CarrierResupplyState.BeginDeckRearm();
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }

        private void BeginCarrierTakeOff()
        {
            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (!SortieSnapshotFuel.HasUsableFuel(snapshot))
            {
                return;
            }

            isLeaving = true;

            // Carrier launch path — do not use ground-return landing miles (that re-applied empty
            // fuel and could surface another deck/takeoff prompt after scene load).
            snapshot.ReturnToRunwayDeck = false;
            snapshot.RestoreWithImmediateTakeoff = false;
            snapshot.IsOpenFieldLanding = false;
            snapshot.HasLandingMiles = false;
            snapshot.OutpostName = string.Empty;
            LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);

            LandingMileFlagState.Clear();
            LandMissionCompleteState.Clear();
            DeckLandingServiceState.Clear();
            CarrierDeckTakeoffState.Begin();
            FlightMissionLaunchState.BeginCarrierLaunch();
            FlightMissionStartBootstrap.ResetForSceneLoad();
            SceneManager.LoadScene(GameScenes.FlightTest);
        }

        private void DrawFailureConfirm()
        {
            var result = EndMissionFailureConfirmDialog.Draw(true);
            if (result == EndMissionFailureConfirmDialog.Result.Confirmed)
            {
                FinishEndMission(applyCampaignFailurePenalty: true);
            }
            else if (result == EndMissionFailureConfirmDialog.Result.Cancelled)
            {
                carrierPhase = CarrierPhase.DeckChoice;
            }
        }

        private void DrawDemotion()
        {
            var title = HudStyleFactory.CreateLabel(42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var detail = HudStyleFactory.CreateLabel(
                20,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.78f, 0.52f),
                wordWrap: true);
            GUI.Label(new Rect(0f, Screen.height * 0.30f, Screen.width, 60f), "DEMOTION", title);
            GUI.Label(
                new Rect(Screen.width * 0.15f, Screen.height * 0.42f, Screen.width * 0.7f, 80f),
                $"{DemotionState.PreviousRank}  →  {DemotionState.NewRank}",
                detail);

            var buttonWidth = UiFitCanvas.Px(280f);
            var buttonHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - UiFitCanvas.Px(40f),
                buttonWidth,
                buttonHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 16))
            {
                DemotionState.Clear();
                isLeaving = true;
                PostMissionContinuation.ContinueAfterDemotion();
            }
        }

        private void DrawBunkerComplete()
        {
            var title = HudStyleFactory.CreateLabel(42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var detail = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.78f, 0.52f));
            GUI.Label(new Rect(0f, Screen.height * 0.33f, Screen.width, 60f), "MISSION COMPLETE", title);
            GUI.Label(
                new Rect(0f, Screen.height * 0.43f, Screen.width, 34f),
                $"{LandMissionCompleteState.CompletedBunkerCode} CLEARED",
                detail);

            var buttonWidth = UiFitCanvas.Px(280f);
            var buttonHeight = UiFitCanvas.Px(52f);
            var continueRect = new Rect(
                UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f,
                UiFitCanvas.Rect.yMax - buttonHeight - UiFitCanvas.Px(40f),
                buttonWidth,
                buttonHeight);
            if (StartPageMenuStyles.DrawMenuButton(continueRect, "CONTINUE", fontSize: 16))
            {
                isLeaving = true;
                CharacterGearSession.PersistActive();
                LandSurfaceSession.BeginReturnAtBunker();
                LandMissionCompleteState.Clear();
                SceneManager.LoadScene(GameScenes.GroundAttack);
            }
        }

        private void RequestEndMission()
        {
            if (CampaignMissionEndFlow.RequiresFailureConfirm)
            {
                carrierPhase = CarrierPhase.FailureConfirm;
                return;
            }

            FinishEndMission(applyCampaignFailurePenalty: false);
        }

        private void FinishEndMission(bool applyCampaignFailurePenalty)
        {
            var result = CampaignMissionEndFlow.FinishEndMission(null, applyCampaignFailurePenalty);
            if (result == CampaignMissionEndFlow.FinishResult.ShowDemotion)
            {
                carrierPhase = CarrierPhase.Demotion;
                return;
            }

            isLeaving = true;
            DemotionState.Clear();
        }
    }
}
