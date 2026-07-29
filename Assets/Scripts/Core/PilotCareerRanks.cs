using UnityEngine;

namespace F89.Core
{
    /// <summary>Rank ladder gated by TotalScore. Used for demotion after career penalties.</summary>
    public static class PilotCareerRanks
    {
        private static readonly string[] Ranks =
        {
            "2nd LT",
            "1st LT",
            "Capt",
            "Maj",
            "Lt Col",
            "Col"
        };

        private static readonly int[] MinTotalScore =
        {
            0,
            500,
            1500,
            4000,
            8000,
            15000
        };

        public static string LowestRank => Ranks[0];

        public static int GetRankIndex(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return 0;
            }

            var normalized = Normalize(rank);
            for (var i = 0; i < Ranks.Length; i++)
            {
                if (Normalize(Ranks[i]) == normalized)
                {
                    return i;
                }
            }

            return 0;
        }

        public static int GetHighestRankIndexForScore(int totalScore)
        {
            var score = Mathf.Max(0, totalScore);
            var best = 0;
            for (var i = 0; i < MinTotalScore.Length; i++)
            {
                if (score >= MinTotalScore[i])
                {
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// Lowers rank when TotalScore no longer supports the current rank.
        /// Returns true when rank changed.
        /// </summary>
        public static bool TryDemoteToScoreFloor(
            CharacterSaveData save,
            out string previousRank,
            out string newRank)
        {
            previousRank = save != null ? save.Rank : LowestRank;
            newRank = previousRank;
            if (save == null)
            {
                return false;
            }

            var currentIndex = GetRankIndex(save.Rank);
            var allowedIndex = GetHighestRankIndexForScore(save.TotalScore);
            if (currentIndex <= allowedIndex)
            {
                return false;
            }

            newRank = Ranks[allowedIndex];
            save.Rank = newRank;
            return true;
        }

        private static string Normalize(string rank) =>
            rank.Trim()
                .Replace("Lieutenant", "LT")
                .Replace("Lieutentant", "LT")
                .Replace("Lt.", "LT")
                .Replace("Lt", "LT")
                .Replace("  ", " ")
                .ToUpperInvariant();
    }
}
