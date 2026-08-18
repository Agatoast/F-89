namespace F89.Core
{
    /// <summary>Which roster a character belongs to. Free Flight characters never enter Campaign.</summary>
    public enum CharacterPlayMode
    {
        Campaign = 0,
        FreeFlight = 1
    }

    /// <summary>Visibility rules for Campaign vs Free Flight character rosters.</summary>
    public static class CharacterPlayModeRules
    {
        public static CharacterPlayMode GetMode(CharacterSaveData save)
        {
            if (save == null)
            {
                return CharacterPlayMode.Campaign;
            }

            return save.PlayModeKind == (int)CharacterPlayMode.FreeFlight
                ? CharacterPlayMode.FreeFlight
                : CharacterPlayMode.Campaign;
        }

        public static bool IsFreeFlightCharacter(CharacterSaveData save) =>
            GetMode(save) == CharacterPlayMode.FreeFlight;

        public static bool HasCompletedCampaign(CharacterSaveData save)
        {
            if (save == null)
            {
                return false;
            }

            if (save.HasCompletedCampaign)
            {
                return true;
            }

            return save.CampaignMissionNumber > CampaignMissionProgress.TotalMissionCount;
        }

        /// <summary>
        /// Campaign roster: campaign characters only (never Free Flight-created).
        /// Free Flight roster: Free Flight characters, plus campaign characters that finished the campaign.
        /// </summary>
        public static bool IsVisibleInCurrentMode(CharacterSaveData save)
        {
            if (save == null)
            {
                return false;
            }

            var mode = GetMode(save);
            if (GamePlayModeState.IsCampaign)
            {
                return mode == CharacterPlayMode.Campaign;
            }

            return mode == CharacterPlayMode.FreeFlight || HasCompletedCampaign(save);
        }
    }
}
