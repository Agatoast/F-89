using F89.Core;
using F89.Flight;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Routes from crash landing pages through mission status and demotion, then Character page.
    /// Survived crashes credit kills, mission score, and completed objectives before routing.
    /// R&amp;D breakthrough rolls run only after a successful carrier END MISSION.
    /// KIA returns to the character select (save) page.
    /// </summary>
    public static class PostMissionContinuation
    {
        public static void ContinueAfterCrashLanding()
        {
            var outcome = CrashLandingOutcomeState.Outcome;
            CrashLandingOutcomeState.Clear();
            PlayerAircraftCrashController.ClearActiveState();

            var save = CharacterSessionState.ActiveSave;
            CampaignMissionEndFlow.SurvivedCrashCreditResult crashCredits = default;
            if (save != null)
            {
                CharacterGearSession.PersistActive();
                if (outcome == CrashLandingOutcome.NotRescued)
                {
                    CharacterSaveRepository.SyncVehicleKillCredit(save);
                    MissionScoreState.AbandonMissionWithoutScoring();
                }
                else
                {
                    crashCredits = CampaignMissionEndFlow.ApplySurvivedCrashCredits(save);
                }

                MissionLaunchOrigin.PreserveAfterCrash(save);
            }

            if (outcome == CrashLandingOutcome.NotRescued && save != null)
            {
                CharacterSaveRepository.MarkKilledInAction(save);
                ContinueToCharacterSelect();
                return;
            }

            if (crashCredits.RequiresCourtMartial)
            {
                ContinueToCharacterSelect();
                return;
            }

            if (save != null && save.IsKilledInAction)
            {
                ContinueToCharacterSelect();
                return;
            }

            if (TryContinueSurvivedCrashDebrief(save, crashCredits))
            {
                return;
            }

            // Use credit result — ApplySurvivedCrashCredits may have already advanced the campaign
            // mission, so re-querying IsPrimaryMissionComplete would check the next mission.
            var primaryIncomplete = GamePlayModeState.IsCampaign
                && save != null
                && !crashCredits.PrimaryMissionComplete;

            if (primaryIncomplete && save != null)
            {
                // Crash/incomplete keeps catalog mission number for retry — do not skip via boss track.
                if (PilotCareerRanks.TryDemoteToScoreFloor(save, out var previousRank, out var newRank))
                {
                    CharacterSaveRepository.WriteBossProgress(save);
                    DemotionState.Begin(previousRank, newRank);
                }
            }

            LandMissionHealthState.Clear();
            LandMissionCompleteState.Clear();

            if (DemotionState.HasPending)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.MissionComplete);
                return;
            }

            if (primaryIncomplete)
            {
                MissionIncompleteState.Begin();
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.MissionIncomplete);
                return;
            }

            ContinueToCharacterPage();
        }

        private static bool TryContinueSurvivedCrashDebrief(
            CharacterSaveData save,
            CampaignMissionEndFlow.SurvivedCrashCreditResult crashCredits)
        {
            if (save == null || !crashCredits.PrimaryMissionComplete)
            {
                return false;
            }

            if (!MissionEndReportState.HasPendingReport
                && !RankPromotionState.HasPendingPromotion
                && !MedalAwardState.HasPendingAward)
            {
                return false;
            }

            LandResearchResultsState.BeginScoreAndMedalsOnly(GameScenes.CharacterPage);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.ResearchResults);
            return true;
        }

        public static void ContinueAfterMissionIncomplete()
        {
            MissionIncompleteState.Clear();
            ContinueToCharacterPage();
        }

        public static void ContinueAfterDemotion()
        {
            LandMissionCompleteState.Clear();
            ContinueToCharacterPage();
        }

        public static void ContinueToCharacterPage()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save != null && save.IsKilledInAction)
            {
                ContinueToCharacterSelect();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        public static void ContinueToCharacterSelect()
        {
            CharacterSessionState.ActiveSave = null;
            CharacterGearSession.Bind(null);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.SelectionPage);
        }

        public static void ContinueToResearchOrCharacter(CharacterSaveData save)
        {
            GamePauseController.ClearPauseOnSceneLoad();
            var discoveries = LandResearchBreakthroughService.RollMissionBreakthroughs(save);
            LandResearchResultsState.BeginMissionEndReports(discoveries, GameScenes.CharacterPage);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(GameScenes.ResearchResults);
        }
    }
}
