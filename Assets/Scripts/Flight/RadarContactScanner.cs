using System.Collections.Generic;
using F89.Core;
using F89.LandCombat;
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
        public bool IsDestroyed;
        public bool HasBunker;
        public bool CanBeTargeted;
        public string Label;
        public LockableTarget Target;
        public AntarcticaBase BaseSite;
    }

    public static class RadarContactScanner
    {
        public const float RangeMiles = 150f;
        public const float MidRangeBandMiles = 100f;
        public const float HostileDetectionMiles = 50f;
        public const float ShortRangeMiles = 25f;
        public const float ShortRangeBandMiles = 5f;

        public static void CollectVisibleContacts(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            List<RadarContact> results)
        {
            CollectVisibleContacts(observerPosition, worldMap, ticSizeWorldUnits, null, results);
        }

        public static void CollectVisibleContacts(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            float? rangeCapMiles,
            List<RadarContact> results)
        {
            results.Clear();
            if (worldMap == null)
            {
                return;
            }

            CollectLockableTargets(observerPosition, worldMap, ticSizeWorldUnits, rangeCapMiles, results);
            CollectBases(observerPosition, worldMap, ticSizeWorldUnits, rangeCapMiles, results);
        }

        private static void CollectLockableTargets(
            Vector3 observerPosition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            float? rangeCapMiles,
            List<RadarContact> results)
        {
            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null
                    || !target.IsAlive
                    || target.IsPlayerAircraft
                    || target.IsFlareDecoy
                    || target.GetComponent<AntarcticaBase>() != null)
                {
                    continue;
                }

                var distanceMiles = CombatThreatRange.DistanceMiles(
                    observerPosition,
                    target.transform.position,
                    worldMap,
                    ticSizeWorldUnits);

                var maxRange = target.IsFriendly ? RangeMiles : HostileDetectionMiles;
                if (rangeCapMiles.HasValue)
                {
                    maxRange = Mathf.Min(maxRange, rangeCapMiles.Value);
                }
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
                    IsDestroyed = false,
                    HasBunker = false,
                    CanBeTargeted = true,
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
            float? rangeCapMiles,
            List<RadarContact> results)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || !baseSite.IsActive)
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
                if (rangeCapMiles.HasValue)
                {
                    maxRange = Mathf.Min(maxRange, rangeCapMiles.Value);
                }
                if (distanceMiles > maxRange)
                {
                    continue;
                }

                var hasRevealedBunker = baseSite.SiteKind == BaseSiteKind.Land
                    && LandBossMissionAssignment.IsBunkerRevealedAtOutpost(
                        CharacterSessionState.ActiveSave,
                        baseSite.BaseName);
                results.Add(new RadarContact
                {
                    WorldPosition = baseSite.transform.position,
                    DistanceMiles = distanceMiles,
                    IsHostile = isHostile,
                    IsBase = true,
                    IsDestroyed = baseSite.IsDestroyed,
                    HasBunker = hasRevealedBunker,
                    // The outpost is a destructible surface building. Its interior bunker
                    // remains a separate ground-combat destination after destruction.
                    CanBeTargeted = !baseSite.IsDestroyed,
                    Label = baseSite.BaseName,
                    Target = baseSite.GetComponent<LockableTarget>(),
                    BaseSite = baseSite
                });
            }
        }
    }
}
