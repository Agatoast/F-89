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
            && !LandBossMissionAssignment.IsPrimaryMissionComplete(CharacterSessionState.ActiveSave);

        /// <param name="landedOutpostName">
        /// Outpost runway where the pilot ended the mission, or null/empty when ending from the carrier.
        /// </param>
        public static FinishResult FinishEndMission(string landedOutpostName, bool applyCampaignFailurePenalty)
        {
            var save = CharacterSessionState.ActiveSave;
            CharacterGearSession.PersistActive();
            CharacterSaveRepository.SyncVehicleKillCredit(save);

            var rankBeforeEnd = save != null ? save.Rank : PilotCareerRanks.LowestRank;
            var scoreResult = MissionScoreState.FinalizeMissionEnd(save);
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

            if (applyCampaignFailurePenalty && GamePlayModeState.IsCampaign && save != null)
            {
                LandBossMissionAssignment.ResolveAssignedMissionWithoutVictory(save);
                if (rankBeforeEnd != save.Rank)
                {
                    CharacterSaveRepository.WriteBossProgress(save);
                    DemotionState.Begin(rankBeforeEnd, save.Rank);
                    ClearMissionSessionState();
                    return FinishResult.ShowDemotion;
                }
            }
            else if (GamePlayModeState.IsCampaign
                     && save != null
                     && LandBossMissionAssignment.HasActiveAssignment(save)
                     && LandBossMissionAssignment.IsPrimaryMissionComplete(save))
            {
                ApplyVictoryOccupation(save, landedOutpostName);
            }

            ClearMissionSessionState();
            PostMissionContinuation.ContinueToResearchOrCharacter(save);
            return FinishResult.LeftForResearch;
        }

        public static void LoadDemotionScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.MissionComplete);
        }

        private static void ApplyVictoryOccupation(CharacterSaveData save, string landedOutpostName)
        {
            var occupationOutpost = !string.IsNullOrWhiteSpace(landedOutpostName)
                ? landedOutpostName
                : save.AssignedBossOutpostName;
            AntarcticaOutpostState.TryApplyMissionCompleteOccupation(occupationOutpost);
            LandBossMissionAssignment.PrepareNextAssignment(save);

            if (!string.IsNullOrWhiteSpace(landedOutpostName))
            {
                LandBossMissionAssignment.PersistLaunchOutpost(save, landedOutpostName);
            }
            else
            {
                LandBossMissionAssignment.ClearMissionLaunchOutpost(save);
            }
        }

        private static void ClearMissionSessionState()
        {
            LandMissionHealthState.Clear();
            LandMissionCompleteState.Clear();
            CarrierResupplyState.Clear();
            OutpostRunwayDeckState.Clear();
            DeckLandingServiceState.Clear();
            OpenFieldLandingState.Clear();
        }
    }
}
