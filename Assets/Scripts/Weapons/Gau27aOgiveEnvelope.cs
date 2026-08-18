using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Forward ogive centered on the aircraft: a circular sector with a 30° arc at max range
    /// (± half-angle from the nose). The gun cursor cannot leave this region.
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
            if (!TryIntersectHorizontalPlane(ray, 0f, out var hit))
            {
                // Ray misses the map plane — aim along flattened view direction, then clamp.
                var view = Flatten(ray.direction);
                if (view.sqrMagnitude < 0.0001f)
                {
                    view = Flatten(forward);
                }

                hit = origin + view.normalized * WorldMapConfig.RangeMilesToWorldUnits(
                    config.ogiveMaxRangeMiles,
                    worldMap,
                    ticSizeWorldUnits);
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

        /// <summary>
        /// Polar clamp into a circular sector centered on the plane:
        /// distance ∈ [min, max], bearing ∈ ±ogiveHalfAngle (30° total arc).
        /// Far edge is a circular arc about the aircraft, not a flat cone face.
        /// </summary>
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
            minWorld = Mathf.Max(0.01f, minWorld);
            ogiveMaxWorld = Mathf.Max(minWorld, ogiveMaxWorld);

            var halfAngleDegrees = Mathf.Max(0.01f, config.ogiveHalfAngleDegrees);

            var offset = worldPoint - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                var onNose = origin + forward * minWorld;
                onNose.y = 0.5f;
                return onNose;
            }

            var distance = offset.magnitude;
            var bearingDegrees = Vector3.SignedAngle(forward, offset / distance, Vector3.up);

            distance = Mathf.Clamp(distance, minWorld, ogiveMaxWorld);
            bearingDegrees = Mathf.Clamp(bearingDegrees, -halfAngleDegrees, halfAngleDegrees);

            var clampedDir = Quaternion.AngleAxis(bearingDegrees, Vector3.up) * forward;
            var clamped = origin + clampedDir * distance;
            clamped.y = 0.5f;
            return clamped;
        }

        /// <summary>
        /// True when the target lies inside the forward ogive sector (±half-angle, min–max range).
        /// </summary>
        public static bool IsWithinOgive(
            Vector3 origin,
            Vector3 forward,
            Vector3 targetPosition,
            Gau27aWeaponConfig config,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            if (config == null)
            {
                return false;
            }

            origin.y = 0f;
            targetPosition.y = 0f;
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
            minWorld = Mathf.Max(0.01f, minWorld);
            ogiveMaxWorld = Mathf.Max(minWorld, ogiveMaxWorld);
            var halfAngleDegrees = Mathf.Max(0.01f, config.ogiveHalfAngleDegrees);

            var offset = targetPosition - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            var distance = offset.magnitude;
            if (distance < minWorld || distance > ogiveMaxWorld)
            {
                return false;
            }

            var bearingDegrees = Vector3.SignedAngle(forward, offset / distance, Vector3.up);
            return Mathf.Abs(bearingDegrees) <= halfAngleDegrees;
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
