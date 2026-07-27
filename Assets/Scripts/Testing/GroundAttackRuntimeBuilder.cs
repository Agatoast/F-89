using F89.LandCombat;
using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class GroundAttackRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<GroundAttackScreenController>() != null)
            {
                if (LandSurfaceSession.HasPendingReturnAtBunker)
                {
                    LandGroundSceneBuilder.BuildIfNeeded();
                }

                return;
            }

            Build();
        }

        public static void Build()
        {
            LandCombatRuntimeBootstrap.BootstrapScene();
            LandGroundSceneBuilder.BuildIfNeeded();

            var root = new GameObject("GroundAttackScreen");
            root.AddComponent<GroundAttackScreenController>();

            SetupCamera();
        }

        private static void SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.84f, 0.88f, 0.93f);
        }
    }
}
