using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Shared leave-ground path used by GroundAttack and Bunker.
    /// Downed outcomes open Death / POW / Escaped pages; voluntary exits go through R&amp;D reports.
    /// </summary>
    public static class LandGroundMissionExit
    {
        public static void Leave()
        {
            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            var downed = health != null && health.IsUnconscious
                ? health.DownedOutcome
                : LandDownedOutcome.None;

            var result = new LandGroundSessionResult
            {
                CompletedVoluntarily = downed == LandDownedOutcome.None,
                TroopsKilled = LandGroundSceneController.SessionKills,
                ScoreEarned = LandGroundSceneController.SessionScore,
                DownedOutcome = downed
            };

            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.RecordGroundSession(save, result);
            }

            Time.timeScale = 1f;

            if (save != null)
            {
                CharacterGearSession.PersistActive();
            }

            if (downed != LandDownedOutcome.None)
            {
                LandCombatModule.ShutdownWithoutHandoff();
                LandDownedOutcomeState.Begin(downed);
                SceneManager.LoadScene(GameScenes.DownedOutcome);
                return;
            }

            var stored = LandMissionHandoffState.GetStoredFlightSnapshot();
            var returningToMenu = stored.ReturnSceneName == GameScenes.StartPage
                || stored.ReturnSceneName == GameScenes.MainMenu
                || (string.IsNullOrEmpty(stored.ReturnSceneName) && !stored.IsValid);

            string continueScene;
            if (returningToMenu || !stored.IsValid)
            {
                LandCombatModule.ShutdownWithoutHandoff();
                continueScene = GameScenes.StartPage;
            }
            else
            {
                LandCombatModule.ExitToFlight(result);
                continueScene = LandMissionHandoffState.PendingReturnSceneName;
            }

            var discoveries = LandResearchBreakthroughService.RollMissionBreakthroughs(save);
            LandResearchResultsState.BeginMissionEndReports(discoveries, continueScene);
            SceneManager.LoadScene(GameScenes.ResearchResults);
        }
    }
}
