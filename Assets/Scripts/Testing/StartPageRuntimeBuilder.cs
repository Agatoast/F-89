using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class StartPageRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<StartPageController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("StartPage");
            root.AddComponent<StartPageController>();

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

    public static class SelectionPageRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<SelectionPageController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("SelectionPage");
            root.AddComponent<SelectionPageController>();

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

    public static class MenuSubpageRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MenuSubpageController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("MenuSubpage");
            root.AddComponent<MenuSubpageController>();

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

    public static class CharacterPageRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<CharacterPageController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("CharacterPage");
            root.AddComponent<CharacterPageController>();

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

    public static class MissionBriefingRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MissionBriefingController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("MissionBriefing");
            root.AddComponent<MissionBriefingController>();

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
            camera.backgroundColor = Color.white;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }

    public static class CharacterLoadoutRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<CharacterLoadoutController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("CharacterLoadout");
            root.AddComponent<CharacterLoadoutController>();

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
            camera.backgroundColor = Color.white;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
    }

    public static class AircraftLoadoutRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<AircraftLoadoutController>() != null)
            {
                return;
            }

            Build();
        }

        public static void Build()
        {
            Time.timeScale = 1f;

            var root = new GameObject("AircraftLoadout");
            root.AddComponent<AircraftLoadoutController>();

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
