using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Canonical plane-weapon Ground Hit Point (GHP) damage definitions.
    /// Troop MaxGHP is <see cref="GroundTargetGhp.Troop"/> (US and UR). Building/vehicle MaxGHP TBD.
    /// </summary>
    public static class PlaneWeaponGhp
    {
        public const int Gau27PerHit = 1;

        /// <summary>AIM-9z air GHP per hit on aerial targets.</summary>
        public const int Aim9zAirHit = 5;

        public const int Agm114VehicleHit = 5;

        public const int Agm88Hit = 3;
        /// <summary>Hit tic plus each surrounding tic (including diagonals) → 3×3.</summary>
        public const int Agm88BlastChebyshevTics = 1;

        public const int Gbu12BuildingOrTroopHit = 6;
        public const int Gbu12VehicleHit = 3;
        /// <summary>Hit tic plus 2 tics around including diagonals → 5×5.</summary>
        public const int Gbu12BlastChebyshevTics = 2;

        public static bool IsBuilding(LockableTarget target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.UnitClass == TargetUnitClass.Building)
            {
                return true;
            }

            if (target.GetComponent<OutpostBuilding>() != null)
            {
                return true;
            }

            var baseSite = target.GetComponent<AntarcticaBase>();
            return baseSite != null && baseSite.SiteKind == BaseSiteKind.Land;
        }

        public static bool IsVehicle(LockableTarget target)
        {
            return target != null && target.IsGroundVehicle;
        }

        public static bool IsFlier(LockableTarget target)
        {
            return target != null && target.IsFlier;
        }

        public static bool IsTroop(LockableTarget target)
        {
            return target != null && target.IsInfantry;
        }

        public static bool CanBeDamagedByAim9z(LockableTarget target)
        {
            if (target == null || !target.IsAlive || target.IsFlareDecoy)
            {
                return false;
            }

            // Air-to-air only — never ground vehicles, troops, buildings, or outposts.
            return target.TargetKind == LockableTargetKind.Air;
        }

        public static void ApplyAim9zHit(
            LockableTarget target,
            Aim9zWeaponConfig config,
            bool wasLockedShot)
        {
            if (!CanBeDamagedByAim9z(target))
            {
                if (target != null && target.TargetKind == LockableTargetKind.Ground)
                {
                    Debug.Log($"AIM-9z cannot damage ground target {target.TargetLabel}.");
                }

                return;
            }

            var damage = config != null ? config.airDamagePerHit : Aim9zAirHit;
            target.ApplyAirDamage(damage, config != null ? config.WeaponName : "AIM-9z", wasLockedShot);
        }

        public static void ApplyGau27Hit(LockableTarget target, bool wasLockedShot = false)
        {
            if (!DirectFireTargetRules.CanBeDamagedByGau27(target))
            {
                return;
            }

            target.ApplyGroundDamage(Gau27PerHit, "GAU-27A", wasLockedShot);
        }

        public static void ApplyAgm114Hit(LockableTarget target, bool wasLockedShot)
        {
            if (!DirectFireTargetRules.CanBeDamaged(target))
            {
                return;
            }

            if (IsBuilding(target))
            {
                Debug.Log($"AGM-114 Hellfire hit {target.TargetLabel} but deals no building GHP.");
                return;
            }

            if (!IsVehicle(target) && !IsTroop(target))
            {
                Debug.Log($"AGM-114 Hellfire hit {target.TargetLabel} but only damages vehicles and troops.");
                return;
            }

            target.ApplyGroundDamage(Agm114VehicleHit, "AGM-114 Hellfire", wasLockedShot);
        }

        public static void ApplyAgm88Impact(
            Vector3 impactWorld,
            LockableTarget primaryTarget,
            float ticSizeWorldUnits,
            bool wasLockedShot)
        {
            var targets = CombatThreatRange.GetCachedLockableTargets();
            var hits = 0;
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (!DirectFireTargetRules.CanBeDamaged(target) || IsBuilding(target))
                {
                    continue;
                }

                if (!IsVehicle(target) && !IsTroop(target))
                {
                    continue;
                }

                var inBlast = TicGrid.IsWithinChebyshev(
                    impactWorld,
                    target.transform.position,
                    ticSizeWorldUnits,
                    Agm88BlastChebyshevTics);
                var isPrimary = target == primaryTarget;
                if (!inBlast && !isPrimary)
                {
                    continue;
                }

                target.ApplyGroundDamage(Agm88Hit, "AGM-88J SiAW", wasLockedShot || isPrimary);
                hits++;
            }

            if (primaryTarget != null && IsBuilding(primaryTarget))
            {
                Debug.Log("AGM-88J SiAW may lock buildings but deals no building GHP.");
            }

            Debug.Log($"AGM-88J SiAW tic blast ({Agm88BlastChebyshevTics}): {hits} vehicle/troop target(s) damaged.");
        }

        public static void ApplyGbu12Detonation(Vector3 impactWorld, float ticSizeWorldUnits, bool wasLockedShot)
        {
            var targets = CombatThreatRange.GetCachedLockableTargets();
            var lockableHits = 0;
            var buildingHits = 0;
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (!DirectFireTargetRules.CanBeDamaged(target))
                {
                    continue;
                }

                if (!TicGrid.IsWithinChebyshev(
                        impactWorld,
                        target.transform.position,
                        ticSizeWorldUnits,
                        Gbu12BlastChebyshevTics))
                {
                    continue;
                }

                var damage = ResolveGbu12Damage(target);
                if (damage <= 0)
                {
                    continue;
                }

                target.ApplyGroundDamage(damage, "GBU-12 Paveway II", wasLockedShot);
                lockableHits++;
                if (IsBuilding(target))
                {
                    buildingHits++;
                }
            }

            Debug.Log(
                $"GBU-12 tic blast ({Gbu12BlastChebyshevTics}): {lockableHits} lockable(s), {buildingHits} building(s).");
        }

        public static int ResolveGbu12Damage(LockableTarget target)
        {
            if (IsVehicle(target))
            {
                return Gbu12VehicleHit;
            }

            if (IsBuilding(target) || IsTroop(target))
            {
                return Gbu12BuildingOrTroopHit;
            }

            if (target != null && target.TargetKind == LockableTargetKind.Ground)
            {
                return Gbu12BuildingOrTroopHit;
            }

            return 0;
        }

        public static bool TryApplyMissileLockedHit(
            IMissileWeaponConfig config,
            LockableTarget target,
            FlightProfile profile,
            bool wasLockedShot)
        {
            if (config is Agm114HellfireWeaponConfig)
            {
                ApplyAgm114Hit(target, wasLockedShot);
                return true;
            }

            if (config is Agm88jSiawWeaponConfig)
            {
                var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
                var impact = target != null ? target.transform.position : Vector3.zero;
                ApplyAgm88Impact(impact, target, ticSize, wasLockedShot);
                return true;
            }

            if (config is Aim9zWeaponConfig aim9z)
            {
                ApplyAim9zHit(target, aim9z, wasLockedShot);
                return true;
            }

            return false;
        }

        public static bool TryApplyMissileImpactBlast(
            IMissileWeaponConfig config,
            Vector3 center,
            float blastRadiusWorld,
            float accuracyMultiplier,
            FlightProfile profile)
        {
            if (config is Agm114HellfireWeaponConfig)
            {
                var targets = CombatThreatRange.GetCachedLockableTargets();
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    if (!DirectFireTargetRules.CanBeDamaged(target)
                        || !target.MatchesWeapon(config.ValidTargetKind)
                        || FlattenDistance(center, target.transform.position) > blastRadiusWorld
                        || Random.value > accuracyMultiplier)
                    {
                        continue;
                    }

                    ApplyAgm114Hit(target, wasLockedShot: false);
                }

                return true;
            }

            if (config is Agm88jSiawWeaponConfig)
            {
                var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
                ApplyAgm88Impact(center, null, ticSize, wasLockedShot: false);
                return true;
            }

            if (config is Aim9zWeaponConfig aim9z)
            {
                var targets = CombatThreatRange.GetCachedLockableTargets();
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    if (!CanBeDamagedByAim9z(target)
                        || FlattenDistance(center, target.transform.position) > blastRadiusWorld
                        || Random.value > accuracyMultiplier)
                    {
                        continue;
                    }

                    ApplyAim9zHit(target, aim9z, wasLockedShot: false);
                }

                return true;
            }

            return false;
        }

        private static float FlattenDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
