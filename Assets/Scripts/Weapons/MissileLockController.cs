using System.Collections.Generic;
using F89.Audio;
using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Weapons
{
    public class MissileLockController : MonoBehaviour
    {
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Camera lockCamera;

        private ILockCapableWeapon lockWeapon;
        private ILockCapableWeapon fireLockWeapon;
        private AudioSource lockToneSource;
        private AudioSource beepSource;
        private AudioClip beepClip;
        private AudioClip lockToneClip;
        private AudioClip iffFriendClip;
        private float lockProgress;
        private float beepTimer;
        private float iffDisplayTimer;
        private bool lockTonePlaying;
        private LockableTarget iffPendingFriendly;
        private LockableTarget friendlyEngagementAuthorized;
        private System.Func<LockableTarget, bool> clickSelectFilter;
        private System.Func<LockableTarget, int> clickSelectPriority;

        public MissileLockState LockState { get; private set; } = MissileLockState.None;
        public LockableTarget SelectedTarget { get; private set; }
        public LockableTarget TrackedTarget => SelectedTarget;
        public bool TargetOutOfRange { get; private set; }
        public bool IffFriendActive { get; private set; }
        public string IffFriendLabel { get; private set; } = string.Empty;
        public float LockProgressNormalized =>
            lockWeapon != null && lockWeapon.LockTimeSeconds > 0f
                ? Mathf.Clamp01(lockProgress / lockWeapon.LockTimeSeconds)
                : 0f;
        public bool ReticleVisible { get; private set; }
        public bool SelectedFriendlyBlocksLock =>
            SelectedTarget != null && SelectedTarget.IsFriendly && lockWeapon != null;
        public bool SelectedTargetKindMismatch =>
            SelectedTarget != null
            && fireLockWeapon != null
            && !SelectedTarget.IsFriendly
            && !MatchesActiveWeaponTarget(SelectedTarget, fireLockWeapon);

        public void SetActiveWeapon(ILockCapableWeapon weapon)
        {
            SetActiveWeapon(weapon, weapon);
        }

        public void SetActiveWeapon(ILockCapableWeapon fireWeapon, ILockCapableWeapon trackingWeapon)
        {
            var nextFire = fireWeapon;
            var nextTracking = trackingWeapon ?? fireWeapon;
            if (fireLockWeapon == nextFire && lockWeapon == nextTracking)
            {
                return;
            }

            fireLockWeapon = nextFire;
            lockWeapon = nextTracking;
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                RestartLockProgressForSelection();
                return;
            }

            if (LockState == MissileLockState.Locked
                && lockWeapon != null
                && CanWeaponLockTarget(SelectedTarget))
            {
                TargetOutOfRange = !IsTargetInWeaponRange(SelectedTarget);
                return;
            }

            RestartLockProgressForSelection();
        }

        public void Configure(AircraftController aircraftController, Camera camera)
        {
            EnsureRuntimeReferences();
            aircraft = aircraftController ?? aircraft;
            lockCamera = camera ?? lockCamera ?? Camera.main;
            EnsureAudio();
            ClearSelection();
        }

        public void SetClickSelectionRules(
            System.Func<LockableTarget, bool> filter,
            System.Func<LockableTarget, int> prioritySelector)
        {
            clickSelectFilter = filter;
            clickSelectPriority = prioritySelector;
        }

        public void UpdateLockProgress(bool weaponActive)
        {
            EnsureRuntimeReferences();
            ReticleVisible = weaponActive;
            TargetOutOfRange = false;
            if (!weaponActive || lockWeapon == null || aircraft == null)
            {
                StopLockProgressOnly();
                UpdateIffDisplay();
                return;
            }

            UpdateIffDisplay();

            if (!LockAudioEnabled)
            {
                StopLockTone();
            }

            if (SelectedTarget != null && !SelectedTarget.IsAlive)
            {
                ClearSelection();
            }

            if (SelectedTarget == null)
            {
                LockState = MissileLockState.None;
                return;
            }

            if (lockWeapon == null)
            {
                StopLockProgressOnly();
                return;
            }

            if (!CanWeaponLockTarget(SelectedTarget))
            {
                StopLockProgressOnly();
                return;
            }

            TargetOutOfRange = !IsTargetInWeaponRange(SelectedTarget);

            if (LockState == MissileLockState.Locked)
            {
                PlayLockToneIfNeeded();
                return;
            }

            if (TargetOutOfRange)
            {
                LockState = MissileLockState.Tracking;
                lockProgress = 0f;
                beepTimer = 0f;
                StopLockTone();
                return;
            }

            lockProgress += Time.deltaTime;
            UpdateBeepAudio();

            if (lockProgress >= lockWeapon.LockTimeSeconds)
            {
                LockState = MissileLockState.Locked;
                PlayLockToneIfNeeded();
                return;
            }

            LockState = MissileLockState.Tracking;
        }

        public bool TrySelectTargetOnClick(Vector2 screenPosition)
        {
            TargetOutOfRange = false;

            if (aircraft == null || lockCamera == null)
            {
                return false;
            }

            var candidate = FindTargetUnderCursor(screenPosition);
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            if (!TryAuthorizeFriendlySelection(candidate))
            {
                return true;
            }

            if (SelectedTarget == candidate)
            {
                return false;
            }

            SelectedTarget = candidate;
            RestartLockProgressForSelection();
            return true;
        }

        public bool TrySelectRadarContact(LockableTarget target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (!TryAuthorizeFriendlySelection(target))
            {
                return true;
            }

            if (SelectedTarget == target)
            {
                return false;
            }

            SelectedTarget = target;
            RestartLockProgressForSelection();
            return true;
        }

        public bool TryCycleNextTargetInRange(
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            System.Predicate<LockableTarget> includeTarget,
            System.Comparison<LockableTarget> sortComparison = null)
        {
            if (aircraft == null || worldMap == null || rangeMiles <= 0f || includeTarget == null)
            {
                return false;
            }

            var candidates = new List<LockableTarget>();
            var targets = CombatThreatRange.GetCachedLockableTargets();
            var observer = aircraft.transform.position;

            foreach (var target in targets)
            {
                if (target == null
                    || !target.IsAlive
                    || !includeTarget(target)
                    || !WeaponLockRange.IsWithinRange(
                        observer,
                        target.transform.position,
                        rangeMiles,
                        worldMap,
                        ticSizeWorldUnits))
                {
                    continue;
                }

                candidates.Add(target);
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            if (sortComparison != null)
            {
                candidates.Sort(sortComparison);
            }
            else
            {
                candidates.Sort((a, b) =>
                {
                    var distanceA = HorizontalDistanceMeters(observer, a.transform.position);
                    var distanceB = HorizontalDistanceMeters(observer, b.transform.position);
                    return distanceA.CompareTo(distanceB);
                });
            }

            var currentIndex = SelectedTarget != null ? candidates.IndexOf(SelectedTarget) : -1;
            var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % candidates.Count;
            SelectedTarget = candidates[nextIndex];
            RestartLockProgressForSelection();
            return true;
        }

        private bool TryAuthorizeFriendlySelection(LockableTarget candidate)
        {
            if (candidate == null || !candidate.IsFriendly)
            {
                iffPendingFriendly = null;
                if (friendlyEngagementAuthorized != null && friendlyEngagementAuthorized != candidate)
                {
                    friendlyEngagementAuthorized = null;
                }

                return true;
            }

            if (!candidate.RespondsWithIff)
            {
                return false;
            }

            if (friendlyEngagementAuthorized == candidate)
            {
                return true;
            }

            if (iffPendingFriendly != candidate)
            {
                iffPendingFriendly = candidate;
                NotifyIffIfFriendly(candidate);
                return false;
            }

            friendlyEngagementAuthorized = candidate;
            iffPendingFriendly = null;
            return true;
        }

        private void NotifyIffIfFriendly(LockableTarget target)
        {
            if (target == null || !target.RespondsWithIff)
            {
                return;
            }

            EnsureAudio();
            TriggerIffFriendResponse(target);
        }

        private static float HorizontalDistanceMeters(Vector3 observer, Vector3 targetPosition)
        {
            var delta = targetPosition - observer;
            delta.y = 0f;
            return delta.magnitude;
        }

        public LockableTarget GetLockedTarget()
        {
            if (LockState != MissileLockState.Locked || SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return null;
            }

            if (fireLockWeapon == null)
            {
                return null;
            }

            if (!CanFireWeaponLockTarget(SelectedTarget))
            {
                return null;
            }

            return IsTargetInWeaponRange(SelectedTarget, fireLockWeapon) ? SelectedTarget : null;
        }

        public void ClearLockAfterFire()
        {
            // Keep the selected target and lock after firing; only a new selection clears it.
        }

        private LockableTarget FindTargetUnderCursor(Vector2 screenPosition)
        {
            return HudTargetSelection.FindTargetAtScreenPosition(
                lockCamera,
                screenPosition,
                null,
                clickSelectFilter,
                clickSelectPriority);
        }

        private bool CanWeaponLockTarget(LockableTarget target)
        {
            if (target == null || !target.IsAlive || lockWeapon == null)
            {
                return false;
            }

            if (target.IsFriendly)
            {
                return friendlyEngagementAuthorized == target;
            }

            return MatchesActiveWeaponTarget(target, lockWeapon);
        }

        private bool CanFireWeaponLockTarget(LockableTarget target)
        {
            if (target == null || !target.IsAlive || fireLockWeapon == null)
            {
                return false;
            }

            if (target.IsFriendly)
            {
                return friendlyEngagementAuthorized == target;
            }

            return MatchesActiveWeaponTarget(target, fireLockWeapon);
        }

        private static bool MatchesActiveWeaponTarget(LockableTarget target, ILockCapableWeapon weapon)
        {
            if (target == null || weapon == null)
            {
                return false;
            }

            if (weapon is Gau27aWeaponConfig || weapon is GenericTargetLockProfile)
            {
                return !target.IsFlareDecoy && !target.IsPlayerAircraft;
            }

            if (!target.MatchesWeapon(weapon.ValidTargetKind))
            {
                return false;
            }

            if (weapon is Agm114HellfireWeaponConfig || weapon is Agm88jSiawWeaponConfig)
            {
                return target.IsGroundVehicle
                    || target.IsInfantry
                    || target.IsBuilding
                    || target.GetComponent<OutpostBuilding>() != null;
            }

            return true;
        }

        public bool ShouldBlockFireForSelection()
        {
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return false;
            }

            if (SelectedTarget.IsFriendly && friendlyEngagementAuthorized != SelectedTarget)
            {
                return true;
            }

            if (fireLockWeapon != null && !MatchesActiveWeaponTarget(SelectedTarget, fireLockWeapon))
            {
                return true;
            }

            return false;
        }

        public bool ShouldBlockFireWithoutSelection()
        {
            return fireLockWeapon != null && (SelectedTarget == null || !SelectedTarget.IsAlive);
        }

        public bool ShouldBlockFireWithoutLock()
        {
            return fireLockWeapon != null && LockState != MissileLockState.Locked;
        }

        private bool IsTargetInLockCoverage(LockableTarget target)
        {
            if (aircraft == null || lockWeapon == null || target == null)
            {
                return false;
            }

            return WeaponLockCoverage.IsWithinLockCoverage(
                aircraft.transform.position,
                aircraft.transform.forward,
                target.transform.position,
                lockWeapon.AimMode,
                lockWeapon.ForwardLockHalfAngleDegrees);
        }

        private bool IsTargetInWeaponRange(LockableTarget target)
        {
            return IsTargetInWeaponRange(target, lockWeapon);
        }

        private bool IsTargetInWeaponRange(LockableTarget target, ILockCapableWeapon weapon)
        {
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (profile == null || target == null || weapon == null)
            {
                return false;
            }

            return WeaponLockRange.IsWithinRange(
                aircraft.transform.position,
                target.transform.position,
                weapon.RangeMiles,
                worldMap,
                profile.ticSizeWorldUnits);
        }

        private void RestartLockProgressForSelection()
        {
            lockProgress = 0f;
            beepTimer = 0f;
            StopLockTone();

            if (SelectedTarget == null
                || lockWeapon == null
                || !SelectedTarget.IsAlive
                || !CanWeaponLockTarget(SelectedTarget))
            {
                LockState = MissileLockState.None;
                TargetOutOfRange = false;
                return;
            }

            LockState = MissileLockState.Tracking;
            TargetOutOfRange = !IsTargetInWeaponRange(SelectedTarget);
            beepTimer = 0f;
            if (!TargetOutOfRange)
            {
                PlayImmediateTrackingBeep();
            }
        }

        private void StopLockProgressOnly()
        {
            lockProgress = 0f;
            beepTimer = 0f;
            LockState = MissileLockState.None;
            StopLockTone();
        }

        private void ClearSelection()
        {
            SelectedTarget = null;
            iffPendingFriendly = null;
            friendlyEngagementAuthorized = null;
            StopLockProgressOnly();
        }

        private void UpdateBeepAudio()
        {
            if (LockState == MissileLockState.Locked
                || lockWeapon == null
                || !LockAudioEnabled
                || GamePauseController.IsPaused)
            {
                return;
            }

            EnsureAudio();
            if (beepSource == null || beepClip == null)
            {
                return;
            }

            AudioListener.pause = false;
            SyncSfxVolume();
            var t = LockProgressNormalized;
            var interval = Mathf.Lerp(lockWeapon.MaxBeepInterval, lockWeapon.MinBeepInterval, t);
            beepTimer -= Time.deltaTime;
            if (beepTimer > 0f)
            {
                return;
            }

            beepTimer = interval;
            beepSource.PlayOneShot(beepClip);
        }

        private void PlayImmediateTrackingBeep()
        {
            if (!LockAudioEnabled || GamePauseController.IsPaused)
            {
                return;
            }

            EnsureAudio();
            SyncSfxVolume();
            if (beepSource == null || beepClip == null)
            {
                return;
            }

            AudioListener.pause = false;
            beepSource.PlayOneShot(beepClip);
            beepTimer = lockWeapon != null
                ? lockWeapon.MaxBeepInterval
                : 0.55f;
        }

        private void PlayLockToneIfNeeded()
        {
            if (lockTonePlaying
                || !LockAudioEnabled
                || GamePauseController.IsPaused)
            {
                return;
            }

            EnsureAudio();
            SyncSfxVolume();
            if (lockToneSource == null || lockToneClip == null)
            {
                return;
            }

            AudioListener.pause = false;
            lockTonePlaying = true;
            lockToneSource.loop = true;
            lockToneSource.clip = lockToneClip;
            lockToneSource.Play();
        }

        private void StopLockTone()
        {
            if (!lockTonePlaying)
            {
                return;
            }

            lockTonePlaying = false;
            if (lockToneSource == null)
            {
                return;
            }

            lockToneSource.loop = false;
            lockToneSource.Stop();
        }

        private void TriggerIffFriendResponse(LockableTarget target)
        {
            IffFriendActive = true;
            IffFriendLabel = target != null ? target.TargetLabel : string.Empty;
            iffDisplayTimer = 2.5f;
            if (!LockAudioEnabled)
            {
                return;
            }

            EnsureAudio();
            SyncSfxVolume();
            if (beepSource != null && iffFriendClip != null)
            {
                AudioListener.pause = false;
                beepSource.PlayOneShot(iffFriendClip);
            }
        }

        private void UpdateIffDisplay()
        {
            if (!IffFriendActive)
            {
                return;
            }

            iffDisplayTimer -= Time.deltaTime;
            if (iffDisplayTimer <= 0f)
            {
                IffFriendActive = false;
                IffFriendLabel = string.Empty;
            }
        }

        private Transform audioRoot;
        private bool audioInitialized;

        private void Awake()
        {
            EnsureRuntimeReferences();
        }

        private void Start()
        {
            EnsureRuntimeReferences();
            EnsureAudio();
        }

        private void EnsureRuntimeReferences()
        {
            if (aircraft == null)
            {
                aircraft = GetComponent<AircraftController>();
            }

            if (lockCamera == null)
            {
                lockCamera = Camera.main;
            }
        }

        private static bool LockAudioEnabled =>
            GameSettings.SoundEnabled && GameSettings.MissileSoundsEnabled;

        private void EnsureAudio()
        {
            if (!audioInitialized)
            {
                GameSettings.Load();
                audioInitialized = true;
            }

            if (beepClip == null)
            {
                beepClip = ProceduralBeepTone.CreateBeep(880f, 0.08f, 0.45f);
            }

            if (lockToneClip == null)
            {
                lockToneClip = ProceduralBeepTone.CreateSustainedLockTone(1320f, 0.35f, 0.4f);
            }

            if (iffFriendClip == null)
            {
                iffFriendClip = ProceduralBeepTone.CreateIffFriendTone();
            }

            if (audioRoot == null)
            {
                var existing = transform.Find("F89_MissileLockAudio");
                if (existing == null)
                {
                    existing = new GameObject("F89_MissileLockAudio").transform;
                    existing.SetParent(transform, false);
                }

                audioRoot = existing;
            }

            if (beepSource == null)
            {
                beepSource = audioRoot.gameObject.AddComponent<AudioSource>();
                beepSource.playOnAwake = false;
                beepSource.spatialBlend = 0f;
                beepSource.volume = GameAudioLevels.MissileTargetingSfxVolume;
            }

            if (lockToneSource == null)
            {
                lockToneSource = audioRoot.gameObject.AddComponent<AudioSource>();
                lockToneSource.playOnAwake = false;
                lockToneSource.spatialBlend = 0f;
                lockToneSource.volume = GameAudioLevels.MissileTargetingSfxVolume;
            }
        }

        private void SyncSfxVolume()
        {
            var volume = GameAudioLevels.MissileTargetingSfxVolume;
            if (beepSource != null)
            {
                beepSource.volume = volume;
            }

            if (lockToneSource != null)
            {
                lockToneSource.volume = volume;
            }
        }

        private void OnDisable()
        {
            StopLockTone();
        }
    }
}
