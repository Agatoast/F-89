using UnityEngine;

namespace F89.Audio
{
    /// <summary>Crossfades clip tail into head so Unity loop playback has no seam pop.</summary>
    public static class SeamlessLoopAudio
    {
        public static AudioClip PrepareLoopClip(AudioClip source, float crossfadeSeconds = 0.12f)
        {
            if (source == null)
            {
                return null;
            }

            var channels = source.channels;
            var frequency = source.frequency;
            var frameCount = source.samples;
            if (frameCount <= 1 || channels <= 0 || frequency <= 0)
            {
                return source;
            }

            var data = new float[frameCount * channels];
            source.GetData(data, 0);

            var crossfadeFrames = Mathf.RoundToInt(frequency * crossfadeSeconds);
            crossfadeFrames = Mathf.Clamp(crossfadeFrames, 1, frameCount / 8);
            var crossfadeSamples = crossfadeFrames * channels;
            if (crossfadeSamples >= data.Length)
            {
                return source;
            }

            for (var i = 0; i < crossfadeSamples; i++)
            {
                var t = crossfadeSamples <= 1 ? 1f : i / (float)(crossfadeSamples - 1);
                var endIndex = data.Length - crossfadeSamples + i;
                var startIndex = i;
                data[endIndex] = data[endIndex] * (1f - t) + data[startIndex] * t;
            }

            var loopClip = AudioClip.Create(
                source.name + "_Loop",
                frameCount,
                channels,
                frequency,
                stream: false);
            loopClip.SetData(data, 0);
            return loopClip;
        }
    }
}
