namespace SaveAntarctica.BunkerDefense.Core
{
    public interface IBunkerDefenseHostResume
    {
        void ResumeToLastLanding(string lastLandedBaseId, string hostUiRequest);
    }

    public static class BunkerDefenseHostResumeRegistry
    {
        public static IBunkerDefenseHostResume Handler { get; set; }
    }

    /// <summary>Tower Defense name kept so F-89 can swap the plug-in without a registry rename.</summary>
    public static class BaseDefenseHostResumeRegistry
    {
        public static IBunkerDefenseHostResume Handler
        {
            get => BunkerDefenseHostResumeRegistry.Handler;
            set => BunkerDefenseHostResumeRegistry.Handler = value;
        }
    }
}
