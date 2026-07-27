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
    }
}
