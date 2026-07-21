#if UNITY_EDITOR
using F89.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace F89.EditorTools
{
    public static class FlightTestSceneCreator
    {
        private const string FlightTestScenePath = "Assets/Scenes/FlightTest.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

        private static bool IsEditorSceneSetupAllowed()
        {
            return !Application.isPlaying;
        }

        [MenuItem("F-89/Setup Main Menu Scene", true)]
        private static bool ValidateSetupMainMenuScene()
        {
            return IsEditorSceneSetupAllowed();
        }

        [MenuItem("F-89/Setup Main Menu Scene")]
        public static void CreateMainMenuScene()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            EnsureScenesFolder();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MainMenuRuntimeBuilder.Build();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            EnsureSceneInBuildSettings(MainMenuScenePath, 0);
            AssetDatabase.SaveAssets();

            Debug.Log($"Main menu scene saved to {MainMenuScenePath}.");
        }

        [MenuItem("F-89/Setup Flight Test Scene", true)]
        private static bool ValidateSetupFlightTestScene()
        {
            return IsEditorSceneSetupAllowed();
        }

        [MenuItem("F-89/Setup Flight Test Scene")]
        public static void CreateFlightTestScene()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            EnsureScenesFolder();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            FlightTestRuntimeBuilder.Build();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, FlightTestScenePath);
            EnsureSceneInBuildSettings(MainMenuScenePath, 0);
            EnsureSceneInBuildSettings(FlightTestScenePath, 1);
            AssetDatabase.SaveAssets();

            Debug.Log($"Flight test scene saved to {FlightTestScenePath}. Press Play to fly.");
        }

        [MenuItem("F-89/Setup All Scenes", true)]
        private static bool ValidateSetupAllScenes()
        {
            return IsEditorSceneSetupAllowed();
        }

        [MenuItem("F-89/Setup All Scenes")]
        public static void CreateAllScenes()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            CreateMainMenuScene();
            CreateFlightTestScene();
            Debug.Log("F-89: Main menu and flight test scenes are ready.");
        }

        [MenuItem("F-89/Open Main Menu Scene", true)]
        private static bool ValidateOpenMainMenuScene()
        {
            return IsEditorSceneSetupAllowed();
        }

        [MenuItem("F-89/Open Main Menu Scene")]
        public static void OpenMainMenuScene()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            if (!System.IO.File.Exists(MainMenuScenePath))
            {
                CreateMainMenuScene();
            }

            EditorSceneManager.OpenScene(MainMenuScenePath);
        }

        [MenuItem("F-89/Open Flight Test Scene", true)]
        private static bool ValidateOpenFlightTestScene()
        {
            return IsEditorSceneSetupAllowed();
        }

        [MenuItem("F-89/Open Flight Test Scene")]
        public static void OpenFlightTestScene()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            if (!System.IO.File.Exists(FlightTestScenePath))
            {
                CreateFlightTestScene();
            }

            EditorSceneManager.OpenScene(FlightTestScenePath);
        }

        public static void CreateFlightTestSceneBatch()
        {
            if (!EnsureEditorSceneSetupAllowed())
            {
                return;
            }

            CreateAllScenes();
        }

        private static bool EnsureEditorSceneSetupAllowed()
        {
            if (!Application.isPlaying)
            {
                return true;
            }

            Debug.LogError("F-89: Stop Play mode before creating or opening scenes from the F-89 menu.");
            return false;
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void EnsureSceneInBuildSettings(string scenePath, int buildIndex)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existingIndex = scenes.FindIndex(scene => scene.path == scenePath);
            if (existingIndex >= 0)
            {
                scenes.RemoveAt(existingIndex);
            }

            while (scenes.Count <= buildIndex)
            {
                scenes.Add(new EditorBuildSettingsScene(string.Empty, false));
            }

            scenes[buildIndex] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
