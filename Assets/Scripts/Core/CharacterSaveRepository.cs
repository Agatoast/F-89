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
                HighestAward = MilitaryMedalIds.DefaultForNewCharacter
            };

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

            if (cachedSaves.Count == 0)
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

                var normalized = MilitaryMedalCatalog.NormalizeAwardId(save.HighestAward);
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
