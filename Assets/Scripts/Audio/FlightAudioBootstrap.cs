using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Audio
{
    /// <summary>
    /// Keeps a single listener and restores music/jet routing after scene loads in players.
    /// </summary>
    public static class FlightAudioBootstrap
    {
        public static void EnsureReady()
        {
            GameSettings.Load();
            EnsureSingleAudioListener();
            GameMusic.EnsurePlaying();
        }

        public static void SyncFromAircraft(AircraftController aircraft)
        {
            if (aircraft == null)
            {
                GameMusic.EnsurePlaying();
                FlightAudio.SetInFlight(false);
                return;
            }

            if (AircraftLandingController.IsTakeoffActive)
            {
                FlightAudio.SetInFlight(true);
                return;
            }

            GameMusic.EnsurePlaying();

            if (AircraftLandingController.IsLandingActive
                || AircraftLandingController.IsLandingComplete
                || aircraft.IsLandingLocked)
            {
                FlightAudio.SetInFlight(false);
                return;
            }

            FlightAudio.SetInFlight(aircraft.CurrentSpeedMph > 5f);
        }

        private static void EnsureSingleAudioListener()
        {
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            AudioListener keep = null;

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                keep = mainCamera.GetComponent<AudioListener>();
                if (keep == null)
                {
                    keep = mainCamera.gameObject.AddComponent<AudioListener>();
                }
            }

            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || listener == keep)
                {
                    continue;
                }

                Object.Destroy(listener);
            }

            if (keep == null && listeners.Length > 0)
            {
                listeners[0].enabled = true;
                return;
            }

            if (keep != null)
            {
                keep.enabled = true;
            }

            AudioListener.pause = false;
        }
    }
}
