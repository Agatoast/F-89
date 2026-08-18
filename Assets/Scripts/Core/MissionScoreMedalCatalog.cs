namespace F89.Core

{

    /// <summary>

    /// Mission score thresholds for end-of-mission medal awards.

    /// When multiple thresholds are met, only the highest score tier is granted once per sortie.

    /// </summary>

    public static class MissionScoreMedalCatalog

    {

        public const int JointServiceCommendationMinMissionScore = 200;

        public const int JointServiceAchievementMinMissionScore = 100;



        private readonly struct ScoreThreshold

        {

            public ScoreThreshold(int minMissionScore, string ribbonId)

            {

                MinMissionScore = minMissionScore;

                RibbonId = ribbonId;

            }



            public int MinMissionScore { get; }

            public string RibbonId { get; }

        }



        private static readonly ScoreThreshold[] Thresholds =

        {

            new(JointServiceCommendationMinMissionScore, MilitaryRibbonIds.JointServiceCommendation),

            new(JointServiceAchievementMinMissionScore, MilitaryRibbonIds.JointServiceAchievement)

        };



        public static bool TryGetHighestQualifyingRibbonId(int missionScore, out string ribbonId)

        {

            ribbonId = null;

            if (missionScore < JointServiceAchievementMinMissionScore)

            {

                return false;

            }



            var bestMinScore = -1;

            var bestPrecedence = int.MaxValue;

            for (var i = 0; i < Thresholds.Length; i++)

            {

                var entry = Thresholds[i];

                if (missionScore < entry.MinMissionScore)

                {

                    continue;

                }



                if (!MilitaryRibbonCatalog.TryGetDefinition(entry.RibbonId, out var definition))

                {

                    continue;

                }



                if (entry.MinMissionScore > bestMinScore

                    || (entry.MinMissionScore == bestMinScore && definition.Precedence < bestPrecedence))

                {

                    bestMinScore = entry.MinMissionScore;

                    bestPrecedence = definition.Precedence;

                    ribbonId = entry.RibbonId;

                }

            }



            return ribbonId != null;

        }

    }

}

