using System.Collections.Generic;
using F89.Core;

namespace F89.UI
{
    /// <summary>
    /// Pending rank promotion pages shown after END MISSION score, before medal awards.
    /// </summary>
    public static class RankPromotionState
    {
        private static readonly Queue<int> PendingRankIndices = new();

        public static bool HasPendingPromotion => PendingRankIndices.Count > 0;

        public static int RankIndex =>
            PendingRankIndices.Count > 0 ? PendingRankIndices.Peek() : -1;

        public static void Enqueue(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= PilotCareerRanks.RankCount)
            {
                return;
            }

            PendingRankIndices.Enqueue(rankIndex);
        }

        public static bool Advance()
        {
            if (PendingRankIndices.Count > 0)
            {
                PendingRankIndices.Dequeue();
            }

            return PendingRankIndices.Count > 0;
        }

        public static void Clear()
        {
            PendingRankIndices.Clear();
        }

        public static string PageId => GameScenes.RankPromotion;

        public static string TitleForCurrent() => "PROMOTION";

        public static string BodyForCurrent()
        {
            var rank = PilotCareerRanks.GetRankName(RankIndex);
            return $"Congratulations!\n\nYou have been promoted to {rank}.";
        }
    }
}
