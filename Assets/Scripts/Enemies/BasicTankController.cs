using System.Collections;
using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using F89.UI;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    public class BasicTankController : MonoBehaviour
    {
        private static readonly List<BasicTankController> ActiveTanks = new List<BasicTankController>(48);
        private static int separationCacheFrame = -1;

        private BasicTankConfig config;
        private EnemySamMissileConfig missileConfig;
        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private LockableTarget playerTarget;
        private float worldUnitsPerMile;
        private string unitLabel;
        private Vector3 homeWorld;

        private int missilesRemaining;
        private int activeMissiles;
        private bool hasWarnedAcquisition;
        private bool engagementStarted;
        private Coroutine engagementRoutine;

        private Vector3 roamPoint;
        private float nextRoamRetargetTime;
        private float roamLateralBias;
        private float roamSpeedScale = 1f;

        public string UnitName =>
            !string.IsNullOrWhiteSpace(unitLabel)
                ? unitLabel
                : config != null
                    ? config.unitName
                    : BasicTankConfig.DefaultUnitName;

        public int MissilesRemaining => missilesRemaining;

        public void Configure(
            BasicTankConfig tankConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            LockableTarget playerLockableTarget,
            float unitsPerMile,
            string uniqueLabel = null)
        {
            config = tankConfig != null
                ? tankConfig
                : ScriptableObject.CreateInstance<BasicTankConfig>();
            worldMap = mapConfig;
            flightProfile = profile;
            playerTarget = playerLockableTarget;
            worldUnitsPerMile = unitsPerMile;
            unitLabel = uniqueLabel;
            missilesRemaining = config.missileCapacity;
            missileConfig = BuildMissileConfig();
            homeWorld = transform.position;
            homeWorld.y = 0f;

            BasicTankVisual.AttachVisual(transform, flightProfile);
            EnsureLockableTarget();
            PickRoamPoint(force: true);
        }

        private void EnsureLockableTarget()
        {
            var target = GetComponent<LockableTarget>();
            if (target == null)
            {
                target = gameObject.AddComponent<LockableTarget>();
            }

            target.Configure(
                UnitName,
                LockableTargetKind.Ground,
                TargetAffiliation.Hostile,
                TargetUnitClass.GroundVehicle);

            var ticSize = flightProfile.ticSizeWorldUnits;
            var planeLength = ticSize * AircraftVisualFactory.VisualSizeMultiplier;
            var tankSize = planeLength * 0.5f;
            target.SetHitRadiusWorld(tankSize * 0.55f);
        }

        private EnemySamMissileConfig BuildMissileConfig()
        {
            var missile = ScriptableObject.CreateInstance<EnemySamMissileConfig>();
            missile.acquisitionRangeMiles = config.acquisitionRangeMiles;
            missile.launchRangeMiles = config.launchRangeMiles;
            missile.rangeMiles = config.missileRangeMiles;
            missile.lockHitChance = config.missileLockHitChance;
            missile.playerDestroyChanceOnHit = config.missilePlayerDestroyChanceOnHit;
            missile.speedMilesPerSecond = config.MissileSpeedMilesPerSecond;
            missile.flareRetargetRangeMiles = config.flareRetargetRangeMiles;
            missile.flareRetargetChance = config.flareRetargetChance;
            missile.flareBurnoutReacquireChance = config.flareBurnoutReacquireChance;
            return missile;
        }

        private void Update()
        {
            if (config == null
                || worldMap == null
                || flightProfile == null
                || GamePauseController.IsPaused)
            {
                return;
            }

            var tankTarget = GetComponent<LockableTarget>();
            if (tankTarget == null || !tankTarget.IsAlive)
            {
                ClearAcquisitionWarning();
                return;
            }

            UpdateRoamMovement();

            if (playerTarget == null || !playerTarget.IsAlive)
            {
                return;
            }

            var distanceMiles = GetDistanceToPlayerMiles();
            UpdateAcquisitionWarning(distanceMiles);

            if (missilesRemaining <= 0 || engagementStarted)
            {
                return;
            }

            if (distanceMiles <= config.launchRangeMiles)
            {
                engagementStarted = true;
                engagementRoutine = StartCoroutine(RunEngagementSequence());
            }
        }

        private void UpdateRoamMovement()
        {
            if (Time.time >= nextRoamRetargetTime
                || FlatDistanceSqr(transform.position, roamPoint) < 0.35f * 0.35f)
            {
                PickRoamPoint(force: false);
            }

            var position = transform.position;
            position.y = 0f;
            var toRoam = roamPoint - position;
            toRoam.y = 0f;

            Vector3 moveDir;
            if (toRoam.sqrMagnitude < 0.04f)
            {
                var jitter = Random.insideUnitSphere;
                jitter.y = 0f;
                moveDir = jitter.sqrMagnitude > 0.0001f ? jitter.normalized : transform.forward;
                moveDir *= 0.35f;
            }
            else
            {
                var forward = toRoam.normalized;
                var lateral = new Vector3(-forward.z, 0f, forward.x) * roamLateralBias;
                moveDir = (forward + lateral).normalized;
            }

            moveDir = ApplyTankSeparation(moveDir);
            if (moveDir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var ticSize = flightProfile.ticSizeWorldUnits;
            var speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                config.MoveSpeedMilesPerSecond * roamSpeedScale,
                worldMap,
                ticSize);
            GroundUnitMovement.TryMoveOnLand(
                transform,
                moveDir.normalized * (speedWorld * Time.deltaTime),
                worldMap,
                worldUnitsPerMile);

            var facing = moveDir;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            }
        }

        private Vector3 ApplyTankSeparation(Vector3 desiredDir)
        {
            var separationWorld = config.separationMiles * worldUnitsPerMile;
            if (separationWorld <= 0.01f)
            {
                return desiredDir;
            }

            RefreshActiveTankCache();
            var separation = Vector3.zero;
            var selfPosition = transform.position;
            selfPosition.y = 0f;
            for (var i = 0; i < ActiveTanks.Count; i++)
            {
                var other = ActiveTanks[i];
                if (other == null || other == this)
                {
                    continue;
                }

                var otherTarget = other.GetComponent<LockableTarget>();
                if (otherTarget != null && !otherTarget.IsAlive)
                {
                    continue;
                }

                var otherPos = other.transform.position;
                otherPos.y = 0f;
                var away = selfPosition - otherPos;
                var distance = away.magnitude;
                if (distance < 0.01f || distance >= separationWorld)
                {
                    continue;
                }

                var strength = 1f - (distance / separationWorld);
                separation += away.normalized * strength;
            }

            if (separation.sqrMagnitude < 0.0001f)
            {
                return desiredDir;
            }

            return (desiredDir + separation * 1.4f).normalized;
        }

        private static void RefreshActiveTankCache()
        {
            if (separationCacheFrame == Time.frameCount)
            {
                return;
            }

            separationCacheFrame = Time.frameCount;
            ActiveTanks.Clear();
            var tanks = FindObjectsByType<BasicTankController>(FindObjectsSortMode.None);
            for (var i = 0; i < tanks.Length; i++)
            {
                if (tanks[i] != null)
                {
                    ActiveTanks.Add(tanks[i]);
                }
            }
        }

        private void PickRoamPoint(bool force)
        {
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var roll = Random.value;
            float radiusMiles;
            if (roll < 0.35f)
            {
                radiusMiles = Random.Range(config.roamRadiusMiles * 0.15f, config.roamRadiusMiles * 0.45f);
            }
            else if (roll < 0.7f)
            {
                radiusMiles = Random.Range(config.roamRadiusMiles * 0.45f, config.roamRadiusMiles * 0.75f);
            }
            else
            {
                radiusMiles = Random.Range(config.roamRadiusMiles * 0.75f, config.roamRadiusMiles);
            }

            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                * (radiusMiles * worldUnitsPerMile);
            roamPoint = homeWorld + offset;
            roamLateralBias = Random.Range(-0.65f, 0.65f);
            roamSpeedScale = Random.Range(0.65f, 1.15f);
            nextRoamRetargetTime = Time.time + (force
                ? Random.Range(0.15f, 0.45f)
                : Random.Range(config.roamRetargetMinSeconds, config.roamRetargetMaxSeconds));
        }

        private void UpdateAcquisitionWarning(float distanceMiles)
        {
            if (distanceMiles <= config.acquisitionRangeMiles)
            {
                if (!hasWarnedAcquisition)
                {
                    hasWarnedAcquisition = true;
                    MissileThreatNotifier.RegisterAcquisitionSource();
                    Debug.Log($"F-89: {UnitName} acquired aircraft.");
                }
            }
            else if (hasWarnedAcquisition)
            {
                ClearAcquisitionWarning();
            }
        }

        private void ClearAcquisitionWarning()
        {
            if (!hasWarnedAcquisition)
            {
                return;
            }

            hasWarnedAcquisition = false;
            MissileThreatNotifier.UnregisterAcquisitionSource();
        }

        private IEnumerator RunEngagementSequence()
        {
            // Stagger so the platoon does not open fire on the same frame.
            var startDelay = Random.Range(0f, Mathf.Max(0f, config.engagementStartStaggerMaxSeconds));
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            while (missilesRemaining > 0
                   && playerTarget != null
                   && playerTarget.IsAlive)
            {
                if (GetDistanceToPlayerMiles() > config.acquisitionRangeMiles)
                {
                    Debug.Log($"F-89: {UnitName} lost track of aircraft — holding fire.");
                    engagementStarted = false;
                    engagementRoutine = null;
                    yield break;
                }

                LaunchMissileAtPlayer();
                missilesRemaining--;
                yield return WaitForMissileResolution();

                if (missilesRemaining <= 0 || playerTarget == null || !playerTarget.IsAlive)
                {
                    engagementRoutine = null;
                    yield break;
                }

                var delay = Random.Range(
                    Mathf.Min(config.postShotDelayMinSeconds, config.postShotDelayMaxSeconds),
                    Mathf.Max(config.postShotDelayMinSeconds, config.postShotDelayMaxSeconds));
                yield return new WaitForSeconds(delay);
            }

            engagementRoutine = null;
        }

        private IEnumerator WaitForMissileResolution()
        {
            while (activeMissiles > 0)
            {
                yield return null;
            }
        }

        private void LaunchMissileAtPlayer()
        {
            if (playerTarget == null || !playerTarget.IsAlive)
            {
                return;
            }

            var launchOrigin = transform.position;
            launchOrigin.y = playerTarget.transform.position.y;
            var toPlayer = playerTarget.transform.position - launchOrigin;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                toPlayer = Vector3.forward;
            }

            activeMissiles++;
            var seeker = MissileSeekerSettings.FromEnemySam(missileConfig, playerTarget);
            HomingMissile.Launch(
                missileConfig,
                worldMap,
                flightProfile,
                launchOrigin + Vector3.up * 0.35f,
                toPlayer.normalized,
                playerTarget,
                lockedShot: true,
                new Color(0.78f, 0.22f, 0.08f),
                accuracyMultiplier: 1f,
                launchVelocityWorld: Vector3.zero,
                seekerSettings: seeker,
                onFlightComplete: OnMissileFlightComplete);

            Debug.Log($"F-89: {UnitName} launched missile ({missilesRemaining} remaining).");
        }

        private void OnMissileFlightComplete()
        {
            activeMissiles = Mathf.Max(0, activeMissiles - 1);
        }

        private float GetDistanceToPlayerMiles()
        {
            var ticSize = flightProfile.ticSizeWorldUnits;
            return CombatThreatRange.DistanceMiles(
                transform.position,
                playerTarget.transform.position,
                worldMap,
                ticSize);
        }

        private static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return (dx * dx) + (dz * dz);
        }

        private void OnDestroy()
        {
            ClearAcquisitionWarning();

            if (engagementRoutine != null)
            {
                StopCoroutine(engagementRoutine);
            }
        }
    }
}
