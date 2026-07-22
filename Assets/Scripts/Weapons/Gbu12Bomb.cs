using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class Gbu12Bomb : MonoBehaviour
    {
        private Gbu12PavewayConfig config;
        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private Vector3 launchOrigin;
        private Vector3 dumbForward;
        private LockableTarget guidedTarget;
        private Vector3 lastGuidancePoint;
        private bool terminalGuidance;
        private float speedWorld;
        private float maxRangeWorld;
        private float maxTravelWorld;
        private float distanceTraveled;
        private Vector3 launchVelocity;
        private float terminalRangeWorld;
        private float flightTimeRemaining;
        private float accuracyMultiplier;
        private float blastRadiusWorld;

        public static Gbu12Bomb Drop(
            Gbu12PavewayConfig weaponConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            LockableTarget lockedTarget,
            Vector3 spawnPosition,
            Vector3 forward,
            float launchAccuracy,
            Vector3 launchVelocityWorld = default)
        {
            var bombObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bombObject.name = weaponConfig.WeaponName;
            bombObject.transform.localScale = new Vector3(0.35f, 0.35f, 0.9f);

            var collider = bombObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = bombObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = new Color(0.45f, 0.42f, 0.38f);
            }

            var bomb = bombObject.AddComponent<Gbu12Bomb>();
            bomb.Initialize(
                weaponConfig,
                mapConfig,
                profile,
                lockedTarget,
                spawnPosition,
                forward,
                launchAccuracy,
                launchVelocityWorld);
            return bomb;
        }

        private void Initialize(
            Gbu12PavewayConfig weaponConfig,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            LockableTarget lockedTarget,
            Vector3 spawnPosition,
            Vector3 forward,
            float launchAccuracy,
            Vector3 launchVelocityWorld)
        {
            config = weaponConfig;
            worldMap = mapConfig;
            flightProfile = profile;
            guidedTarget = lockedTarget;
            launchOrigin = spawnPosition;
            launchVelocity = Flatten(launchVelocityWorld);
            dumbForward = Flatten(forward).normalized;
            accuracyMultiplier = Mathf.Clamp01(launchAccuracy);
            distanceTraveled = 0f;

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                config.speedMilesPerSecond,
                worldMap,
                ticSize);
            maxRangeWorld = WorldMapConfig.RangeMilesToWorldUnits(
                config.rangeMiles,
                worldMap,
                ticSize);
            maxTravelWorld = maxRangeWorld;
            if (guidedTarget != null)
            {
                var initialTargetDistance = FlattenDistance(spawnPosition, guidedTarget.transform.position);
                maxTravelWorld = Mathf.Max(maxRangeWorld, initialTargetDistance * 1.08f);
            }

            var minimumFlightSeconds = speedWorld > 0f
                ? (maxTravelWorld / speedWorld) * 1.35f
                : config.maxFlightTimeSeconds;
            flightTimeRemaining = Mathf.Max(config.maxFlightTimeSeconds, minimumFlightSeconds);
            terminalRangeWorld = worldMap != null
                ? worldMap.MilesToTics(config.terminalGuidanceMiles) * ticSize
                : config.terminalGuidanceMiles * 20f * ticSize;
            blastRadiusWorld = config.blastRadiusTics * ticSize;

            transform.position = spawnPosition;
            lastGuidancePoint = guidedTarget != null
                ? guidedTarget.transform.position
                : spawnPosition + dumbForward * maxRangeWorld;
            if (Flatten(forward).sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(Flatten(forward).normalized, Vector3.up);
            }
        }

        private void Update()
        {
            flightTimeRemaining -= Time.deltaTime;
            if (flightTimeRemaining <= 0f)
            {
                Detonate(transform.position);
                return;
            }

            if (distanceTraveled >= maxTravelWorld)
            {
                Detonate(transform.position);
                return;
            }

            if (guidedTarget == null)
            {
                UpdateDumbFlight();
                return;
            }

            UpdateGuidedFlight();
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

        private void AdvanceFlight(Vector3 moveStep)
        {
            transform.position += moveStep;
            distanceTraveled += moveStep.magnitude;
        }

        private void UpdateDumbFlight()
        {
            var moveStep = dumbForward * (GetEffectiveSpeedWorld(dumbForward) * Time.deltaTime);
            AdvanceFlight(moveStep);
            transform.rotation = Quaternion.LookRotation(dumbForward, Vector3.up);
        }

        private void UpdateGuidedFlight()
        {
            UpdateGuidance();

            var destination = guidedTarget != null && guidedTarget.IsAlive
                ? guidedTarget.transform.position
                : lastGuidancePoint;

            var toDestination = Flatten(destination - transform.position);
            var travelDirection = toDestination.normalized;
            var stepSpeed = GetEffectiveSpeedWorld(travelDirection);
            if (toDestination.magnitude <= stepSpeed * Time.deltaTime + 0.05f)
            {
                Detonate(destination);
                return;
            }

            var moveStep = travelDirection * (stepSpeed * Time.deltaTime);
            AdvanceFlight(moveStep);
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
        }

        private void UpdateGuidance()
        {
            if (guidedTarget == null || !guidedTarget.IsAlive)
            {
                terminalGuidance = false;
                return;
            }

            var distanceToGuided = FlattenDistance(transform.position, guidedTarget.transform.position);
            terminalGuidance = distanceToGuided <= terminalRangeWorld;
            lastGuidancePoint = guidedTarget.transform.position;
        }

        private void Detonate(Vector3 center)
        {
            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            var scatter = (1f - accuracyMultiplier) * config.maxScatterTicsAtZeroAccuracy * ticSize;
            if (scatter > 0f)
            {
                center += new Vector3(Random.Range(-scatter, scatter), 0f, Random.Range(-scatter, scatter));
            }

            var intended = guidedTarget != null ? guidedTarget.TargetLabel : "none";
            var hits = 0;
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (!DirectFireTargetRules.CanBeDamaged(target)
                    || !target.MatchesWeapon(config.ValidTargetKind))
                {
                    continue;
                }

                if (FlattenDistance(center, target.transform.position) > blastRadiusWorld)
                {
                    continue;
                }

                target.RegisterHit(config.WeaponName, target == guidedTarget);
                hits++;
            }

            Debug.Log(
                $"{config.WeaponName} detonated near {intended}. {hits} target(s) hit. Accuracy at drop: {accuracyMultiplier:P0}.");
            Destroy(gameObject);
        }

        private static float FlattenDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
