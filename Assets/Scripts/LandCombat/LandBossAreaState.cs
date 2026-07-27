namespace F89.LandCombat
{
    /// <summary>Keeps the selected named boss area active across surface, briefing, and bunker scenes.</summary>
    public static class LandBossAreaState
    {
        public static int ActiveBossNumber { get; private set; }

        public static bool HasActiveArea =>
            LandBossAreaCatalog.TryGet(ActiveBossNumber, out _);

        public static string SurfaceCode =>
            LandBossAreaCatalog.TryGet(ActiveBossNumber, out var area) ? area.SurfaceCode : string.Empty;

        public static string BunkerCode =>
            LandBossAreaCatalog.TryGet(ActiveBossNumber, out var area) ? area.BunkerCode : string.Empty;

        public static void BeginArea(int bossNumber)
        {
            ActiveBossNumber = LandBossAreaCatalog.TryGet(bossNumber, out _) ? bossNumber : 0;
        }

        public static bool TryGetActiveArea(out LandBossAreaCatalog.Definition area) =>
            LandBossAreaCatalog.TryGet(ActiveBossNumber, out area);

        public static void Clear() => ActiveBossNumber = 0;
    }
}
