namespace F89.Core
{
    /// <summary>
    /// Active session play mode. Campaign uses the 53-mission path; Free Flight skips missions.
    /// </summary>
    public static class GamePlayModeState
    {
        public static bool IsCampaign { get; private set; } = true;

        public static bool IsFreeFlight => !IsCampaign;

        public static CharacterPlayMode ActivePlayMode =>
            IsCampaign ? CharacterPlayMode.Campaign : CharacterPlayMode.FreeFlight;

        public static void EnterCampaign()
        {
            IsCampaign = true;
        }

        public static void EnterFreeFlight()
        {
            IsCampaign = false;
        }

        public static void ResetToCampaignDefault()
        {
            IsCampaign = true;
        }
    }
}
