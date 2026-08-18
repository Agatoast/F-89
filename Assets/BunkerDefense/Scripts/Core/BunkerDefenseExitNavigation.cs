using F89.UI;
using SaveAntarctica.BunkerDefense.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveAntarctica.BunkerDefense.Core
{
    public static class BunkerDefenseExitNavigation
    {
        public static void ReturnToMissionMenu()
        {
            Time.timeScale = 1f;
            GamePauseController.ClearPauseOnSceneLoad();
            CombatAudio.StopBriefingClaxon();

            if (TryLoad(F89.Core.GameScenes.StartPage)
                || TryLoad(F89.Core.GameScenes.MainMenu)
                || TryLoadHostReturnScene()
                || TryLoad(GameScenes.MainMenuScene))
            {
                return;
            }

            Debug.LogWarning("Bunker Defense: could not return to mission menu — no scene in Build Settings.");
        }

        private static bool TryLoadHostReturnScene()
        {
            var returnScene = FlightMissionData.Instance?.ReturnSceneName;
            if (string.IsNullOrWhiteSpace(returnScene) || returnScene == GameScenes.PlayScene)
            {
                return false;
            }

            return TryLoad(returnScene);
        }

        private static bool TryLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }
    }
}
