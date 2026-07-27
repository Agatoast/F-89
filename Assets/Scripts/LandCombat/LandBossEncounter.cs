using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Boss 1 bunker encounter: dialogue gating, intro countdown, defeat tracking.</summary>
    public static class LandBossEncounter
    {
        public const float IntroCountdownSeconds = 5f;
        public const string Boss1ObjectName = "Boss1";

        /// <summary>Session flag — once true, bunker entry skips the Boss1 dialogue page.</summary>
        public static bool Boss1Defeated { get; private set; }

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

        public static void MarkBoss1Defeated()
        {
            Boss1Defeated = true;
            Debug.Log("F-89 Bunker: Boss 1 defeated.");
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
    }
}
