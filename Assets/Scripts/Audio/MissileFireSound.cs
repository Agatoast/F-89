using F89.Core;
using UnityEngine;

namespace F89.Audio
{
    public static class MissileFireSound
    {
        private const string ClipResourcePath = "Audio/MissileFire";

        /// <summary>Launch cue at Sound FX 50%.</summary>
        private const float VolumeAtFiftyPercent = 0.55f;

        private static AudioClip clip;
        private static AudioSource source;

        public static void Play(MonoBehaviour host)
        {
            if (host == null || !EnsureSource(host))
            {
                return;
            }

            source.volume = (GameSettings.SfxVolumePercent / 100f) * (VolumeAtFiftyPercent * 2f);
            source.PlayOneShot(clip);
        }

        private static bool EnsureSource(MonoBehaviour host)
        {
            if (clip == null)
            {
                clip = Resources.Load<AudioClip>(ClipResourcePath);
                if (clip == null)
                {
                    Debug.LogWarning($"F-89: Missile fire sound missing at Resources/{ClipResourcePath}.");
                    return false;
                }
            }

            if (source != null)
            {
                return true;
            }

            var audioObject = new GameObject("F89_MissileFireSound");
            audioObject.transform.SetParent(host.transform, false);
            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            return true;
        }
    }
}
