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

        public static CharacterSaveData CreateSave(string name, string rank = "2nd LT")
        {
            EnsureLoaded();

            var save = new CharacterSaveData
            {
                Id = Guid.NewGuid().ToString("N"),
                Rank = string.IsNullOrWhiteSpace(rank) ? "2nd LT" : rank.Trim(),
                Name = TrimCharacterName(name),
                LastPlayedUtc = DateTime.UtcNow.ToString("o"),
                HighestAward = MilitaryMedalIds.DefaultForNewCharacter,
                EarnedRibbonIds = new[] { MilitaryRibbonIds.FruitSalad },
                MaxHitPoints = 100,
                Move = 3,
                DamageResistance = 0
            };

            EnsureGearInitialized(save);
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
            return PlayerPrefs.GetString(LastSelectedSaveIdKey, string.Empty);
        }

        public static void SetLastSelectedSaveId(string saveId)
        {
            if (string.IsNullOrEmpty(saveId))
            {
                return;
            }

            PlayerPrefs.SetString(LastSelectedSaveIdKey, saveId);
            PlayerPrefs.Save();
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
            if (save == null || penalty <= 0)
            {
                return;
            }

            EnsureLoaded();
            save.CareerScoreModifier -= penalty;
            ReconcileTotalScore(save);
            WriteToDisk();
        }

        /// <summary>Cuts TotalScore by a fraction (e.g. 0.5 = 50%). Does not change rank by itself.</summary>
        public static void ApplyTotalScoreFractionPenalty(CharacterSaveData save, float fractionKept)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            ReconcileTotalScore(save);
            var kept = Mathf.Clamp01(fractionKept);
            var newTotal = Mathf.Max(0, Mathf.RoundToInt(save.TotalScore * kept));
            var folderScore = PilotScoreService.ComputeTotalDestroyScore(save);
            save.CareerScoreModifier = newTotal - folderScore;
            save.TotalScore = newTotal;
            WriteToDisk();
        }

        /// <summary>Total score = kill-folder destroy points plus career modifiers (penalties).</summary>
        public static void ReconcileTotalScore(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            var folderScore = PilotScoreService.ComputeTotalDestroyScore(save);
            if (save.CareerScoreModifier == 0
                && save.UrKillCreditBackfilled
                && save.TotalScore != folderScore)
            {
                save.CareerScoreModifier = save.TotalScore - folderScore;
            }

            save.TotalScore = Mathf.Max(0, folderScore + save.CareerScoreModifier);
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
            ReconcileTotalScore(save);
            WriteToDisk();

            if (CharacterSessionState.ActiveSave != null && CharacterSessionState.ActiveSave.Id == save.Id)
            {
                CharacterSessionState.ActiveSave = null;
                CharacterGearSession.Bind(null);
                AircraftLoadoutState.ResetForNewSortie();
                LandMissionHandoffState.Clear();
            }
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

        /// <summary>Syncs vehicle/troop kill totals from per-level UR kill folders.</summary>
        public static void SyncVehicleKillCredit(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            EnsureLoaded();
            EnsureUrKillArrays(save);
            WriteToDisk();
        }

        /// <summary>Adds a ribbon if the character has not already earned it. Updates HighestAward.</summary>
        public static bool TryGrantRibbon(CharacterSaveData save, string ribbonId)
        {
            if (save == null || string.IsNullOrEmpty(ribbonId))
            {
                return false;
            }

            EnsureLoaded();
            save.EarnedRibbonIds ??= Array.Empty<string>();
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                if (save.EarnedRibbonIds[i] == ribbonId)
                {
                    return false;
                }
            }

            var updated = new string[save.EarnedRibbonIds.Length + 1];
            for (var i = 0; i < save.EarnedRibbonIds.Length; i++)
            {
                updated[i] = save.EarnedRibbonIds[i];
            }

            updated[updated.Length - 1] = ribbonId;
            save.EarnedRibbonIds = updated;
            var derived = MilitaryMedalCatalog.GetHighestMedalIdFromEarnedRibbons(save.EarnedRibbonIds);
            save.HighestAward = MilitaryMedalCatalog.NormalizeAwardId(derived);
            WriteToDisk();
            return true;
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
            EnsureUrKillArrays(save);
        }

        public static void EnsureUrKillArrays(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            if (save.UrVehicleKillsByLevel == null || save.UrVehicleKillsByLevel.Length != UrKillCredit.LevelCount)
            {
                save.UrVehicleKillsByLevel = new int[UrKillCredit.LevelCount];
            }

            if (save.UrTroopKillsByLevel == null || save.UrTroopKillsByLevel.Length != UrKillCredit.LevelCount)
            {
                save.UrTroopKillsByLevel = new int[UrKillCredit.LevelCount];
            }

            if (!save.UrKillCreditBackfilled)
            {
                var folderKillCount = UrKillCredit.Sum(save.UrVehicleKillsByLevel)
                    + UrKillCredit.Sum(save.UrTroopKillsByLevel);
                if (folderKillCount > 0)
                {
                    save.UrKillCreditBackfilled = true;
                }
                else if (save.DestroyedWorldTargetIds is { Length: > 0 })
                {
                    BackfillUrKillArraysFromDestroyedTargets(save);
                    save.UrKillCreditBackfilled = true;
                }
            }

            save.EnemyVehiclesKilled = UrKillCredit.Sum(save.UrVehicleKillsByLevel);
            save.EnemyTroopsKilled = UrKillCredit.Sum(save.UrTroopKillsByLevel);
            ReconcileTotalScore(save);
        }

        private static void BackfillUrKillArraysFromDestroyedTargets(CharacterSaveData save)
        {
            var catalog = Enemies.VehicleUnitCatalog.LoadOrDefault();
            for (var i = 0; i < save.DestroyedWorldTargetIds.Length; i++)
            {
                var targetId = save.DestroyedWorldTargetIds[i];
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    continue;
                }

                var separator = targetId.IndexOf("::", StringComparison.Ordinal);
                if (separator < 0 || separator >= targetId.Length - 2)
                {
                    continue;
                }

                var label = targetId.Substring(separator + 2).Trim();
                if (!catalog.TryGetByAbbreviation(label, VehicleUnitDesignation.UR, out var definition)
                    || definition == null)
                {
                    continue;
                }

                var index = UrKillCredit.LevelToIndex(definition.vehicleLevel);
                if (definition.isTroop)
                {
                    save.UrTroopKillsByLevel[index]++;
                }
                else
                {
                    save.UrVehicleKillsByLevel[index]++;
                }
            }

            ReconcileTotalScore(save);
        }

        public static void EnsureBossMissionInitialized(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            const int bossSlotCount = 10;
            if (save.BossMissionOutpostNames == null || save.BossMissionOutpostNames.Length != bossSlotCount)
            {
                save.BossMissionOutpostNames = new string[bossSlotCount];
            }

            MigrateBoss1Outpost(save);
            MigrateBoss2Outpost(save);
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

        private static void EnsureBossProgressInitialized(CharacterSaveData save)
        {
            const int bossSlotCount = 11;
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
            NormalizeLoadedGear();
            NormalizeLoadedAttributes();

            if (cachedSaves.Count == 0)
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
            // Intentionally empty — character scores are owned by gameplay / explicit resets.
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
            }

            if (changed)
            {
                WriteToDisk();
            }
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
