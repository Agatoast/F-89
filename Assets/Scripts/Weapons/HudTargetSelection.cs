using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Weapons
{
    public static class HudTargetSelection
    {
        public static LockableTarget FindTargetAtScreenPosition(
            Camera camera,
            Vector2 screenPosition,
            LockableTargetKind? requiredKind = null,
            System.Func<LockableTarget, bool> extraFilter = null,
            System.Func<LockableTarget, int> prioritySelector = null)
        {
            if (camera == null)
            {
                return null;
            }

            var fromMarker = FindTargetUnderHudMarker(
                camera,
                screenPosition,
                requiredKind,
                extraFilter,
                prioritySelector);
            if (fromMarker != null)
            {
                return fromMarker;
            }

            return FindTargetUnderRaycast(
                camera,
                screenPosition,
                requiredKind,
                extraFilter,
                prioritySelector);
        }

        private static LockableTarget FindTargetUnderHudMarker(
            Camera camera,
            Vector2 screenPosition,
            LockableTargetKind? requiredKind,
            System.Func<LockableTarget, bool> extraFilter,
            System.Func<LockableTarget, int> prioritySelector)
        {
            var half = (HudTargetMarkerLayout.SquareSize + HudTargetMarkerLayout.PickPadding) * 0.5f;
            var targets = CombatThreatRange.GetCachedLockableTargets();
            LockableTarget best = null;
            var bestDistanceSq = float.MaxValue;
            var bestPriority = int.MaxValue;
            var guiPoint = HudTargetMarkerLayout.ScreenToGui(screenPosition);

            foreach (var target in targets)
            {
                if (!IsCandidate(target, requiredKind, extraFilter))
                {
                    continue;
                }

                if (!HudTargetMarkerLayout.TryGetGuiCenter(camera, target.transform.position, out var guiCenter))
                {
                    continue;
                }

                if (Mathf.Abs(guiPoint.x - guiCenter.x) > half || Mathf.Abs(guiPoint.y - guiCenter.y) > half)
                {
                    continue;
                }

                var priority = prioritySelector != null ? prioritySelector(target) : 0;
                var distanceSq = (guiPoint - guiCenter).sqrMagnitude;
                if (priority > bestPriority || (priority == bestPriority && distanceSq >= bestDistanceSq))
                {
                    continue;
                }

                bestPriority = priority;
                bestDistanceSq = distanceSq;
                best = target;
            }

            return best;
        }

        private static LockableTarget FindTargetUnderRaycast(
            Camera camera,
            Vector2 screenPosition,
            LockableTargetKind? requiredKind,
            System.Func<LockableTarget, bool> extraFilter,
            System.Func<LockableTarget, int> prioritySelector)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 100000f);
            LockableTarget best = null;
            var bestHitDistance = float.MaxValue;
            var bestPriority = int.MaxValue;

            foreach (var hit in hits)
            {
                var target = hit.collider.GetComponentInParent<LockableTarget>();
                if (!IsCandidate(target, requiredKind, extraFilter))
                {
                    continue;
                }

                var priority = prioritySelector != null ? prioritySelector(target) : 0;
                if (priority > bestPriority || (priority == bestPriority && hit.distance >= bestHitDistance))
                {
                    continue;
                }

                bestPriority = priority;
                bestHitDistance = hit.distance;
                best = target;
            }

            return best;
        }

        private static bool IsCandidate(
            LockableTarget target,
            LockableTargetKind? requiredKind,
            System.Func<LockableTarget, bool> extraFilter)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (requiredKind.HasValue && !target.MatchesWeapon(requiredKind.Value))
            {
                return false;
            }

            return extraFilter == null || extraFilter(target);
        }
    }
}
