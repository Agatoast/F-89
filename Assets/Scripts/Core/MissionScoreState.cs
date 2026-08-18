using UnityEngine;



namespace F89.Core

{

    public readonly struct MissionEndScoreResult

    {

        public MissionEndScoreResult(
            int missionScore,
            bool newBestMissionScore,
            bool requiresCourtMartial,
            int promotedRankIndex = -1)

        {

            MissionScore = missionScore;

            NewBestMissionScore = newBestMissionScore;

            RequiresCourtMartial = requiresCourtMartial;

            PromotedRankIndex = promotedRankIndex;

        }



        public int MissionScore { get; }

        public bool NewBestMissionScore { get; }

        public bool RequiresCourtMartial { get; }

        /// <summary>Index into <see cref="PilotCareerRanks"/> when rank rose this mission; otherwise -1.</summary>
        public int PromotedRankIndex { get; }

    }



    /// <summary>

    /// Active sortie mission score (level-weighted enemy and friendly kills).

    /// Permanent kill counts and career totals live on <see cref="CharacterSaveData"/>.

    /// Session score is mirrored onto the active save so scene loads cannot wipe it.

    /// </summary>

    public static class MissionScoreState

    {

        public static int SessionScore { get; private set; }



        public static void BeginNewSortie(CharacterSaveData save)

        {

            SessionScore = 0;

            MirrorToSave(save);

            if (save != null)

            {

                CharacterSaveRepository.WriteWorldProgress(save);

            }

        }



        public static void Reset()

        {

            BeginNewSortie(CharacterSessionState.ActiveSave);

        }



        /// <summary>

        /// Restores in-progress sortie score after scene loads or character re-select.

        /// Never clobbers a live session total with a stale zero on the save.

        /// </summary>

        public static void HydrateFromSave(CharacterSaveData save)

        {

            SyncWithSave(save);

        }



        public static void AddMissionPoints(int points)

        {

            if (points == 0)

            {

                return;

            }



            SessionScore += points;

            PersistScoreChange(CharacterSessionState.ActiveSave);

        }



        public static void RegisterFriendlyKillPoints(int pointValue)

        {

            if (pointValue <= 0)

            {

                return;

            }



            SessionScore -= pointValue;

            PersistScoreChange(CharacterSessionState.ActiveSave);

        }



        /// <summary>Drops the in-progress mission score without updating career totals.</summary>

        public static void AbandonMissionWithoutScoring()

        {

            BeginNewSortie(CharacterSessionState.ActiveSave);

        }



        /// <summary>Called only from END MISSION — rolls session score into career totals.</summary>

        public static MissionEndScoreResult FinalizeMissionEnd(CharacterSaveData save)

        {

            if (save == null)

            {

                var empty = new MissionEndScoreResult(0, false, false);

                SessionScore = 0;

                return empty;

            }



            var missionScore = SyncWithSave(save);

            var newBest = missionScore > save.BestMissionScore;

            if (newBest)

            {

                save.BestMissionScore = missionScore;

            }



            save.TotalScore += missionScore;

            var rankBefore = PilotCareerRanks.GetRankIndex(save.Rank);
            PilotCareerRanks.SyncCareerRankProgress(save);
            var rankAfter = PilotCareerRanks.GetRankIndex(save.Rank);
            var promotedRankIndex = rankAfter > rankBefore ? rankAfter : -1;

            var requiresCourtMartial = PilotCareerRanks.ShouldImprisonForNegativeTotal(save);

            var result = new MissionEndScoreResult(
                missionScore,
                newBest,
                requiresCourtMartial,
                promotedRankIndex);

            BeginNewSortie(save);

            return result;

        }



        private static int SyncWithSave(CharacterSaveData save)

        {

            if (save == null)

            {

                SessionScore = 0;

                return 0;

            }



            if (SessionScore == 0 && save.ActiveSortieMissionScore != 0)

            {

                SessionScore = save.ActiveSortieMissionScore;

                return SessionScore;

            }



            if (SessionScore != save.ActiveSortieMissionScore)

            {

                MirrorToSave(save);

                PersistScoreChange(save);

            }



            return SessionScore;

        }



        private static void PersistScoreChange(CharacterSaveData save)

        {

            MirrorToSave(save);

            if (save != null)

            {

                CharacterSaveRepository.WriteWorldProgress(save);

            }

        }



        private static void MirrorToSave(CharacterSaveData save)

        {

            if (save == null)

            {

                return;

            }



            save.ActiveSortieMissionScore = SessionScore;

        }

    }

}


