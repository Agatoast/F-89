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
        private float dotRadiusWorld;
        private Vector3 launchVelocity;
        private LockableTarget lockedTarget;
        private Transform aircraftTransform;
        private WorldMapConfig worldMap;
        private float ticSizeWorld;

        public static void Fire(
            Gau27aWeaponConfig weaponConfig,
            FlightProfile profile,
            WorldMapConfig mapConfig,
            Vector3 spawnPoint,
            Vector3 destination,
            Vector3 launchVelocityWorld = default,
            LockableTarget lockedTargetForHit = null,
            Transform aircraftTransformForOgive = null)
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
                mapConfig,
                spawnPoint,
                destination,
                launchVelocityWorld,
                lockedTargetForHit,
                aircraftTransformForOgive);
        }

        private void Initialize(
            Gau27aWeaponConfig weaponConfig,
            FlightProfile profile,
            WorldMapConfig mapConfig,
            Vector3 spawnPoint,
            Vector3 destination,
            Vector3 launchVelocityWorld,
            LockableTarget lockedTargetForHit,
            Transform aircraftTransformForOgive)
        {
            config = weaponConfig;
            worldMap = mapConfig;
            aimPoint = destination;
            aimPoint.y = 0.5f;
            launchVelocity = Flatten(launchVelocityWorld);
            lockedTarget = lockedTargetForHit;
            aircraftTransform = aircraftTransformForOgive;

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            ticSizeWorld = ticSize;
            speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                weaponConfig.roundSpeedMilesPerSecond,
                worldMap,
                ticSize);
            dotRadiusWorld = weaponConfig.crosshairDotRadiusTics * ticSize;

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
            var target = FindTargetUnderCrosshairDot();
            if (target != null)
            {
                PlaneWeaponGhp.ApplyGau27Hit(target, wasLockedShot: false);
                return;
            }

            if (TryHitLockedTargetInOgive(out target))
            {
                PlaneWeaponGhp.ApplyGau27Hit(target, wasLockedShot: true);
            }
        }

        private bool TryHitLockedTargetInOgive(out LockableTarget target)
        {
            target = lockedTarget;
            if (target == null || !target.IsAlive || aircraftTransform == null)
            {
                return false;
            }

            if (!DirectFireTargetRules.CanBeDamagedByGau27(target))
            {
                return false;
            }

            var forward = aircraftTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            if (!Gau27aOgiveEnvelope.IsWithinOgive(
                    aircraftTransform.position,
                    forward,
                    target.transform.position,
                    config,
                    worldMap,
                    ticSizeWorld))
            {
                return false;
            }

            return true;
        }

        private LockableTarget FindTargetUnderCrosshairDot()
        {
            var targets = CombatThreatRange.GetCachedLockableTargets();
            return DirectFireTargetRules.FindGau27TargetUnderCrosshairDot(aimPoint, dotRadiusWorld, targets);
        }
    }
}
