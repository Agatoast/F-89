namespace F89.LandCombat
{
    public static class LandGroundSceneController
    {
        public static int SessionKills { get; private set; }
        public static int SessionScore { get; private set; }

        public static void ResetSession()
        {
            SessionKills = 0;
            SessionScore = 0;
        }

        public static void RegisterKill(int enemyLevel)
        {
            SessionKills++;
            SessionScore += LandUrEnemyStats.ClampLevel(enemyLevel);
        }
    }
}
