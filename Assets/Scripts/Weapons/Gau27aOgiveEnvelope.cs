using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Forward cone ogive in front of the aircraft. The crosshair may sit anywhere inside;
    /// rounds still travel at most <see cref="Gau27aWeaponConfig.maxRangeMiles"/>.
    /// </summary>
    public static class Gau27aOgiveEnvelope
    {
        public static bool TryResolveFromScreen(
            Camera camera,
            Vector2 screenPosition,
            Vector3 origin,
            Vector3 forward,
            Gau27aWeaponConfig config,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out Vector3 clampedWorldPoint)
        {
            clampedWorldPoint = default;
            if (camera == null || config == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(screenPosition);
            if (!TryIntersectHorizontalPlane(ray, origin.y, out var hit))
            {
                return false;
            }

            clampedWorldPoint = ClampToOgive(
                origin,
                forward,
                hit,
                config,
                worldMap,
                ticSizeWorldUnits);
            return true;
        }

        public static Vector3 ClampToOgive(
            Vector3 origin,
            Vector3 forward,
            Vector3 worldPoint,
            Gau27aWeaponConfig config,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            origin.y = 0f;
            worldPoint.y = 0f;
            forward = Flatten(forward);
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            var ogiveMaxWorld = WorldMapConfig.RangeMilesToWorldUnits(
                config.ogiveMaxRangeMiles,
                worldMap,
                ticSizeWorldUnits);
            var minWorld = WorldMapConfig.RangeMilesToWorldUnits(
                config.minCrosshairMiles,
                worldMap,
                ticSizeWorldUnits);
            var halfAngleRadians = config.ogiveHalfAngleDegrees * Mathf.Deg2Rad;

            var offset = worldPoint - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                var result = origin + forward * Mathf.Max(minWorld, 0.01f);
                result.y = 0.5f;
                return result;
            }

            var forwardDistance = Vector3.Dot(offset, forward);
            var lateral = offset - forward * forwardDistance;
            var lateralDistance = lateral.magnitude;

            if (forwardDistance < minWorld)
            {
                forwardDistance = minWorld;
                lateral = Vector3.zero;
                lateralDistance = 0f;
            }
            else
            {
                forwardDistance = Mathf.Min(forwardDistance, ogiveMaxWorld);
                var maxLateral = forwardDistance * Mathf.Tan(halfAngleRadians);
                if (lateralDistance > maxLateral)
                {
                    lateral = lateral / lateralDistance * maxLateral;
                }
            }

            var clamped = origin + forward * forwardDistance + lateral;
            clamped.y = 0.5f;
            return clamped;
        }

        public static Vector3 ClampFireDestination(
            Vector3 spawnPoint,
            Vector3 crosshairWorldPoint,
            Gau27aWeaponConfig config,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            spawnPoint.y = 0.5f;
            crosshairWorldPoint.y = 0.5f;

            var toAim = crosshairWorldPoint - spawnPoint;
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.0001f)
            {
                return crosshairWorldPoint;
            }

            var maxWorld = WorldMapConfig.RangeMilesToWorldUnits(
                config.maxRangeMiles,
                worldMap,
                ticSizeWorldUnits);
            if (toAim.magnitude <= maxWorld)
            {
                return crosshairWorldPoint;
            }

            var destination = spawnPoint + toAim.normalized * maxWorld;
            destination.y = 0.5f;
            return destination;
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
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
