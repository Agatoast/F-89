using System.Collections;
using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.Audio
{
    /// <summary>
    /// GAU-27A fire: plays the full burst clip, then loops the final 0.5s while the trigger is held.
    /// Sound runs only while <see cref="SetFiring"/> is called with true each frame (LMB / Fire held).
    /// </summary>
    public sealed class Gau27FireSound : MonoBehaviour
    {
        private static readonly string[] ClipResourcePaths =
        {
            "Audio/GAU27Fire",
            "Audio/GAU-27Fire"
        };

        private const float TailLoopDurationSeconds = 0.5f;
        private const float VolumeAtFiftyPercent = 0.125f;

        private AudioSource introSource;
        private AudioSource loopSource;
        private AudioClip fireClip;
        private AudioClip tailClip;
        private bool burstActive;
        private bool loopPlaying;
        private bool loadFailed;
        private bool triggerHeldThisFrame;
        private Coroutine startRoutine;

        public static void EnsureOn(GameObject player)
        {
            if (player == null || player.GetComponent<Gau27FireSound>() != null)
            {
                return;
            }

            player.AddComponent<Gau27FireSound>();
        }

        private void Awake()
        {
            EnsureClipsAndSources();
        }

        /// <summary>Call every frame: true only while GAU is selected and Fire (LMB) is held.</summary>
        public void SetFiring(bool firing)
        {
            triggerHeldThisFrame = firing;

            if (!firing)
            {
                StopBurst();
                return;
            }

            if (GamePauseController.IsPaused || !GameSettings.SoundEnabled)
            {
                StopBurst();
                return;
            }

            if (!EnsureClipsAndSources())
            {
                return;
            }

            ApplyVolume();
            AudioListener.pause = false;

            if (!burstActive)
            {
                BeginBurst();
                return;
            }

            UpdateBurstLoop();
        }

        public void NotifyFireReleased()
        {
            triggerHeldThisFrame = false;
            StopBurst();
        }

        private void OnDisable() => NotifyFireReleased();

        private void OnDestroy() => NotifyFireReleased();

        private void LateUpdate()
        {
            // Hard stop if nothing requested fire this frame (selecting the gun alone must be silent).
            if (!triggerHeldThisFrame && burstActive)
            {
                StopBurst();
            }

            triggerHeldThisFrame = false;
        }

        private void BeginBurst()
        {
            burstActive = true;
            loopPlaying = false;

            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }

            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
            }

            startRoutine = StartCoroutine(StartIntroWhenReady());
        }

        private IEnumerator StartIntroWhenReady()
        {
            if (fireClip != null && fireClip.loadState != AudioDataLoadState.Loaded)
            {
                fireClip.LoadAudioData();
                var guard = 0;
                while (fireClip != null
                       && fireClip.loadState == AudioDataLoadState.Loading
                       && guard++ < 120)
                {
                    yield return null;
                }
            }

            if (!burstActive || introSource == null || fireClip == null)
            {
                startRoutine = null;
                yield break;
            }

            if (tailClip == null)
            {
                tailClip = CreateTailLoopClip(fireClip, TailLoopDurationSeconds);
            }

            ApplyVolume();
            introSource.Stop();
            introSource.clip = fireClip;
            introSource.loop = false;
            introSource.Play();

            yield return null;
            if (burstActive && !loopPlaying && introSource != null && !introSource.isPlaying)
            {
                introSource.clip = fireClip;
                introSource.Play();
            }

            startRoutine = null;
        }

        private void UpdateBurstLoop()
        {
            if (loopPlaying || fireClip == null || introSource == null || loopSource == null)
            {
                return;
            }

            if (startRoutine != null && !introSource.isPlaying)
            {
                return;
            }

            var introNearTail = introSource.isPlaying
                && fireClip.length > TailLoopDurationSeconds
                && introSource.time >= fireClip.length - TailLoopDurationSeconds;
            var introFinished = startRoutine == null && !introSource.isPlaying;

            if (!introNearTail && !introFinished)
            {
                return;
            }

            if (introSource.isPlaying)
            {
                introSource.Stop();
            }

            var clip = tailClip != null ? tailClip : fireClip;
            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.time = 0f;
            loopSource.Play();
            loopPlaying = true;
        }

        private void StopBurst()
        {
            burstActive = false;
            loopPlaying = false;

            if (startRoutine != null)
            {
                StopCoroutine(startRoutine);
                startRoutine = null;
            }

            if (introSource != null)
            {
                introSource.Stop();
                introSource.clip = null;
            }

            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.clip = null;
            }
        }

        private bool EnsureClipsAndSources()
        {
            if (loadFailed)
            {
                return false;
            }

            if (fireClip == null)
            {
                for (var i = 0; i < ClipResourcePaths.Length && fireClip == null; i++)
                {
                    fireClip = Resources.Load<AudioClip>(ClipResourcePaths[i]);
                }

                if (fireClip == null)
                {
                    loadFailed = true;
                    Debug.LogError(
                        "F-89: GAU-27 fire sound missing at Resources/Audio/GAU27Fire "
                        + "(also tried Audio/GAU-27Fire).");
                    return false;
                }
            }

            if (introSource == null)
            {
                introSource = CreateChildSource("F89_Gau27FireIntro");
            }

            if (loopSource == null)
            {
                loopSource = CreateChildSource("F89_Gau27FireLoop");
            }

            return true;
        }

        private AudioSource CreateChildSource(string objectName)
        {
            var audioObject = new GameObject(objectName);
            audioObject.transform.SetParent(transform, false);
            var source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            source.priority = 32;
            source.dopplerLevel = 0f;
            return source;
        }

        private static AudioClip CreateTailLoopClip(AudioClip source, float tailSeconds)
        {
            if (source == null || source.samples <= 0 || source.channels <= 0 || source.frequency <= 0)
            {
                return null;
            }

            if (source.loadState != AudioDataLoadState.Loaded)
            {
                return null;
            }

            var channels = source.channels;
            var frequency = source.frequency;
            var tailFrames = Mathf.Clamp(
                Mathf.CeilToInt(tailSeconds * frequency),
                1,
                source.samples);
            var startFrame = source.samples - tailFrames;
            var samples = new float[tailFrames * channels];

            try
            {
                source.GetData(samples, startFrame);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"F-89: Could not slice GAU-27 fire tail loop. {exception.Message}");
                return null;
            }

            var tail = AudioClip.Create(
                source.name + "_TailLoop",
                tailFrames,
                channels,
                frequency,
                stream: false);
            tail.SetData(samples, 0);
            return tail;
        }

        private void ApplyVolume()
        {
            var volume = (GameSettings.SfxVolumePercent / 100f) * (VolumeAtFiftyPercent * 2f);
            if (introSource != null)
            {
                introSource.volume = volume;
            }

            if (loopSource != null)
            {
                loopSource.volume = volume;
            }
        }
    }
}
