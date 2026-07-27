using UnityEngine;

namespace F89.Core
{
    public static class GameSettings
    {
        private const string SoundEnabledKey = "F89.SoundEnabled";
        private const string MusicEnabledKey = "F89.MusicEnabled";
        private const string MissileSoundsEnabledKey = "F89.MissileSoundsEnabled";
        private const string FullscreenEnabledKey = "F89.FullscreenEnabled";
        private const string WindowedWidthKey = "F89.WindowedWidth";
        private const string WindowedHeightKey = "F89.WindowedHeight";

        private const int DefaultWindowedWidth = 1280;
        private const int DefaultWindowedHeight = 720;

        public static bool SoundEnabled { get; private set; } = true;
        public static bool MusicEnabled { get; private set; } = true;
        public static bool MissileSoundsEnabled { get; private set; } = true;
        public static bool FullscreenEnabled { get; private set; } = true;

        public static void Load()
        {
            SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            MissileSoundsEnabled = PlayerPrefs.GetInt(MissileSoundsEnabledKey, 1) == 1;
            FullscreenEnabled = PlayerPrefs.GetInt(FullscreenEnabledKey, 1) == 1;
            ApplySound();
            ApplyMusic();
            ApplyDisplayMode();
        }

        public static void SetSoundEnabled(bool enabled)
        {
            SoundEnabled = enabled;
            PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplySound();
        }

        public static void ToggleSound()
        {
            SetSoundEnabled(!SoundEnabled);
        }

        public static void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyMusic();
        }

        public static void ToggleMusic()
        {
            SetMusicEnabled(!MusicEnabled);
        }

        public static void SetMissileSoundsEnabled(bool enabled)
        {
            MissileSoundsEnabled = enabled;
            PlayerPrefs.SetInt(MissileSoundsEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleMissileSounds()
        {
            SetMissileSoundsEnabled(!MissileSoundsEnabled);
        }

        public static void SetFullscreenEnabled(bool enabled)
        {
            if (!enabled && Screen.fullScreen)
            {
                PlayerPrefs.SetInt(WindowedWidthKey, Screen.width);
                PlayerPrefs.SetInt(WindowedHeightKey, Screen.height);
            }

            FullscreenEnabled = enabled;
            PlayerPrefs.SetInt(FullscreenEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyDisplayMode();
        }

        public static void ToggleFullscreen()
        {
            SetFullscreenEnabled(!FullscreenEnabled);
        }

        public static string SoundLabel => SoundEnabled ? "Sound On" : "Sound Off";

        public static string MusicLabel => MusicEnabled ? "Music On" : "Music Off";

        public static string MissileSoundsLabel =>
            MissileSoundsEnabled ? "Missile Sounds On" : "Missile Sounds Off";

        public static string DisplayLabel => FullscreenEnabled ? "Full Screen" : "Window";

        private static void ApplySound()
        {
            AudioListener.volume = SoundEnabled ? 1f : 0f;
        }

        private static void ApplyMusic()
        {
            GameMusic.ApplyMuteState();
        }

        private static void ApplyDisplayMode()
        {
            if (FullscreenEnabled)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                return;
            }

            var width = PlayerPrefs.GetInt(WindowedWidthKey, DefaultWindowedWidth);
            var height = PlayerPrefs.GetInt(WindowedHeightKey, DefaultWindowedHeight);
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
    }
}
