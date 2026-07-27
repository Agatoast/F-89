namespace F89.Core
{
    public static class GameScenes
    {
        public const string LoadingScreen = "LoadingScreen";
        public const string StartPage = "MainMenu";
        public const string MainMenu = "MainMenu";
        public const string SelectionPage = "SelectionPage";
        public const string CharacterPage = "CharacterPage";
        public const string MissionBriefing = "MissionBriefing";
        public const string CharacterLoadout = "CharacterLoadout";
        public const string AircraftLoadout = "AircraftLoadout";
        public const string MenuSubpage = "MenuSubpage";
        public const string FlightTest = "FlightTest";
        public const string GroundAttack = "GroundAttack";
        public const string Bunker = "Bunker";
        public const string MissionComplete = "MissionComplete";

        /// <summary>Shared runtime scene for end-of-mission R&amp;D report pages.</summary>
        public const string ResearchResults = "ResearchResults";
        public const string NoBreakPage = "NoBreakPage";
        public const string HelmetRnDPage = "HelmetRnD";
        public const string VestRnDPage = "VestRnD";
        public const string WeaponRnDPage = "WeaponRnD";
        public const string BootsRnDPage = "BootsRnD";

        /// <summary>Shared runtime scene for Death / POW / Escaped pages after 0 HP.</summary>
        public const string DownedOutcome = "DownedOutcome";
        public const string DeathScreen = "DeathScreen";
        public const string POWScreen = "POWScreen";
        public const string EscapedScreen = "EscapedScreen";
        public const string FrozenDeath = "FrozenDeath";
        public const string Boss1 = "Boss1";
        public const string Boss2 = "Boss2";
        public const string Boss3 = "Boss3";
        public const string Boss4 = "Boss4";
        public const string Boss5 = "Boss5";
        public const string Boss6 = "Boss6";
        public const string Boss7 = "Boss7";
        public const string Boss8 = "Boss8";
        public const string Boss9 = "Boss9";
        public const string Boss10 = "Boss10";

        public static bool IsGameplayScene(string sceneName)
        {
            return sceneName == FlightTest || sceneName == GroundAttack || sceneName == Bunker;
        }

        public static bool IsResearchResultsScene(string sceneName)
        {
            return sceneName == ResearchResults
                || sceneName == NoBreakPage
                || sceneName == HelmetRnDPage
                || sceneName == VestRnDPage
                || sceneName == WeaponRnDPage
                || sceneName == BootsRnDPage
                || sceneName == "HelmetR&D"
                || sceneName == "VestR&D"
                || sceneName == "WeaponR&D"
                || sceneName == "BootsR&D";
        }

        public static bool IsDownedOutcomeScene(string sceneName)
        {
            return sceneName == DownedOutcome
                || sceneName == DeathScreen
                || sceneName == POWScreen
                || sceneName == EscapedScreen
                || sceneName == FrozenDeath;
        }

        public static int GetBossNumber(string sceneName)
        {
            if (sceneName == Boss1) return 1;
            if (sceneName == Boss2) return 2;
            if (sceneName == Boss3) return 3;
            if (sceneName == Boss4) return 4;
            if (sceneName == Boss5) return 5;
            if (sceneName == Boss6) return 6;
            if (sceneName == Boss7) return 7;
            if (sceneName == Boss8) return 8;
            if (sceneName == Boss9) return 9;
            if (sceneName == Boss10) return 10;
            return 0;
        }

        public static string GetBossScene(int bossNumber)
        {
            switch (bossNumber)
            {
                case 1: return Boss1;
                case 2: return Boss2;
                case 3: return Boss3;
                case 4: return Boss4;
                case 5: return Boss5;
                case 6: return Boss6;
                case 7: return Boss7;
                case 8: return Boss8;
                case 9: return Boss9;
                case 10: return Boss10;
                default: return Boss1;
            }
        }
    }
}
