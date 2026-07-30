using F89.Core;
using F89.LandCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Shared leave-ground path used by GroundAttack and Bunker.
    /// Downed outcomes open Death / POW / Escaped pages; voluntary exits return without R&amp;D rolls.
    /// R&amp;D breakthrough rolls run only after a successful carrier END MISSION.
    /// </summary>
    public static class LandGroundMissionExit
    {
        public static void Leave()
        {
            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            var downed = health != null && health.IsUnconscious
                ? health.DownedOutcome
                : LandDownedOutcome.None;
            if (downed != LandDownedOutcome.None)
            {
                LandBossEncounter.ResetActiveBossHealthAfterDeath();
            }
            else if (SceneManager.GetActiveScene().name == GameScenes.Bunker)
            {
                LandBossEncounter.CaptureActiveBossHealthFromBunker();
            }

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

            LandMissionHandoffState.ForceReloadFromPrefs();
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

            if (save != null
                && (downed != LandDownedOutcome.None || continueScene != GameScenes.FlightTest))
            {
                MissionScoreState.FinalizeToSave(save);
            }

            Time.timeScale = 1f;

            if (save != null)
            {
                CharacterGearSession.PersistActive();
            }

            if (downed != LandDownedOutcome.None)
            {
                LandCombatModule.ShutdownWithoutHandoff();
                LandMissionHealthState.Clear();
                LandDownedOutcomeState.Begin(downed);
                SceneManager.LoadScene(GameScenes.DownedOutcome);
                return;
            }

            // Taking off from a ground landing immediately returns to active flight.
            // Mission completion is evaluated only when the aircraft later lands on the CV.
            if (continueScene == GameScenes.FlightTest)
            {
                SceneManager.LoadScene(continueScene);
                return;
            }

            SceneManager.LoadScene(continueScene);
        }
    }
}
