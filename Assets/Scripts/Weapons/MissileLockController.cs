using F89.Audio;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class MissileLockController : MonoBehaviour
    {
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Camera lockCamera;

        private ILockCapableWeapon lockWeapon;
        private AudioSource audioSource;
        private AudioClip beepClip;
        private AudioClip lockToneClip;
        private AudioClip iffFriendClip;
        private float lockProgress;
        private float beepTimer;
        private float iffDisplayTimer;
        private bool lockTonePlaying;

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
            && lockWeapon != null
            && !SelectedTarget.IsFriendly
            && !SelectedTarget.MatchesWeapon(lockWeapon.ValidTargetKind);

        public void SetActiveWeapon(ILockCapableWeapon weapon)
        {
            if (lockWeapon == weapon)
            {
                return;
            }

            lockWeapon = weapon;
            RestartLockProgressForSelection();
        }

        public void Configure(AircraftController aircraftController, Camera camera)
        {
            aircraft = aircraftController;
            lockCamera = camera;
            EnsureAudio();
            ClearSelection();
        }

        public void UpdateLockProgress(bool weaponActive)
        {
            ReticleVisible = weaponActive;
            TargetOutOfRange = false;
            if (!weaponActive || lockWeapon == null || aircraft == null)
            {
                StopLockProgressOnly();
                UpdateIffDisplay();
                return;
            }

            UpdateIffDisplay();

            if (SelectedTarget != null && !SelectedTarget.IsAlive)
            {
                ClearSelection();
            }

            if (SelectedTarget == null)
            {
                LockState = MissileLockState.None;
                return;
            }

            if (!CanWeaponLockTarget(SelectedTarget))
            {
                StopLockProgressOnly();
                return;
            }

            if (ShouldDropSelectionForForwardArcLoss())
            {
                ClearSelection();
                return;
            }

            TargetOutOfRange = !IsTargetInWeaponRange(SelectedTarget);

            if (LockState == MissileLockState.Locked)
            {
                if (!IsTargetLockable(SelectedTarget))
                {
                    ClearSelection();
                    return;
                }

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
            if (candidate == null)
            {
                return false;
            }

            if (lockWeapon != null && !IsSelectableTarget(candidate))
            {
                return true;
            }

            if (SelectedTarget == candidate)
            {
                if (lockWeapon != null && CanWeaponLockTarget(candidate))
                {
                    TargetOutOfRange = !IsTargetInWeaponRange(candidate);
                }

                return false;
            }

            SelectedTarget = candidate;

            if (lockWeapon != null && candidate.RespondsWithIff && candidate.MatchesWeapon(lockWeapon.ValidTargetKind))
            {
                EnsureAudio();
                TriggerIffFriendResponse(candidate);
            }

            RestartLockProgressForSelection();
            return true;
        }

        public bool TrySelectRadarContact(LockableTarget target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (lockWeapon != null && !IsSelectableTarget(target))
            {
                return true;
            }

            if (SelectedTarget == target)
            {
                if (lockWeapon != null && CanWeaponLockTarget(target))
                {
                    TargetOutOfRange = !IsTargetInWeaponRange(target);
                }

                return false;
            }

            SelectedTarget = target;

            if (lockWeapon != null
                && target.RespondsWithIff
                && target.MatchesWeapon(lockWeapon.ValidTargetKind))
            {
                EnsureAudio();
                TriggerIffFriendResponse(target);
            }

            RestartLockProgressForSelection();
            return true;
        }

        public LockableTarget GetLockedTarget()
        {
            if (LockState != MissileLockState.Locked || SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return null;
            }

            return IsTargetLockable(SelectedTarget) ? SelectedTarget : null;
        }

        public void ClearLockAfterFire()
        {
            PauseLockTracking();
        }

        private LockableTarget FindTargetUnderCursor(Vector2 screenPosition)
        {
            var target = HudTargetSelection.FindTargetAtScreenPosition(lockCamera, screenPosition);
            if (target == null || lockWeapon == null)
            {
                return target;
            }

            if (lockWeapon.AimMode == WeaponAimMode.ForwardLock
                && CanWeaponLockTarget(target)
                && !IsTargetInLockCoverage(target))
            {
                return null;
            }

            return target;
        }

        private bool CanWeaponLockTarget(LockableTarget target)
        {
            if (target == null || !target.IsAlive || lockWeapon == null || target.IsFriendly)
            {
                return false;
            }

            if (!target.MatchesWeapon(lockWeapon.ValidTargetKind))
            {
                return false;
            }

            return true;
        }

        private bool IsSelectableTarget(LockableTarget target)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            if (lockWeapon == null)
            {
                return true;
            }

            return target.MatchesWeapon(lockWeapon.ValidTargetKind);
        }

        public bool ShouldBlockFireForSelection()
        {
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return false;
            }

            if (SelectedTarget.IsFriendly)
            {
                return true;
            }

            if (lockWeapon != null && !SelectedTarget.MatchesWeapon(lockWeapon.ValidTargetKind))
            {
                return true;
            }

            return false;
        }

        public bool ShouldBlockFireWithoutSelection()
        {
            return lockWeapon != null && (SelectedTarget == null || !SelectedTarget.IsAlive);
        }

        public bool ShouldBlockFireWithoutLock()
        {
            return lockWeapon != null && LockState != MissileLockState.Locked;
        }

        private bool IsTargetLockable(LockableTarget target)
        {
            return IsTargetInWeaponRange(target) && IsTargetInLockCoverage(target);
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
            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (profile == null || target == null || lockWeapon == null)
            {
                return false;
            }

            return WeaponLockRange.IsWithinRange(
                aircraft.transform.position,
                target.transform.position,
                lockWeapon.RangeMiles,
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
        }

        private bool ShouldDropSelectionForForwardArcLoss()
        {
            if (SelectedTarget == null || lockWeapon == null || aircraft == null)
            {
                return false;
            }

            if (lockWeapon.AimMode != WeaponAimMode.ForwardLock || !CanWeaponLockTarget(SelectedTarget))
            {
                return false;
            }

            return !IsTargetInLockCoverage(SelectedTarget);
        }

        private void StopLockProgressOnly()
        {
            lockProgress = 0f;
            beepTimer = 0f;
            LockState = MissileLockState.None;
            StopLockTone();
        }

        private void PauseLockTracking()
        {
            StopLockProgressOnly();
        }

        private void ClearSelection()
        {
            SelectedTarget = null;
            PauseLockTracking();
        }

        private void UpdateBeepAudio()
        {
            if (LockState == MissileLockState.Locked || lockWeapon == null)
            {
                return;
            }

            EnsureAudio();
            if (audioSource == null)
            {
                return;
            }

            var t = LockProgressNormalized;
            var interval = Mathf.Lerp(lockWeapon.MaxBeepInterval, lockWeapon.MinBeepInterval, t);
            beepTimer -= Time.deltaTime;
            if (beepTimer > 0f)
            {
                return;
            }

            beepTimer = interval;
            audioSource.PlayOneShot(beepClip);
        }

        private void PlayLockToneIfNeeded()
        {
            if (lockTonePlaying)
            {
                return;
            }

            EnsureAudio();
            lockTonePlaying = true;
            audioSource.loop = true;
            audioSource.clip = lockToneClip;
            audioSource.Play();
        }

        private void StopLockTone()
        {
            if (!lockTonePlaying)
            {
                return;
            }

            lockTonePlaying = false;
            audioSource.loop = false;
            audioSource.Stop();
        }

        private void TriggerIffFriendResponse(LockableTarget target)
        {
            IffFriendActive = true;
            IffFriendLabel = target != null ? target.TargetLabel : string.Empty;
            iffDisplayTimer = 2.5f;
            audioSource.PlayOneShot(iffFriendClip);
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

        private void EnsureAudio()
        {
            if (audioSource != null)
            {
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.7f;
            beepClip = ProceduralBeepTone.CreateBeep(880f, 0.06f);
            lockToneClip = ProceduralBeepTone.CreateLockTone(1320f, 0.6f);
            iffFriendClip = ProceduralBeepTone.CreateIffFriendTone();
        }

        private void OnDisable()
        {
            StopLockTone();
        }
    }
}
