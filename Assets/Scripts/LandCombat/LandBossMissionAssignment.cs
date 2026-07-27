using F89.Core;

namespace F89.LandCombat
{
    /// <summary>
    /// Links boss missions to flight-map outposts. Bunkers stay hidden until the mission
    /// is briefed, launched, and then persist at that outpost for the character.
    /// </summary>
    public static class LandBossMissionAssignment
    {
        public const int BossMissionCount = LandBossEncounter.LastBossNumber;

        private static readonly string[] DesignatedOutposts =
        {
            "Outpost 02",
            "Outpost 04",
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
                $"Proceed to {save.AssignedBossOutpostName}. After launch, the {area.BunkerCode} bunker "
                + $"will appear at that outpost. Neutralize surface guards and eliminate the UR level "
                + $"{LandBossEncounter.GetEnemyLevel(save.AssignedBossNumber)} boss force.";
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
