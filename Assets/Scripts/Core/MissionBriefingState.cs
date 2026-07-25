namespace F89.Core
{
    public static class MissionBriefingState
    {
        public const string OperationName = "Save Antarctica";

        /// <summary>Placeholder until campaign progression assigns per-mission objectives.</summary>
        public const int BailOutScorePenalty = 2500;

        public static string MissionObjective { get; private set; } =
            "Establish air superiority over the Antarctic theater and support allied ground operations.";

        public static void PrepareNextMission(CharacterSaveData save)
        {
            MissionObjective =
                "Establish air superiority over the Antarctic theater and support allied ground operations.";
        }
    }
}
