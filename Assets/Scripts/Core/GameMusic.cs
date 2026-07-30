using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Persistent background music. Starts on the loading screen and survives scene changes.
    /// Pause Music On/Off mutes this channel without stopping playback.
    /// </summary>
    public static class GameMusic
    {
        private const string ClipResourcePath = "Audio/MainSong";
        private const string PlayerObjectName = "F89_GameMusic";

        /// <summary>
        /// Linear volume at Music 50%. Present under menus/flight, not competing with SFX.
        /// Music 100% is 2× this value.
        /// </summary>
        public const float VolumeAtFiftyPercent = 0.15f;

        private static AudioSource musicSource;

        public static float CurrentLinearVolume =>
            (GameSettings.MusicVolumePercent / 100f) * (VolumeAtFiftyPercent * 2f);

        public static void EnsurePlaying()
        {
            GameSettings.Load();
            EnsureSource();
            ApplyVolume();
            ApplyMuteState();

            if (musicSource == null || musicSource.clip == null)
            {
                return;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public static void PauseForFlight()
        {
            EnsureSource();
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        public static void ResumeFromFlight()
        {
            GameSettings.Load();
            EnsureSource();
            ApplyVolume();
            ApplyMuteState();

            if (musicSource == null || musicSource.clip == null)
            {
                return;
            }

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public static void Bind(AudioSource source)
        {
            musicSource = source;
            ApplyVolume();
            ApplyMuteState();
        }

        public static void Unbind(AudioSource source)
        {
            if (musicSource == source)
            {
                musicSource = null;
            }
        }

        public static void ApplyMuteState()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.mute = !GameSettings.MusicEnabled;
        }

        public static void ApplyVolume()
        {
            if (musicSource == null)
            {
                return;
            }

            musicSource.volume = CurrentLinearVolume;
        }

        private static void EnsureSource()
        {
            if (musicSource != null)
            {
                return;
            }

            var existing = GameObject.Find(PlayerObjectName);
            if (existing != null)
            {
                musicSource = existing.GetComponent<AudioSource>();
                if (musicSource != null)
                {
                    return;
                }
            }

            var clip = Resources.Load<AudioClip>(ClipResourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"F-89: Theme music missing at Resources/{ClipResourcePath}.");
                return;
            }

            var go = new GameObject(PlayerObjectName);
            Object.DontDestroyOnLoad(go);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 0;
            // Keep theme playing while the pause menu freezes other audio.
            musicSource.ignoreListenerPause = true;
        }
    }
}
