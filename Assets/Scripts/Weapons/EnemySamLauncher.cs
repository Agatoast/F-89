using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class EnemySamLauncher : MonoBehaviour
    {
        [SerializeField] private EnemySamMissileConfig missileConfig;
        [SerializeField] private float launchCooldownSeconds = 8f;

        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private LockableTarget playerTarget;
        private float cooldownRemaining;
        private bool hasWarnedAcquisition;

        public void Configure(
            EnemySamMissileConfig config,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            LockableTarget playerLockableTarget)
        {
            missileConfig = config;
            worldMap = mapConfig;
            flightProfile = profile;
            playerTarget = playerLockableTarget;
        }

        private void Update()
        {
            if (missileConfig == null
                || worldMap == null
                || flightProfile == null
                || playerTarget == null
                || !playerTarget.IsAlive)
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }

            var ticSize = flightProfile.ticSizeWorldUnits;
            var distanceMiles = CombatThreatRange.DistanceMiles(
                transform.position,
                playerTarget.transform.position,
                worldMap,
                ticSize);

            if (distanceMiles <= missileConfig.acquisitionRangeMiles)
            {
                if (!hasWarnedAcquisition)
                {
                    hasWarnedAcquisition = true;
                    MissileThreatNotifier.RegisterAcquisitionSource();
                    Debug.Log("F-89: MISSILE ACQUISITION — enemy SAM has locked the aircraft.");
                }
            }
            else if (hasWarnedAcquisition)
            {
                hasWarnedAcquisition = false;
                MissileThreatNotifier.UnregisterAcquisitionSource();
            }

            if (cooldownRemaining > 0f || distanceMiles > missileConfig.launchRangeMiles)
            {
                return;
            }

            LaunchMissile();
            cooldownRemaining = launchCooldownSeconds;
        }

        private void LaunchMissile()
        {
            var launchOrigin = transform.position;
            launchOrigin.y = playerTarget.transform.position.y;
            var toPlayer = playerTarget.transform.position - launchOrigin;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                toPlayer = Vector3.forward;
            }

            var forward = toPlayer.normalized;
            var seeker = MissileSeekerSettings.FromEnemySam(missileConfig, playerTarget);
            HomingMissile.Launch(
                missileConfig,
                worldMap,
                flightProfile,
                launchOrigin + Vector3.up * 0.35f,
                forward,
                playerTarget,
                lockedShot: true,
                new Color(0.92f, 0.18f, 0.08f),
                accuracyMultiplier: 1f,
                launchVelocityWorld: Vector3.zero,
                seekerSettings: seeker);

            Debug.Log("F-89: Enemy SAM launched.");
        }

        private void OnDestroy()
        {
            if (hasWarnedAcquisition)
            {
                MissileThreatNotifier.UnregisterAcquisitionSource();
                hasWarnedAcquisition = false;
            }
        }
    }
}
