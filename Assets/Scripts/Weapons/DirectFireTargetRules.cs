using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    public enum DirectFireTargetPriority
    {
        ClosestAny,
        GroundVehiclesFirst
    }

    public static class DirectFireTargetRules
    {
        /// <summary>
        /// GAU-27A may damage hostile air and ground targets (not flares, player, or carrier).
        /// </summary>
        public static bool CanBeDamagedByGau27(LockableTarget target)
        {
            return CanBeDamaged(target);
        }

        public static bool CanBeDamaged(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFlareDecoy
                || target.IsPlayerAircraft)
            {
                return false;
            }

            var baseSite = target.GetComponent<AntarcticaBase>();
            // Neutral land outposts are destructible targets; only the carrier remains protected.
            return baseSite == null || baseSite.SiteKind != BaseSiteKind.Carrier;
        }

        public static LockableTarget FindClosestAtPoint(
            Vector3 aimPoint,
            float weaponHitRadiusWorld,
            LockableTarget[] targets)
        {
            return FindClosestAtPoint(aimPoint, weaponHitRadiusWorld, targets, DirectFireTargetPriority.GroundVehiclesFirst);
        }

        public static LockableTarget FindGau27TargetUnderCrosshairDot(
            Vector3 aimPoint,
            float dotRadiusWorld,
            LockableTarget[] targets)
        {
            return FindClosestAtPoint(aimPoint, dotRadiusWorld, targets, DirectFireTargetPriority.ClosestAny);
        }

        public static LockableTarget FindClosestGau27TargetAtPoint(
            Vector3 aimPoint,
            float weaponHitRadiusWorld,
            LockableTarget[] targets)
        {
            return FindClosestAtPoint(aimPoint, weaponHitRadiusWorld, targets, DirectFireTargetPriority.ClosestAny);
        }

        public static LockableTarget FindClosestAtPoint(
            Vector3 aimPoint,
            float weaponHitRadiusWorld,
            LockableTarget[] targets,
            DirectFireTargetPriority priority)
        {
            aimPoint.y = 0f;

            LockableTarget closest = null;
            LockableTarget closestVehicle = null;
            var closestDistance = float.MaxValue;
            var closestVehicleDistance = float.MaxValue;

            foreach (var target in targets)
            {
                if (!CanBeDamaged(target))
                {
                    continue;
                }

                var targetPosition = target.transform.position;
                targetPosition.y = 0f;
                var distance = Vector3.Distance(targetPosition, aimPoint);
                var targetRadius = target.GetHitRadiusWorld();
                if (distance > weaponHitRadiusWorld + targetRadius)
                {
                    continue;
                }

                if (priority == DirectFireTargetPriority.GroundVehiclesFirst && target.IsGroundVehicle)
                {
                    if (distance < closestVehicleDistance)
                    {
                        closestVehicleDistance = distance;
                        closestVehicle = target;
                    }

                    continue;
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = target;
                }
            }

            if (priority == DirectFireTargetPriority.GroundVehiclesFirst)
            {
                return closestVehicle != null ? closestVehicle : closest;
            }

            return closest;
        }
    }
}
