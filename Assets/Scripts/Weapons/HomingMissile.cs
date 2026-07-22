using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class HomingMissile : MonoBehaviour
    {
        private static readonly List<HomingMissile> ActiveMissiles = new List<HomingMissile>(16);

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
        private MissileSeekerSettings seekerSettings;
        private Vector3 expiredFlareWaypoint;
        private bool hasExpiredFlareWaypoint;
        private bool processedFlareBurnout;
        private float flareRetargetRangeWorld;
        private float accuracyMultiplier = 1f;
        private System.Action onFlightComplete;

        public bool IsIncomingEnemyThreat { get; private set; }
        public Vector3 WorldPosition => transform.position;
        public float SpeedMilesPerSecond => config != null ? config.SpeedMilesPerSecond : 0f;

        public bool IsActivelyTargetingPlayer()
        {
            if (!IsIncomingEnemyThreat)
            {
                return false;
            }

            if (lockedTarget != null && lockedTarget.IsAlive)
            {
                if (lockedTarget.IsPlayerAircraft)
                {
                    return true;
                }

                if (lockedTarget.IsFlareDecoy)
                {
                    return false;
                }
            }

            if (hasExpiredFlareWaypoint)
            {
                return false;
            }

            var primary = seekerSettings.PrimaryTarget;
            return primary != null && primary.IsAlive && primary.IsPlayerAircraft;
        }

        public static bool HasAnyActivelyTargetingPlayer()
        {
            for (var i = ActiveMissiles.Count - 1; i >= 0; i--)
            {
                var missile = ActiveMissiles[i];
                if (missile == null)
                {
                    ActiveMissiles.RemoveAt(i);
                    continue;
                }

                if (missile.IsActivelyTargetingPlayer())
                {
                    return true;
                }
            }

            return false;
        }

        public static void CollectIncomingEnemyThreats(List<HomingMissile> results)
        {
            results.Clear();
            for (var i = ActiveMissiles.Count - 1; i >= 0; i--)
            {
                var missile = ActiveMissiles[i];
                if (missile == null)
                {
                    ActiveMissiles.RemoveAt(i);
                    continue;
                }

                if (missile.IsIncomingEnemyThreat)
                {
                    results.Add(missile);
                }
            }
        }

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
            Vector3 launchVelocityWorld = default,
            MissileSeekerSettings seekerSettings = default,
            System.Action onFlightComplete = null)
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
                launchVelocityWorld,
                seekerSettings,
                onFlightComplete);
            return missile;
        }

        private void Initialize(
            IMissileWeaponConfig weaponConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            Vector3 spawnPosition,
            Vector3 forward,
            LockableTarget target,
            bool lockedShot,
            float launchAccuracyMultiplier,
            Vector3 launchVelocityWorld,
            MissileSeekerSettings seeker,
            System.Action flightCompleteCallback)
        {
            onFlightComplete = flightCompleteCallback;
            accuracyMultiplier = Mathf.Clamp01(launchAccuracyMultiplier);
            config = weaponConfig;
            worldMap = mapConfig;
            flightProfile = profile;
            lockedTarget = target;
            wasLockedShot = lockedShot;
            seekerSettings = seeker;
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
            flareRetargetRangeWorld = WorldMapConfig.RangeMilesToWorldUnits(
                seekerSettings.FlareRetargetRangeMiles,
                worldMap,
                ticSize);

            transform.position = spawnPosition;
            launchPosition = spawnPosition;
            distanceTraveled = 0f;
            if (launchForward.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(launchForward, Vector3.up);
            }

            IsIncomingEnemyThreat = seeker.RespondsToFlares
                && seeker.PrimaryTarget != null
                && seeker.PrimaryTarget.IsPlayerAircraft;

            if (IsIncomingEnemyThreat)
            {
                ActiveMissiles.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveMissiles.Remove(this);
        }

        private void Update()
        {
            lifetimeRemaining -= Time.deltaTime;
            if (lifetimeRemaining <= 0f)
            {
                CompleteFlight();
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
            if (seekerSettings.RespondsToFlares)
            {
                UpdateFlareSeekerBehavior();
            }
            else if (lockedTarget == null || !lockedTarget.IsAlive)
            {
                CompleteFlight();
                return;
            }

            Vector3 targetPoint;
            if (TryGetActiveTargetPoint(out targetPoint))
            {
                FlyTowardPoint(targetPoint);
                return;
            }

            CompleteFlight();
        }

        private void CompleteFlight()
        {
            onFlightComplete?.Invoke();
            onFlightComplete = null;
            Destroy(gameObject);
        }

        private void UpdateFlareSeekerBehavior()
        {
            var flareTarget = FindBestFlareTarget();
            if (flareTarget != null
                && (seekerSettings.FlareRetargetChance >= 1f
                    || Random.value <= seekerSettings.FlareRetargetChance))
            {
                lockedTarget = flareTarget;
                hasExpiredFlareWaypoint = false;
                processedFlareBurnout = false;
                return;
            }

            if (lockedTarget != null
                && lockedTarget.IsFlareDecoy
                && !lockedTarget.IsAlive
                && !processedFlareBurnout)
            {
                processedFlareBurnout = true;
                expiredFlareWaypoint = lockedTarget.transform.position;
                hasExpiredFlareWaypoint = true;

                if (TryReacquirePrimaryTarget())
                {
                    lockedTarget = seekerSettings.PrimaryTarget;
                    hasExpiredFlareWaypoint = false;
                    return;
                }

                lockedTarget = null;
            }
        }

        private LockableTarget FindBestFlareTarget()
        {
            if (flareRetargetRangeWorld <= 0f)
            {
                return null;
            }

            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            LockableTarget best = null;
            var bestDistance = float.MaxValue;
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !target.IsFlareDecoy)
                {
                    continue;
                }

                var distanceMiles = CombatThreatRange.DistanceMiles(
                    transform.position,
                    target.transform.position,
                    worldMap,
                    ticSize);
                if (distanceMiles > seekerSettings.FlareRetargetRangeMiles)
                {
                    continue;
                }

                var distanceWorld = FlattenDistance(transform.position, target.transform.position);
                if (distanceWorld < bestDistance)
                {
                    bestDistance = distanceWorld;
                    best = target;
                }
            }

            return best;
        }

        private bool TryReacquirePrimaryTarget()
        {
            var primary = seekerSettings.PrimaryTarget;
            if (primary == null || !primary.IsAlive)
            {
                return false;
            }

            if (Random.value > seekerSettings.FlareBurnoutReacquireChance)
            {
                return false;
            }

            return WeaponLockCoverage.IsWithinLockCoverage(
                transform.position,
                transform.forward,
                primary.transform.position,
                WeaponAimMode.ForwardLock,
                seekerSettings.SeekerForwardHalfAngleDegrees);
        }

        private bool TryGetActiveTargetPoint(out Vector3 targetPoint)
        {
            if (lockedTarget != null && lockedTarget.IsAlive)
            {
                targetPoint = lockedTarget.transform.position;
                return true;
            }

            if (hasExpiredFlareWaypoint)
            {
                targetPoint = expiredFlareWaypoint;
                return true;
            }

            targetPoint = default;
            return false;
        }

        private void FlyTowardPoint(Vector3 targetPoint)
        {
            var toTarget = Flatten(targetPoint - transform.position);
            var distance = toTarget.magnitude;
            var travelDirection = distance > 0.0001f ? toTarget / distance : transform.forward;
            var stepSpeed = GetEffectiveSpeedWorld(travelDirection);
            var hitRadius = HitRadiusWorld();

            if (distance <= stepSpeed * Time.deltaTime + hitRadius)
            {
                ResolveArrival(targetPoint);
                return;
            }

            var step = stepSpeed * Time.deltaTime;
            transform.position += travelDirection * step;
            distanceTraveled += step;
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);

            if (distanceTraveled >= maxRangeWorld)
            {
                CompleteFlight();
            }
        }

        private void ResolveArrival(Vector3 targetPoint)
        {
            if (lockedTarget != null && lockedTarget.IsAlive)
            {
                if (lockedTarget.IsFlareDecoy)
                {
                    DetonateOnFlare(lockedTarget);
                    return;
                }

                ResolveLockedHit(lockedTarget);
                return;
            }

            if (hasExpiredFlareWaypoint)
            {
                DetonateAtFlareLocation(targetPoint);
            }
        }

        private void DetonateOnFlare(LockableTarget flareTarget)
        {
            var flare = flareTarget.GetComponent<CountermeasureFlare>();
            if (flare != null)
            {
                flare.DetonateFromMissile(config.WeaponName);
            }
            else
            {
                flareTarget.ExpireWithoutHit();
            }

            CompleteFlight();
        }

        private void DetonateAtFlareLocation(Vector3 location)
        {
            Debug.Log($"{config.WeaponName} detonated at expired flare position.");
            CompleteFlight();
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

                if (target.IsFlareDecoy)
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

                target.RegisterHit(config.WeaponName, false, ResolveDestroyChanceFor(target));
                hits++;
            }

            Debug.Log(
                $"{config.WeaponName} dumb-fire impact. {hits} target(s) damaged. Accuracy: {accuracyMultiplier:P0}.");
            CompleteFlight();
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
                target.RegisterHit(config.WeaponName, true, ResolveDestroyChanceFor(target));
            }
            else
            {
                Debug.Log(
                    $"{config.WeaponName} missed {target.TargetLabel} (locked shot, {effectiveChance:P0} effective chance).");
            }

            CompleteFlight();
        }

        private float ResolveDestroyChanceFor(LockableTarget target)
        {
            if (target == null || !target.IsPlayerAircraft)
            {
                return 1f;
            }

            if (config is EnemySamMissileConfig enemyConfig)
            {
                return enemyConfig.playerDestroyChanceOnHit;
            }

            return 1f;
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
