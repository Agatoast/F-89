using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>Runway deck menu while dismounted on the ground map (returns to parked flight deck).</summary>
    public static class GroundRunwayDeckMenu
    {
        private static RunwayDeckConfirmDialog.Action pendingConfirm;
        private static bool endMissionFailureConfirmVisible;

        public static bool IsOpen => LandLandedPlane.IsTakeOffPromptPending;

        public static void Draw()
        {
            if (!LandLandedPlane.IsTakeOffPromptPending)
            {
                return;
            }

            if (endMissionFailureConfirmVisible)
            {
                var failureResult = EndMissionFailureConfirmDialog.Draw(true);
                if (failureResult == EndMissionFailureConfirmDialog.Result.Confirmed)
                {
                    ExecuteEndMission(applyCampaignFailurePenalty: true);
                }
                else if (failureResult == EndMissionFailureConfirmDialog.Result.Cancelled)
                {
                    endMissionFailureConfirmVisible = false;
                }

                return;
            }

            if (pendingConfirm != RunwayDeckConfirmDialog.Action.None)
            {
                var confirmResult = RunwayDeckConfirmDialog.Draw(pendingConfirm);
                if (confirmResult == RunwayDeckConfirmDialog.Result.Confirmed)
                {
                    ExecuteConfirmedAction(pendingConfirm);
                }
                else if (confirmResult == RunwayDeckConfirmDialog.Result.Cancelled)
                {
                    pendingConfirm = RunwayDeckConfirmDialog.Action.None;
                }

                return;
            }

            var deckResult = RunwayDeckMenuDialog.Draw(true, RunwayDeckMenuOptions.BuildGroundPlaneOptions());
            if (deckResult == RunwayDeckMenuDialog.Result.Rearm)
            {
                pendingConfirm = RunwayDeckConfirmDialog.Action.Rearm;
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.Refuel)
            {
                pendingConfirm = RunwayDeckConfirmDialog.Action.Refuel;
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.Dismount)
            {
                Cancel();
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.TakeOff)
            {
                ExecuteTakeOff();
            }
            else if (deckResult == RunwayDeckMenuDialog.Result.EndMission)
            {
                RequestEndMission();
            }
        }

        public static void Cancel()
        {
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;
            endMissionFailureConfirmVisible = false;
            LandLandedPlane.CancelTakeOffPrompt();
        }

        private static void RequestEndMission()
        {
            if (CampaignMissionEndFlow.RequiresFailureConfirm)
            {
                endMissionFailureConfirmVisible = true;
                return;
            }

            ExecuteEndMission(applyCampaignFailurePenalty: false);
        }

        private static void ExecuteEndMission(bool applyCampaignFailurePenalty)
        {
            endMissionFailureConfirmVisible = false;
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;
            LandLandedPlane.CancelTakeOffPrompt();

            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                outpostName = LandOutpostLandingState.ActiveOutpostName;
            }

            var result = CampaignMissionEndFlow.FinishEndMission(outpostName, applyCampaignFailurePenalty);
            if (result == CampaignMissionEndFlow.FinishResult.ShowDemotion)
            {
                CampaignMissionEndFlow.LoadDemotionScene();
            }
        }

        private static void ExecuteTakeOff()
        {
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;
            endMissionFailureConfirmVisible = false;
            if (LandOutpostLandingState.HasActiveOutpost)
            {
                Time.timeScale = 1f;
            }

            LandLandedPlane.CancelTakeOffPrompt();

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (snapshot.IsValid)
            {
                snapshot.ReturnToRunwayDeck = false;
                snapshot.RestoreWithImmediateTakeoff = true;
                snapshot.IsOpenFieldLanding = OpenFieldLandingState.IsActive;
                if (OpenFieldLandingState.IsActive && OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
                {
                    snapshot.HasLandingMiles = true;
                    snapshot.LandingMileX = OpenFieldLandingState.LandingMiles.x;
                    snapshot.LandingMileY = OpenFieldLandingState.LandingMiles.y;
                    LandingMileFlagState.SetFromMiles(
                        OpenFieldLandingState.LandingMiles,
                        snapshot.LandingRotationY);
                }
                else
                {
                    LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
                }

                LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
            }

            OutpostRunwayDeckState.RequestDeckTakeoffOnRestore();
            LandGroundMissionExit.Leave();
        }

        private static void ExecuteConfirmedAction(RunwayDeckConfirmDialog.Action action)
        {
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;
            LandLandedPlane.CancelTakeOffPrompt();

            switch (action)
            {
                case RunwayDeckConfirmDialog.Action.Rearm:
                    BeginGroundRearm();
                    break;
                case RunwayDeckConfirmDialog.Action.Refuel:
                    OutpostRunwayDeckState.RequestDeckRefuelOnRestore();
                    LandGroundMissionExit.Leave();
                    break;
                case RunwayDeckConfirmDialog.Action.Dismount:
                    break;
            }
        }

        private static void BeginGroundRearm()
        {
            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                outpostName = LandOutpostLandingState.ActiveOutpostName;
            }

            CharacterGearSession.PersistActive();
            FriendlyOutpostTakeoffState.BeginDeckRearm(outpostName);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }
    }
}
