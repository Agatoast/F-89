using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class LoadingScreenRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<LoadingScreenController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("LoadingScreen");
            root.AddComponent<LoadingScreenController>();

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
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
