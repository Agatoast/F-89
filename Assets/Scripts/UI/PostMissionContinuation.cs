using F89.Core;
using F89.Flight;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Routes from crash landing pages through mission status, demotion, R&amp;D, then Character page.
    /// </summary>
    public static class PostMissionContinuation
    {
        public static void ContinueAfterCrashLanding()
        {
            CrashLandingOutcomeState.Clear();
            PlayerAircraftCrashController.ClearActiveState();

            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterGearSession.PersistActive();
                CharacterSaveRepository.SyncVehicleKillCredit(save);
            }

            var primaryIncomplete = GamePlayModeState.IsCampaign
                && save != null
                && !LandBossMissionAssignment.IsPrimaryMissionComplete(save);

            if (primaryIncomplete && save != null)
            {
                CharacterSaveRepository.ApplyTotalScoreFractionPenalty(save, 0.5f);
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

            ContinueToResearchOrCharacter(save);
        }

        public static void ContinueAfterMissionIncomplete()
        {
            MissionIncompleteState.Clear();
            ContinueToResearchOrCharacter(CharacterSessionState.ActiveSave);
        }

        public static void ContinueAfterDemotion()
        {
            LandMissionCompleteState.Clear();
            ContinueToResearchOrCharacter(CharacterSessionState.ActiveSave);
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
