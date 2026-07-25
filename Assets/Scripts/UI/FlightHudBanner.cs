using UnityEngine;

namespace F89.UI
{
    public static class FlightHudBanner
    {
        public static string Message { get; private set; } = string.Empty;
        public static float MessageUntil { get; private set; }

        public static bool HasActiveMessage =>
            !string.IsNullOrEmpty(Message) && Time.unscaledTime < MessageUntil;

        public static void Show(string message, float durationSeconds)
        {
            Message = message;
            MessageUntil = Time.unscaledTime + durationSeconds;
        }

        public static void Clear()
        {
            Message = string.Empty;
            MessageUntil = 0f;
        }
    }
}
