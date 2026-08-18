namespace F89.Core
{
    public static class CampaignMissionProgress
    {
        public const int TotalMissionCount = 53;

        public static int GetCurrentMissionNumber(CharacterSaveData save)
        {
            if (save == null || save.CampaignMissionNumber <= 0)
            {
                return 1;
            }

            return save.CampaignMissionNumber;
        }

        public static void EnsureInitialized(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            if (save.CampaignMissionNumber <= 0)
            {
                save.CampaignMissionNumber = 1;
            }

            if (!save.HasCompletedCampaign && save.CampaignMissionNumber > TotalMissionCount)
            {
                save.HasCompletedCampaign = true;
            }

            CampaignMissionObjectiveState.RecoverCompletedOccupationSites(save);
        }

        public static void AdvanceAfterPrimaryVictory(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureInitialized(save);
            var completedMission = GetCurrentMissionNumber(save);
            save.CampaignMissionNumber = completedMission + 1;
            if (completedMission >= TotalMissionCount)
            {
                save.HasCompletedCampaign = true;
            }

            F89.LandCombat.LandBossMissionAssignment.SyncAssignmentToCampaignMission(save);
            CampaignMissionSiteRestore.EnsureActiveMissionSiteRestored(save);

            CharacterSaveRepository.WriteWorldProgress(save);
        }

        public static bool IsCampaignFinished(CharacterSaveData save) =>
            CharacterPlayModeRules.HasCompletedCampaign(save);
    }
}
