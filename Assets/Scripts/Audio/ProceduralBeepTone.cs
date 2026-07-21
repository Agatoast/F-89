using UnityEngine;

namespace F89.Audio
{
    public static class ProceduralBeepTone
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateBeep(float frequencyHz, float durationSeconds, float volume = 0.35f)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = GetEnvelope(t, durationSeconds);
                data[i] = Mathf.Sin(Mathf.PI * 2f * frequencyHz * t) * envelope * volume;
            }

            var clip = AudioClip.Create("Beep", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateIffFriendTone(float volume = 0.32f)
        {
            const float toneDuration = 0.09f;
            const float gapDuration = 0.05f;
            var toneSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * toneDuration));
            var gapSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * gapDuration));
            var data = new float[toneSamples * 2 + gapSamples];
            WriteTone(data, 0, toneSamples, 660f, toneDuration, volume);
            WriteTone(data, toneSamples + gapSamples, toneSamples, 880f, toneDuration, volume);

            var clip = AudioClip.Create("IffFriend", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateLockTone(float frequencyHz, float durationSeconds, float volume = 0.28f)
        {
            var sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)SampleRate;
                var fadeIn = Mathf.Clamp01(t / 0.05f);
                var fadeOut = Mathf.Clamp01((durationSeconds - t) / 0.08f);
                var envelope = fadeIn * fadeOut;
                data[i] = Mathf.Sin(Mathf.PI * 2f * frequencyHz * t) * envelope * volume;
            }

            var clip = AudioClip.Create("LockTone", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void WriteTone(
            float[] buffer,
            int startIndex,
            int sampleCount,
            float frequencyHz,
            float durationSeconds,
            float volume)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = GetEnvelope(t, durationSeconds);
                buffer[startIndex + i] = Mathf.Sin(Mathf.PI * 2f * frequencyHz * t) * envelope * volume;
            }
        }

        private static float GetEnvelope(float timeSeconds, float durationSeconds)
        {
            var attack = Mathf.Clamp01(timeSeconds / 0.01f);
            var release = Mathf.Clamp01((durationSeconds - timeSeconds) / 0.04f);
            return attack * release;
        }
    }
}
