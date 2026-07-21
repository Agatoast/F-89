using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    public static class WeaponAimRange
    {
        public static Vector2 ClampScreenPositionToRange(
            Camera camera,
            Vector3 origin,
            Vector2 screenPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (camera == null || rangeMiles <= 0f)
            {
                return screenPosition;
            }

            var rangeWorld = WeaponLockRange.ToWorldUnits(rangeMiles, worldMap, ticSizeWorldUnits);
            if (rangeWorld <= 0f)
            {
                return screenPosition;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!TryIntersectHorizontalPlane(ray, origin.y, out var hit))
            {
                return screenPosition;
            }

            var offset = hit - origin;
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance <= rangeWorld)
            {
                return screenPosition;
            }

            var clampedWorld = origin + offset.normalized * rangeWorld;
            clampedWorld.y = origin.y;
            var clampedScreen = camera.WorldToScreenPoint(clampedWorld);
            if (clampedScreen.z < 0f)
            {
                return screenPosition;
            }

            return new Vector2(clampedScreen.x, clampedScreen.y);
        }

        public static bool TryResolveAimWorldPoint(
            Camera camera,
            Vector2 screenPosition,
            Vector3 origin,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out Vector3 aimPoint)
        {
            aimPoint = default;
            if (camera == null)
            {
                return false;
            }

            var clampedScreen = ClampScreenPositionToRange(
                camera,
                origin,
                screenPosition,
                rangeMiles,
                worldMap,
                ticSizeWorldUnits);
            var ray = camera.ScreenPointToRay(clampedScreen);
            if (!TryIntersectHorizontalPlane(ray, origin.y, out aimPoint))
            {
                return false;
            }

            aimPoint.y = origin.y;
            return true;
        }

        private static bool TryIntersectHorizontalPlane(Ray ray, float planeY, out Vector3 point)
        {
            point = default;
            if (Mathf.Abs(ray.direction.y) < 0.0001f)
            {
                return false;
            }

            var t = (planeY - ray.origin.y) / ray.direction.y;
            if (t < 0f)
            {
                return false;
            }

            point = ray.origin + ray.direction * t;
            return true;
        }
    }
}
