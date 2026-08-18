using System;
using System.Collections.Generic;
using System.IO;
using F89.Enemies;
using F89.LandCombat;
using F89.UI;
using UnityEngine;

namespace F89.Core
{
    public static class CharacterSaveRepository
    {
        private const string SaveFileName = "character_saves.json";
        private const string LastSelectedSaveIdKey = "F89.LastSelectedSaveId";
        private const string LastSelectedCampaignSaveIdKey = "F89.LastSelectedSaveId.Campaign";
        private const string LastSelectedFreeFlightSaveIdKey = "F89.LastSelectedSaveId.FreeFlight";
        private const string AllSavesClearedKey = "F89.AllSavesCleared.v2";
        private const int MaxCharacterNameLength = CharacterNameLimits.MaxLength;

        private static List<CharacterSaveData> cachedSaves;
        private static bool isLoaded;

        public static IReadOnlyList<CharacterSaveData> Saves
        {
            get
            {
                EnsureLoaded();
                return cachedSaves;
            }
        }

        public static CharacterSaveData FindById(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var save in cachedSaves)
            {
                if (save != null && save.Id == id)
                {
                    return save;
                }
            }

            return null;
        }

        public static CharacterSaveData CreateSave(string name, string rank = "2LT")
        {
            EnsureLoaded();

            var save = new CharacterSaveData
            {
                Id = Guid.NewGuid().ToString("N"),
                Rank = string.IsNullOrWhiteSpace(rank) ? "2LT" : rank.Trim(),
                Name = TrimCharacterName(name),
                LastPlayedUtc = DateTime.UtcNow.ToString("o"),
                HighestAward = MilitaryMedalIds.DefaultForNewCharacter,
                EarnedRibbonIds = new[] { MilitaryRibbonIds.FruitSalad },
                EarnedRibbonCounts = new[] { 1 },
                MaxHitPoints = 100,
                Move = 3,
                DamageResistance = 0,
                PlayModeKind = (int)GamePlayModeState.ActivePlayMode,
                CampaignMissionNumber = 1,
                HasCompletedCampaign = false
            };

            EnsureGearInitialized(save);
            LandDefaultLoadout.EquipStarterEquipment(save);
            GameplaySessionBootstrap.ClearStalePersistedSession();
            cachedSaves.Add(save);
            WriteToDisk();
            return save;
        }

        public static void TouchLastPlayed(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.LastPlayedUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk();
        }

        public static string GetLastSelectedSaveId()
        {
            var modeKey = GetLastSelectedSaveIdKeyForActiveMode();
            var modeValue = PlayerPrefs.GetString(modeKey, string.Empty);
            if (!string.IsNullOrEmpty(modeValue))
            {
                return modeValue;
            }

            // Legacy single-key preference (pre Campaign / Free Flight split).
            return PlayerPrefs.GetString(LastSelectedSaveIdKey, string.Empty);
        }

        public static void SetLastSelectedSaveId(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            PlayerPrefs.SetString(GetLastSelectedSaveIdKeyForActiveMode(), saveId);
            PlayerPrefs.SetString(LastSelectedSaveIdKey, saveId);
            PlayerPrefs.Save();
        }

        private static string GetLastSelectedSaveIdKeyForActiveMode()
        {
            return GamePlayModeState.IsCampaign
                ? LastSelectedCampaignSaveIdKey
                : LastSelectedFreeFlightSaveIdKey;
        }

        public static void WritePortrait(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            WriteToDisk();
        }

        public static bool DeleteSave(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var removed = cachedSaves.RemoveAll(save => save != null && save.Id == id);
            if (removed <= 0)
            {
                return false;
            }

            WipeCharacterSidecars(id);

            if (GetLastSelectedSaveId() == id)
            {
                PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
                PlayerPrefs.Save();
            }

            if (CharacterSessionState.ActiveSave != null && CharacterSessionState.ActiveSave.Id == id)
            {
                CharacterSessionState.ActiveSave = null;
                CharacterGearSession.Bind(null);
                AircraftLoadoutState.ResetForNewSortie();
                LandMissionHandoffState.Clear();
            }

            WriteToDisk();
            return true;
        }

        public static void MoveSaveToFront(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            var index = cachedSaves.FindIndex(save => save != null && save.Id == id);
            if (index <= 0)
            {
                return;
            }

            var save = cachedSaves[index];
            cachedSaves.RemoveAt(index);
            cachedSaves.Insert(0, save);
            WriteToDisk();
        }

        public static void ClearAllSaves()
        {
            EnsureLoaded();
            foreach (var save in cachedSaves)
            {
                if (save != null && !string.IsNullOrEmpty(save.Id))
                {
                    WipeCharacterSidecars(save.Id);
                }
            }

            CharacterPortraitService.DeleteAllCustomPortraits();
            cachedSaves.Clear();
            PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
            PlayerPrefs.DeleteKey(LastSelectedCampaignSaveIdKey);
            PlayerPrefs.DeleteKey(LastSelectedFreeFlightSaveIdKey);
            PlayerPrefs.Save();
            CharacterSessionState.ActiveSave = null;
            CharacterGearSession.Bind(null);
            AircraftLoadoutState.ResetForNewSortie();
            LandMissionHandoffState.Clear();
            WriteToDisk();
        }

        private static void WipeCharacterSidecars(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            CharacterPortraitService.DeleteCustomPortrait(saveId);
            LandDefaultLoadout.ClearPrefsForSave(saveId);
        }

        public static void ApplyScorePenalty(CharacterSaveData save, int penalty)
        {
            // Legacy bail-out penalties removed — career score is mission totals only.
        }

        public static void ApplyTotalScoreFractionPenalty(CharacterSaveData save, float fractionKept)
        {
            // Legacy failure score cuts removed — demotion uses current TotalScore only.
        }

        public static void MarkCourtMartialed(CharacterSaveData save)
        {
            if (save == null || save.IsCourtMartialed || save.IsKilledInAction)
            {
                return;
            }

            EnsureLoaded();
            save.IsCourtMartialed = true;
            save.CourtMartialedUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk();
            ClearActiveSessionIfMatches(save);
        }

        private static void ClearActiveSessionIfMatches(CharacterSaveData save)
        {
            if (CharacterSessionState.ActiveSave == null || CharacterSessionState.ActiveSave.Id != save.Id)
            {
                return;
            }

            CharacterSessionState.ActiveSave = null;
            CharacterGearSession.Bind(null);
            AircraftLoadoutState.ResetForNewSortie();
            LandMissionHandoffState.Clear();
        }

        public static void MarkKilledInAction(CharacterSaveData save)
        {
            if (save == null || save.IsKilledInAction)
            {
                return;
            }

            EnsureLoaded();
            save.IsKilledInAction = true;
            save.KilledInActionUtc = DateTime.UtcNow.ToString("o");
            WriteToDisk();
            ClearActiveSessionIfMatches(save);
        }

        public static IReadOnlyList<CharacterSaveData> GetKilledInActionSaves()
        {
            EnsureLoaded();
            var results = new List<CharacterSaveData>();
            foreach (var save in cachedSaves)
            {
                if (save != null && save.IsKilledInAction)
                {
                    results.Add(save);
                }
            }

            return results;
        }

        /// <summary>Syncs vehicle/troop kill folders from save arrays and destroyed flight-map targets.</summary>
        public static void SyncVehicleKillCredit(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            if (EnsureUrKillArrays(save))
            {
                WriteToDisk();
            }
        }

        private static void NormalizeUrKillProgress()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                if (EnsureUrKillArrays(save))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        public static bool EnsureUrKillArrays(CharacterSaveData save)
        {
            return EnsureUrKillArrays(save, out _);
        }

        /// <returns>True when kill folders or legacy totals were repaired.</returns>
        private static bool EnsureUrKillArrays(CharacterSaveData save, out bool repaired)
        {
            repaired = false;
            if (save == null)
            {
                return false;
            }

            var vehicleBefore = UrKillCredit.Sum(save.UrVehicleKillsByLevel);
            var troopBefore = UrKillCredit.Sum(save.UrTroopKillsByLevel);
            var legacyVehicleBefore = save.EnemyVehiclesKilled;
            var legacyTroopBefore = save.EnemyTroopsKilled;

            ResizeUrKillArray(ref save.UrVehicleKillsByLevel);
            ResizeUrKillArray(ref save.UrTroopKillsByLevel);

            if (UrKillCredit.Sum(save.UrVehicleKillsByLevel) == 0)
            {
                repaired |= BackfillUrVehicleKillsFromDestroyedTargets(save);
            }

            if (UrKillCredit.Sum(save.UrTroopKillsByLevel) == 0)
            {
                repaired |= BackfillUrTroopKillsFromDestroyedTargets(save);
            }

            save.EnemyVehiclesKilled = Mathf.Max(
                save.EnemyVehiclesKilled,
                UrKillCredit.Sum(save.UrVehicleKillsByLevel));
            save.EnemyTroopsKilled = Mathf.Max(
                save.EnemyTroopsKilled,
                UrKillCredit.Sum(save.UrTroopKillsByLevel));

            repaired |= vehicleBefore != UrKillCredit.Sum(save.UrVehicleKillsByLevel)
                || troopBefore != UrKillCredit.Sum(save.UrTroopKillsByLevel)
                || legacyVehicleBefore != save.EnemyVehiclesKilled
                || legacyTroopBefore != save.EnemyTroopsKilled;
            return repaired;
        }

        private static void ResizeUrKillArray(ref int[] values)
        {
            if (values != null && values.Length == UrKillCredit.LevelCount)
            {
                return;
            }

            var resized = new int[UrKillCredit.LevelCount];
            if (values != null)
            {
                var copyLength = Mathf.Min(values.Length, resized.Length);
                for (var i = 0; i < copyLength; i++)
                {
                    resized[i] = values[i];
                }
            }

            values = resized;
        }

        private static bool BackfillUrVehicleKillsFromDestroyedTargets(CharacterSaveData save)
        {
            if (save?.DestroyedWorldTargetIds == null || save.DestroyedWorldTargetIds.Length == 0)
            {
                return false;
            }

            var counts = new int[UrKillCredit.LevelCount];
            var credited = false;
            var catalog = Enemies.VehicleUnitCatalog.LoadOrDefault();
            for (var i = 0; i < save.DestroyedWorldTargetIds.Length; i++)
            {
                if (!TryParseDestroyedUrTarget(
                        save.DestroyedWorldTargetIds[i],
                        catalog,
                        out var definition)
                    || definition == null
                    || definition.isTroop)
                {
                    continue;
                }

                counts[UrKillCredit.LevelToIndex(definition.vehicleLevel)]++;
                credited = true;
            }

            if (!credited)
            {
                return false;
            }

            save.UrVehicleKillsByLevel = counts;
            return true;
        }

        private static bool BackfillUrTroopKillsFromDestroyedTargets(CharacterSaveData save)
        {
            if (save?.DestroyedWorldTargetIds == null || save.DestroyedWorldTargetIds.Length == 0)
            {
                return false;
            }

            var counts = new int[UrKillCredit.LevelCount];
            var credited = false;
            var catalog = Enemies.VehicleUnitCatalog.LoadOrDefault();
            for (var i = 0; i < save.DestroyedWorldTargetIds.Length; i++)
            {
                if (!TryParseDestroyedUrTarget(
                        save.DestroyedWorldTargetIds[i],
                        catalog,
                        out var definition)
                    || definition == null
                    || !definition.isTroop)
                {
                    continue;
                }

                counts[UrKillCredit.LevelToIndex(definition.vehicleLevel)]++;
                credited = true;
            }

            if (!credited)
            {
                return false;
            }

            save.UrTroopKillsByLevel = counts;
            return true;
        }

        private static bool TryParseDestroyedUrTarget(
            string targetId,
            VehicleUnitCatalog catalog,
            out VehicleUnitDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(targetId))
            {
                return false;
            }

            var separator = targetId.IndexOf("::", StringComparison.Ordinal);
            if (separator < 0 || separator >= targetId.Length - 2)
            {
                return false;
            }

            var label = targetId.Substring(separator + 2).Trim();
            return TryResolveUrDefinitionFromTargetLabel(catalog, label, out definition);
        }

        /// <summary>
        /// Grants a ribbon the first time only (returns true for award UI).
        /// Repeat star devices use <see cref="TryIncrementRibbonAwardCount"/>.
        /// </summary>
        public static bool TryGrantRibbon(CharacterSaveData save, string ribbonId)
        {
            return TryGrantRibbon(save, ribbonId, allowRepeatAward: false);
        }

        /// <summary>
        /// Increments a repeat-award ribbon when this sortie earned another device.
        /// </summary>
        public static bool TryIncrementRibbonAwardCount(CharacterSaveData save, string ribbonId)
        {
            if (save == null || string.IsNullOrEmpty(ribbonId))
            {
                return false;
            }

            EnsureLoaded();
            EnsureRibbonCountsInitialized(save);
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                if (save.EarnedRibbonIds[i] != ribbonId)
                {
                    continue;
                }

                if (!MilitaryRibbonCatalog.SupportsAwardDevices(ribbonId)
                    || MilitaryRibbonCatalog.IsSingleAwardOnly(ribbonId)
                    || save.EarnedRibbonCounts[i] >= MilitaryRibbonAwardDevices.MaxTrackedAwards)
                {
                    return false;
                }

                save.EarnedRibbonCounts[i]++;
                WriteToDisk();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Score medals: first award or repeat device from this sortie's mission score only.
        /// Awards the single highest score tier for the sortie (200+ → Commendation, not Achievement).
        /// </summary>
        public static bool TryGrantScoreMedalForMission(
            CharacterSaveData save,
            int missionScore,
            out string ribbonId,
            out int awardCountAfterGrant)
        {
            ribbonId = null;
            awardCountAfterGrant = 0;
            if (!MissionScoreMedalCatalog.TryGetHighestQualifyingRibbonId(missionScore, out ribbonId))
            {
                return false;
            }

            if (TryGrantRibbon(save, ribbonId))
            {
                awardCountAfterGrant = 1;
                return true;
            }

            if (!TryIncrementRibbonAwardCount(save, ribbonId))
            {
                return false;
            }

            awardCountAfterGrant = GetRibbonAwardCount(save, ribbonId);
            return true;
        }

        /// <summary>
        /// Backfill only: grant if missing; never increment an existing ribbon count.
        /// </summary>
        public static bool TryEnsureRibbonGranted(CharacterSaveData save, string ribbonId)
        {
            return TryGrantRibbon(save, ribbonId, allowRepeatAward: false);
        }

        public static bool HasEarnedRibbon(CharacterSaveData save, string ribbonId)
        {
            return GetRibbonAwardCount(save, ribbonId) > 0;
        }

        private static bool TryGrantRibbon(CharacterSaveData save, string ribbonId, bool allowRepeatAward)
        {
            if (save == null || string.IsNullOrEmpty(ribbonId))
            {
                return false;
            }

            EnsureLoaded();
            EnsureRibbonCountsInitialized(save);
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                if (save.EarnedRibbonIds[i] != ribbonId)
                {
                    continue;
                }

                return false;
            }

            var idUpdated = new string[save.EarnedRibbonIds.Length + 1];
            var countUpdated = new int[save.EarnedRibbonCounts.Length + 1];
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                idUpdated[i] = save.EarnedRibbonIds[i];
                countUpdated[i] = save.EarnedRibbonCounts[i];
            }

            idUpdated[idUpdated.Length - 1] = ribbonId;
            countUpdated[countUpdated.Length - 1] = 1;
            save.EarnedRibbonIds = idUpdated;
            save.EarnedRibbonCounts = countUpdated;
            var derived = MilitaryMedalCatalog.GetHighestMedalIdFromEarnedRibbons(save.EarnedRibbonIds);
            save.HighestAward = MilitaryMedalCatalog.NormalizeAwardId(derived);
            WriteToDisk();
            return true;
        }

        public static int GetRibbonAwardCount(CharacterSaveData save, string ribbonId)
        {
            if (save == null || string.IsNullOrEmpty(ribbonId))
            {
                return 0;
            }

            EnsureRibbonCountsInitialized(save);
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                if (save.EarnedRibbonIds[i] == ribbonId)
                {
                    return Mathf.Max(0, save.EarnedRibbonCounts[i]);
                }
            }

            return 0;
        }

        private static void EnsureRibbonCountsInitialized(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.EarnedRibbonIds ??= Array.Empty<string>();
            if (save.EarnedRibbonCounts == null || save.EarnedRibbonCounts.Length != save.EarnedRibbonIds.Length)
            {
                var counts = new int[save.EarnedRibbonIds.Length];
                for (var i = 0; i < counts.Length; i++)
                {
                    if (MilitaryRibbonCatalog.IsSingleAwardOnly(save.EarnedRibbonIds[i]))
                    {
                        counts[i] = 1;
                        continue;
                    }

                    var existing = save.EarnedRibbonCounts != null && i < save.EarnedRibbonCounts.Length
                        ? save.EarnedRibbonCounts[i]
                        : 1;
                    counts[i] = Mathf.Clamp(existing <= 0 ? 1 : existing, 1, MilitaryRibbonAwardDevices.MaxTrackedAwards);
                }

                save.EarnedRibbonCounts = counts;
            }
            else
            {
                for (var i = 0; i < save.EarnedRibbonCounts.Length; i++)
                {
                    if (save.EarnedRibbonCounts[i] <= 0)
                    {
                        save.EarnedRibbonCounts[i] = 1;
                    }
                    else if (MilitaryRibbonCatalog.IsSingleAwardOnly(save.EarnedRibbonIds[i]))
                    {
                        save.EarnedRibbonCounts[i] = 1;
                    }
                    else if (save.EarnedRibbonCounts[i] > MilitaryRibbonAwardDevices.MaxTrackedAwards)
                    {
                        save.EarnedRibbonCounts[i] = MilitaryRibbonAwardDevices.MaxTrackedAwards;
                    }
                }
            }
        }

        public static void EnsureGearInitialized(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            save.Loadout ??= new CharacterLoadoutSaveData();
            save.Vault ??= new CharacterVaultSaveData();
            if (save.Vault.SlotCount <= 0)
            {
                save.Vault.SlotCount = CharacterVaultSaveData.DefaultSlotCount;
            }

            NormalizeLoadoutInventory(save.Loadout);
            NormalizeVaultItems(save.Vault);
            EnsureBossProgressInitialized(save);
            EnsureBossMissionInitialized(save);
            SyncDefeatedBossMaskFromProgress(save);
            SyncBossKillAwardMaskFromCampaignProgress(save);
            EnsureBossKillAwardMaskInitialized(save);
            EnsureUrKillArrays(save);
        }

        /// <summary>
        /// Grants boss-kill awards for boss missions whose outpost was made friendly after END MISSION.
        /// Repairs saves where bunker kills completed the campaign step but masks were never written.
        /// </summary>
        public static void SyncBossKillAwardMaskFromCampaignProgress(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureBossMissionInitialized(save);
            var changed = false;
            for (var bossNumber = LandBossEncounter.FirstBossNumber;
                 bossNumber <= LandBossEncounter.LastBossNumber;
                 bossNumber++)
            {
                var bit = 1 << (bossNumber - 1);
                if ((save.BossKillAwardMask & bit) != 0)
                {
                    continue;
                }

                var outpostIndex = bossNumber - 1;
                if (save.BossMissionOutpostNames == null
                    || outpostIndex >= save.BossMissionOutpostNames.Length)
                {
                    continue;
                }

                var outpostName = save.BossMissionOutpostNames[outpostIndex];
                if (string.IsNullOrWhiteSpace(outpostName)
                    || !AntarcticaOutpostState.IsFriendlyOccupied(save, outpostName))
                {
                    continue;
                }

                save.BossKillAwardMask |= bit;
                save.DefeatedBossMask |= bit;
                changed = true;
            }

            if (changed)
            {
                WriteBossProgress(save);
            }
        }

        /// <summary>
        /// Backfills <see cref="CharacterSaveData.BossKillAwardMask"/> from legacy defeat data once.
        /// </summary>
        public static void EnsureBossKillAwardMaskInitialized(CharacterSaveData save)
        {
            if (save == null || save.BossKillAwardMask != 0 || save.DefeatedBossMask == 0)
            {
                return;
            }

            save.BossKillAwardMask = save.DefeatedBossMask;
            WriteBossProgress(save);
        }

        /// <summary>
        /// Ensures <see cref="CharacterSaveData.DefeatedBossMask"/> reflects saved boss HP slots
        /// (0 HP = defeated) for characters created before defeat tracking was reliable.
        /// </summary>
        public static void SyncDefeatedBossMaskFromProgress(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureBossProgressInitialized(save);
            var changed = false;
            for (var bossNumber = LandBossEncounter.FirstBossNumber;
                 bossNumber <= LandBossEncounter.LastBossNumber;
                 bossNumber++)
            {
                var bit = 1 << (bossNumber - 1);
                if ((save.DefeatedBossMask & bit) != 0)
                {
                    continue;
                }

                if (bossNumber >= save.BossPrimaryHitPoints.Length)
                {
                    continue;
                }

                var savedHp = save.BossPrimaryHitPoints[bossNumber];
                if (savedHp >= -0.001f && savedHp <= 0.001f)
                {
                    save.DefeatedBossMask |= bit;
                    changed = true;
                }
            }

            if (changed)
            {
                WriteBossProgress(save);
            }
        }

        private static bool TryResolveUrDefinitionFromTargetLabel(
            VehicleUnitCatalog catalog,
            string label,
            out VehicleUnitDefinition definition)
        {
            definition = null;
            if (catalog == null || string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            if (Enemies.OutpostVehicleSpawner.TryParseUnitSlotLabel(
                    label,
                    out var designation,
                    out var abbreviation,
                    out _))
            {
                if (designation != VehicleUnitDesignation.UR)
                {
                    return false;
                }

                return catalog.TryGetByAbbreviation(
                    abbreviation,
                    VehicleUnitDesignation.UR,
                    out definition);
            }

            return catalog.TryGetByAbbreviation(label, VehicleUnitDesignation.UR, out definition);
        }

        public static void EnsureBossMissionInitialized(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            const int bossSlotCount = LandBossEncounter.LastBossNumber;
            if (save.BossMissionOutpostNames == null || save.BossMissionOutpostNames.Length != bossSlotCount)
            {
                save.BossMissionOutpostNames = new string[bossSlotCount];
            }

            MigrateBoss1Outpost(save);
            MigrateBoss2Outpost(save);
            MigrateBoss10Outpost(save);
            LandBossMissionAssignment.InitializeBossMissionOutpostLinks(save);
        }

        private static void MigrateBoss1Outpost(CharacterSaveData save)
        {
            if (save.BossMissionOutpostNames.Length == 0)
            {
                return;
            }

            var linked = save.BossMissionOutpostNames[0];
            if (string.IsNullOrWhiteSpace(linked) || linked == LandBossMissionAssignment.LegacyBoss1OutpostName)
            {
                save.BossMissionOutpostNames[0] = LandBossMissionAssignment.Boss1OutpostName;
            }

            if (save.AssignedBossNumber == 1
                && (string.IsNullOrWhiteSpace(save.AssignedBossOutpostName)
                    || save.AssignedBossOutpostName == LandBossMissionAssignment.LegacyBoss1OutpostName))
            {
                save.AssignedBossOutpostName = LandBossMissionAssignment.Boss1OutpostName;
            }
        }

        private static void MigrateBoss2Outpost(CharacterSaveData save)
        {
            if (save.BossMissionOutpostNames.Length < 2)
            {
                return;
            }

            var linked = save.BossMissionOutpostNames[1];
            if (string.IsNullOrWhiteSpace(linked) || linked == LandBossMissionAssignment.LegacyBoss2OutpostName)
            {
                save.BossMissionOutpostNames[1] = LandBossMissionAssignment.Boss2OutpostName;
            }

            if (save.AssignedBossNumber == 2
                && (string.IsNullOrWhiteSpace(save.AssignedBossOutpostName)
                    || save.AssignedBossOutpostName == LandBossMissionAssignment.LegacyBoss2OutpostName))
            {
                save.AssignedBossOutpostName = LandBossMissionAssignment.Boss2OutpostName;
            }
        }

        private static void MigrateBoss10Outpost(CharacterSaveData save)
        {
            if (save.BossMissionOutpostNames.Length < 10)
            {
                return;
            }

            var linked = save.BossMissionOutpostNames[9];
            if (string.IsNullOrWhiteSpace(linked)
                || string.Equals(linked, LandBossMissionAssignment.LegacyBoss10OutpostName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(linked, LandBossMissionAssignment.Boss10OutpostName, StringComparison.OrdinalIgnoreCase))
            {
                save.BossMissionOutpostNames[9] = "Outpost 18";
            }

            if (save.AssignedBossNumber == 10
                && (string.IsNullOrWhiteSpace(save.AssignedBossOutpostName)
                    || string.Equals(
                        save.AssignedBossOutpostName,
                        LandBossMissionAssignment.LegacyBoss10OutpostName,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        save.AssignedBossOutpostName,
                        LandBossMissionAssignment.Boss10OutpostName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                save.AssignedBossOutpostName = "Outpost 18";
            }

            // Saves on mission 10 could have AssignedBossNumber = 0 when OP-13-SE was not linked.
            if (save.AssignedBossNumber == 0
                && CampaignMissionProgress.GetCurrentMissionNumber(save) == 10
                && CampaignMissionObjectiveState.TryResolveOutpostBaseName("OP-13-SE", out var baseName))
            {
                save.AssignedBossNumber = 4;
                save.AssignedBossOutpostName = baseName;
                save.BossMissionOutpostNames[3] = baseName;
                save.RevealedBunkerMask |= 1 << 3;
            }
        }

        public static void WriteGear(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            EnsureGearInitialized(save);
            WriteToDisk();
        }

        public static void WriteBossProgress(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            EnsureGearInitialized(save);
            WriteToDisk();
        }

        /// <summary>Persists per-character flight-map world state.</summary>
        public static void WriteWorldProgress(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            EnsureGearInitialized(save);
            WriteToDisk();
        }

        public static void EnsureBunkerDefenseProgress(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            if (save.ConsumedBunkerDefenseMissionIds == null)
            {
                save.ConsumedBunkerDefenseMissionIds = Array.Empty<string>();
            }

            if (save.CompletedBunkerDefenseMissionIds == null)
            {
                save.CompletedBunkerDefenseMissionIds = Array.Empty<string>();
            }
        }

        private static void EnsureBossProgressInitialized(CharacterSaveData save)
        {
            const int bossSlotCount = 20;
            if (save.BossPrimaryHitPoints == null || save.BossPrimaryHitPoints.Length != bossSlotCount)
            {
                save.BossPrimaryHitPoints = CreateBossHealthSlots(bossSlotCount);
            }

            if (save.BossSecondaryHitPoints == null || save.BossSecondaryHitPoints.Length != bossSlotCount)
            {
                save.BossSecondaryHitPoints = CreateBossHealthSlots(bossSlotCount);
            }
        }

        private static float[] CreateBossHealthSlots(int count)
        {
            var slots = new float[count];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = -1f;
            }

            return slots;
        }

        public static void RecordGroundSession(CharacterSaveData save, LandGroundSessionResult result)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            WriteToDisk();
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            isLoaded = true;
            cachedSaves = new List<CharacterSaveData>();

            var path = GetSavePath();
            if (!File.Exists(path))
            {
                WriteToDisk();
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var collection = JsonUtility.FromJson<CharacterSaveCollection>(json);
                if (collection?.Saves != null)
                {
                    cachedSaves.AddRange(collection.Saves);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"F-89: Failed to load character saves. {exception.Message}");
            }

            PurgeLegacyDemoSaves();
            ClearAllSavesOnce();
            NormalizeLoadedAwards();
            NormalizeLoadedRibbons();
            NormalizeLoadedScores();
            NormalizeLoadedRanks();
            NormalizeUrKillProgress();
            NormalizeLoadedGear();
            NormalizeLoadedAttributes();
            NormalizeLoadedPlayModes();
            NormalizeSuccessfulEndMissionProgress();
            NormalizeLaunchOrigins();

            if (cachedSaves.Count == 0)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedRanks()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null || save.IsCourtMartialed || save.IsKilledInAction)
                {
                    continue;
                }

                var before = save.Rank;
                PilotCareerRanks.SyncCareerRankProgress(save);
                if (save.Rank != before)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedAttributes()
        {
            const int moveMin = 3;
            const int moveMax = 15;
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                var maxHp = save.MaxHitPoints <= 0 ? 100 : save.MaxHitPoints;
                var move = Mathf.Clamp(save.Move <= 0 ? 3 : save.Move, moveMin, moveMax);
                var dr = Mathf.Max(0, save.DamageResistance);
                if (save.MaxHitPoints == maxHp && save.Move == move && save.DamageResistance == dr)
                {
                    continue;
                }

                save.MaxHitPoints = maxHp;
                save.Move = move;
                save.DamageResistance = dr;
                changed = true;
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedPlayModes()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                if (save.PlayModeKind != (int)CharacterPlayMode.Campaign
                    && save.PlayModeKind != (int)CharacterPlayMode.FreeFlight)
                {
                    save.PlayModeKind = (int)CharacterPlayMode.Campaign;
                    changed = true;
                }

                if (save.CampaignMissionNumber <= 0)
                {
                    save.CampaignMissionNumber = 1;
                    changed = true;
                }

                if (!save.HasCompletedCampaign
                    && save.CampaignMissionNumber > CampaignMissionProgress.TotalMissionCount)
                {
                    save.HasCompletedCampaign = true;
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLaunchOrigins()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null || MissionLaunchOrigin.HasSavedLaunchOutpost(save))
                {
                    continue;
                }

                var before = save.MissionLaunchOutpostName ?? string.Empty;
                MissionLaunchOrigin.EnsureDefaultLaunchOriginIfMissing(save);
                var after = save.MissionLaunchOutpostName ?? string.Empty;
                if (!string.Equals(before, after, StringComparison.Ordinal))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        /// <summary>
        /// Backfills one-time threshold ribbons for legacy saves (no award screen).
        /// Does not inflate SuccessfulEndMissionCount or repeat star devices.
        /// </summary>
        private static void NormalizeSuccessfulEndMissionProgress()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                if (save.SuccessfulEndMissionCount < 0)
                {
                    save.SuccessfulEndMissionCount = 0;
                    changed = true;
                }

                // Legacy backfill only — never overwrite the real END MISSION counter.
                var inferredEndMissions = save.CampaignMissionNumber > 1
                    ? save.CampaignMissionNumber - 1
                    : 0;
                if (save.HasCompletedCampaign)
                {
                    inferredEndMissions = Math.Max(inferredEndMissions, CampaignMissionProgress.TotalMissionCount);
                }

                var thresholdEndMissions = Math.Max(save.SuccessfulEndMissionCount, inferredEndMissions);

                if (thresholdEndMissions >= 1)
                {
                    if (TryEnsureRibbonGranted(save, MilitaryRibbonIds.CombatAction))
                    {
                        changed = true;
                    }

                    if (TryEnsureRibbonGranted(save, MilitaryRibbonIds.AntarcticaService))
                    {
                        changed = true;
                    }
                }

                if (thresholdEndMissions >= 5
                    && TryEnsureRibbonGranted(save, MilitaryRibbonIds.GoodConduct))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedAwards()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                var derived = MilitaryMedalCatalog.GetHighestMedalIdFromEarnedRibbons(save.EarnedRibbonIds);
                var normalized = MilitaryMedalCatalog.NormalizeAwardId(derived);
                if (save.HighestAward == normalized)
                {
                    continue;
                }

                save.HighestAward = normalized;
                changed = true;
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedScores()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                if (!save.HasAchievedFirstRank
                    && (PilotCareerRanks.GetRankIndex(save.Rank) >= PilotCareerRanks.FirstRankIndex
                        || save.TotalScore >= PilotCareerRanks.GetMinTotalScoreForRankIndex(PilotCareerRanks.FirstRankIndex)))
                {
                    save.HasAchievedFirstRank = true;
                    changed = true;
                }

                if (!save.IsCourtMartialed
                    && !save.IsKilledInAction
                    && PilotCareerRanks.ShouldImprisonForNegativeTotal(save))
                {
                    save.IsCourtMartialed = true;
                    save.CourtMartialedUtc = DateTime.UtcNow.ToString("o");
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadedRibbons()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                if (save.EarnedRibbonIds == null)
                {
                    // Fresh characters get FruitSalad in CreateSave; null means empty legacy data.
                    save.EarnedRibbonIds = Array.Empty<string>();
                    changed = true;
                }

                var countsWereMissing = save.EarnedRibbonCounts == null
                    || save.EarnedRibbonCounts.Length != save.EarnedRibbonIds.Length;
                EnsureRibbonCountsInitialized(save);
                if (ClampInflatedRibbonAwardCounts(save))
                {
                    changed = true;
                }

                if (countsWereMissing)
                {
                    changed = true;
                }

                if (TryRepairMissingScoreMedalBackfill(save))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        /// <summary>
        /// Legacy repair: score-medal backfill used BestMissionScore and could grant Achievement at 200+.
        /// </summary>
        private static bool TryRepairMissingScoreMedalBackfill(CharacterSaveData save)
        {
            if (save == null || save.BestMissionScore < MissionScoreMedalCatalog.JointServiceCommendationMinMissionScore)
            {
                return false;
            }

            if (HasEarnedRibbon(save, MilitaryRibbonIds.JointServiceCommendation))
            {
                return false;
            }

            return TryEnsureRibbonGranted(save, MilitaryRibbonIds.JointServiceCommendation);
        }

        /// <summary>
        /// Repeat ribbon devices only come from END MISSION grants — at most one per successful sortie.
        /// Repairs saves inflated by legacy load-time re-grants.
        /// </summary>
        private static bool ClampInflatedRibbonAwardCounts(CharacterSaveData save)
        {
            if (save?.EarnedRibbonIds == null || save.EarnedRibbonCounts == null)
            {
                return false;
            }

            var maxRepeatable = Math.Max(1, save.SuccessfulEndMissionCount);
            var changed = false;
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                if (MilitaryRibbonCatalog.IsSingleAwardOnly(save.EarnedRibbonIds[i]))
                {
                    continue;
                }

                if (save.EarnedRibbonCounts[i] > maxRepeatable)
                {
                    save.EarnedRibbonCounts[i] = maxRepeatable;
                    changed = true;
                }
            }

            return changed;
        }

        private static void ClearAllSavesOnce()
        {
            if (PlayerPrefs.GetInt(AllSavesClearedKey, 0) != 0)
            {
                return;
            }

            cachedSaves.Clear();
            PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
            PlayerPrefs.Save();
            CharacterSessionState.ActiveSave = null;
            PlayerPrefs.SetInt(AllSavesClearedKey, 1);
            WriteToDisk();
        }

        private static void PurgeLegacyDemoSaves()
        {
            var removedCount = cachedSaves.RemoveAll(IsLegacyDemoSave);
            if (removedCount <= 0)
            {
                return;
            }

            var lastSelectedId = GetLastSelectedSaveId();
            if (!string.IsNullOrEmpty(lastSelectedId) && FindById(lastSelectedId) == null)
            {
                PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
                PlayerPrefs.Save();
            }

            if (CharacterSessionState.ActiveSave != null && IsLegacyDemoSave(CharacterSessionState.ActiveSave))
            {
                CharacterSessionState.ActiveSave = null;
            }

            WriteToDisk();
        }

        private static bool IsLegacyDemoSave(CharacterSaveData save)
        {
            if (save == null || string.IsNullOrWhiteSpace(save.Name))
            {
                return false;
            }

            switch (save.Name.Trim().ToLowerInvariant())
            {
                case "reyes":
                case "chen":
                case "rr":
                    return true;
                default:
                    return false;
            }
        }

        private static string TrimCharacterName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Pilot";
            }

            var trimmed = name.Trim();
            if (trimmed.Length > MaxCharacterNameLength)
            {
                trimmed = trimmed.Substring(0, MaxCharacterNameLength);
            }

            return trimmed;
        }

        private static void NormalizeLoadedGear()
        {
            var changed = false;
            foreach (var save in cachedSaves)
            {
                if (save == null)
                {
                    continue;
                }

                var beforeLoadout = save.Loadout?.Inventory?.Length ?? -1;
                EnsureGearInitialized(save);
                var afterLoadout = save.Loadout?.Inventory?.Length ?? -1;
                if (beforeLoadout != afterLoadout)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static void NormalizeLoadoutInventory(CharacterLoadoutSaveData loadout)
        {
            if (loadout == null)
            {
                return;
            }

            const int mainSlotCount = 24;
            if (loadout.Inventory == null || loadout.Inventory.Length < mainSlotCount)
            {
                var resized = new CharacterGearInstanceSaveData[mainSlotCount];
                if (loadout.Inventory != null)
                {
                    for (var i = 0; i < loadout.Inventory.Length && i < resized.Length; i++)
                    {
                        resized[i] = loadout.Inventory[i];
                    }
                }

                loadout.Inventory = resized;
            }
        }

        private static void NormalizeVaultItems(CharacterVaultSaveData vault)
        {
            if (vault == null)
            {
                return;
            }

            if (vault.SlotCount <= 0)
            {
                vault.SlotCount = CharacterVaultSaveData.DefaultSlotCount;
            }

            if (vault.Items != null && vault.Items.Length == vault.SlotCount)
            {
                return;
            }

            var resized = new CharacterGearInstanceSaveData[vault.SlotCount];
            if (vault.Items != null)
            {
                for (var i = 0; i < vault.Items.Length && i < resized.Length; i++)
                {
                    resized[i] = vault.Items[i];
                }
            }

            vault.Items = resized;
        }

        private static void WriteToDisk()
        {
            var collection = new CharacterSaveCollection
            {
                Saves = cachedSaves.ToArray()
            };
            var json = JsonUtility.ToJson(collection, prettyPrint: true);
            File.WriteAllText(GetSavePath(), json);
        }

        private static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }
}
