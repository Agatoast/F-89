using F89.Core;
using F89.Flight;
using F89.UI;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Roaming movement and catalog combat for outpost vehicles and troops (same rules for both).
    /// </summary>
    public sealed class OutpostGroundUnitBehavior : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<OutpostGroundUnitBehavior> ActiveUnits =
            new(128);

        private static int separationCacheFrame = -1;

        private VehicleUnitDefinition definition;
        private WorldMapConfig worldMap;
        private FlightProfile flightProfile;
        private LockableTarget playerTarget;
        private float worldUnitsPerMile;
        private Vector3 homeWorld;
        private Vector3 roamPoint;
        private float nextRoamRetargetTime;
        private float roamLateralBias;
        private float roamSpeedScale = 1f;
        private float roamRadiusMiles = 0.85f;
        private float altitudeWorld;
        private float nextFireTime;
        private LockableTarget lockedCombatTarget;
        private bool lockedCombatUsesAir;

        public void Configure(
            VehicleUnitDefinition unitDefinition,
            WorldMapConfig mapConfig,
            FlightProfile profile,
            float unitsPerMile,
            LockableTarget playerLockableTarget,
            float roamRadiusMilesOverride = 0.85f)
        {
            definition = unitDefinition;
            worldMap = mapConfig;
            flightProfile = profile;
            worldUnitsPerMile = unitsPerMile;
            playerTarget = playerLockableTarget;
            roamRadiusMiles = roamRadiusMilesOverride;
            homeWorld = transform.position;
            homeWorld.y = 0f;

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            altitudeWorld = ResolveAltitudeWorld(definition, ticSize);

            var pos = transform.position;
            pos.y = altitudeWorld;
            transform.position = pos;

            nextFireTime = Time.time + VehicleCombatRules.RollFireDelaySeconds(definition);
            ClearCombatLock();
            PickRoamPoint(force: true);
        }

        private void OnEnable()
        {
            if (!ActiveUnits.Contains(this))
            {
                ActiveUnits.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveUnits.Remove(this);
        }

        private void Update()
        {
            if (definition == null
                || worldMap == null
                || flightProfile == null
                || GamePauseController.IsPaused)
            {
                return;
            }

            var selfTarget = GetComponent<LockableTarget>();
            if (selfTarget != null && !selfTarget.IsAlive)
            {
                return;
            }

            UpdateRoamMovement();
            TryCombat();
        }

        private void UpdateRoamMovement()
        {
            if (Time.time >= nextRoamRetargetTime
                || FlatDistanceSqr(transform.position, roamPoint) < 0.35f * 0.35f)
            {
                PickRoamPoint(force: false);
            }

            var position = transform.position;
            position.y = altitudeWorld;
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

            moveDir = ApplySeparation(moveDir);
            if (moveDir.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var ticSize = flightProfile.ticSizeWorldUnits;
            var speedMph = definition != null ? definition.speedMph : 30f;
            var speedWorld = WorldMapConfig.MilesPerSecondToWorldUnits(
                (speedMph / 3600f) * roamSpeedScale,
                worldMap,
                ticSize);

            if (definition != null && definition.isFlier)
            {
                transform.position = position + moveDir.normalized * (speedWorld * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, altitudeWorld, transform.position.z);
            }
            else
            {
                GroundUnitMovement.TryMoveOnLand(
                    transform,
                    moveDir.normalized * (speedWorld * Time.deltaTime),
                    worldMap,
                    worldUnitsPerMile);
                position = transform.position;
                position.y = 0f;
                transform.position = position;
            }

            var facing = moveDir;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            }
        }

        private void TryCombat()
        {
            if (!definition.IsHostile || Time.time < nextFireTime)
            {
                return;
            }

            var target = SelectCombatTarget(out var useAirCombat);
            if (target == null)
            {
                return;
            }

            if (useAirCombat)
            {
                TryAirShot(target);
            }
            else
            {
                TryGroundShot(target);
            }

            nextFireTime = Time.time + VehicleCombatRules.RollFireDelaySeconds(definition);
        }

        private LockableTarget SelectCombatTarget(out bool useAirCombat)
        {
            useAirCombat = false;

            if (lockedCombatTarget != null)
            {
                if (!lockedCombatTarget.IsAlive
                    || !IsValidLockedCombatTarget(lockedCombatTarget, lockedCombatUsesAir)
                    || !IsLockedCombatTargetInRange(lockedCombatTarget, lockedCombatUsesAir))
                {
                    ClearCombatLock();
                }
                else
                {
                    useAirCombat = lockedCombatUsesAir;
                    return lockedCombatTarget;
                }
            }

            var acquired = AcquireNewCombatTarget(out useAirCombat);
            if (acquired != null)
            {
                lockedCombatTarget = acquired;
                lockedCombatUsesAir = useAirCombat;
            }

            return acquired;
        }

        private LockableTarget AcquireNewCombatTarget(out bool useAirCombat)
        {
            useAirCombat = false;

            if (playerTarget != null
                && playerTarget.IsAlive
                && definition.planeVsOtherTargetChance > 0f
                && Random.value <= definition.planeVsOtherTargetChance
                && IsWithinAirRange(playerTarget))
            {
                useAirCombat = true;
                return playerTarget;
            }

            var groundTarget = FindClosestGroundTarget();
            if (groundTarget != null)
            {
                return groundTarget;
            }

            if (playerTarget != null && playerTarget.IsAlive && IsWithinAirRange(playerTarget))
            {
                useAirCombat = true;
                return playerTarget;
            }

            return null;
        }

        private void ClearCombatLock()
        {
            lockedCombatTarget = null;
            lockedCombatUsesAir = false;
        }

        private bool IsValidLockedCombatTarget(LockableTarget target, bool useAirCombat)
        {
            if (useAirCombat)
            {
                return IsValidAirCombatTarget(target);
            }

            return IsValidGroundCombatTarget(target);
        }

        private bool IsLockedCombatTargetInRange(LockableTarget target, bool useAirCombat)
        {
            if (target == null)
            {
                return false;
            }

            return useAirCombat ? IsWithinAirRange(target) : IsWithinGroundRange(target);
        }

        private bool IsValidAirCombatTarget(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFlareDecoy
                || target.GetComponent<AntarcticaBase>() != null)
            {
                return false;
            }

            if (definition.IsHostile)
            {
                return target.IsPlayerAircraft
                    || ((target.Affiliation == TargetAffiliation.Hostile || target.IsNeutral)
                        && (target.TargetKind == LockableTargetKind.Air || target.IsFlier));
            }

            return target.Affiliation == TargetAffiliation.Hostile
                && (target.TargetKind == LockableTargetKind.Air
                    || target.IsFlier
                    || target.IsPlayerAircraft);
        }

        private bool IsWithinGroundRange(LockableTarget target)
        {
            if (target == null || definition.groundRangeTics <= 0f)
            {
                return false;
            }

            return HorizontalDistanceTics(transform.position, target.transform.position) <= definition.groundRangeTics;
        }

        private void TryAirShot(LockableTarget target)
        {
            GroundUnitAirMissileLauncher.LaunchAtAirTarget(
                definition,
                transform,
                target,
                worldMap,
                flightProfile);
        }

        private void TryGroundShot(LockableTarget target)
        {
            if (!VehicleCombatRules.RollGroundHit(definition))
            {
                Debug.Log(
                    $"F-89: {definition.abbreviation} missed {target.TargetLabel} (ground shot, {definition.chanceToHitGround:P0}).");
                return;
            }

            if (definition.groundHitDamage > 0)
            {
                target.ApplyGroundDamage(definition.groundHitDamage, definition.abbreviation, wasLockedShot: false);
                return;
            }

            if (!VehicleCombatRules.RollKillOnHit(definition))
            {
                Debug.Log(
                    $"F-89: {definition.abbreviation} hit {target.TargetLabel} but failed kill roll ({definition.chanceToKill:P0}).");
                return;
            }

            var damage = target.HasGroundHitPoints
                ? Mathf.Max(1, target.CurrentGroundHitPoints)
                : 1;
            target.ApplyGroundDamage(damage, definition.abbreviation, wasLockedShot: false);
        }

        private LockableTarget FindClosestGroundTarget()
        {
            if (definition.groundRangeTics <= 0f)
            {
                return null;
            }

            LockableTarget closest = null;
            var closestDistance = float.MaxValue;
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (!IsValidGroundCombatTarget(target))
                {
                    continue;
                }

                var distanceTics = HorizontalDistanceTics(transform.position, target.transform.position);
                if (distanceTics > definition.groundRangeTics || distanceTics >= closestDistance)
                {
                    continue;
                }

                closestDistance = distanceTics;
                closest = target;
            }

            return closest;
        }

        private bool IsValidGroundCombatTarget(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsPlayerAircraft
                || target.IsFlareDecoy
                || target.GetComponent<AntarcticaBase>() != null)
            {
                return false;
            }

            if (target.IsFriendly || target.IsNeutral)
            {
                return false;
            }

            return target.TargetKind == LockableTargetKind.Ground
                || target.IsInfantry
                || target.IsGroundVehicle;
        }

        private bool IsWithinAirRange(LockableTarget target)
        {
            if (target == null || definition.airRangeTics <= 0f)
            {
                return false;
            }

            return AirDistanceTics(transform.position, target.transform.position) <= definition.airRangeTics;
        }

        private float HorizontalDistanceTics(Vector3 a, Vector3 b)
        {
            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b) / ticSize;
        }

        private float AirDistanceTics(Vector3 a, Vector3 b)
        {
            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            return Vector3.Distance(a, b) / ticSize;
        }

        private Vector3 ApplySeparation(Vector3 desiredDir)
        {
            if (definition != null && definition.fliesOverGroundUnits)
            {
                return desiredDir;
            }

            var ticSize = flightProfile != null ? flightProfile.ticSizeWorldUnits : 1f;
            var separationWorld = OutpostGroundRules.MinCenterSeparationWorld(ticSize);
            RefreshActiveUnitCache();

            var separation = Vector3.zero;
            var selfPosition = transform.position;
            selfPosition.y = 0f;

            for (var i = 0; i < ActiveUnits.Count; i++)
            {
                var other = ActiveUnits[i];
                if (other == null || other == this || !ShouldSeparateFrom(other))
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

            if (OutpostBuildingCluster.TryGetKeepOutPush(selfPosition, out var keepOutPush, out _))
            {
                separation += keepOutPush * 1.6f;
            }

            if (separation.sqrMagnitude < 0.0001f)
            {
                return desiredDir;
            }

            return (desiredDir + separation * 1.4f).normalized;
        }

        private bool ShouldSeparateFrom(OutpostGroundUnitBehavior other)
        {
            if (other?.definition == null || definition == null)
            {
                return true;
            }

            // Ground units ignore helicopters flying overhead; helicopters ignore everyone below.
            if (definition.fliesOverGroundUnits && !other.definition.isFlier)
            {
                return false;
            }

            if (other.definition.fliesOverGroundUnits && !definition.isFlier)
            {
                return false;
            }

            return true;
        }

        private static float ResolveAltitudeWorld(VehicleUnitDefinition unitDefinition, float ticSize)
        {
            if (unitDefinition == null || !unitDefinition.isFlier)
            {
                return 0f;
            }

            var altitudeTacs = unitDefinition.fliesOverGroundUnits
                ? OutpostGroundRules.HelicopterCruiseAltitudeTacs
                : 1.5f;
            return TacScale.TacsToWorld(altitudeTacs, ticSize);
        }

        private static void RefreshActiveUnitCache()
        {
            if (separationCacheFrame == Time.frameCount)
            {
                return;
            }

            separationCacheFrame = Time.frameCount;
            ActiveUnits.RemoveAll(unit => unit == null);
        }

        private void PickRoamPoint(bool force)
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var roll = Random.value;
                float radiusMiles;
                if (roll < 0.35f)
                {
                    radiusMiles = Random.Range(roamRadiusMiles * 0.15f, roamRadiusMiles * 0.45f);
                }
                else if (roll < 0.7f)
                {
                    radiusMiles = Random.Range(roamRadiusMiles * 0.45f, roamRadiusMiles * 0.75f);
                }
                else
                {
                    radiusMiles = Random.Range(roamRadiusMiles * 0.75f, roamRadiusMiles);
                }

                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle))
                    * (radiusMiles * worldUnitsPerMile);
                var candidate = homeWorld + offset;

                if (definition != null && definition.isFlier)
                {
                    roamPoint = candidate;
                    roamPoint.y = altitudeWorld;
                    ScheduleRetarget(force);
                    return;
                }

                if (GroundUnitMovement.IsAllowedWorldPosition(candidate, worldMap, worldUnitsPerMile))
                {
                    roamPoint = candidate;
                    ScheduleRetarget(force);
                    return;
                }
            }

            roamPoint = transform.position;
            roamPoint.y = altitudeWorld;
            ScheduleRetarget(force);
        }

        private void ScheduleRetarget(bool force)
        {
            roamLateralBias = Random.Range(-0.65f, 0.65f);
            roamSpeedScale = Random.Range(0.65f, 1.15f);
            nextRoamRetargetTime = Time.time + (force
                ? Random.Range(0.15f, 0.45f)
                : Random.Range(1.5f, 4f));
        }

        private static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return (dx * dx) + (dz * dz);
        }
    }
}
