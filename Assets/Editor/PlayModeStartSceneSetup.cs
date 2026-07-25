#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace F89.EditorTools
{
    [InitializeOnLoad]
    public static class PlayModeStartSceneSetup
    {
        private const string LoadingScreenScenePath = "Assets/Scenes/LoadingScreen.unity";

        static PlayModeStartSceneSetup()
        {
            var startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoadingScreenScenePath);
            if (startScene == null)
            {
                return;
            }

            if (EditorSceneManager.playModeStartScene != startScene)
            {
                EditorSceneManager.playModeStartScene = startScene;
            }
        }
    }
}
#endif
