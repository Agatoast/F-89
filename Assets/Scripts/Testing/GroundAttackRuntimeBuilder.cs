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
                return;
            }

            Build();
        }

        public static void Build()
        {
            LandCombatRuntimeBootstrap.BootstrapScene();

            var root = new GameObject("GroundAttackScreen");
            root.AddComponent<GroundAttackScreenController>();

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
            camera.backgroundColor = new Color(0.1f, 0.12f, 0.1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
