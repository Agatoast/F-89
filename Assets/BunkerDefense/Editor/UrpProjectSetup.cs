#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SaveAntarctica.BunkerDefense.EditorTools
{
    public static class UrpProjectSetup
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string RendererPath = SettingsFolder + "/BD_URP_Renderer.asset";
        private const string PipelinePath = SettingsFolder + "/BD_URP_Asset.asset";

        [InitializeOnLoadMethod]
        private static void AutoEnsureUrp()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (GraphicsSettings.defaultRenderPipeline != null)
                {
                    return;
                }

                EnsureUrpConfigured();
            };
        }

        [MenuItem("Bunker Defense/Setup URP (2D)")]
        public static void MenuSetup() => EnsureUrpConfigured();

        public static void EnsureUrpConfigured()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
            {
                var parent = Path.GetDirectoryName(SettingsFolder)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(SettingsFolder));
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
