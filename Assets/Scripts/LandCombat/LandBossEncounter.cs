using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Bunker boss encounter state, intro countdown, and session defeat tracking.</summary>
    public static class LandBossEncounter
    {
        public const float IntroCountdownSeconds = 5f;
        public const int FirstBossNumber = 1;
        public const int LastBossNumber = 10;

        private static readonly bool[] DefeatedBosses = new bool[LastBossNumber + 1];
        private static int boss10DefeatedEnemyCount;

        public static bool IsIntroActive { get; private set; }
        public static float IntroEndsAt { get; private set; }

        public static float RemainingSeconds =>
            IsIntroActive ? Mathf.Max(0f, IntroEndsAt - Time.unscaledTime) : 0f;

        public static void BeginIntroCountdown(float durationSeconds = IntroCountdownSeconds)
        {
            IsIntroActive = true;
            IntroEndsAt = Time.unscaledTime + Mathf.Max(0.1f, durationSeconds);
        }

        public static bool TickIntroFinished()
        {
            if (!IsIntroActive)
            {
                return false;
            }

            if (Time.unscaledTime < IntroEndsAt)
            {
                return false;
            }

            IsIntroActive = false;
            return true;
        }

        /// <summary>Boss 1 is UR level 2 through Boss 9 at UR level 10; Boss 10 is two UR level 10 enemies.</summary>
        public static int GetEnemyLevel(int bossNumber) =>
            Mathf.Min(ClampBossNumber(bossNumber) + 1, LandUrEnemyStats.MaxLevel);

        public static bool IsDefeated(int bossNumber)
        {
            bossNumber = ClampBossNumber(bossNumber);
            var save = CharacterSessionState.ActiveSave;
            return save != null
                ? (save.DefeatedBossMask & (1 << (bossNumber - 1))) != 0
                : DefeatedBosses[bossNumber];
        }

        public static bool IsGuardCleared(int bossNumber)
        {
            // Surface guards respawn on every landing until the boss is defeated.
            return IsDefeated(bossNumber);
        }

        public static int GetEnemyCount(int bossNumber) =>
            ClampBossNumber(bossNumber) == LastBossNumber ? 2 : 1;

        public static void BeginBossFight(int bossNumber)
        {
            bossNumber = ClampBossNumber(bossNumber);
            if (bossNumber == LastBossNumber && !IsDefeated(LastBossNumber))
            {
                boss10DefeatedEnemyCount = 0;
                for (var enemyIndex = 0; enemyIndex < GetEnemyCount(LastBossNumber); enemyIndex++)
                {
                    if (GetSavedHitPoints(LastBossNumber, enemyIndex) == 0f)
                    {
                        boss10DefeatedEnemyCount++;
                    }
                }
            }
        }

        /// <summary>-1 means no saved encounter; 0 means this boss enemy has already died.</summary>
        public static float GetSavedHitPoints(int bossNumber, int enemyIndex = 0)
        {
            var save = CharacterSessionState.ActiveSave;
            bossNumber = ClampBossNumber(bossNumber);
            if (save == null)
            {
                return -1f;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var slots = enemyIndex == 0 ? save.BossPrimaryHitPoints : save.BossSecondaryHitPoints;
            return slots[bossNumber];
        }

        public static void CaptureActiveBossHealthFromBunker()
        {
            if (!LandBossAreaState.TryGetActiveArea(out var area) || IsDefeated(area.BossNumber))
            {
                return;
            }

            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var enemyIndex = 0; enemyIndex < GetEnemyCount(area.BossNumber); enemyIndex++)
            {
                var objectName = GetObjectName(area.BossNumber, enemyIndex);
                for (var i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i] != null && enemies[i].gameObject.name == objectName)
                    {
                        SetSavedHitPoints(area.BossNumber, enemyIndex, enemies[i].CurrentHealth);
                        break;
                    }
                }
            }
        }

        /// <summary>Deaths reset an undefeated bunker encounter instead of preserving its partial HP.</summary>
        public static void ResetActiveBossHealthAfterDeath()
        {
            if (!LandBossAreaState.TryGetActiveArea(out var area) || IsDefeated(area.BossNumber))
            {
                return;
            }

            for (var enemyIndex = 0; enemyIndex < GetEnemyCount(area.BossNumber); enemyIndex++)
            {
                SetSavedHitPoints(area.BossNumber, enemyIndex, -1f, writeToDisk: false);
            }

            boss10DefeatedEnemyCount = 0;
            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.WriteBossProgress(save);
            }
        }

        /// <summary>Developer test reset for a single boss area and its exterior guards.</summary>
        public static void ResetBossForTest(int bossNumber)
        {
            bossNumber = ClampBossNumber(bossNumber);
            DefeatedBosses[bossNumber] = false;
            boss10DefeatedEnemyCount = bossNumber == LastBossNumber ? 0 : boss10DefeatedEnemyCount;

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var bit = 1 << (bossNumber - 1);
            save.DefeatedBossMask &= ~bit;
            save.BossGuardClearedMask &= ~bit;
            save.BossPrimaryHitPoints[bossNumber] = -1f;
            save.BossSecondaryHitPoints[bossNumber] = -1f;
            CharacterSaveRepository.WriteBossProgress(save);
        }

        public static void ResetAllBossesForTest()
        {
            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)
            {
                ResetBossForTest(bossNumber);
            }
        }

        public static string GetObjectName(int bossNumber, int enemyIndex = 0)
        {
            bossNumber = ClampBossNumber(bossNumber);
            return bossNumber == LastBossNumber
                ? $"Boss{bossNumber}_{enemyIndex + 1}"
                : $"Boss{bossNumber}";
        }

        public static bool IsBossObjectName(string objectName)
        {
            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)
            {
                for (var enemyIndex = 0; enemyIndex < GetEnemyCount(bossNumber); enemyIndex++)
                {
                    if (objectName == GetObjectName(bossNumber, enemyIndex))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static int GetNextUndefeatedBoss()
        {
            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)
            {
                if (!IsDefeated(bossNumber))
                {
                    return bossNumber;
                }
            }

            return 0;
        }

        public static bool TryMarkDefeated(string objectName)
        {
            for (var bossNumber = FirstBossNumber; bossNumber < LastBossNumber; bossNumber++)
            {
                if (objectName != GetObjectName(bossNumber))
                {
                    continue;
                }

                SetSavedHitPoints(bossNumber, 0, 0f);
                MarkDefeated(bossNumber);
                Debug.Log($"F-89 Bunker: Boss {bossNumber} defeated.");
                return true;
            }

            for (var enemyIndex = 0; enemyIndex < GetEnemyCount(LastBossNumber); enemyIndex++)
            {
                if (objectName != GetObjectName(LastBossNumber, enemyIndex))
                {
                    continue;
                }

                SetSavedHitPoints(LastBossNumber, enemyIndex, 0f);
                boss10DefeatedEnemyCount++;
                if (boss10DefeatedEnemyCount >= GetEnemyCount(LastBossNumber))
                {
                    MarkDefeated(LastBossNumber);
                    Debug.Log("F-89 Bunker: Both Boss 10 enemies defeated.");
                }

                return true;
            }

            return false;
        }

        public static void ClearIntro()
        {
            IsIntroActive = false;
            IntroEndsAt = 0f;
        }

        public static void Clear()
        {
            ClearIntro();
        }

        private static int ClampBossNumber(int bossNumber) =>
            Mathf.Clamp(bossNumber, FirstBossNumber, LastBossNumber);

        /// <summary>
        /// Marks a boss mission resolved for campaign progression (victory or abandoned early-end).
        /// </summary>
        public static void MarkMissionResolved(int bossNumber)
        {
            MarkDefeated(ClampBossNumber(bossNumber));
        }

        private static void MarkDefeated(int bossNumber)
        {
            DefeatedBosses[bossNumber] = true;
            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            save.DefeatedBossMask |= 1 << (bossNumber - 1);
            save.BossGuardClearedMask |= 1 << (bossNumber - 1);
            for (var enemyIndex = 0; enemyIndex < GetEnemyCount(bossNumber); enemyIndex++)
            {
                SetSavedHitPoints(bossNumber, enemyIndex, 0f, writeToDisk: false);
            }

            CharacterSaveRepository.WriteBossProgress(save);
        }

        private static void SetSavedHitPoints(int bossNumber, int enemyIndex, float hitPoints, bool writeToDisk = true)
        {
            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            CharacterSaveRepository.EnsureGearInitialized(save);
            var slots = enemyIndex == 0 ? save.BossPrimaryHitPoints : save.BossSecondaryHitPoints;
            slots[ClampBossNumber(bossNumber)] = Mathf.Clamp(hitPoints, -1f, LandUrEnemyStats.MaxHitPoints);
            if (writeToDisk)
            {
                CharacterSaveRepository.WriteBossProgress(save);
            }
        }
    }
}
