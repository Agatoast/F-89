namespace F89.Core
{
    /// <summary>
    /// Secondary infantry counts for indicated-target waypoint landings (mission number + 4).
    /// Looked up by waypoint MissionNumber from CampaignWaypointLayout — not by SiteCode strings.
    /// </summary>
    public static class CampaignWaypointSecondaryCatalog
    {
        public static int GetSecondaryInfantryCount(string siteCode)
        {
            if (!CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint))
            {
                return 5;
            }

            return waypoint.MissionNumber switch
            {
                1 => 5,
                2 => 6,
                3 => 7,
                4 => 8,
                5 => 9,
                6 => 10,
                7 => 11,
                8 => 12,
                9 => 13,
                10 => 14,
                11 => 15,
                12 => 16,
                13 => 17,
                14 => 18,
                15 => 19,
                16 => 20,
                17 => 21,
                18 => 22,
                19 => 23,
                20 => 24,
                21 => 25,
                22 => 26,
                23 => 27,
                24 => 28,
                25 => 29,
                26 => 30,
                27 => 31,
                28 => 32,
                29 => 33,
                30 => 34,
                31 => 35,
                32 => 36,
                33 => 37,
                34 => 38,
                35 => 39,
                36 => 40,
                37 => 41,
                38 => 42,
                39 => 43,
                40 => 44,
                41 => 45,
                42 => 46,
                43 => 47,
                44 => 48,
                45 => 49,
                46 => 50,
                47 => 51,
                48 => 52,
                49 => 53,
                50 => 54,
                51 => 55,
                52 => 56,
                53 => 57,
                _ => 5
            };
        }
    }
}
