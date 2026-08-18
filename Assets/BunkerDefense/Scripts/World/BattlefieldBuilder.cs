using SaveAntarctica.BunkerDefense.Combat;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public static class BattlefieldBuilder
    {
        public static void Build(int hangarHitPoints, int mapNumber = 1)
        {
            var camera = EnsureCamera();
            BattlefieldLayout.ApplyMission(mapNumber);
            BattlefieldLayout.BindCamera(camera);
            BunkerFrame.Build(camera);
            HangarTarget.Create(hangarHitPoints);
            SandbagBunker.Create();
        }

        public static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = BattlefieldLayout.CameraOrthoSize;
            cam.transform.position = BattlefieldLayout.CameraPosition;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.2f, 0.22f, 1f);
            ScreenShake.Ensure(cam);
            CombatAudio.Ensure();
            return cam;
        }
    }
}
