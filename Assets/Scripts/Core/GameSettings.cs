using UnityEngine;

namespace F89.Core
{
    public static class GameSettings
    {
        private const string SoundEnabledKey = "F89.SoundEnabled";
        private const string MusicEnabledKey = "F89.MusicEnabled";
        private const string MissileSoundsEnabledKey = "F89.MissileSoundsEnabled";
        private const string SoundVolumePercentKey = "F89.SoundVolumePercent";
        private const string MusicVolumePercentKey = "F89.MusicVolumePercent";
        private const string SfxVolumePercentKey = "F89.SfxVolumePercent";
        private const string FullscreenEnabledKey = "F89.FullscreenEnabled";
        private const string WindowedWidthKey = "F89.WindowedWidth";
        private const string WindowedHeightKey = "F89.WindowedHeight";

        private const int DefaultWindowedWidth = 1280;
        private const int DefaultWindowedHeight = 720;
        private const int DefaultSoundVolumePercent = 100;
        private const int DefaultMusicVolumePercent = 50;
        private const int DefaultSfxVolumePercent = 50;

        public static bool SoundEnabled { get; private set; } = true;
        public static bool MusicEnabled { get; private set; } = true;
        public static bool MissileSoundsEnabled { get; private set; } = true;
        public static bool FullscreenEnabled { get; private set; } = true;

        /// <summary>Master output 0–100.</summary>
        public static int SoundVolumePercent { get; private set; } = DefaultSoundVolumePercent;

        /// <summary>Music channel 0–100. 50 ≈ prior comfortable theme level.</summary>
        public static int MusicVolumePercent { get; private set; } = DefaultMusicVolumePercent;

        /// <summary>Sound FX channel 0–100. 50 ≈ prior missile-lock reference.</summary>
        public static int SfxVolumePercent { get; private set; } = DefaultSfxVolumePercent;

        public static void Load()
        {
            SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            MissileSoundsEnabled = PlayerPrefs.GetInt(MissileSoundsEnabledKey, 1) == 1;
            SoundVolumePercent = ClampPercent(
                PlayerPrefs.GetInt(SoundVolumePercentKey, DefaultSoundVolumePercent));
            MusicVolumePercent = ClampPercent(
                PlayerPrefs.GetInt(MusicVolumePercentKey, DefaultMusicVolumePercent));
            SfxVolumePercent = ClampPercent(
                PlayerPrefs.GetInt(SfxVolumePercentKey, DefaultSfxVolumePercent));
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

        public static void SetSoundVolumePercent(int percent)
        {
            SoundVolumePercent = ClampPercent(percent);
            PlayerPrefs.SetInt(SoundVolumePercentKey, SoundVolumePercent);
            PlayerPrefs.Save();
            ApplySound();
        }

        public static void SetMusicVolumePercent(int percent)
        {
            MusicVolumePercent = ClampPercent(percent);
            PlayerPrefs.SetInt(MusicVolumePercentKey, MusicVolumePercent);
            PlayerPrefs.Save();
            ApplyMusic();
        }

        public static void SetSfxVolumePercent(int percent)
        {
            SfxVolumePercent = ClampPercent(percent);
            PlayerPrefs.SetInt(SfxVolumePercentKey, SfxVolumePercent);
            PlayerPrefs.Save();
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
            AudioListener.volume = SoundEnabled ? SoundVolumePercent / 100f : 0f;
        }

        private static void ApplyMusic()
        {
            GameMusic.ApplyMuteState();
            GameMusic.ApplyVolume();
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

        private static int ClampPercent(int percent) => Mathf.Clamp(percent, 0, 100);
    }
}
