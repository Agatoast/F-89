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

        public static bool IsGameplayScene(string sceneName)
        {
            return sceneName == FlightTest || sceneName == GroundAttack;
        }
    }
}
