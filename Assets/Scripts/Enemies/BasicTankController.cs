using System.Collections;
using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    public class BasicTankController : MonoBehaviour
    {
        private BasicTankConfig config;
        private EnemySamMissileConfig missileConfig;
        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private LockableTarget playerTarget;
        private float worldUnitsPerMile;

        private int missilesRemaining;
        private int activeSalvoMissiles;
        private bool hasWarnedAcquisition;
        private bool isRetreating;
        private bool engagementStarted;
        private Coroutine engagementRoutine;

        public string UnitName => config != null ? config.unitName : BasicTankConfig.DefaultUnitName;
        public int MissilesRemaining => missilesRemaining;

        public void Configure(
            BasicTankConfig tankConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            LockableTarget playerLockableTarget,
            float unitsPerMile)
        {
            config = tankConfig != null
                ? tankConfig
                : ScriptableObject.CreateInstance<BasicTankConfig>();
            worldMap = mapConfig;
            flightProfile = profile;
            playerTarget = playerLockableTarget;
            worldUnitsPerMile = unitsPerMile;
            missilesRemaining = config.missileCapacity;
            missileConfig = BuildMissileConfig();

            BasicTankVisual.AttachVisual(transform, flightProfile);
            EnsureLockableTarget();
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
                || playerTarget == null
                || !playerTarget.IsAlive)
            {
                return;
            }

            var tankTarget = GetComponent<LockableTarget>();
            if (tankTarget == null || !tankTarget.IsAlive)
            {
                ClearAcquisitionWarning();
                return;
            }

            var distanceMiles = GetDistanceToPlayerMiles();
            UpdateAcquisitionWarning(distanceMiles);

            if (missilesRemaining <= 0)
            {
                return;
            }

            if (!engagementStarted && distanceMiles <= config.launchRangeMiles)
            {
                engagementStarted = true;
                isRetreating = true;
                engagementRoutine = StartCoroutine(RunEngagementSequence());
            }

            if (isRetreating)
            {
                RetreatFromPlayer();
            }
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
            Debug.Log($"F-89: {UnitName} launching first salvo.");
            yield return LaunchSalvo(config.missilesPerSalvo);
            yield return WaitForSalvoResolution();

            if (missilesRemaining <= 0 || playerTarget == null || !playerTarget.IsAlive)
            {
                yield break;
            }

            if (GetDistanceToPlayerMiles() > config.acquisitionRangeMiles)
            {
                Debug.Log($"F-89: {UnitName} lost track of aircraft before second salvo.");
                yield break;
            }

            Debug.Log($"F-89: {UnitName} reacquired aircraft — launching second salvo.");
            yield return LaunchSalvo(config.missilesPerSalvo);
        }

        private IEnumerator LaunchSalvo(int salvoSize)
        {
            var launches = Mathf.Min(salvoSize, missilesRemaining);
            for (var i = 0; i < launches; i++)
            {
                LaunchMissileAtPlayer();
                missilesRemaining--;

                if (i < launches - 1)
                {
                    yield return new WaitForSeconds(config.salvoLaunchIntervalSeconds);
                }
            }
        }

        private IEnumerator WaitForSalvoResolution()
        {
            while (activeSalvoMissiles > 0)
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

            activeSalvoMissiles++;
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
                onFlightComplete: OnSalvoMissileComplete);

            Debug.Log($"F-89: {UnitName} launched missile ({missilesRemaining} remaining).");
        }

        private void OnSalvoMissileComplete()
        {
            activeSalvoMissiles = Mathf.Max(0, activeSalvoMissiles - 1);
        }

        private void RetreatFromPlayer()
        {
            if (playerTarget == null)
            {
                return;
            }

            var away = transform.position - playerTarget.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = -transform.forward;
            }

            away.Normalize();
            var ticSize = flightProfile.ticSizeWorldUnits;
            var speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                config.RetreatSpeedMilesPerSecond,
                worldMap,
                ticSize);
            GroundUnitMovement.TryMoveOnLand(
                transform,
                away * (speedWorld * Time.deltaTime),
                worldMap,
                worldUnitsPerMile);
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
