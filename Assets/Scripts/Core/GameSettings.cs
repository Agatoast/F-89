using UnityEngine;

namespace F89.Core
{
    public static class GameSettings
    {
        private const string SoundEnabledKey = "F89.SoundEnabled";
        private const string FullscreenEnabledKey = "F89.FullscreenEnabled";
        private const string WindowedWidthKey = "F89.WindowedWidth";
        private const string WindowedHeightKey = "F89.WindowedHeight";

        private const int DefaultWindowedWidth = 1280;
        private const int DefaultWindowedHeight = 720;

        public static bool SoundEnabled { get; private set; } = true;
        public static bool FullscreenEnabled { get; private set; } = true;

        public static void Load()
        {
            SoundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
            FullscreenEnabled = PlayerPrefs.GetInt(FullscreenEnabledKey, 1) == 1;
            ApplySound();
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

        public static string DisplayLabel => FullscreenEnabled ? "Full Screen" : "Window";

        private static void ApplySound()
        {
            AudioListener.volume = SoundEnabled ? 1f : 0f;
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
