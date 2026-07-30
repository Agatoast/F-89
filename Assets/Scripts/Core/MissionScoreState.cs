namespace F89.Core
{
    /// <summary>Points earned during the active sortie (flight + ground segments before mission end).</summary>
    public static class MissionScoreState
    {
        public static int SessionScore { get; private set; }

        public static void Reset()
        {
            SessionScore = 0;
        }

        public static void AddPoints(int points)
        {
            if (points > 0)
            {
                SessionScore += points;
            }
        }

        public static void FinalizeToSave(CharacterSaveData save)
        {
            if (save == null)
            {
                Reset();
                return;
            }

            if (SessionScore > save.BestMissionScore)
            {
                save.BestMissionScore = SessionScore;
            }

            CharacterSaveRepository.WriteWorldProgress(save);
            Reset();
        }
    }
}
