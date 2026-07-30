using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Audio
{
    /// <summary>GAU-27A fire loop: full burst clip, then repeats the final 0.25s while firing continues.</summary>
    public sealed class Gau27FireSound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/GAU-27Fire";
        private const float TailLoopDurationSeconds = 0.25f;

        /// <summary>Gun fire at Sound FX 50%.</summary>
        private const float VolumeAtFiftyPercent = 0.5f;

        private AudioSource audioSource;
        private AudioClip fireClip;
        private float tailStartTime;
        private bool burstActive;

        public static void EnsureOn(GameObject player)
        {
            if (player == null || player.GetComponent<Gau27FireSound>() != null)
            {
                return;
            }

            player.AddComponent<Gau27FireSound>();
        }

        public void NotifyFireReleased()
        {
            burstActive = false;
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        public void OnRoundFired()
        {
            if (!EnsureAudioSource())
            {
                return;
            }

            ApplyVolume();

            if (ShouldMuteForGameplay())
            {
                NotifyFireReleased();
                return;
            }

            burstActive = true;

            if (!audioSource.isPlaying)
            {
                audioSource.clip = fireClip;
                audioSource.loop = false;
                audioSource.time = 0f;
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (audioSource == null || fireClip == null)
            {
                return;
            }

            ApplyVolume();

            if (ShouldMuteForGameplay())
            {
                NotifyFireReleased();
                return;
            }

            if (!burstActive || !audioSource.isPlaying)
            {
                return;
            }

            if (audioSource.time >= fireClip.length - 0.01f)
            {
                audioSource.time = tailStartTime;
            }
        }

        private bool EnsureAudioSource()
        {
            if (audioSource != null && fireClip != null)
            {
                return true;
            }

            fireClip = Resources.Load<AudioClip>(ClipResourcePath);
            if (fireClip == null)
            {
                Debug.LogWarning($"F-89: GAU-27 fire sound missing at Resources/{ClipResourcePath}.");
                return false;
            }

            tailStartTime = Mathf.Max(0f, fireClip.length - TailLoopDurationSeconds);

            if (audioSource == null)
            {
                var audioObject = new GameObject("F89_Gau27FireSound");
                audioObject.transform.SetParent(transform, false);
                audioSource = audioObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.loop = false;
            }

            return true;
        }

        private void ApplyVolume()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.volume = (GameSettings.SfxVolumePercent / 100f) * (VolumeAtFiftyPercent * 2f);
        }

        private static bool ShouldMuteForGameplay()
        {
            if (GamePauseController.IsPaused)
            {
                return true;
            }

            if (AircraftLandingController.IsLandingActive
                || AircraftLandingController.IsLandingComplete
                || PlayerAircraftCrashController.IsCrashActive)
            {
                return true;
            }

            return false;
        }
    }
}
