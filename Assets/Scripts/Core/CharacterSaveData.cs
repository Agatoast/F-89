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
        /// <summary>UR vehicle kills indexed by level 1–10 (slot index = level - 1).</summary>
        public int[] UrVehicleKillsByLevel = new int[UrKillCredit.LevelCount];
        /// <summary>UR troop kills indexed by level 1–10 (slot index = level - 1).</summary>
        public int[] UrTroopKillsByLevel = new int[UrKillCredit.LevelCount];
        public int BestMissionScore;
        public int TotalScore;
        /// <summary>
        /// Sticky once the pilot reaches 1st LT (or 500 total score). Never cleared on demotion;
        /// enables GCMP when total score falls below zero.
        /// </summary>
        public bool HasAchievedFirstRank;
        /// <summary>Character imprisoned after total mission score fell below zero post-promotion.</summary>
        public bool IsCourtMartialed;
        public string CourtMartialedUtc = string.Empty;
        /// <summary>Character died in action and can no longer be flown.</summary>
        public bool IsKilledInAction;
        public string KilledInActionUtc = string.Empty;
        public string PortraitId = string.Empty;
        public string VehicleKillSummary = string.Empty;
        public string TroopKillSummary = string.Empty;
        public string[] EarnedRibbonIds = Array.Empty<string>();
        /// <summary>Max Hit Points for ground combat.</summary>
        public int MaxHitPoints = 100;
        /// <summary>Relative ground Move rating (3–15).</summary>
        public int Move = 3;
        /// <summary>Inherent Damage Resistance (characters start at 0; gear adds later).</summary>
        public int DamageResistance;
        public CharacterLoadoutSaveData Loadout = new CharacterLoadoutSaveData();
        public CharacterVaultSaveData Vault = new CharacterVaultSaveData();
        /// <summary>Per-type R&amp;D success chance percent (0–30). Each researched item of that type adds 0.5.</summary>
        public float ResearchHelmetChancePercent;
        public float ResearchVestChancePercent;
        public float ResearchWeaponChancePercent;
        public float ResearchBootsChancePercent;
        /// <summary>Tech level unlocked for Basic Loadout / R&amp;D floor per slot: Helmet, Vest, Weapon, Boots (1–10). Starts at 1.</summary>
        public int ResearchHelmetTechLevel = 1;
        public int ResearchVestTechLevel = 1;
        public int ResearchWeaponTechLevel = 1;
        public int ResearchBootsTechLevel = 1;
        /// <summary>Per-character boss progress. Bit 0 represents Boss 1.</summary>
        public int DefeatedBossMask;
        /// <summary>Per-character boss-area guard progress. Bit 0 represents BF1 guards.</summary>
        public int BossGuardClearedMask;
        /// <summary>Active boss mission assigned at briefing (0 = none).</summary>
        public int AssignedBossNumber;
        /// <summary>Flight-map outpost linked to the active assigned boss mission.</summary>
        public string AssignedBossOutpostName = string.Empty;
        /// <summary>Bit 0 set once Boss 1 has been launched; reveals that mission's bunker at its outpost.</summary>
        public int RevealedBunkerMask;
        /// <summary>Outpost name permanently linked to each boss mission slot.</summary>
        public string[] BossMissionOutpostNames = System.Array.Empty<string>();
        /// <summary>Saved live boss HP by boss number; -1 means no active saved encounter.</summary>
        public float[] BossPrimaryHitPoints = Array.Empty<float>();
        /// <summary>Second boss HP slot, used by the two-enemy Boss 10 encounter.</summary>
        public float[] BossSecondaryHitPoints = Array.Empty<float>();
        /// <summary>Land outposts whose surface buildings have been destroyed by this character.</summary>
        public string[] DestroyedOutpostNames = Array.Empty<string>();
        /// <summary>Land outposts cleared by ground troops and now friendly.</summary>
        public string[] FriendlyOccupiedOutpostNames = Array.Empty<string>();
        /// <summary>Outpost runway used for the current campaign sortie launch (empty = carrier).</summary>
        public string MissionLaunchOutpostName = string.Empty;
        /// <summary>Flight-map units destroyed by this character, keyed by outpost and unit label.</summary>
        public string[] DestroyedWorldTargetIds = Array.Empty<string>();
        /// <summary>Per-character default aircraft payload, restored whenever Aircraft Loadout opens.</summary>
        public bool HasDefaultAircraftPayload;
        public int[] DefaultAircraftLinkedPairWeapons = Array.Empty<int>();
        public int DefaultAircraftWingTipWeapon;
        public int DefaultAircraftGunRounds = 300;

        public string VehicleKillDisplay =>
            string.IsNullOrWhiteSpace(VehicleKillSummary)
                ? $"Enemy vehicles killed: {EnemyVehiclesKilled:N0}"
                : VehicleKillSummary;

        public string TroopKillDisplay =>
            string.IsNullOrWhiteSpace(TroopKillSummary)
                ? $"Enemy troops killed: {EnemyTroopsKilled:N0}"
                : TroopKillSummary;

        public string DisplayRankAndName
        {
            get
            {
                if (IsCourtMartialed)
                {
                    return $"(GCMP) {DisplayRank} {Name}";
                }

                return IsKilledInAction ? $"(KIA) {DisplayRank} {Name}" : $"{DisplayRank} {Name}";
            }
        }

        public string ListLabel => $"{DisplayRankAndName}  {LastPlayedLabel}";

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
