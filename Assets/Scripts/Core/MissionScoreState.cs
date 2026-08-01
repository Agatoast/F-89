using UnityEngine;

namespace F89.Core
{
    public readonly struct MissionEndScoreResult
    {
        public MissionEndScoreResult(int missionScore, bool newBestMissionScore, bool requiresCourtMartial)
        {
            MissionScore = missionScore;
            NewBestMissionScore = newBestMissionScore;
            RequiresCourtMartial = requiresCourtMartial;
        }

        public int MissionScore { get; }
        public bool NewBestMissionScore { get; }
        public bool RequiresCourtMartial { get; }
    }

    /// <summary>
    /// Active sortie mission score (level-weighted enemy and friendly kills).
    /// Permanent kill counts and career totals live on <see cref="CharacterSaveData"/>.
    /// </summary>
    public static class MissionScoreState
    {
        public const int CourtMartialFriendlyKillThreshold = -100;

        public static int SessionScore { get; private set; }
        /// <summary>Running friendly-kill penalty for this mission (zero or negative).</summary>
        public static int FriendlyKillPoints { get; private set; }

        public static void Reset()
        {
            SessionScore = 0;
            FriendlyKillPoints = 0;
        }

        public static void AddMissionPoints(int points)
        {
            SessionScore += points;
        }

        public static void RegisterFriendlyKillPoints(int pointValue)
        {
            if (pointValue <= 0)
            {
                return;
            }

            var penalty = -pointValue;
            FriendlyKillPoints += penalty;
            SessionScore += penalty;
        }

        public static bool IsCourtMartialPending =>
            FriendlyKillPoints <= CourtMartialFriendlyKillThreshold;

        /// <summary>Drops the in-progress mission score without updating career totals.</summary>
        public static void AbandonMissionWithoutScoring()
        {
            Reset();
        }

        /// <summary>Called only from END MISSION — rolls session score into career totals.</summary>
        public static MissionEndScoreResult FinalizeMissionEnd(CharacterSaveData save)
        {
            if (save == null)
            {
                var empty = new MissionEndScoreResult(0, false, false);
                Reset();
                return empty;
            }

            var missionScore = SessionScore;
            var newBest = missionScore > save.BestMissionScore;
            if (newBest)
            {
                save.BestMissionScore = missionScore;
            }

            save.TotalScore += missionScore;
            var requiresCourtMartial = IsCourtMartialPending;
            var result = new MissionEndScoreResult(missionScore, newBest, requiresCourtMartial);
            Reset();
            CharacterSaveRepository.WriteWorldProgress(save);
            return result;
        }
    }
}
