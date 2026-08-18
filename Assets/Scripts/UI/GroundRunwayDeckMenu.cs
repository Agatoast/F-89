using System;
using F89.Core;
using F89.Flight;
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
        private static bool endMissionLeaving;

        public static bool IsOpen => LandLandedPlane.IsTakeOffPromptPending;

        public static void Draw()
        {
            if (endMissionLeaving)
            {
                GUI.color = new Color(0.03f, 0.05f, 0.07f, 1f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                return;
            }

            if (!LandLandedPlane.IsTakeOffPromptPending)
            {
                return;
            }

            if (endMissionFailureConfirmVisible)
            {
                var failureResult = EndMissionFailureConfirmDialog.Draw(true);
                if (failureResult == EndMissionFailureConfirmDialog.Result.Confirmed)
                {
                    QueueEndMission(applyCampaignFailurePenalty: true);
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
            endMissionLeaving = false;
            LandLandedPlane.CancelTakeOffPrompt();
        }

        private static void RequestEndMission()
        {
            CampaignWaypointPlatoonState.TrySyncActiveMissionBeforeEnd(CharacterSessionState.ActiveSave);

            if (CampaignMissionEndFlow.RequiresFailureConfirm)
            {
                endMissionFailureConfirmVisible = true;
                return;
            }

            QueueEndMission(applyCampaignFailurePenalty: false);
        }

        private static void QueueEndMission(bool applyCampaignFailurePenalty)
        {
            endMissionFailureConfirmVisible = false;
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;
            endMissionLeaving = true;
            LandLandedPlane.CancelTakeOffPrompt();
            GamePauseController.ClearPauseOnSceneLoad();
            Time.timeScale = 1f;
            AudioListener.pause = false;

            var outpostName = ResolveEndMissionOutpostName();
            GamePauseController.ScheduleAfterGui(() =>
                CompleteEndMission(outpostName, applyCampaignFailurePenalty));
        }

        private static string ResolveEndMissionOutpostName()
        {
            var outpostName = OutpostRunwayDeckState.ParkedOutpostName;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                outpostName = LandOutpostLandingState.ActiveOutpostName;
            }

            if (string.IsNullOrWhiteSpace(outpostName))
            {
                outpostName = CharacterSessionState.ActiveSave?.MissionLaunchOutpostName;
            }

            return outpostName;
        }

        private static void CompleteEndMission(string outpostName, bool applyCampaignFailurePenalty)
        {
            endMissionFailureConfirmVisible = false;
            pendingConfirm = RunwayDeckConfirmDialog.Action.None;

            try
            {
                var result = CampaignMissionEndFlow.FinishEndMission(outpostName, applyCampaignFailurePenalty);
                if (result == CampaignMissionEndFlow.FinishResult.ShowDemotion)
                {
                    CampaignMissionEndFlow.LoadDemotionScene();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[F-89] Ground runway END MISSION failed: " + ex);
                GamePauseController.ClearPauseOnSceneLoad();
                Time.timeScale = 1f;
                AudioListener.pause = false;
                SceneManager.LoadScene(GameScenes.CharacterPage);
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
                if (WaypointLandingState.IsActive)
                {
                    snapshot.WaypointSiteCode = WaypointLandingState.SiteCode;
                    snapshot.OutpostName = string.Empty;
                    FlightGroundReturnService.EnsureWaypointLandingFlag(snapshot);
                }
                else if (OpenFieldLandingState.IsActive && OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
                {
                    snapshot.HasLandingMiles = true;
                    snapshot.LandingMileX = OpenFieldLandingState.LandingMiles.x;
                    snapshot.LandingMileY = OpenFieldLandingState.LandingMiles.y;
                    LandingMileFlagState.SetFromMiles(
                        OpenFieldLandingState.LandingMiles,
                        snapshot.LandingRotationY);
                }

                LandingMileFlagState.TryApplyToSnapshot(ref snapshot);
                if (!snapshot.HasLandingMiles)
                {
                    LandingMileFlagState.ApplyToSnapshot(ref snapshot);
                }

                LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
            }

            FlightMissionLaunchState.ClearStaleCarrierWhenSortieReturnPending();
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

            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (!snapshot.IsValid)
            {
                snapshot = new LandSortieSnapshot
                {
                    IsValid = true,
                    ReturnSceneName = GameScenes.FlightTest
                };
            }

            snapshot.ReturnToRunwayDeck = true;
            snapshot.RestoreWithImmediateTakeoff = false;
            if (!string.IsNullOrWhiteSpace(outpostName))
            {
                snapshot.OutpostName = outpostName;
            }

            LandMissionHandoffState.UpdateStoredFlightSnapshot(snapshot);
            CharacterGearSession.PersistActive();
            CarrierResupplyState.Clear();
            FlightMissionLaunchState.Clear();
            FriendlyOutpostTakeoffState.BeginDeckRearm(outpostName);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }
    }
}
