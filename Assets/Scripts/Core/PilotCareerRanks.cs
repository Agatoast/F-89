using UnityEngine;

namespace F89.Core
{
    /// <summary>Rank ladder gated by TotalScore. Used for promotion, demotion, and GCMP imprisonment.</summary>
    public static class PilotCareerRanks
    {
        /// <summary>Index of 1st LT — first rank above starting 2nd LT.</summary>
        public const int FirstRankIndex = 1;

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

        public static int GetMinTotalScoreForRankIndex(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= MinTotalScore.Length)
            {
                return 0;
            }

            return MinTotalScore[rankIndex];
        }

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
        /// Syncs rank to the total-score floor (promote or demote). Records first-rank achievement once.
        /// Demotion does not clear <see cref="CharacterSaveData.HasAchievedFirstRank"/> — a demoted 2nd LT
        /// who once reached 1st LT remains eligible for GCMP when total score is negative.
        /// </summary>
        public static void SyncCareerRankProgress(CharacterSaveData save)
        {
            if (save == null || save.IsCourtMartialed || save.IsKilledInAction)
            {
                return;
            }

            var allowedIndex = GetHighestRankIndexForScore(save.TotalScore);
            var currentIndex = GetRankIndex(save.Rank);
            if (allowedIndex != currentIndex)
            {
                save.Rank = Ranks[allowedIndex];
            }

            if (save.HasAchievedFirstRank)
            {
                return;
            }

            if (GetRankIndex(save.Rank) >= FirstRankIndex
                || save.TotalScore >= GetMinTotalScoreForRankIndex(FirstRankIndex))
            {
                save.HasAchievedFirstRank = true;
            }
        }

        /// <summary>
        /// Once the pilot has reached 1st LT, negative total score triggers GCMP — even if demoted to 2nd LT.
        /// </summary>
        public static bool ShouldImprisonForNegativeTotal(CharacterSaveData save)
        {
            return save != null
                && !save.IsCourtMartialed
                && !save.IsKilledInAction
                && save.HasAchievedFirstRank
                && save.TotalScore < 0;
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
