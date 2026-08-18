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
        public const int BossMissionCount = LandBossEncounter.CampaignLastBossNumber;
        public const string Boss1OutpostName = "Outpost South";
        public const string LegacyBoss1OutpostName = "Outpost 02";
        public const string Boss2OutpostName = "Outpost 01";
        public const string LegacyBoss2OutpostName = "Outpost 04";
        public const string Boss10OutpostName = "Outpost 13 SE";
        public const string LegacyBoss10OutpostName = "Outpost 13";

        // Boss N → outpost/station label from Docs/MissionCatalog.md (map layout labels).
        private static readonly string[] DesignatedOutposts =
        {
            Boss1OutpostName,           // Boss01 — Mission 01 OP-South
            Boss2OutpostName,           // Boss02 — Mission 05 OP-01
            LegacyBoss10OutpostName,    // Boss03 — Mission 08 OP-13
            Boss10OutpostName,          // Boss04 — Mission 10 OP-13-SE
            "Outpost 43",               // Boss05 — Mission 13 OP-43
            "Outpost 21",               // Boss06 — Mission 15 OP-21
            "Rothera Research",         // Boss07 — Mission 18 STN-Rothera
            "Palmer Station",           // Boss08 — Mission 21 STN-Palmer
            "Marambio Base",            // Boss09 — Mission 24 STN-Marambio
            "Outpost 18",               // Boss10 — Mission 27 OP-18
            "Outpost 05",               // Boss11 — Mission 31 OP-05
            "Outpost 33",               // Boss12 — Mission 34 OP-33
            "Outpost 27",               // Boss13 — Mission 38 OP-27
            "Outpost 41",               // Boss14 — Mission 42 OP-41
            "Concordia Station",        // Boss15 — Mission 44 STN-Concordia
            "Neumayer III",             // Boss16 — Mission 46 STN-Neumayer-III
            "Halley VI",                // Boss17 — Mission 47 STN-Halley-VI
            "Outpost 44",               // Boss18 — Mission 49 OP-44
            "Amundsen-Scott"            // Boss19 — Mission 53 STN-Amundsen-Scott
        };

        public static bool HasActiveAssignment(CharacterSaveData save) =>
            save != null && save.AssignedBossNumber >= 1 && save.AssignedBossNumber <= BossMissionCount;

        public static bool IsActiveMissionOutpost(CharacterSaveData save, string outpostName)
        {
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (!CampaignMissionObjectiveState.TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignMissionSiteCatalog.IsOutpostOrStationSite(siteCode))
            {
                return false;
            }

            if (!CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var baseName))
            {
                return false;
            }

            return string.Equals(baseName, outpostName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when the current catalog mission primary objectives are complete.</summary>
        public static bool IsPrimaryMissionComplete(CharacterSaveData save) =>
            CampaignMissionObjectiveState.IsPrimaryMissionComplete(save);

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
            // Catalog mission order owns progression. Boss links follow the current SiteCode.
            SyncAssignmentToCampaignMission(save);
        }

        /// <summary>
        /// Links bunker/boss state to the current catalog mission outpost when applicable.
        /// Waypoint missions clear the boss assignment so it cannot steal the blue marker.
        /// </summary>
        public static void SyncAssignmentToCampaignMission(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureBossMissionInitialized(save);
            CampaignMissionProgress.EnsureInitialized(save);

            if (!CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var siteCode)
                || CampaignMissionSiteCatalog.IsWaypointSite(siteCode)
                || !CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var outpostName))
            {
                save.AssignedBossNumber = 0;
                save.AssignedBossOutpostName = string.Empty;
                CharacterSaveRepository.WriteBossProgress(save);
                return;
            }

            save.AssignedBossOutpostName = outpostName;
            var bossNumber = FindDesignatedBossForOutpost(outpostName);
            save.AssignedBossNumber = bossNumber;
            if (bossNumber >= 1 && bossNumber <= BossMissionCount)
            {
                save.BossMissionOutpostNames[bossNumber - 1] = outpostName;
                if (MissionProgressState.IsMissionInProgress())
                {
                    save.RevealedBunkerMask |= 1 << (bossNumber - 1);
                }
            }

            CharacterSaveRepository.WriteBossProgress(save);
        }

        private static int FindDesignatedBossForOutpost(string outpostName)
        {
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return 0;
            }

            for (var i = 0; i < DesignatedOutposts.Length; i++)
            {
                if (string.Equals(DesignatedOutposts[i], outpostName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        CampaignMapLayoutState.NormalizeSiteName(DesignatedOutposts[i]),
                        CampaignMapLayoutState.NormalizeSiteName(outpostName),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1;
                }
            }

            return 0;
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
                if (!OutpostNamesMatch(save.BossMissionOutpostNames[i], outpostName))
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

                if (!OutpostNamesMatch(save.BossMissionOutpostNames[i], outpostName))
                {
                    continue;
                }

                bossNumber = i + 1;
                return true;
            }

            return false;
        }

        private static bool OutpostNamesMatch(string linkedOutpost, string outpostName)
        {
            if (string.IsNullOrWhiteSpace(linkedOutpost) || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (string.Equals(linkedOutpost, outpostName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(
                CampaignMapLayoutState.NormalizeSiteName(linkedOutpost),
                CampaignMapLayoutState.NormalizeSiteName(outpostName),
                StringComparison.OrdinalIgnoreCase);
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
            if (save == null
                || save.BossMissionOutpostNames == null
                || save.BossMissionOutpostNames.Length == 0)
            {
                return;
            }

            for (var i = 0; i < BossMissionCount && i < save.BossMissionOutpostNames.Length; i++)
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

        /// <summary>Parks the aircraft at this outpost runway for the next sortie launch.</summary>
        public static void PersistLaunchOutpost(CharacterSaveData save, string outpostName)
        {
            if (save == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return;
            }

            var trimmed = outpostName.Trim();
            if (CampaignMapLayoutState.TryGetSite(trimmed, out var site)
                && site != null
                && !string.IsNullOrWhiteSpace(site.Label))
            {
                trimmed = CampaignMapLayoutState.NormalizeSiteName(site.Label);
            }

            if (string.Equals(save.MissionLaunchOutpostName, trimmed, StringComparison.Ordinal))
            {
                return;
            }

            save.MissionLaunchOutpostName = trimmed;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        private static int GetNextUndefeatedBoss(CharacterSaveData save)
        {
            for (var bossNumber = LandBossEncounter.FirstBossNumber;
                 bossNumber <= LandBossEncounter.CampaignLastBossNumber;
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
