using System;
using System.Collections.Generic;
using System.IO;
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

            if (GetLastSelectedSaveId() == id)
            {
                PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
                PlayerPrefs.Save();
            }

            if (CharacterSessionState.ActiveSave != null && CharacterSessionState.ActiveSave.Id == id)
            {
                CharacterSessionState.ActiveSave = null;
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
            cachedSaves.Clear();
            PlayerPrefs.DeleteKey(LastSelectedSaveIdKey);
            PlayerPrefs.Save();
            CharacterSessionState.ActiveSave = null;
            WriteToDisk();
        }

        public static void ApplyScorePenalty(CharacterSaveData save, int penalty)
        {
            if (save == null || penalty <= 0)
            {
                return;
            }

            EnsureLoaded();
            save.TotalScore = Mathf.Max(0, save.TotalScore - penalty);
            WriteToDisk();
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
            if (result.TroopsKilled > 0)
            {
                save.EnemyTroopsKilled += result.TroopsKilled;
            }

            if (result.ScoreEarned > 0)
            {
                save.TotalScore += result.ScoreEarned;
                if (result.ScoreEarned > save.BestMissionScore)
                {
                    save.BestMissionScore = result.ScoreEarned;
                }
            }

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
                    save.EarnedRibbonIds = Array.Empty<string>();
                    changed = true;
                }

                if (string.Equals(save.Name, "Don", StringComparison.OrdinalIgnoreCase))
                {
                    save.EarnedRibbonIds = GetAllRibbonIdsExcept(MilitaryRibbonIds.PrisonerOfWar);
                    changed = true;
                }
            }

            if (changed)
            {
                WriteToDisk();
            }
        }

        private static string[] GetAllRibbonIdsExcept(string excludedRibbonId)
        {
            var allRibbonIds = MilitaryRibbonCatalog.GetAllRibbonIds();
            var filtered = new List<string>(allRibbonIds.Length);
            foreach (var ribbonId in allRibbonIds)
            {
                if (ribbonId == excludedRibbonId)
                {
                    continue;
                }

                filtered.Add(ribbonId);
            }

            return filtered.ToArray();
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
