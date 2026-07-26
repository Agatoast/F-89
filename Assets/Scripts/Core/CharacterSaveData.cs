using System;

namespace F89.Core
{
    [Serializable]
    public class CharacterSaveData
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Rank = "2nd LT";
        public string Name = "Pilot";
        public string LastPlayedUtc = string.Empty;
        public string HighestAward = MilitaryMedalIds.DefaultForNewCharacter;
        public int EnemyVehiclesKilled;
        public int EnemyTroopsKilled;
        public int BestMissionScore;
        public int TotalScore;
        public string PortraitId = string.Empty;
        public string VehicleKillSummary = string.Empty;
        public string TroopKillSummary = string.Empty;
        public string[] EarnedRibbonIds = Array.Empty<string>();
        public CharacterLoadoutSaveData Loadout = new CharacterLoadoutSaveData();
        public CharacterVaultSaveData Vault = new CharacterVaultSaveData();
        /// <summary>Per-type R&amp;D success chance percent (0–30). Each researched item of that type adds 0.5.</summary>
        public float ResearchHelmetChancePercent;
        public float ResearchVestChancePercent;
        public float ResearchWeaponChancePercent;
        public float ResearchBootsChancePercent;
        /// <summary>Tech level being researched / accepted per R&amp;D slot: Helmet, Vest, Weapon, Boots (1–10).</summary>
        public int ResearchHelmetTechLevel = 1;
        public int ResearchVestTechLevel = 1;
        public int ResearchWeaponTechLevel = 1;
        public int ResearchBootsTechLevel = 1;

        public string VehicleKillDisplay =>
            string.IsNullOrWhiteSpace(VehicleKillSummary)
                ? $"Enemy vehicles killed: {EnemyVehiclesKilled:N0}"
                : VehicleKillSummary;

        public string TroopKillDisplay =>
            string.IsNullOrWhiteSpace(TroopKillSummary)
                ? $"Enemy troops killed: {EnemyTroopsKilled:N0}"
                : TroopKillSummary;

        public string DisplayRankAndName => $"{DisplayRank} {Name}";

        public string LastPlayedLabel
        {
            get
            {
                if (string.IsNullOrEmpty(LastPlayedUtc)
                    || !DateTime.TryParse(LastPlayedUtc, out var played))
                {
                    return "Never played";
                }

                return played.ToLocalTime().ToString("g");
            }
        }

        public string ListLabel => $"{DisplayRank} {Name}  {LastPlayedLabel}";

        public string DisplayRank => AbbreviateRank(Rank);

        private static string AbbreviateRank(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
            {
                return rank;
            }

            return rank
                .Replace("Lieutenant", "LT")
                .Replace("Lieutentant", "LT")
                .Replace("Lt", "LT");
        }
    }

    [Serializable]
    public class CharacterSaveCollection
    {
        public CharacterSaveData[] Saves = Array.Empty<CharacterSaveData>();
    }
}
