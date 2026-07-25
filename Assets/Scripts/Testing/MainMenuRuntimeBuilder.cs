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
            Time.timeScale = 1f;
            GameSettings.Load();
            GameKeyBindings.Load();
            EnsurePauseController();
            RemoveLegacyMainMenuRoots();

            var sceneName = SceneManager.GetActiveScene().name;
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

            if (sceneName == GameScenes.GroundAttack)
            {
                GroundAttackRuntimeBuilder.BuildIfNeeded();
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
            if (Object.FindAnyObjectByType<F89.UI.GamePauseController>() != null)
            {
                return;
            }

            var pauseObject = new GameObject("GamePauseController");
            pauseObject.AddComponent<F89.UI.GamePauseController>();
        }
    }
}
