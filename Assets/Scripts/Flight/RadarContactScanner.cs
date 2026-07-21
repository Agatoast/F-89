using System.Collections.Generic;
using F89.Core;
using F89.Weapons;
using UnityEngine;

namespace F89.Flight
{
    public struct RadarContact
    {
        public Vector3 WorldPosition;
        public float DistanceMiles;
        public bool IsHostile;
        public bool IsBase;
        public string Label;
        public LockableTarget Target;
        public AntarcticaBase BaseSite;
    }

    public static class RadarContactScanner
    {
        public const float RangeMiles = 150f;
        public const float HostileDetectionMiles = 50f;

        public static void CollectVisibleContacts(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            List<RadarContact> results)
        {
            results.Clear();
            if (worldMap == null)
            {
                return;
            }

            CollectLockableTargets(observerPosition, worldMap, ticSizeWorldUnits, results);
            CollectBases(observerPosition, worldMap, ticSizeWorldUnits, results);
        }

        private static void CollectLockableTargets(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            List<RadarContact> results)
        {
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive)
                {
                    continue;
                }

                var distanceMiles = CombatThreatRange.DistanceMiles(
                    observerPosition,
                    target.transform.position,
                    worldMap,
                    ticSizeWorldUnits);

                var maxRange = target.IsFriendly ? RangeMiles : HostileDetectionMiles;
                if (distanceMiles > maxRange)
                {
                    continue;
                }

                results.Add(new RadarContact
                {
                    WorldPosition = target.transform.position,
                    DistanceMiles = distanceMiles,
                    IsHostile = !target.IsFriendly,
                    IsBase = false,
                    Label = target.TargetLabel,
                    Target = target,
                    BaseSite = null
                });
            }
        }

        private static void CollectBases(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            List<RadarContact> results)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive || baseSite.IsDestroyed)
                {
                    continue;
                }

                var distanceMiles = CombatThreatRange.DistanceMiles(
                    observerPosition,
                    baseSite.transform.position,
                    worldMap,
                    ticSizeWorldUnits);

                var isHostile = baseSite.Control == BaseControl.Hostile;
                var maxRange = isHostile ? HostileDetectionMiles : RangeMiles;
                if (distanceMiles > maxRange)
                {
                    continue;
                }

                results.Add(new RadarContact
                {
                    WorldPosition = baseSite.transform.position,
                    DistanceMiles = distanceMiles,
                    IsHostile = isHostile,
                    IsBase = true,
                    Label = baseSite.BaseName,
                    Target = baseSite.GetComponent<LockableTarget>(),
                    BaseSite = baseSite
                });
            }
        }
    }
}
