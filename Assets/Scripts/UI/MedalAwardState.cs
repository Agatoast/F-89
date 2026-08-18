using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Pending medal award pages shown after END MISSION score, before R&amp;D reports.
    /// </summary>
    public static class MedalAwardState
    {
        private static readonly Queue<string> PendingRibbonIds = new();
        private static readonly Queue<int> PendingAwardCounts = new();

        public static bool HasPendingAward => PendingRibbonIds.Count > 0;

        public static string RibbonId =>
            PendingRibbonIds.Count > 0 ? PendingRibbonIds.Peek() : string.Empty;

        public static int CurrentAwardCount =>
            PendingAwardCounts.Count > 0 ? PendingAwardCounts.Peek() : 1;

        public static void Enqueue(string ribbonId, int awardCountAfterGrant = 1)
        {
            if (string.IsNullOrEmpty(ribbonId))
            {
                return;
            }

            PendingRibbonIds.Enqueue(ribbonId);
            PendingAwardCounts.Enqueue(Mathf.Max(1, awardCountAfterGrant));
        }

        /// <summary>Compatibility wrapper for a single pending award.</summary>
        public static void Begin(string ribbonId)
        {
            Clear();
            Enqueue(ribbonId);
        }

        /// <summary>Advances past the current award. Returns true when more awards remain.</summary>
        public static bool Advance()
        {
            if (PendingRibbonIds.Count > 0)
            {
                PendingRibbonIds.Dequeue();
            }

            if (PendingAwardCounts.Count > 0)
            {
                PendingAwardCounts.Dequeue();
            }

            return PendingRibbonIds.Count > 0;
        }

        public static void Clear()
        {
            PendingRibbonIds.Clear();
            PendingAwardCounts.Clear();
        }

        public static string PageId =>
            RibbonId switch
            {
                MilitaryRibbonIds.CombatAction => GameScenes.CombatActionAward,
                MilitaryRibbonIds.AntarcticaService => GameScenes.AntarcticaServiceAward,
                MilitaryRibbonIds.GoodConduct => GameScenes.GoodConductAward,
                _ => string.Empty
            };

        public static string TitleForCurrent()
        {
            if (!MilitaryRibbonCatalog.TryGetDefinition(RibbonId, out var definition))
            {
                return "MEDAL AWARDED";
            }

            return $"{definition.DisplayName.ToUpperInvariant()} AWARDED";
        }

        public static string BodyForCurrent()
        {
            return RibbonId switch
            {
                MilitaryRibbonIds.CombatAction =>
                    "For completing your first successful mission in Antarctica,\n\nyou have been awarded the Combat Action Ribbon.",
                MilitaryRibbonIds.AntarcticaService =>
                    "For completing your first successful mission in Antarctica,\n\nyou have been awarded the Antarctica Service Medal.",
                MilitaryRibbonIds.GoodConduct =>
                    "For completing five successful missions in Antarctica,\n\nyou have been awarded the Good Conduct Medal.",
                MilitaryRibbonIds.JointServiceCommendation =>
                    CurrentAwardCount > 1
                        ? "For earning a mission score of 200 or higher,\n\na bronze star device has been added to your Joint Services Commendation Medal."
                        : "For earning a mission score of 200 or higher,\n\nyou have been awarded the Joint Services Commendation Medal.",
                MilitaryRibbonIds.JointServiceAchievement =>
                    CurrentAwardCount > 1
                        ? "For earning a mission score of 100 or higher,\n\na bronze star device has been added to your Joint Services Achievement Medal."
                        : "For earning a mission score of 100 or higher,\n\nyou have been awarded the Joint Services Achievement Medal.",
                _ => "You have been awarded a medal for distinguished service."
            };
        }
    }
}
