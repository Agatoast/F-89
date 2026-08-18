using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    /// <summary>First-person nest: look out the sandbag slit. Depth 0 = horizon, 1 = throw range at the ledge.</summary>
    public static class BattlefieldLayout
    {
        public const string DefaultFrameResourcePath = BattlefieldBackgroundCatalog.DefaultFrameResourcePath;

        /// <summary>Opening through the bags, viewport space (origin bottom-left).</summary>
        public static Rect OpeningViewport { get; private set; } = new(0.075f, 0.106f, 0.845f, 0.783f);

        public static string FrameResourcePath { get; private set; } = DefaultFrameResourcePath;

        public const float ThrowDepth = 0.78f;
        public const float VehicleStopDepth = 0.5f;
        /// <summary>Sky/ground line within the opening (0 = bottom, 1 = top).</summary>
        public const float HorizonHeightInOpening = 0.55f;
        public const float ChargeEndHeightInOpening = 0.18f;
        public const float SpawnHorizontalInsetFraction = 0.035f;
        public static float SpawnLowerByPixels { get; private set; } = BattlefieldBackgroundCatalog.DefaultSpawnLowerByPixels;
        public const float CameraOrthoSize = 5.15f;
        public static readonly Vector3 CameraPosition = new(0f, 0f, -10f);

        public static Rect OpeningWorld { get; private set; }
        public static Vector2 GunMountWorld { get; private set; }
        public static Vector2 MuzzleWorld { get; private set; }
        public static Vector2 HangarImpact { get; private set; }

        public static void ApplyMission(int mapNumber)
        {
            mapNumber = Mathf.Clamp(mapNumber, 1, DefenseSiteCatalog.MissionCount);
            FrameResourcePath = BattlefieldBackgroundCatalog.GetFrameResourcePath(mapNumber);
            OpeningViewport = BattlefieldBackgroundCatalog.GetOpeningViewport(mapNumber);
            SpawnLowerByPixels = BattlefieldBackgroundCatalog.GetSpawnLowerByPixels(mapNumber);
        }

        public static void BindCamera(Camera camera)
        {
            var min = camera.ViewportToWorldPoint(new Vector3(OpeningViewport.xMin, OpeningViewport.yMin, 0f));
            var max = camera.ViewportToWorldPoint(new Vector3(OpeningViewport.xMax, OpeningViewport.yMax, 0f));
            OpeningWorld = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            var screenBottom = camera.transform.position.y - camera.orthographicSize;
            GunMountWorld = new Vector2(OpeningWorld.center.x, screenBottom);
            MuzzleWorld = new Vector2(OpeningWorld.center.x, screenBottom + camera.orthographicSize * 0.55f);
            HangarImpact = new Vector2(OpeningWorld.center.x, OpeningWorld.yMin - 0.35f);
        }

        public static Vector2 FieldToWorld(float laneX, float depth)
        {
            depth = Mathf.Clamp01(depth);
            var opening = OpeningWorld;
            if (opening.width < 0.01f || opening.height < 0.01f)
            {
                return Vector2.zero;
            }

            var lane = Mathf.Clamp(laneX, -1f, 1f);
            var inset = Mathf.Max(0.08f, opening.width * SpawnHorizontalInsetFraction);
            var halfSpan = Mathf.Max(0.12f, opening.width * 0.5f - inset);
            var x = opening.center.x + lane * halfSpan;

            var horizonY = opening.yMin + opening.height * HorizonHeightInOpening - PixelsToWorldY(SpawnLowerByPixels);
            var nearY = opening.yMin + opening.height * ChargeEndHeightInOpening - PixelsToWorldY(SpawnLowerByPixels);
            var y = Mathf.Lerp(horizonY, nearY, depth);

            return ClampToOpeningWorld(new Vector2(x, y), inset * 0.5f);
        }

        public static Vector2 ClampToOpeningWorld(Vector2 worldPoint, float inset = 0.08f)
        {
            var opening = OpeningWorld;
            if (opening.width < 0.01f || opening.height < 0.01f)
            {
                return worldPoint;
            }

            return new Vector2(
                Mathf.Clamp(worldPoint.x, opening.xMin + inset, opening.xMax - inset),
                Mathf.Clamp(worldPoint.y, opening.yMin + inset, opening.yMax - inset));
        }

        public static float RandomSpawnLane()
        {
            return Random.Range(-1f, 1f);
        }

        public static float ScaleForDepth(float depth, bool rusher)
        {
            var near = rusher ? 1.35f : 1.55f;
            return Mathf.Lerp(0.14f, near, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(depth)));
        }

        public static float ScaleForVehicleDepth(float depth)
        {
            return Mathf.Lerp(0.24f, 1.05f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(depth)));
        }

        public static float RandomLane()
        {
            return RandomSpawnLane();
        }

        /// <summary>Top band of the viewport opening — above the horizon.</summary>
        public static Vector2 GetSkySpawnPosition(bool fromLeft, out float moveSign)
        {
            var opening = OpeningWorld;
            moveSign = fromLeft ? 1f : -1f;
            if (opening.width < 0.01f || opening.height < 0.01f)
            {
                return Vector2.zero;
            }

            var inset = Mathf.Max(0.08f, opening.width * SpawnHorizontalInsetFraction);
            var bandHeight = opening.height * GameConstants.SkyBandHeightFraction;
            var bandBottom = opening.yMax - bandHeight;
            var y = Random.Range(bandBottom + inset * 0.25f, opening.yMax - inset * 0.25f);
            var x = fromLeft ? opening.xMin + inset : opening.xMax - inset;
            return new Vector2(x, y);
        }

        public static float GetSkyCrashGroundY()
        {
            var opening = OpeningWorld;
            if (opening.height < 0.01f)
            {
                return 0f;
            }

            return opening.yMin + opening.height * HorizonHeightInOpening - PixelsToWorldY(SpawnLowerByPixels);
        }

        public static Vector2 GetOpeningCenterWorld()
        {
            var opening = OpeningWorld;
            if (opening.width < 0.01f || opening.height < 0.01f)
            {
                return HangarImpact;
            }

            return opening.center;
        }

        public static bool ViewportInOpening(Vector2 viewport)
        {
            return OpeningViewport.Contains(viewport);
        }

        public static Vector2 ClampScreenToOpening(Vector2 screenPoint, Camera camera)
        {
            if (camera == null)
            {
                return screenPoint;
            }

            var bottomLeft = camera.ViewportToScreenPoint(new Vector3(OpeningViewport.xMin, OpeningViewport.yMin, 0f));
            var topRight = camera.ViewportToScreenPoint(new Vector3(OpeningViewport.xMax, OpeningViewport.yMax, 0f));
            return new Vector2(
                Mathf.Clamp(screenPoint.x, Mathf.Min(bottomLeft.x, topRight.x), Mathf.Max(bottomLeft.x, topRight.x)),
                Mathf.Clamp(screenPoint.y, Mathf.Min(bottomLeft.y, topRight.y), Mathf.Max(bottomLeft.y, topRight.y)));
        }

        public static Vector2 ScreenToOpeningAimWorld(Vector2 screenPoint, Camera camera)
        {
            if (camera == null)
            {
                return MuzzleWorld;
            }

            var clamped = ClampScreenToOpening(screenPoint, camera);
            return camera.ScreenToWorldPoint(clamped);
        }

        private static float PixelsToWorldY(float pixels)
        {
            var camera = Camera.main;
            if (camera == null || camera.pixelHeight <= 0)
            {
                return pixels * 0.01f;
            }

            var worldHeight = camera.orthographicSize * 2f;
            return pixels / camera.pixelHeight * worldHeight;
        }
    }
}
