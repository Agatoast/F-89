using F89.MidAirRefuel;
using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class MidAirRefuelRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MidAirRefuelController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GamePauseController.EnsureExists();

            var root = new GameObject("MidAirRefuel");
            root.AddComponent<MidAirRefuelController>();

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
            camera.backgroundColor = new Color(0.55f, 0.72f, 0.92f);
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
