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
            crosshairDistanceMiles = config != null ? config.maxRangeMiles : 2f;
            roundsRemaining = config != null ? config.startingRounds : 0;
            UpdateCrosshairPosition();
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
            if (config != null)
            {
                crosshairDistanceMiles = config.maxRangeMiles;
            }
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

            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var forward = GetHorizontalForward();
            var origin = aircraft.transform.position;

            var minWorld = MilesToWorldDistance(config.minCrosshairMiles, ticSize);
            var maxWorld = MilesToWorldDistance(config.maxRangeMiles, ticSize);

            var nearPoint = origin + forward * minWorld;
            var farPoint = origin + forward * maxWorld;
            nearPoint.y = 0.5f;
            farPoint.y = 0.5f;

            var nearScreen = camera.WorldToScreenPoint(nearPoint);
            var farScreen = camera.WorldToScreenPoint(farPoint);
            if (nearScreen.z < 0f && farScreen.z < 0f)
            {
                UpdateCrosshairPosition();
                return;
            }

            var lineStart = new Vector2(nearScreen.x, nearScreen.y);
            var lineEnd = new Vector2(farScreen.x, farScreen.y);
            var slideT = ProjectOntoLineSegment(screenPosition, lineStart, lineEnd);
            crosshairDistanceMiles = Mathf.Lerp(
                config.minCrosshairMiles,
                config.maxRangeMiles,
                slideT);
            UpdateCrosshairPosition();
        }

        public void UpdateCrosshairPosition()
        {
            if (aircraft == null || config == null)
            {
                return;
            }

            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var distanceWorld = MilesToWorldDistance(crosshairDistanceMiles, ticSize);

            var forward = GetHorizontalForward();
            var crosshairWorld = aircraft.transform.position + forward * distanceWorld;
            crosshairWorld.y = 0.5f;
            CrosshairWorldPoint = crosshairWorld;

            var camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera != null)
            {
                CrosshairScreenPoint = camera.WorldToScreenPoint(CrosshairWorldPoint);
            }
        }

        public void TryFire(float accuracyMultiplier, bool fireHeld)
        {
            if (!fireHeld || config == null || aircraft == null)
            {
                return;
            }

            if (!unlimitedAmmo && roundsRemaining <= 0)
            {
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

            GauRound.Fire(
                config,
                aircraft.Profile,
                aircraft.WorldMap,
                spawnPoint,
                CrosshairWorldPoint,
                accuracyMultiplier,
                GetHorizontalForward() * aircraft.CurrentSpeed);
        }

        public LockableTarget GetTargetUnderCrosshair()
        {
            if (config == null || aircraft == null)
            {
                return null;
            }

            var ticSize = aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f;
            var hitRadius = config.hitRadiusTics * ticSize;
            var aimPoint = CrosshairWorldPoint;
            aimPoint.y = 0f;

            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            return DirectFireTargetRules.FindClosestAtPoint(aimPoint, hitRadius, targets);
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

        private static float ProjectOntoLineSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
        {
            var segment = segmentEnd - segmentStart;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f)
            {
                return 0f;
            }

            var t = Vector2.Dot(point - segmentStart, segment) / lengthSquared;
            return Mathf.Clamp01(t);
        }
    }
}
