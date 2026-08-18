using UnityEngine;

namespace F89.Core
{
    /// <summary>Rank ladder gated by TotalScore. Used for promotion, demotion, and GCMP imprisonment.</summary>
    public static class PilotCareerRanks
    {
        /// <summary>Index of 1LT — first rank above starting 2LT.</summary>
        public const int FirstRankIndex = 1;

        private static readonly string[] Ranks =
        {
            "2LT",
            "1LT",
            "CPT",
            "MAJ",
            "LTC",
            "COL",
            "GEN"
        };

        private static readonly int[] MinTotalScore =
        {
            0,      // 2LT
            100,    // 1LT
            1250,   // CPT
            4000,   // MAJ
            8000,   // LTC
            15000,  // COL
            25000   // BRIG
        };

        public static string LowestRank => Ranks[0];

        public static int RankCount => Ranks.Length;

        public static string GetRankName(int rankIndex)
        {
            if (rankIndex < 0 || rankIndex >= Ranks.Length)
            {
                return Ranks[0];
            }

            return Ranks[rankIndex];
        }

        /// <summary>
        /// Resources path suffix under <c>CharacterPage/Ranks/</c> (no extension).
        /// Drop PNGs at Assets/Resources/CharacterPage/Ranks/{name}.png
        /// </summary>
        public static string GetRankInsigniaResourceName(int rankIndex) =>
            rankIndex switch
            {
                0 => "rank_0_2nd_lt",
                1 => "rank_1_1st_lt",
                2 => "rank_2_cpt",
                3 => "rank_3_maj",
                4 => "rank_4_lt_col",
                5 => "rank_5_col",
                6 => "rank_6_brig",
                _ => "rank_0_2nd_lt"
            };

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
        /// Demotion does not clear <see cref="CharacterSaveData.HasAchievedFirstRank"/> — a demoted 2LT
        /// who once reached 1LT remains eligible for GCMP when total score is negative.
        /// </summary>
        public static void SyncCareerRankProgress(CharacterSaveData save)
        {
            if (save == null || save.IsCourtMartialed || save.IsKilledInAction)
            {
                return;
            }

            var allowedIndex = GetHighestRankIndexForScore(save.TotalScore);
            var canonicalRank = Ranks[allowedIndex];
            if (save.Rank != canonicalRank)
            {
                save.Rank = canonicalRank;
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
        /// Once the pilot has reached 1LT, negative total score triggers GCMP — even if demoted to 2LT.
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

        private static string Normalize(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return string.Empty;
            }

            var s = rank.Trim().ToUpperInvariant();
            s = s.Replace("LIEUTENANT", "LT").Replace("LIEUTENTANT", "LT");
            s = s.Replace("COLONEL", "COL").Replace("MAJOR", "MAJ");
            s = s.Replace("CAPTAIN", "CPT").Replace("BRIGADIER", "BRIG");
            s = s.Replace("GENERAL", "GEN");
            s = s.Replace("LT.", "LT");
            s = s.Replace(" ", string.Empty);
            s = s.Replace(".", string.Empty);

            if (s.StartsWith("2ND"))
            {
                s = "2" + s.Substring(3);
            }

            if (s.StartsWith("1ST"))
            {
                s = "1" + s.Substring(3);
            }

            if (s is "LTCL" or "LTCOL")
            {
                s = "LTC";
            }

            return s switch
            {
                "CAPT" => "CPT",
                "BRIG" => "GEN",
                _ => s
            };
        }
    }
}
