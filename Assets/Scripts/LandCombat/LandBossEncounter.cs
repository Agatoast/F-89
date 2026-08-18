using F89.Core;

using UnityEngine;



namespace F89.LandCombat

{

    /// <summary>Bunker boss encounter state, intro countdown, and session defeat tracking.</summary>

    public static class LandBossEncounter

    {

        public const float IntroCountdownSeconds = 5f;

        public const int FirstBossNumber = 1;

        public const int CampaignLastBossNumber = 19;

        public const int LastBossNumber = 19;



        private static readonly bool[] DefeatedBosses = new bool[LastBossNumber + 1];



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



        /// <summary>Boss 1 is UR level 2 through Boss 9+ at UR level 10 infantry.</summary>

        public static int GetEnemyLevel(int bossNumber) =>
            LandUrEnemyStats.ScaleMissionTroopLevel(
                Mathf.Min(ClampBossNumber(bossNumber) + 1, LandUrEnemyStats.MaxLevel));



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

            bossNumber = ClampBossNumber(bossNumber);

            var save = CharacterSessionState.ActiveSave;

            return save != null

                ? (save.BossGuardClearedMask & (1 << (bossNumber - 1))) != 0

                : false;

        }



        public static void MarkGuardsCleared(int bossNumber)

        {

            bossNumber = ClampBossNumber(bossNumber);

            var save = CharacterSessionState.ActiveSave;

            if (save == null || IsGuardCleared(bossNumber))

            {

                return;

            }



            save.BossGuardClearedMask |= 1 << (bossNumber - 1);

            CharacterSaveRepository.WriteBossProgress(save);

        }

        public static void ClearGuardsCleared(int bossNumber)
        {
            bossNumber = ClampBossNumber(bossNumber);
            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                return;
            }

            var bit = 1 << (bossNumber - 1);
            if ((save.BossGuardClearedMask & bit) == 0)
            {
                return;
            }

            save.BossGuardClearedMask &= ~bit;
            CharacterSaveRepository.WriteBossProgress(save);
        }



        /// <summary>Full-health reset for undefeated bosses when the player rearms at CV or a friendly base.</summary>

        public static void ResetUndefeatedBossHealthOnRearm()

        {

            var save = CharacterSessionState.ActiveSave;

            if (save == null)

            {

                return;

            }



            CharacterSaveRepository.EnsureGearInitialized(save);

            var changed = false;

            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)

            {

                if (IsDefeated(bossNumber))

                {

                    continue;

                }



                if (save.BossPrimaryHitPoints[bossNumber] > -0.5f)

                {

                    save.BossPrimaryHitPoints[bossNumber] = -1f;

                    changed = true;

                }

            }



            if (changed)

            {

                CharacterSaveRepository.WriteBossProgress(save);

            }

        }



        public static int GetEnemyCount(int bossNumber) => 1;



        /// <summary>-1 means no saved encounter; 0 means this boss enemy has already died.</summary>

        public static float GetSavedHitPoints(int bossNumber, int enemyIndex = 0)

        {

            var save = CharacterSessionState.ActiveSave;

            bossNumber = ClampBossNumber(bossNumber);

            if (save == null || enemyIndex != 0)

            {

                return enemyIndex == 0 ? -1f : 0f;

            }



            CharacterSaveRepository.EnsureGearInitialized(save);

            return save.BossPrimaryHitPoints[bossNumber];

        }



        public static void CaptureActiveBossHealthFromBunker()

        {

            if (!LandBossAreaState.TryGetActiveArea(out var area) || IsDefeated(area.BossNumber))

            {

                return;

            }



            var objectName = GetObjectName(area.BossNumber);

            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);

            for (var i = 0; i < enemies.Length; i++)

            {

                if (enemies[i] != null && enemies[i].gameObject.name == objectName)

                {

                    SetSavedHitPoints(area.BossNumber, enemies[i].CurrentHealth);

                    break;

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



            SetSavedHitPoints(area.BossNumber, -1f, writeToDisk: false);

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



            var save = CharacterSessionState.ActiveSave;

            if (save == null)

            {

                return;

            }



            CharacterSaveRepository.EnsureGearInitialized(save);

            var bit = 1 << (bossNumber - 1);

            save.DefeatedBossMask &= ~bit;
            save.BossKillAwardMask &= ~bit;
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



        public static string GetObjectName(int bossNumber, int enemyIndex = 0) =>

            enemyIndex == 0 ? $"Boss{ClampBossNumber(bossNumber)}" : string.Empty;



        public static bool IsBossObjectName(string objectName)

        {

            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)

            {

                if (objectName == GetObjectName(bossNumber))

                {

                    return true;

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

            for (var bossNumber = FirstBossNumber; bossNumber <= LastBossNumber; bossNumber++)

            {

                if (objectName != GetObjectName(bossNumber))

                {

                    continue;

                }



                SetSavedHitPoints(bossNumber, 0f);

                MarkDefeated(bossNumber);

                Debug.Log($"F-89 Bunker: Boss {bossNumber} defeated.");

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
        /// Marks a boss mission resolved for campaign progression without a bunker kill.
        /// Does not grant a boss-kill award or mark the boss defeated.
        /// </summary>
        public static void MarkMissionResolved(int bossNumber)
        {
        }

        public static bool IsBossKillAwarded(CharacterSaveData save, int bossNumber)
        {
            bossNumber = ClampBossNumber(bossNumber);
            if (save == null || bossNumber <= 0)
            {
                return false;
            }

            return (save.BossKillAwardMask & (1 << (bossNumber - 1))) != 0;
        }

        private static void MarkDefeated(int bossNumber)

        {

            DefeatedBosses[bossNumber] = true;

            var save = CharacterSessionState.ActiveSave;

            if (save == null)

            {

                return;

            }



            var bit = 1 << (bossNumber - 1);
            save.DefeatedBossMask |= bit;
            save.BossKillAwardMask |= bit;
            save.BossGuardClearedMask |= bit;

            SetSavedHitPoints(bossNumber, 0f, writeToDisk: false);

            CharacterSaveRepository.WriteBossProgress(save);

        }



        private static void SetSavedHitPoints(int bossNumber, float hitPoints, bool writeToDisk = true)

        {

            var save = CharacterSessionState.ActiveSave;

            if (save == null)

            {

                return;

            }



            CharacterSaveRepository.EnsureGearInitialized(save);

            save.BossPrimaryHitPoints[ClampBossNumber(bossNumber)] =

                Mathf.Clamp(hitPoints, -1f, LandUrEnemyStats.MaxHitPoints);

            if (writeToDisk)

            {

                CharacterSaveRepository.WriteBossProgress(save);

            }

        }

    }

}


