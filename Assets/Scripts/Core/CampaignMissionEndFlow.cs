using F89.Enemies;
using F89.LandCombat;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.Core
{
    /// <summary>
    /// Shared campaign END MISSION handling for carrier deck and outpost runway menus.
    /// Friendly occupation is applied only here — not on landing or resupply.
    /// </summary>
    public static class CampaignMissionEndFlow
    {
        public enum FinishResult
        {
            LeftForResearch = 0,
            ShowDemotion = 1,
            CourtMartialed = 2
        }

        public static bool RequiresFailureConfirm =>
            GamePlayModeState.IsCampaign
            && !CampaignMissionObjectiveState.IsPrimaryMissionComplete(CharacterSessionState.ActiveSave);

        public readonly struct SurvivedCrashCreditResult
        {
            public SurvivedCrashCreditResult(
                bool requiresCourtMartial,
                bool primaryMissionComplete,
                int missionScore,
                bool newBestMissionScore)
            {
                RequiresCourtMartial = requiresCourtMartial;
                PrimaryMissionComplete = primaryMissionComplete;
                MissionScore = missionScore;
                NewBestMissionScore = newBestMissionScore;
            }

            public bool RequiresCourtMartial { get; }
            public bool PrimaryMissionComplete { get; }
            public int MissionScore { get; }
            public bool NewBestMissionScore { get; }
        }

        /// <summary>
        /// Credits kills, mission score, and any completed primary/secondary progress when the pilot
        /// survives a crash landing (rescued or wounded). Does not roll R&amp;D breakthroughs.
        /// </summary>
        public static SurvivedCrashCreditResult ApplySurvivedCrashCredits(CharacterSaveData save)
        {
            if (save == null)
            {
                return default;
            }

            SyncMissionProgressBeforeCredit(save);
            CharacterSaveRepository.SyncVehicleKillCredit(save);

            var scoreResult = MissionScoreState.FinalizeMissionEnd(save);
            EnqueueRankPromotionIfAny(scoreResult);
            if (scoreResult.RequiresCourtMartial)
            {
                CharacterSaveRepository.MarkCourtMartialed(save);
                ClearMissionSessionState();
                return new SurvivedCrashCreditResult(
                    requiresCourtMartial: true,
                    primaryMissionComplete: false,
                    scoreResult.MissionScore,
                    scoreResult.NewBestMissionScore);
            }

            var primaryComplete = GamePlayModeState.IsCampaign
                && CampaignMissionObjectiveState.IsPrimaryMissionComplete(save);
            if (primaryComplete)
            {
                ApplyPrimaryVictoryProgress(save, scoreResult.MissionScore, grantEndMissionAwards: true);
                MissionEndReportState.Begin(
                    scoreResult.MissionScore,
                    scoreResult.NewBestMissionScore,
                    requiresCourtMartial: false);
                ClearMissionSessionState();
            }

            return new SurvivedCrashCreditResult(
                requiresCourtMartial: false,
                primaryMissionComplete: primaryComplete,
                scoreResult.MissionScore,
                scoreResult.NewBestMissionScore);
        }

        /// <param name="landedOutpostName">
        /// Outpost runway where the pilot ended the mission, or null/empty when ending from the carrier.
        /// Used only to remember the next launch runway — occupation always uses the mission SiteCode.
        /// </param>
        public static FinishResult FinishEndMission(string landedOutpostName, bool applyCampaignFailurePenalty)
        {
            var save = CharacterSessionState.ActiveSave;
            CharacterGearSession.PersistActive();
            CharacterSaveRepository.SyncVehicleKillCredit(save);

            SyncMissionProgressBeforeCredit(save);

            var scoreResult = MissionScoreState.FinalizeMissionEnd(save);
            EnqueueRankPromotionIfAny(scoreResult);
            MissionEndReportState.Begin(
                scoreResult.MissionScore,
                scoreResult.NewBestMissionScore,
                scoreResult.RequiresCourtMartial);

            if (scoreResult.RequiresCourtMartial && save != null)
            {
                CharacterSaveRepository.MarkCourtMartialed(save);
                ClearMissionSessionState();
                PostMissionContinuation.ContinueToResearchOrCharacter(save);
                return FinishResult.CourtMartialed;
            }

            var primaryComplete = GamePlayModeState.IsCampaign
                && save != null
                && CampaignMissionObjectiveState.IsPrimaryMissionComplete(save);

            if (primaryComplete)
            {
                ApplyPrimaryVictoryProgress(save, scoreResult.MissionScore, grantEndMissionAwards: true);
            }
            else if (applyCampaignFailurePenalty && GamePlayModeState.IsCampaign && save != null)
            {
                // Failure keeps CampaignMissionNumber (retry). Do not skip catalog missions via boss track.
                if (PilotCareerRanks.TryDemoteToScoreFloor(save, out var previousRank, out var newRank))
                {
                    CharacterSaveRepository.WriteBossProgress(save);
                    DemotionState.Begin(previousRank, newRank);
                    PersistLaunchOriginAfterEndMission(save, landedOutpostName);
                    ClearMissionSessionState();
                    return FinishResult.ShowDemotion;
                }
            }

            // Park next sortie at the friendly runway used for END MISSION. Null/empty carrier END MISSION
            // does not clear the saved last-land base.
            PersistLaunchOriginAfterEndMission(save, landedOutpostName);
            ClearMissionSessionState();
            PostMissionContinuation.ContinueToResearchOrCharacter(save);
            return FinishResult.LeftForResearch;
        }

        public static void LoadDemotionScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MissionComplete);
        }

        private const int GoodConductSuccessfulMissionThreshold = 5;

        private static void SyncMissionProgressBeforeCredit(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CampaignWaypointPlatoonState.TrySyncActiveMissionBeforeEnd(save);
            TrySyncActiveOutpostSecondaryClearance(save);
        }

        private static void TrySyncActiveOutpostSecondaryClearance(CharacterSaveData save)
        {
            if (!CampaignMissionObjectiveState.TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return;
            }

            var guardCount = OutpostRunwayDeckState.ResolveRunwayGuardCount(baseName);
            if (guardCount <= 0
                || !OutpostGroundGuardState.AreSurfaceGuardsCleared(baseName, guardCount))
            {
                return;
            }

            if (LandBossMissionAssignment.TryGetBossForOutpost(save, baseName, out var bossNumber))
            {
                OutpostGroundGuardState.TryMarkBossGuardsCleared(baseName, bossNumber, guardCount);
            }
        }

        private static void ApplyPrimaryVictoryProgress(
            CharacterSaveData save,
            int missionScore,
            bool grantEndMissionAwards)
        {
            if (save == null)
            {
                return;
            }

            CampaignMissionObjectiveState.TryApplyVictoryOccupation(save);
            CampaignMissionProgress.AdvanceAfterPrimaryVictory(save);
            LandBossMissionAssignment.SyncAssignmentToCampaignMission(save);
            if (grantEndMissionAwards)
            {
                RecordSuccessfulEndMissionAwards(save, missionScore);
            }
        }

        private static void EnqueueRankPromotionIfAny(MissionEndScoreResult scoreResult)
        {
            if (scoreResult.PromotedRankIndex >= 0)
            {
                RankPromotionState.Enqueue(scoreResult.PromotedRankIndex);
            }
        }

        private static void RecordSuccessfulEndMissionAwards(CharacterSaveData save, int missionScore)
        {
            if (save == null)
            {
                return;
            }

            save.SuccessfulEndMissionCount++;
            CharacterSaveRepository.WriteBossProgress(save);

            if (save.SuccessfulEndMissionCount == 1)
            {
                if (CharacterSaveRepository.TryGrantRibbon(save, MilitaryRibbonIds.CombatAction))
                {
                    MedalAwardState.Enqueue(MilitaryRibbonIds.CombatAction);
                }

                if (CharacterSaveRepository.TryGrantRibbon(save, MilitaryRibbonIds.AntarcticaService))
                {
                    MedalAwardState.Enqueue(MilitaryRibbonIds.AntarcticaService);
                }
            }

            if (save.SuccessfulEndMissionCount >= GoodConductSuccessfulMissionThreshold
                && CharacterSaveRepository.TryGrantRibbon(save, MilitaryRibbonIds.GoodConduct))
            {
                MedalAwardState.Enqueue(MilitaryRibbonIds.GoodConduct);
            }

            if (CharacterSaveRepository.TryGrantScoreMedalForMission(
                    save,
                    missionScore,
                    out var scoreMedalId,
                    out var scoreMedalAwardCount))
            {
                MedalAwardState.Enqueue(scoreMedalId, scoreMedalAwardCount);
            }
        }

        /// <summary>
        /// Remembers the End Mission runway for the next fresh sortie when ending from a runway.
        /// Carrier END MISSION clears the saved launch base so the next sortie starts from the deck.
        /// </summary>
        private static void PersistLaunchOriginAfterEndMission(CharacterSaveData save, string landedOutpostName)
        {
            if (save == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(landedOutpostName))
            {
                MissionLaunchOrigin.ClearLastLandedBase(save);
                return;
            }

            LandBossMissionAssignment.PersistLaunchOutpost(save, landedOutpostName.Trim());
        }

        private static void ClearMissionSessionState()
        {
            BunkerDefenseIntegration.ConsumeSortieHitModifierIfNeeded();
            LandMissionHandoffState.Clear();
            LandingMileFlagState.Clear();
            LandMissionHealthState.Clear();
            LandMissionCompleteState.Clear();
            CarrierResupplyState.Clear();
            OutpostRunwayDeckState.Clear();
            DeckLandingServiceState.Clear();
            OpenFieldLandingState.Clear();
            GridSquareVehicleSpawner.ResetForNewMission();
        }
    }
}
