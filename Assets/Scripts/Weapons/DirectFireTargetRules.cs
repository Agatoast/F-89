using F89.Core;
using UnityEngine;

namespace F89.Weapons
{
    public static class DirectFireTargetRules
    {
        public static bool CanBeDamaged(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFlareDecoy
                || target.IsPlayerAircraft)
            {
                return false;
            }

            return target.GetComponent<AntarcticaBase>() == null;
        }

        public static LockableTarget FindClosestAtPoint(
            Vector3 aimPoint,
            float weaponHitRadiusWorld,
            LockableTarget[] targets)
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

                if (target.IsGroundVehicle)
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

            return closestVehicle != null ? closestVehicle : closest;
        }
    }
}
