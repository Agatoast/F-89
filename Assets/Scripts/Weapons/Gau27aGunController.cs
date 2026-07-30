using F89.Audio;
using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class Gau27aGunController : MonoBehaviour
    {
        [SerializeField] private Gau27aWeaponConfig config;
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Camera aimCamera;

        private float crosshairDistanceMiles;
        private float fireCooldown;
        private int roundsRemaining;
        private bool unlimitedAmmo;
        private Gau27FireSound fireSound;

        public float CrosshairDistanceMiles => crosshairDistanceMiles;
        public int RoundsRemaining => roundsRemaining;
        public Vector3 CrosshairWorldPoint { get; private set; }
        public Vector3 CrosshairScreenPoint { get; private set; }
        public bool HasTargetUnderCrosshair => GetTargetUnderCrosshair() != null;

        public void Configure(Gau27aWeaponConfig weaponConfig, AircraftController aircraftController, Camera camera)
        {
            config = weaponConfig;
            aircraft = aircraftController;
            aimCamera = camera;
            fireSound = GetComponent<Gau27FireSound>();
            roundsRemaining = config != null ? config.startingRounds : 0;
            ResetCrosshairDistance();
        }

        public void SetRounds(int rounds)
        {
            roundsRemaining = Mathf.Clamp(rounds, 0, AircraftLoadoutState.MaxGunRounds);
        }

        public void SetUnlimitedAmmo(bool enabled)
        {
            unlimitedAmmo = enabled;
            if (unlimitedAmmo)
            {
                roundsRemaining = 9999;
            }
        }

        public void ResetCrosshairDistance()
        {
            if (config == null || aircraft == null)
            {
                return;
            }

            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var forward = GetHorizontalForward();
            var defaultWorld = aircraft.transform.position
                + forward * MilesToWorldDistance(config.maxRangeMiles, ticSize);
            SetCrosshairToWorldPoint(defaultWorld);
        }

        public void SetCrosshairToWorldPoint(Vector3 worldPoint)
        {
            if (config == null || aircraft == null)
            {
                return;
            }

            var profile = aircraft.Profile;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            CrosshairWorldPoint = Gau27aOgiveEnvelope.ClampToOgive(
                aircraft.transform.position,
                GetHorizontalForward(),
                worldPoint,
                config,
                aircraft.WorldMap,
                ticSize);
            crosshairDistanceMiles = CombatThreatRange.DistanceMiles(
                aircraft.transform.position,
                CrosshairWorldPoint,
                aircraft.WorldMap,
                ticSize);
            UpdateCrosshairScreenPoint();
        }

        public void SetCrosshairDistanceMiles(float miles)
        {
            if (config == null || aircraft == null)
            {
                return;
            }

            miles = Mathf.Clamp(miles, config.minCrosshairMiles, config.ogiveMaxRangeMiles);
            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var forward = GetHorizontalForward();
            SetCrosshairToWorldPoint(
                aircraft.transform.position + forward * MilesToWorldDistance(miles, ticSize));
        }

        public void UpdateCrosshairFromMouse(Vector2 screenPosition)
        {
            if (aircraft == null || config == null)
            {
                return;
            }

            var camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            var profile = aircraft.Profile;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (!Gau27aOgiveEnvelope.TryResolveFromScreen(
                    camera,
                    screenPosition,
                    aircraft.transform.position,
                    GetHorizontalForward(),
                    config,
                    aircraft.WorldMap,
                    ticSize,
                    out var worldPoint))
            {
                UpdateCrosshairScreenPoint();
                return;
            }

            CrosshairWorldPoint = worldPoint;
            crosshairDistanceMiles = CombatThreatRange.DistanceMiles(
                aircraft.transform.position,
                CrosshairWorldPoint,
                aircraft.WorldMap,
                ticSize);
            UpdateCrosshairScreenPoint();
        }

        public void UpdateCrosshairPosition()
        {
            if (aircraft == null || config == null)
            {
                return;
            }

            var profile = aircraft.Profile;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            CrosshairWorldPoint = Gau27aOgiveEnvelope.ClampToOgive(
                aircraft.transform.position,
                GetHorizontalForward(),
                CrosshairWorldPoint,
                config,
                aircraft.WorldMap,
                ticSize);
            crosshairDistanceMiles = CombatThreatRange.DistanceMiles(
                aircraft.transform.position,
                CrosshairWorldPoint,
                aircraft.WorldMap,
                ticSize);
            UpdateCrosshairScreenPoint();
        }

        public void TryFire(float accuracyMultiplier, bool fireHeld)
        {
            if (!fireHeld || config == null || aircraft == null)
            {
                fireSound?.NotifyFireReleased();
                return;
            }

            if (!unlimitedAmmo && roundsRemaining <= 0)
            {
                fireSound?.NotifyFireReleased();
                return;
            }

            fireCooldown -= Time.deltaTime;
            if (fireCooldown > 0f)
            {
                return;
            }

            fireCooldown = config.roundsPerSecond > 0f ? 1f / config.roundsPerSecond : 0.1f;
            if (!unlimitedAmmo)
            {
                roundsRemaining--;
            }
            else
            {
                roundsRemaining = 9999;
            }

            var spawnPoint = aircraft.transform.position + GetHorizontalForward() * 0.6f;
            spawnPoint.y = 0.5f;
            var profile = aircraft.Profile;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var fireDestination = Gau27aOgiveEnvelope.ClampFireDestination(
                spawnPoint,
                CrosshairWorldPoint,
                config,
                aircraft.WorldMap,
                ticSize);

            GauRound.Fire(
                config,
                aircraft.Profile,
                aircraft.WorldMap,
                spawnPoint,
                fireDestination,
                GetHorizontalForward() * aircraft.CurrentSpeed);

            fireSound?.OnRoundFired();
        }

        public LockableTarget GetTargetUnderCrosshair()
        {
            if (config == null || aircraft == null)
            {
                return null;
            }

            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var dotRadius = config.crosshairDotRadiusTics * ticSize;
            var aimPoint = CrosshairWorldPoint;
            aimPoint.y = 0f;

            var targets = CombatThreatRange.GetCachedLockableTargets();
            return DirectFireTargetRules.FindGau27TargetUnderCrosshairDot(aimPoint, dotRadius, targets);
        }

        private void UpdateCrosshairScreenPoint()
        {
            var camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera != null)
            {
                CrosshairScreenPoint = camera.WorldToScreenPoint(CrosshairWorldPoint);
            }
        }

        private Vector3 GetHorizontalForward()
        {
            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return forward.normalized;
        }

        private float MilesToWorldDistance(float miles, float ticSize)
        {
            return WorldMapConfig.RangeMilesToWorldUnits(miles, aircraft.WorldMap, ticSize);
        }
    }
}
