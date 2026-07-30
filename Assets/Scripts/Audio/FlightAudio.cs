using F89.Core;
using UnityEngine;

namespace F89.Audio
{
    /// <summary>
    /// Swaps MainSong and the in-flight jet loop: music on deck/landed, jet engine while airborne.
    /// </summary>
    public static class FlightAudio
    {
        private static bool inFlight;

        public static bool IsInFlight => inFlight;

        public static void SetInFlight(bool flying)
        {
            if (inFlight == flying)
            {
                return;
            }

            inFlight = flying;
            if (flying)
            {
                GameMusic.PauseForFlight();
            }
            else
            {
                GameMusic.ResumeFromFlight();
            }
        }

        public static void Reset()
        {
            if (!inFlight)
            {
                return;
            }

            inFlight = false;
            GameMusic.ResumeFromFlight();
        }
    }
}
