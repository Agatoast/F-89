using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Audio
{
    /// <summary>Looping jet engine bed while the player aircraft is in flight.</summary>
    public sealed class JetEngineSound : MonoBehaviour
    {
        private const string ClipResourcePath = "Audio/StealthJetFlying";

        /// <summary>Engine loop at Sound FX 50% (half prior jet loudness).</summary>
        private const float VolumeAtFiftyPercent = 0.225f;

        private AudioSource audioSource;
        private AircraftController aircraft;

        public static void EnsureOn(GameObject player)
        {
            if (player == null || player.GetComponent<JetEngineSound>() != null)
            {
                return;
            }

            player.AddComponent<JetEngineSound>();
        }

        private void Awake()
        {
            aircraft = GetComponent<AircraftController>();
            EnsureAudioSource();
        }

        private void Start()
        {
            SyncFlightAudioFromLandingState();
        }

        private void SyncFlightAudioFromLandingState()
        {
            if (aircraft == null)
            {
                return;
            }

            if (AircraftLandingController.IsTakeoffActive)
            {
                FlightAudio.SetInFlight(true);
                return;
            }

            if (AircraftLandingController.IsLandingActive
                || AircraftLandingController.IsLandingComplete
                || aircraft.IsLandingLocked)
            {
                FlightAudio.SetInFlight(false);
                return;
            }

            if (aircraft.CurrentSpeedMph > 5f)
            {
                FlightAudio.SetInFlight(true);
            }
        }

        private void OnDestroy()
        {
            FlightAudio.Reset();
        }

        private void Update()
        {
            if (audioSource == null)
            {
                return;
            }

            if (aircraft != null
                && aircraft.CurrentSpeedMph > 5f
                && !FlightAudio.IsInFlight
                && !AircraftLandingController.IsLandingActive
                && !AircraftLandingController.IsLandingComplete
                && !aircraft.IsLandingLocked)
            {
                FlightAudio.SetInFlight(true);
            }

            ApplyVolume();

            if (ShouldPlay())
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private bool ShouldPlay()
        {
            if (aircraft == null
                || GamePauseController.IsPaused
                || PlayerAircraftCrashController.IsCrashActive)
            {
                return false;
            }

            return FlightAudio.IsInFlight
                || AircraftLandingController.IsTakeoffActive
                || aircraft.CurrentSpeedMph > 5f;
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(ClipResourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"F-89: Jet engine sound missing at Resources/{ClipResourcePath}.");
                return;
            }

            clip = SeamlessLoopAudio.PrepareLoopClip(clip);

            var audioObject = new GameObject("F89_JetEngineSound");
            audioObject.transform.SetParent(transform, false);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.priority = 64;
        }

        private void ApplyVolume()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.volume = (GameSettings.SfxVolumePercent / 100f) * (VolumeAtFiftyPercent * 2f);
        }
    }
}
