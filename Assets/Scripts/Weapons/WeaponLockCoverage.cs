using UnityEngine;

namespace F89.Weapons
{
    public static class WeaponLockCoverage
    {
        public static bool IsWithinLockCoverage(
            Vector3 craftPosition,
            Vector3 craftForward,
            Vector3 targetPosition,
            WeaponAimMode aimMode,
            float forwardLockHalfAngleDegrees)
        {
            if (aimMode != WeaponAimMode.ForwardLock)
            {
                return true;
            }

            var toTarget = targetPosition - craftPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            var forward = craftForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            return Vector3.Angle(forward.normalized, toTarget.normalized) <= forwardLockHalfAngleDegrees;
        }
    }
}
