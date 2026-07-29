namespace F89.Core
{
    /// <summary>
    /// Campaign is the only defined play mode today. Non-campaign will skip early-end career penalties.
    /// </summary>
    public static class GamePlayModeState
    {
        public static bool IsCampaign { get; set; } = true;
    }
}
