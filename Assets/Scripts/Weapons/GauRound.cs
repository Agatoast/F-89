using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class GauRound : MonoBehaviour
    {
        private Gau27aWeaponConfig config;
        private Vector3 aimPoint;
        private float speedWorld;
        private float hitRadiusWorld;
        private float hitChance;
        private Vector3 launchVelocity;

        public static void Fire(
            Gau27aWeaponConfig weaponConfig,
            FlightProfile profile,
            WorldMapConfig worldMap,
            Vector3 spawnPoint,
            Vector3 destination,
            float accuracyMultiplier,
            Vector3 launchVelocityWorld = default)
        {
            if (weaponConfig == null)
            {
                return;
            }

            var roundObject = new GameObject($"{weaponConfig.WeaponName} Round");
            var round = roundObject.AddComponent<GauRound>();
            round.Initialize(
                weaponConfig,
                profile,
                worldMap,
                spawnPoint,
                destination,
                accuracyMultiplier,
                launchVelocityWorld);
        }

        private void Initialize(
            Gau27aWeaponConfig weaponConfig,
            FlightProfile profile,
            WorldMapConfig worldMap,
            Vector3 spawnPoint,
            Vector3 destination,
            float accuracyMultiplier,
            Vector3 launchVelocityWorld)
        {
            config = weaponConfig;
            aimPoint = destination;
            aimPoint.y = 0.5f;
            hitChance = weaponConfig.hitChancePerRound * Mathf.Clamp01(accuracyMultiplier);
            launchVelocity = Flatten(launchVelocityWorld);

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                weaponConfig.roundSpeedMilesPerSecond,
                worldMap,
                ticSize);
            hitRadiusWorld = weaponConfig.hitRadiusTics * ticSize;

            spawnPoint.y = 0.5f;
            transform.position = spawnPoint;

            var toAim = aimPoint - spawnPoint;
            toAim.y = 0f;
            if (toAim.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toAim.normalized, Vector3.up);
            }
        }

        private void Update()
        {
            if (config == null || speedWorld <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var current = transform.position;
            current.y = 0.5f;
            var toAim = aimPoint - current;
            toAim.y = 0f;
            var remaining = toAim.magnitude;
            var travelDirection = toAim.normalized;
            var stepSpeed = GetEffectiveSpeedWorld(travelDirection);
            if (remaining <= stepSpeed * Time.deltaTime + 0.01f)
            {
                ResolveImpact();
                Destroy(gameObject);
                return;
            }

            transform.position = current + travelDirection * (stepSpeed * Time.deltaTime);
        }

        private float GetEffectiveSpeedWorld(Vector3 travelDirection)
        {
            if (travelDirection.sqrMagnitude < 0.0001f)
            {
                return speedWorld;
            }

            travelDirection.Normalize();
            var inherited = Vector3.Dot(launchVelocity, travelDirection);
            return speedWorld + Mathf.Max(0f, inherited);
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }

        private void ResolveImpact()
        {
            var target = FindTargetAtAimPoint();
            if (target == null)
            {
                return;
            }

            if (Random.value > hitChance)
            {
                return;
            }

            target.RegisterHit(config.WeaponName, false);
        }

        private LockableTarget FindTargetAtAimPoint()
        {
            var checkPoint = aimPoint;
            checkPoint.y = 0f;

            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            LockableTarget closest = null;
            var closestDistance = float.MaxValue;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var targetPosition = target.transform.position;
                targetPosition.y = 0f;
                var delta = targetPosition - checkPoint;
                var distance = delta.magnitude;
                var targetRadius = GetTargetHitRadius(target);
                if (distance > hitRadiusWorld + targetRadius || distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                closest = target;
            }

            return closest;
        }

        private static float GetTargetHitRadius(LockableTarget target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                var extents = collider.bounds.extents;
                return Mathf.Max(extents.x, extents.z);
            }

            return 1f;
        }
    }
}
