using System;

namespace SaveAntarctica.BunkerDefense.Core
{
    /// <summary>
    /// Same 18 F-89 sites as Tower Defense / Base Defense. Bunker Defense replaces that minigame.
    /// </summary>
    public static class DefenseSiteCatalog
    {
        public const int MissionCount = 18;

        private static readonly string[] SiteCodeByMap =
        {
            "OP-SOUTH",
            "OP-01",
            "OP-13",
            "OP-13-SE",
            "OP-43",
            "OP-21",
            "OP-18",
            "OP-05",
            "OP-33",
            "OP-27",
            "OP-41",
            "OP-44",
            "OP-27",
            "OP-41",
            "STN-CONCORDIA",
            "STN-NEUMAYER-III",
            "STN-HALLEY-VI",
            "OP-44"
        };

        /// <summary>F-89 campaign mission numbers that launch from these sites.</summary>
        private static readonly int[] F89MissionNumberByMap =
        {
            1, 5, 8, 10, 13, 15, 27, 31, 34, 38, 42, 49, 38, 42, 44, 46, 47, 49
        };

        public static string GetSiteCode(int mapNumber)
        {
            if (mapNumber < 1 || mapNumber > SiteCodeByMap.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(mapNumber), mapNumber, $"Defense maps are 1-{MissionCount}.");
            }

            return SiteCodeByMap[mapNumber - 1];
        }

        public static bool TryGetSiteCode(int mapNumber, out string siteCode)
        {
            if (mapNumber < 1 || mapNumber > SiteCodeByMap.Length)
            {
                siteCode = string.Empty;
                return false;
            }

            siteCode = SiteCodeByMap[mapNumber - 1];
            return true;
        }

        public static string GetMapLabel(int mapNumber) => GetBriefingLabel(mapNumber);

        public static string GetBriefingLabel(int mapNumber)
        {
            return $"Map {mapNumber:00} - {GetSiteCode(mapNumber)}";
        }

        public static int GetF89MissionNumber(int mapNumber)
        {
            if (mapNumber < 1 || mapNumber > F89MissionNumberByMap.Length)
            {
                return -1;
            }

            return F89MissionNumberByMap[mapNumber - 1];
        }

        public static int GetMapNumber(string siteOrMissionId)
        {
            if (string.IsNullOrWhiteSpace(siteOrMissionId))
            {
                return -1;
            }

            var normalized = siteOrMissionId.Trim();
            for (var i = 0; i < SiteCodeByMap.Length; i++)
            {
                if (string.Equals(SiteCodeByMap[i], normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }

            if (TryParseMissionNumber(normalized, out var missionNumber))
            {
                var introMap = GetIntroMapForMissionNumber(missionNumber);
                if (introMap > 0)
                {
                    return introMap;
                }

                for (var i = 0; i < F89MissionNumberByMap.Length; i++)
                {
                    if (F89MissionNumberByMap[i] == missionNumber)
                    {
                        return i + 1;
                    }
                }
            }

            return -1;
        }

        /// <summary>Campaign-only defense triggers that use an intro map before OP/STN assault sites.</summary>
        public static int GetIntroMapForMissionNumber(int missionNumber) =>
            missionNumber == 3 ? 1 : -1;

        public static bool TryParseMissionNumber(string value, out int missionNumber)
        {
            missionNumber = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var text = value.Trim();
            if (text.StartsWith("Mission", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring("Mission".Length).Trim().TrimStart('-', '\u2014', ' ');
            }

            return int.TryParse(text, out missionNumber) && missionNumber > 0;
        }
    }

    /// <summary>Tower Defense name kept so F-89 can swap the plug-in without a catalog rename.</summary>
    public static class BaseDefenseSiteCatalog
    {
        public const int MissionCount = DefenseSiteCatalog.MissionCount;

        public static string GetSiteCode(int mapNumber) => DefenseSiteCatalog.GetSiteCode(mapNumber);

        public static bool TryGetSiteCode(int mapNumber, out string siteCode) =>
            DefenseSiteCatalog.TryGetSiteCode(mapNumber, out siteCode);

        public static int GetMapNumber(string siteCode) => DefenseSiteCatalog.GetMapNumber(siteCode);
    }
}
