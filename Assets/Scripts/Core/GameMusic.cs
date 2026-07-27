using UnityEngine;

namespace F89.Core
{
    /// <summary>
    /// Background music channel. Register an AudioSource when music is wired up;
    /// Settings Music On/Off mutes it without affecting SFX.
    /// </summary>
    public static class GameMusic
    {
        private static AudioSource musicSource;

        public static void Bind(AudioSource source)
        {
            musicSource = source;
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
    }
}
