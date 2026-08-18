using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public static class CombatAudio
    {
        private static AudioSource _source;
        private static AudioSource _alarmSource;

        public static void Ensure()
        {
            if (_source != null)
            {
                return;
            }

            var camera = Camera.main;
            var host = camera != null ? camera.gameObject : new GameObject("CombatAudio");
            _source = host.GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = host.AddComponent<AudioSource>();
            }

            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
        }

        public static void PlayGrenadeExplosion()
        {
            PlayOneShot(GameConstants.GrenadeExplosionSoundResource, 0.92f);
        }

        public static void PlayRocketExplosion()
        {
            PlayOneShot(GameConstants.RocketExplosionSoundResource, 1f);
        }

        public static void PlayLightningStrike()
        {
            Ensure();
            var clip = Resources.Load<AudioClip>(GameConstants.LightningDischargeSoundResource)
                ?? CreateProceduralLightningDischarge();
            _source.PlayOneShot(clip, GameConstants.LightningDischargeVolume);
        }

        public static void PlayBriefingClaxon()
        {
            EnsureAlarmSource();
            if (_alarmSource.isPlaying)
            {
                return;
            }

            var clip = Resources.Load<AudioClip>(GameConstants.ClaxonSoundResource) ?? CreateProceduralClaxon();
            _alarmSource.clip = clip;
            _alarmSource.loop = true;
            _alarmSource.volume = GameConstants.BriefingClaxonVolume;
            _alarmSource.Play();
        }

        public static void StopBriefingClaxon()
        {
            if (_alarmSource != null && _alarmSource.isPlaying)
            {
                _alarmSource.Stop();
            }
        }

        private static void EnsureAlarmSource()
        {
            if (_alarmSource != null)
            {
                return;
            }

            Ensure();
            _alarmSource = _source.gameObject.AddComponent<AudioSource>();
            _alarmSource.spatialBlend = 0f;
            _alarmSource.playOnAwake = false;
        }

        private static AudioClip CreateProceduralClaxon()
        {
            const int sampleRate = 44100;
            const float cycleSeconds = 0.5f;
            const int cycles = 6;
            var totalSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * cycleSeconds * cycles));
            var data = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
            {
                var t = i / (float)sampleRate;
                var cycleT = t % cycleSeconds;
                var frequency = cycleT < cycleSeconds * 0.5f ? 880f : 660f;
                data[i] = Mathf.Sin(Mathf.PI * 2f * frequency * t) * 0.29f;
            }

            var clip = AudioClip.Create("BriefingClaxon", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateProceduralLightningDischarge()
        {
            const int sampleRate = 44100;
            const float duration = 0.38f;
            var totalSamples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            var data = new float[totalSamples];
            var rng = new System.Random(0x4C494748); // stable fallback if asset missing
            for (var i = 0; i < totalSamples; i++)
            {
                var t = i / (float)sampleRate;
                var env = Mathf.Exp(-t * 14f) * (1f - Mathf.Exp(-t * 420f));
                var crack = t < 0.012f ? (rng.NextDouble() * 2.0 - 1.0) : 0.0;
                var buzz = Mathf.Sin(Mathf.PI * 2f * 1800f * t) * Mathf.Exp(-t * 22f);
                var hum = Mathf.Sin(Mathf.PI * 2f * 120f * t) * Mathf.Exp(-t * 9f) * 0.35f;
                var staticNoise = (rng.NextDouble() * 2.0 - 1.0) * env * (t < 0.08f ? 0.55f : 0.08f);
                var sample = (float)(crack * 0.95 + buzz * 0.42 + hum + staticNoise * 0.28) * env;
                data[i] = Mathf.Clamp(sample * 0.82f, -1f, 1f);
            }

            var clip = AudioClip.Create("LightningDischarge", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void PlayOneShot(string resourcePath, float volume)
        {
            Ensure();
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                Debug.LogWarning($"Bunker Defense: missing audio at Resources/{resourcePath}.");
                return;
            }

            _source.PlayOneShot(clip, volume);
        }
    }
}
