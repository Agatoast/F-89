using F89.Core;

namespace F89.LandCombat
{
    public static class LandGroundSceneController
    {
        public static int SessionKills { get; private set; }

        public static void ResetSession()
        {
            SessionKills = 0;
        }

        public static void RegisterKill(int enemyLevel)
        {
            SessionKills++;
        }
    }
}
