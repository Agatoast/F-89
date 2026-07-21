using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class HomingMissile : MonoBehaviour
    {
        private IMissileWeaponConfig config;
        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private LockableTarget lockedTarget;
        private Vector3 launchForward;
        private bool wasLockedShot;
        private float speedWorld;
        private float lifetimeRemaining;
        private float maxRangeWorld;
        private float distanceTraveled;
        private Vector3 launchPosition;
        private Vector3 launchVelocity;

        public static HomingMissile Launch(
            IMissileWeaponConfig weaponConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            Vector3 spawnPosition,
            Vector3 forward,
            LockableTarget target,
            bool lockedShot,
            Color bodyColor,
            float accuracyMultiplier,
            Vector3 launchVelocityWorld = default)
        {
            var missileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missileObject.name = lockedShot
                ? $"{weaponConfig.WeaponName} (locked)"
                : weaponConfig.WeaponName;

            missileObject.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);

            var collider = missileObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = missileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = bodyColor;
            }

            var missile = missileObject.AddComponent<HomingMissile>();
            missile.Initialize(
                weaponConfig,
                mapConfig,
                profile,
                spawnPosition,
                forward,
                target,
                lockedShot,
                accuracyMultiplier,
                launchVelocityWorld);
            return missile;
        }

        private float accuracyMultiplier = 1f;

        private void Initialize(
            IMissileWeaponConfig weaponConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            Vector3 spawnPosition,
            Vector3 forward,
            LockableTarget target,
            bool lockedShot,
            float launchAccuracyMultiplier,
            Vector3 launchVelocityWorld)
        {
            accuracyMultiplier = Mathf.Clamp01(launchAccuracyMultiplier);
            config = weaponConfig;
            worldMap = mapConfig;
            flightProfile = profile;
            lockedTarget = target;
            wasLockedShot = lockedShot;
            launchForward = Flatten(forward).normalized;
            launchVelocity = Flatten(launchVelocityWorld);
            lifetimeRemaining = config.MissileLifetimeSeconds;

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                config.SpeedMilesPerSecond,
                worldMap,
                ticSize);
            maxRangeWorld = WorldMapConfig.RangeMilesToWorldUnits(
                config.RangeMiles,
                worldMap,
                ticSize);

            transform.position = spawnPosition;
            launchPosition = spawnPosition;
            distanceTraveled = 0f;
            if (launchForward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(launchForward, Vector3.up);
            }
        }

        private void Update()
        {
            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (wasLockedShot)
            {
                UpdateLockedFlight();
                return;
            }

            UpdateUnlockedFlight();
        }

        private void UpdateLockedFlight()
        {
            if (lockedTarget == null || !lockedTarget.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            var targetPoint = lockedTarget.transform.position;
            var toTarget = Flatten(targetPoint - transform.position);
            var distance = toTarget.magnitude;
            var travelDirection = toTarget.normalized;
            var stepSpeed = GetEffectiveSpeedWorld(travelDirection);
            if (distance <= stepSpeed * Time.deltaTime + HitRadiusWorld())
            {
                ResolveLockedHit(lockedTarget);
                return;
            }

            var step = stepSpeed * Time.deltaTime;
            transform.position += travelDirection * step;
            distanceTraveled += step;
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

            if (distanceTraveled >= maxRangeWorld)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateUnlockedFlight()
        {
            var stepSpeed = GetEffectiveSpeedWorld(launchForward);
            var step = launchForward * (stepSpeed * Time.deltaTime);
            transform.position += step;
            distanceTraveled += step.magnitude;

            if (distanceTraveled >= maxRangeWorld)
            {
                DetonateAtImpact(launchPosition + launchForward * maxRangeWorld);
            }
        }

        private void DetonateAtImpact(Vector3 center)
        {
            center.y = 0.5f;
            var blastRadius = ImpactBlastRadiusWorld();
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            var hits = 0;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !target.MatchesWeapon(config.ValidTargetKind))
                {
                    continue;
                }

                if (FlattenDistance(center, target.transform.position) > blastRadius)
                {
                    continue;
                }

                if (Random.value > accuracyMultiplier)
                {
                    continue;
                }

                target.RegisterHit(config.WeaponName, false);
                hits++;
            }

            Debug.Log(
                $"{config.WeaponName} dumb-fire impact. {hits} target(s) damaged. Accuracy: {accuracyMultiplier:P0}.");
            Destroy(gameObject);
        }

        private float ImpactBlastRadiusWorld()
        {
            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            return Mathf.Max(config.CollisionRadiusTics * 2f, 1.5f) * ticSize;
        }

        private static float FlattenDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void ResolveLockedHit(LockableTarget target)
        {
            var effectiveChance = config.LockHitChance * accuracyMultiplier;
            var hit = Random.value <= effectiveChance;
            if (hit)
            {
                target.RegisterHit(config.WeaponName, true);
            }
            else
            {
                Debug.Log(
                    $"{config.WeaponName} missed {target.TargetLabel} (locked shot, {effectiveChance:P0} effective chance).");
            }

            Destroy(gameObject);
        }

        private float HitRadiusWorld()
        {
            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            return config.CollisionRadiusTics * ticSize;
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
    }
}
