using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Testing
{
    public static class SceneBootstrap
    {
        private static bool isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            BootstrapActiveScene();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BootstrapActiveScene();
        }

        private static void BootstrapActiveScene()
        {
            F89.UI.GamePauseController.ClearPauseOnSceneLoad();
            Time.timeScale = 1f;
            GameSettings.Load();
            GameKeyBindings.Load();
            EnsurePauseController();
            RemoveLegacyMainMenuRoots();
            GameMusic.EnsurePlaying();

            var sceneName = SceneManager.GetActiveScene().name;
            if (!GameScenes.IsGameplayScene(sceneName))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            if (sceneName == GameScenes.LoadingScreen)
            {
                LoadingScreenRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.MainMenu || sceneName == GameScenes.StartPage)
            {
                StartPageRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.SelectionPage)
            {
                SelectionPageRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.CharacterPage)
            {
                CharacterPageRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.MissionBriefing)
            {
                MissionBriefingRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.CharacterLoadout)
            {
                CharacterLoadoutRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.AircraftLoadout)
            {
                AircraftLoadoutRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.MenuSubpage)
            {
                MenuSubpageRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (GameScenes.IsResearchResultsScene(sceneName))
            {
                ResearchResultsRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (GameScenes.IsDownedOutcomeScene(sceneName))
            {
                DownedOutcomeRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (GameScenes.IsCrashLandingOutcomeScene(sceneName))
            {
                CrashLandingOutcomeRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (GameScenes.IsMissionStatusReportScene(sceneName))
            {
                MissionIncompleteRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.GroundAttack)
            {
                GroundAttackRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.Bunker)
            {
                BunkerRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.MARefuel)
            {
                MidAirRefuelRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.BunkerPlayScene)
            {
                return;
            }

            if (sceneName == GameScenes.MissionComplete)
            {
                MissionCompleteRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (GameScenes.GetBossNumber(sceneName) != 0)
            {
                Boss1RuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.FlightTest || ShouldBootstrapGameplayInEditorScene(sceneName))
            {
                FlightTestRuntimeBuilder.BuildIfNeeded();
            }
        }

        private static void RemoveLegacyMainMenuRoots()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "MainMenu")
                {
                    Object.Destroy(roots[i]);
                }
            }
        }

        private static bool ShouldBootstrapGameplayInEditorScene(string sceneName)
        {
            return string.IsNullOrEmpty(sceneName)
                || sceneName == "Untitled"
                || sceneName.StartsWith("Temp");
        }

        private static void EnsurePauseController()
        {
            F89.UI.GamePauseController.EnsureExists();
        }
    }
}
