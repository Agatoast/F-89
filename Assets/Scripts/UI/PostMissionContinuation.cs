using F89.Core;
using F89.Flight;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Routes from crash landing pages through mission status and demotion, then Character page.
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
            if (save != null)
            {
                CharacterGearSession.PersistActive();
                CharacterSaveRepository.SyncVehicleKillCredit(save);
                MissionScoreState.AbandonMissionWithoutScoring();
            }

            if (outcome == CrashLandingOutcome.NotRescued && save != null)
            {
                CharacterSaveRepository.MarkKilledInAction(save);
                Time.timeScale = 1f;
                SceneManager.LoadScene(GameScenes.SelectionPage);
                return;
            }

            var primaryIncomplete = GamePlayModeState.IsCampaign                && save != null
                && !LandBossMissionAssignment.IsPrimaryMissionComplete(save);

            if (primaryIncomplete && save != null)
            {
                LandBossMissionAssignment.ResolveAssignedMissionWithoutVictory(save);
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
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        public static void ContinueToResearchOrCharacter(CharacterSaveData save)
        {
            var discoveries = LandResearchBreakthroughService.RollMissionBreakthroughs(save);
            LandResearchResultsState.BeginMissionEndReports(discoveries, GameScenes.CharacterPage);
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.ResearchResults);
        }
    }
}
