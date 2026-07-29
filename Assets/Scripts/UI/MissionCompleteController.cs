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
            var detail = HudStyleFactory.CreateLabel(
                18,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                new Color(0.82f, 0.78f, 0.52f));
            GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 54f), "ON DECK", title);
            GUI.Label(
                new Rect(0f, Screen.height * 0.36f, Screen.width, 34f),
                "USS MARTIN VAN BUREN",
                detail);

            var buttonWidth = UiFitCanvas.Px(320f);
            var buttonHeight = UiFitCanvas.Px(52f);
            var gap = UiFitCanvas.Px(18f);
            var centerX = UiFitCanvas.Rect.x + (UiFitCanvas.Rect.width - buttonWidth) * 0.5f;
            var startY = UiFitCanvas.Rect.y + UiFitCanvas.Rect.height * 0.52f;

            var refuelRect = new Rect(centerX, startY, buttonWidth, buttonHeight);
            var endRect = new Rect(centerX, startY + buttonHeight + gap, buttonWidth, buttonHeight);

            if (StartPageMenuStyles.DrawMenuButton(refuelRect, "REFUEL AND REARM", fontSize: 16))
            {
                BeginRefuelAndRearm();
            }

            if (StartPageMenuStyles.DrawMenuButton(endRect, "END MISSION", fontSize: 16))
            {
                RequestEndMission();
            }
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

        private void BeginRefuelAndRearm()
        {
            isLeaving = true;
            CharacterGearSession.PersistActive();
            LandMissionHealthState.Clear();
            CarrierResupplyState.Begin();
            CharacterLoadoutNavState.MarkEnteredFromMissionBrief();
            LandMissionCompleteState.Clear();
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }

        private void RequestEndMission()
        {
            var save = CharacterSessionState.ActiveSave;
            var primaryIncomplete = GamePlayModeState.IsCampaign
                && !LandBossMissionAssignment.IsPrimaryMissionComplete(save);
            if (primaryIncomplete)
            {
                carrierPhase = CarrierPhase.FailureConfirm;
                return;
            }

            FinishEndMission(applyCampaignFailurePenalty: false);
        }

        private void FinishEndMission(bool applyCampaignFailurePenalty)
        {
            var save = CharacterSessionState.ActiveSave;
            CharacterGearSession.PersistActive();
            CharacterSaveRepository.SyncVehicleKillCredit(save);

            if (applyCampaignFailurePenalty && GamePlayModeState.IsCampaign && save != null)
            {
                CharacterSaveRepository.ApplyTotalScoreFractionPenalty(save, 0.5f);
                LandBossMissionAssignment.ResolveAssignedMissionWithoutVictory(save);
                if (PilotCareerRanks.TryDemoteToScoreFloor(save, out var previousRank, out var newRank))
                {
                    CharacterSaveRepository.WriteBossProgress(save);
                    DemotionState.Begin(previousRank, newRank);
                    LandMissionHealthState.Clear();
                    LandMissionCompleteState.Clear();
                    carrierPhase = CarrierPhase.Demotion;
                    return;
                }
            }
            else if (GamePlayModeState.IsCampaign
                     && save != null
                     && LandBossMissionAssignment.HasActiveAssignment(save)
                     && LandBossMissionAssignment.IsPrimaryMissionComplete(save))
            {
                // Victory already marked the boss; advance assignment for the next briefing.
                LandBossMissionAssignment.PrepareNextAssignment(save);
            }

            GoToCharacterPage();
        }

        private void GoToCharacterPage()
        {
            isLeaving = true;
            LandMissionHealthState.Clear();
            LandMissionCompleteState.Clear();
            CarrierResupplyState.Clear();
            DemotionState.Clear();
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }
    }
}
