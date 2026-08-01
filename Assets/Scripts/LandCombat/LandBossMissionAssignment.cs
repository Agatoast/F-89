using System;
using F89.Core;

namespace F89.LandCombat
{
    /// <summary>
    /// Links boss missions to flight-map outposts. Surface guards appear once a mission
    /// is briefed; the bunker and boss encounter unlock only after the sortie is launched.
    /// </summary>
    public static class LandBossMissionAssignment
    {
        public const int BossMissionCount = LandBossEncounter.LastBossNumber;
        public const string Boss1OutpostName = "Outpost South";
        public const string LegacyBoss1OutpostName = "Outpost 02";
        public const string Boss2OutpostName = "Outpost 01";
        public const string LegacyBoss2OutpostName = "Outpost 04";

        private static readonly string[] DesignatedOutposts =
        {
            Boss1OutpostName,
            Boss2OutpostName,
            "Outpost 05",
            "Outpost 06",
            "Outpost 07",
            "Outpost 08",
            "Outpost 09",
            "Outpost 10",
            "Outpost 12",
            "Outpost 13"
        };

        public static bool HasActiveAssignment(CharacterSaveData save) =>
            save != null && save.AssignedBossNumber >= 1 && save.AssignedBossNumber <= BossMissionCount;

        public static bool IsActiveMissionOutpost(CharacterSaveData save, string outpostName)
        {
            if (!HasActiveAssignment(save) || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            return string.Equals(save.AssignedBossOutpostName, outpostName, StringComparison.Ordinal);
        }

        /// <summary>True when the assigned primary air objectives are already complete.</summary>
        public static bool IsPrimaryMissionComplete(CharacterSaveData save)
        {
            if (!HasActiveAssignment(save))
            {
                return true;
            }

            return OutpostPrimaryObjective.AreAirObjectivesDestroyed(save.AssignedBossOutpostName);
        }

        /// <summary>
        /// Marks the active assigned mission complete without requiring a boss kill,
        /// so the pilot becomes eligible for the next mission.
        /// </summary>
        public static void ResolveAssignedMissionWithoutVictory(CharacterSaveData save)
        {
            if (!HasActiveAssignment(save))
            {
                return;
            }

            LandBossEncounter.MarkMissionResolved(save.AssignedBossNumber);
            PrepareNextAssignment(save);
        }

        public static void PrepareNextAssignment(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            var bossNumber = GetNextUndefeatedBoss(save);
            if (bossNumber <= 0)
            {
                save.AssignedBossNumber = 0;
                save.AssignedBossOutpostName = string.Empty;
                CharacterSaveRepository.WriteBossProgress(save);
                return;
            }

            var outpostName = GetDesignatedOutpostName(bossNumber);
            save.AssignedBossNumber = bossNumber;
            save.AssignedBossOutpostName = outpostName;
            save.BossMissionOutpostNames[bossNumber - 1] = outpostName;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        /// <summary>Call when the player launches from the carrier on an assigned sortie.</summary>
        public static void MarkAssignedMissionRun(CharacterSaveData save)
        {
            if (!HasActiveAssignment(save))
            {
                return;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            save.RevealedBunkerMask |= 1 << (save.AssignedBossNumber - 1);
            CharacterSaveRepository.WriteBossProgress(save);
        }

        public static bool IsBunkerRevealedAtOutpost(CharacterSaveData save, string outpostName)
        {
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            return TryGetRevealedBossForOutpost(save, outpostName, out _);
        }

        public static bool TryGetBossForOutpost(CharacterSaveData save, string outpostName, out int bossNumber)
        {
            bossNumber = 0;
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            for (var i = 0; i < BossMissionCount; i++)
            {
                if (!string.Equals(save.BossMissionOutpostNames[i], outpostName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                bossNumber = i + 1;
                return true;
            }

            return false;
        }

        public static bool TryGetRevealedBossForOutpost(CharacterSaveData save, string outpostName, out int bossNumber)
        {
            bossNumber = 0;
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            for (var i = 0; i < BossMissionCount; i++)
            {
                if ((save.RevealedBunkerMask & (1 << i)) == 0)
                {
                    continue;
                }

                if (string.Equals(save.BossMissionOutpostNames[i], outpostName, System.StringComparison.Ordinal))
                {
                    bossNumber = i + 1;
                    return true;
                }
            }

            return false;
        }

        public static string GetDesignatedOutpostName(int bossNumber)
        {
            var index = bossNumber - 1;
            if (index < 0 || index >= DesignatedOutposts.Length)
            {
                return string.Empty;
            }

            return DesignatedOutposts[index];
        }

        /// <summary>Writes the canonical boss-to-outpost links into the active save.</summary>
        public static void InitializeBossMissionOutpostLinks(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            for (var i = 0; i < BossMissionCount; i++)
            {
                save.BossMissionOutpostNames[i] = GetDesignatedOutpostName(i + 1);
            }
        }

        public static string BuildMissionObjective(CharacterSaveData save)
        {
            if (!HasActiveAssignment(save))
            {
                return "Establish air superiority over the Antarctic theater and support allied ground operations.";
            }

            if (!LandBossAreaCatalog.TryGet(save.AssignedBossNumber, out var area))
            {
                return "Establish air superiority over the Antarctic theater and support allied ground operations.";
            }

            return
                $"Proceed to {save.AssignedBossOutpostName}. Destroy the bunker building and runway tower "
                + $"from the air, then land and clear the {area.BunkerCode} bunker "
                + $"(UR level {LandBossEncounter.GetEnemyLevel(save.AssignedBossNumber)}).";
        }

        /// <summary>
        /// Chooses a friendly occupied outpost runway for campaign launch when available.
        /// Boss 1 always launches from the carrier.
        /// </summary>
        public static void AssignMissionLaunchOrigin(CharacterSaveData save)
        {
            ClearMissionLaunchOutpost(save);
        }

        /// <summary>Clears launch origin so the next sortie starts from the carrier deck.</summary>
        public static void ClearMissionLaunchOutpost(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.MissionLaunchOutpostName = string.Empty;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        /// <summary>Parks the aircraft at this outpost runway for the next sortie launch.</summary>
        public static void PersistLaunchOutpost(CharacterSaveData save, string outpostName)
        {
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            save.MissionLaunchOutpostName = outpostName;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        private static int GetNextUndefeatedBoss(CharacterSaveData save)
        {
            for (var bossNumber = LandBossEncounter.FirstBossNumber;
                 bossNumber <= LandBossEncounter.LastBossNumber;
                 bossNumber++)
            {
                if (!LandBossEncounter.IsDefeated(bossNumber))
                {
                    return bossNumber;
                }
            }

            return 0;
        }
    }

}
