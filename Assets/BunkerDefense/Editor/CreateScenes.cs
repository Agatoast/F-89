#if UNITY_EDITOR
using System.Collections.Generic;
using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.EditorTools
{
    public static class CreateScenes
    {
        private const string ScenesFolder = "Assets/BunkerDefense/Scenes";
        private const string MenuScenePath = ScenesFolder + "/MainMenuScene.unity";
        private const string PlayScenePath = ScenesFolder + "/BunkerPlayScene.unity";

        [MenuItem("Bunker Defense/Create Scenes", false, 1)]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Bunker Defense: Stop Play Mode before creating scenes.");
                return;
            }

            EnsureScenesFolder();

            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var menuRoot = new GameObject("MainMenu");
            menuRoot.AddComponent<MainMenuController>();
            EditorSceneManager.SaveScene(menu, MenuScenePath);

            var play = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.15f;
            cam.transform.position = new Vector3(0.35f, 0.15f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.72f, 0.8f, 0.86f, 1f);
            camGo.AddComponent<AudioListener>();
            new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>();
            EditorSceneManager.SaveScene(play, PlayScenePath);

            ApplyBuildSettings();
            EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log("Bunker Defense scenes created under Assets/BunkerDefense/Scenes.");
        }

        [MenuItem("Bunker Defense/Create Scenes", true)]
        public static bool ValidateCreate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Bunker Defense/Apply Build Settings")]
        public static void ApplyBuildSettings()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var scenes = new List<EditorBuildSettingsScene>();
            var seen = new HashSet<string>();

            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (existing.path == null || !seen.Add(existing.path))
                {
                    continue;
                }

                scenes.Add(existing);
            }

            EnsureSceneInBuild(scenes, seen, MenuScenePath);
            EnsureSceneInBuild(scenes, seen, PlayScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/BunkerDefense"))
            {
                AssetDatabase.CreateFolder("Assets", "BunkerDefense");
            }

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/BunkerDefense", "Scenes");
            }
        }

        private static void EnsureSceneInBuild(
            List<EditorBuildSettingsScene> scenes,
            HashSet<string> seen,
            string path)
        {
            if (!seen.Add(path))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
#endif
