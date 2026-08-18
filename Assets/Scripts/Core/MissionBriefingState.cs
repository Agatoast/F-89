namespace F89.Core
{
    public static class MissionBriefingState
    {
        /// <summary>Placeholder until campaign progression assigns per-mission operation titles.</summary>
        public static string OperationName { get; private set; } = "Save Antarctica";

        /// <summary>Placeholder until campaign progression assigns per-mission objectives.</summary>
        public const int BailOutScorePenalty = 2500;

        public static string MissionObjective { get; private set; } =
            "Establish air superiority over the Antarctic theater and support allied ground operations.";

        public static void RefreshCurrentMissionBrief(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CampaignMissionProgress.EnsureInitialized(save);
            F89.LandCombat.LandBossMissionAssignment.SyncAssignmentToCampaignMission(save);
            CampaignMissionSiteRestore.EnsureActiveMissionSiteRestored(save);
            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);
            if (CampaignMissionBriefCatalog.TryGetPlayerBrief(missionNumber, out var operationName, out var missionObjective))
            {
                OperationName = operationName;
                MissionObjective = missionObjective;
                return;
            }

            OperationName = "Save Antarctica";
            MissionObjective = F89.LandCombat.LandBossMissionAssignment.BuildMissionObjective(save);
        }

        public static void PrepareNextMission(CharacterSaveData save)
        {
            GameplaySessionBootstrap.ClearStalePersistedSession();
            MissionScoreState.BeginNewSortie(save);
            CampaignMissionProgress.EnsureInitialized(save);
            F89.LandCombat.LandBossMissionAssignment.SyncAssignmentToCampaignMission(save);
            CampaignMissionSiteRestore.EnsureActiveMissionSiteRestored(save);

            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);

            if (CampaignMissionBriefCatalog.TryGetPlayerBrief(missionNumber, out var operationName, out var missionObjective))
            {
                OperationName = operationName;
                MissionObjective = missionObjective;
                return;
            }

            OperationName = "Save Antarctica";
            MissionObjective = F89.LandCombat.LandBossMissionAssignment.BuildMissionObjective(save);
        }
    }
}
