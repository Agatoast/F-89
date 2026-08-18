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
        public const string MARefuel = "MARefuel";
        public const string BunkerPlayScene = "BunkerPlayScene";
        public const string MissionComplete = "MissionComplete";

        /// <summary>Shared runtime scene for end-of-mission R&amp;D report pages.</summary>
        public const string ResearchResults = "ResearchResults";
        public const string NoBreakPage = "NoBreakPage";
        public const string HelmetRnDPage = "HelmetRnD";
        public const string VestRnDPage = "VestRnD";
        public const string WeaponRnDPage = "WeaponRnD";
        public const string BootsRnDPage = "BootsRnD";

        /// <summary>Shared runtime scene for crash landing rescue / wounded / KIA / Purple Heart pages.</summary>
        public const string CrashLandingOutcome = "CrashLandingOutcome";
        public const string CrashLandingRescue = "CrashLandingRescue";
        public const string CrashLandingWounded = "CrashLandingWounded";
        public const string CrashLandingKIA = "CrashLandingKIA";
        public const string PurpleHeartAward = "PurpleHeartAward";
        public const string AntarcticaServiceAward = "AntarcticaServiceAward";
        public const string GoodConductAward = "GoodConductAward";
        public const string CombatActionAward = "CombatActionAward";
        public const string RankPromotion = "RankPromotion";
        public const string MissionIncomplete = "MissionIncomplete";

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
        public const string Boss11 = "Boss11";
        public const string Boss12 = "Boss12";
        public const string Boss13 = "Boss13";
        public const string Boss14 = "Boss14";
        public const string Boss15 = "Boss15";
        public const string Boss16 = "Boss16";
        public const string Boss17 = "Boss17";
        public const string Boss18 = "Boss18";
        public const string Boss19 = "Boss19";

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
                || sceneName == AntarcticaServiceAward
                || sceneName == GoodConductAward
                || sceneName == CombatActionAward
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

        public static bool IsCrashLandingOutcomeScene(string sceneName)
        {
            return sceneName == CrashLandingOutcome
                || sceneName == CrashLandingRescue
                || sceneName == CrashLandingWounded
                || sceneName == CrashLandingKIA
                || sceneName == PurpleHeartAward;
        }

        public static bool IsMissionStatusReportScene(string sceneName)
        {
            return sceneName == MissionIncomplete;
        }

        public static int GetBossNumber(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("Boss", System.StringComparison.Ordinal))
            {
                return 0;
            }

            return int.TryParse(sceneName.Substring(4), out var bossNumber) ? bossNumber : 0;
        }

        public static string GetBossScene(int bossNumber) =>
            bossNumber >= 1 ? $"Boss{bossNumber}" : Boss1;
    }
}
