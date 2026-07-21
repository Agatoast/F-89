using F89.Core;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Testing
{
    public static class MainMenuRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MainMenuController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var menuRoot = new GameObject("MainMenu");
            menuRoot.AddComponent<MainMenuController>();

            SetupCamera();
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }

    public static class SceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Time.timeScale = 1f;

            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == GameScenes.MainMenu)
            {
                MainMenuRuntimeBuilder.BuildIfNeeded();
                return;
            }

            if (sceneName == GameScenes.FlightTest || ShouldBootstrapGameplayInEditorScene(sceneName))
            {
                FlightTestRuntimeBuilder.BuildIfNeeded();
            }
        }

        private static bool ShouldBootstrapGameplayInEditorScene(string sceneName)
        {
            return string.IsNullOrEmpty(sceneName)
                || sceneName == "Untitled"
                || sceneName.StartsWith("Temp");
        }
    }
}
