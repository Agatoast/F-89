using F89.Audio;
using F89.Controls;
using F89.Core;
using F89.Flight;
using F89.UI;
using UnityEngine;

namespace F89.Weapons
{
    public enum SelectedWeapon
    {
        None,
        Aim9z,
        Agm88jSiaw,
        Gbu12Paveway,
        Agm114Hellfire,
        Gau27a
    }

    public enum HudTargetFilter
    {
        None,
        AirOnly,
        GroundOnly,
        AirAndGround
    }

    public class PlayerWeaponController : MonoBehaviour
    {
        private static readonly Color Aim9zMissileColor = new Color(0.85f, 0.2f, 0.1f);
        private static readonly Color Agm88jMissileColor = new Color(0.35f, 0.38f, 0.32f);
        private static readonly Color Agm114MissileColor = new Color(0.5f, 0.45f, 0.35f);

        [SerializeField] private Aim9zWeaponConfig aim9zConfig;
        [SerializeField] private Agm88jSiawWeaponConfig agm88jConfig;
        [SerializeField] private Gbu12PavewayConfig gbu12Config;
        [SerializeField] private Agm114HellfireWeaponConfig agm114Config;
        [SerializeField] private Gau27aWeaponConfig gau27aConfig;
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private PlayerAircraftInput inputSource;
        [SerializeField] private MissileLockController lockController;
        [SerializeField] private WeaponTargetPaint targetPaint;
        [SerializeField] private Gau27aGunController gau27aGun;
        [SerializeField] private PlaneRadarOverlay radarOverlay;


        private int aim9zRemaining;
        private int agm88jRemaining;
        private int gbu12Remaining;
        private int agm114Remaining;
        private bool inventoryInitialized;

        public SelectedWeapon ActiveWeapon { get; private set; } = SelectedWeapon.None;
        public int Aim9zRemaining => aim9zRemaining;
        public int Agm88jRemaining => agm88jRemaining;
        public int Gbu12Remaining => gbu12Remaining;
        public int Agm114Remaining => agm114Remaining;
        public bool HasSortieInventory => inventoryInitialized;
        public Gau27aGunController Gau27aGun => gau27aGun;
        public MissileLockController LockController => lockController;

        private void Awake()
        {
            EnsureRuntimeReferences();
        }

        private void EnsureRuntimeReferences()
        {
            if (aircraft == null)
            {
                aircraft = GetComponent<AircraftController>();
            }

            if (inputSource == null)
            {
                inputSource = GetComponent<PlayerAircraftInput>();
            }

            if (lockController == null)
            {
                lockController = GetComponent<MissileLockController>();
            }

            if (targetPaint == null)
            {
                targetPaint = GetComponent<WeaponTargetPaint>();
            }

            if (gau27aGun == null)
            {
                gau27aGun = GetComponent<Gau27aGunController>();
            }
        }

        public HudTargetFilter ActiveHudTargetFilter => ActiveWeapon switch
        {
            SelectedWeapon.Aim9z => HudTargetFilter.AirOnly,
            SelectedWeapon.Agm88jSiaw => HudTargetFilter.GroundOnly,
            SelectedWeapon.Agm114Hellfire => HudTargetFilter.GroundOnly,
            SelectedWeapon.Gbu12Paveway => HudTargetFilter.GroundOnly,
            SelectedWeapon.Gau27a => HudTargetFilter.AirAndGround,
            // No weapon: still allow Tab select / HUD markers (cycle uses short-radar range).
            _ => HudTargetFilter.AirAndGround
        };

        public float ActiveWeaponRangeMiles => GetWeaponRangeMiles(ActiveWeapon);

        public float GetWeaponRangeMiles(SelectedWeapon weapon)
        {
            return weapon switch
            {
                SelectedWeapon.Aim9z => aim9zConfig != null ? aim9zConfig.rangeMiles : 0f,
                SelectedWeapon.Agm88jSiaw => agm88jConfig != null ? agm88jConfig.rangeMiles : 0f,
                SelectedWeapon.Agm114Hellfire => agm114Config != null ? agm114Config.rangeMiles : 0f,
                SelectedWeapon.Gbu12Paveway => gbu12Config != null ? gbu12Config.rangeMiles : 0f,
                SelectedWeapon.Gau27a => gau27aConfig != null ? gau27aConfig.ogiveMaxRangeMiles : 0f,
                _ => 0f
            };
        }

        public bool ShouldShowHudMarkerFor(LockableTarget target)
        {
            if (target == null || !target.IsAlive || target.IsFlareDecoy || target.IsPlayerAircraft)
            {
                return false;
            }

            return ActiveHudTargetFilter switch
            {
                HudTargetFilter.AirOnly => target.TargetKind == LockableTargetKind.Air,
                HudTargetFilter.GroundOnly => target.TargetKind == LockableTargetKind.Ground,
                HudTargetFilter.AirAndGround => true,
                _ => false
            };
        }

        public LockableTarget GetActiveHudTarget()
        {
            if (lockController == null || lockController.SelectedTarget == null || !lockController.SelectedTarget.IsAlive)
            {
                return null;
            }

            return lockController.SelectedTarget;
        }

        public bool HasLockedTarget =>
            lockController != null && lockController.GetLockedTarget() != null;

        public string ActiveWeaponEngagementLabel => ActiveWeapon switch
        {
            SelectedWeapon.Gau27a => "GUN",
            SelectedWeapon.Aim9z => "A-A MSLS",
            SelectedWeapon.Agm88jSiaw or SelectedWeapon.Agm114Hellfire or SelectedWeapon.Gbu12Paveway =>
                "A-G MSLS",
            _ => "---"
        };

        public Vector2 GetClampedAimScreenPosition()
        {
            if (inputSource == null || aircraft == null)
            {
                return default;
            }

            var aim = inputSource.Current.aimScreenPosition;
            if (ActiveWeapon == SelectedWeapon.None)
            {
                return aim;
            }

            var rangeMiles = ActiveWeaponRangeMiles;
            if (rangeMiles <= 0f)
            {
                return aim;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return aim;
            }

            var profile = aircraft.Profile;
            return WeaponAimRange.ClampScreenPositionToRange(
                camera,
                aircraft.transform.position,
                aim,
                rangeMiles,
                aircraft.WorldMap,
                profile != null ? profile.ticSizeWorldUnits : 1f);
        }

        public void Configure(
            Aim9zWeaponConfig aim9z,
            Agm88jSiawWeaponConfig agm88j,
            Gbu12PavewayConfig gbu12,
            Agm114HellfireWeaponConfig agm114,
            Gau27aWeaponConfig gau27a,
            AircraftController aircraftController,
            PlayerAircraftInput input,
            MissileLockController lockSystem,
            WeaponTargetPaint paint,
            Gau27aGunController gun)
        {
            aim9zConfig = aim9z;
            agm88jConfig = agm88j;
            gbu12Config = gbu12;
            agm114Config = agm114;
            gau27aConfig = gau27a;
            aircraft = aircraftController;
            inputSource = input;
            lockController = lockSystem;
            targetPaint = paint;
            gau27aGun = gun;
            if (!inventoryInitialized)
            {
                aim9zRemaining = aim9z != null ? aim9z.startingMissileCount : 0;
                agm88jRemaining = agm88j != null ? agm88j.startingMissileCount : 0;
                gbu12Remaining = gbu12 != null ? gbu12.startingBombCount : 0;
                agm114Remaining = agm114 != null ? agm114.startingMissileCount : 0;
                inventoryInitialized = true;
            }

            gau27aGun?.Configure(gau27a, aircraftController, Camera.main);
        }

        public void ApplySortieLoadout(int aim9z, int agm88j, int gbu12, int agm114, int gauRounds)
        {
            aim9zRemaining = Mathf.Max(0, aim9z);
            agm88jRemaining = Mathf.Max(0, agm88j);
            gbu12Remaining = Mathf.Max(0, gbu12);
            agm114Remaining = Mathf.Max(0, agm114);
            inventoryInitialized = true;
            gau27aGun?.SetRounds(gauRounds);
        }

        public int ComputeRemainingPayloadLbs()
        {
            var gunRounds = gau27aGun != null
                ? Mathf.Min(gau27aGun.RoundsRemaining, AircraftLoadoutState.MaxGunRounds)
                : 0;
            return AircraftLoadoutState.ComputePayloadLbs(
                aim9zRemaining,
                agm88jRemaining,
                gbu12Remaining,
                agm114Remaining,
                gunRounds);
        }

        public void SetRadarOverlay(PlaneRadarOverlay overlay)
        {
            radarOverlay = overlay;
        }

        private void Update()
        {
            EnsureRuntimeReferences();
            var gauFireSound = GetComponent<Gau27FireSound>();
            if (GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || inputSource == null
                || aircraft == null
                || lockController == null)
            {
                gauFireSound?.NotifyFireReleased();
                lockController?.UpdateLockProgress(false);
                return;
            }

            if (!PlayerAircraftCombatState.CanOperateWeapons(aircraft))
            {
                gauFireSound?.NotifyFireReleased();
                lockController.UpdateLockProgress(false);
                HandleWeaponSelect(inputSource.Current);
                return;
            }

            var input = inputSource.Current;
            var rawAimScreen = input.aimScreenPosition;
            var aimScreen = GetClampedAimScreenPosition();
            lockController.SetClickSelectionRules(IsClickSelectCandidate, GetClickSelectPriority);
            HandleWeaponSelect(input);

            if (ActiveWeapon != SelectedWeapon.Gau27a)
            {
                GetComponent<Gau27FireSound>()?.NotifyFireReleased();
            }

            if (ActiveWeapon == SelectedWeapon.Gau27a && gau27aGun != null)
            {
                if (input.cycleTargetPressed)
                {
                    TryCycleTarget();
                    AlignGauCrosshairToSelectedTarget();
                }

                HandleTargetSelectionClick(input, rawAimScreen);
                ApplyLockControllerState(gau27aConfig, gau27aConfig);

                gau27aGun.UpdateCrosshairFromMouse(rawAimScreen);

                // GAU fire/sound: physical Fire binding only (default LMB). Ignore AutoFire —
                // selecting the gun must not start the burst until the trigger is held.
                var triggerHeld = GameKeyBindings.IsHeld(GameKeyBindingIds.Fire);
                Gau27FireSound.EnsureOn(gameObject);
                GetComponent<Gau27FireSound>()?.SetFiring(
                    triggerHeld && gau27aGun.RoundsRemaining > 0);

                var lockedTarget = lockController.GetLockedTarget();
                gau27aGun.TryFire(aircraft.GetWeaponAccuracyMultiplier(), triggerHeld, lockedTarget);
                return;
            }

            if (input.cycleTargetPressed)
            {
                TryCycleTarget();
            }

            var fireLockWeapon = GetActiveLockWeapon();
            HandleTargetSelectionClick(
                input,
                rawAimScreen,
                fireLockWeapon != null ? FireActiveWeapon : null,
                aimScreen);
            ApplyLockControllerState(fireLockWeapon, ResolveTrackingLockWeapon(fireLockWeapon));
        }

        private void ApplyLockControllerState(ILockCapableWeapon fireLockWeapon, ILockCapableWeapon trackingLockWeapon)
        {
            if (trackingLockWeapon != null)
            {
                lockController.SetActiveWeapon(fireLockWeapon, trackingLockWeapon);
                lockController.UpdateLockProgress(true);
                return;
            }

            lockController.SetActiveWeapon(null);
            lockController.UpdateLockProgress(false);
        }

        private ILockCapableWeapon ResolveTrackingLockWeapon(ILockCapableWeapon fireLockWeapon)
        {
            if (fireLockWeapon != null)
            {
                return fireLockWeapon;
            }

            if (lockController != null
                && lockController.SelectedTarget != null
                && lockController.SelectedTarget.IsAlive)
            {
                return GenericTargetLockProfile.Instance;
            }

            return null;
        }

        private void HandleTargetSelectionClick(
            AircraftControlInput input,
            Vector2 rawAimScreen,
            System.Action<Vector2> fireAction = null,
            Vector2 aimScreen = default)
        {
            var click = Input.GetMouseButtonDown(0);
            if (!click && !input.firePressed)
            {
                return;
            }

            if (click)
            {
                if (lockController.TrySelectTargetOnClick(rawAimScreen))
                {
                    return;
                }

                var radar = ResolveRadarOverlay();
                if (radar != null && radar.TrySelectBlipAtScreenPosition(rawAimScreen))
                {
                    return;
                }
            }

            TryFireIfReady(fireAction, aimScreen);
        }

        private bool TryFireIfReady(System.Action<Vector2> fireAction, Vector2 aimScreen)
        {
            if (fireAction == null)
            {
                return false;
            }

            if (lockController.ShouldBlockFireForSelection())
            {
                return false;
            }

            if (lockController.ShouldBlockFireWithoutSelection())
            {
                return false;
            }

            if (lockController.ShouldBlockFireWithoutLock())
            {
                return false;
            }

            fireAction.Invoke(aimScreen);
            return true;
        }

        private PlaneRadarOverlay ResolveRadarOverlay()
        {
            if (radarOverlay == null)
            {
                radarOverlay = Object.FindAnyObjectByType<PlaneRadarOverlay>();
            }

            return radarOverlay;
        }

        private void TryCycleTarget()
        {
            if (aircraft == null || lockController == null)
            {
                return;
            }

            // Tab cycles contacts inside short-radar range (weapon optional).
            // Land outposts remain selectable out to hostile-detection range so they match radar.
            var rangeMiles = RadarContactScanner.HostileDetectionMiles;
            if (rangeMiles <= 0f)
            {
                return;
            }

            var profile = aircraft.Profile;
            var worldMap = aircraft.WorldMap;
            if (profile == null || worldMap == null)
            {
                return;
            }

            lockController.TryCycleNextTargetInRange(
                rangeMiles,
                worldMap,
                profile.ticSizeWorldUnits,
                IsCycleCandidate,
                CompareCycleCandidates);
        }

        private int CompareCycleCandidates(LockableTarget a, LockableTarget b)
        {
            if (a == null || b == null)
            {
                return 0;
            }

            var priority = GetCyclePriority(a).CompareTo(GetCyclePriority(b));
            if (priority != 0)
            {
                return priority;
            }

            if (aircraft == null)
            {
                return 0;
            }

            var observer = aircraft.transform.position;
            var distanceA = HorizontalDistanceMeters(observer, a.transform.position);
            var distanceB = HorizontalDistanceMeters(observer, b.transform.position);
            return distanceA.CompareTo(distanceB);
        }

        private static float HorizontalDistanceMeters(Vector3 observer, Vector3 targetPosition)
        {
            var delta = targetPosition - observer;
            delta.y = 0f;
            return delta.magnitude;
        }

        private int GetCyclePriority(LockableTarget target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            switch (ActiveWeapon)
            {
                case SelectedWeapon.Agm88jSiaw:
                case SelectedWeapon.Agm114Hellfire:
                    if (target.IsGroundVehicle)
                    {
                        return 0;
                    }

                    if (target.IsInfantry)
                    {
                        return 1;
                    }

                    return 2;
                case SelectedWeapon.Gbu12Paveway:
                    if (target.IsBuilding)
                    {
                        return 0;
                    }

                    return 1;
                default:
                    return 0;
            }
        }

        private void AlignGauCrosshairToSelectedTarget()
        {
            if (gau27aGun == null
                || aircraft == null
                || lockController == null
                || lockController.SelectedTarget == null
                || !lockController.SelectedTarget.IsAlive)
            {
                return;
            }

            var profile = aircraft.Profile;
            var worldMap = aircraft.WorldMap;
            if (profile == null || worldMap == null)
            {
                return;
            }

            gau27aGun.SetCrosshairToWorldPoint(lockController.SelectedTarget.transform.position);
        }

        private bool IsClickSelectCandidate(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFlareDecoy
                || target.IsPlayerAircraft)
            {
                return false;
            }

            if (target.IsFriendly)
            {
                return target.RespondsWithIff;
            }

            if (target.IsNeutral)
            {
                return false;
            }

            var outpostBuilding = target.GetComponent<OutpostBuilding>();
            if (outpostBuilding != null && !OutpostPrimaryObjective.IsMissionHostileTarget(target))
            {
                return false;
            }

            var baseSite = target.GetComponent<AntarcticaBase>();
            if (baseSite != null && baseSite.SiteKind == BaseSiteKind.Carrier)
            {
                return false;
            }

            switch (ActiveWeapon)
            {
                case SelectedWeapon.Aim9z:
                    return target.IsAirTarget;
                case SelectedWeapon.Agm88jSiaw:
                case SelectedWeapon.Agm114Hellfire:
                    return target.IsGroundVehicle
                        || target.IsInfantry
                        || target.IsBuilding
                        || target.GetComponent<OutpostBuilding>() != null;
                case SelectedWeapon.Gbu12Paveway:
                    return target.TargetKind == LockableTargetKind.Ground;
                default:
                    return true;
            }
        }

        private int GetClickSelectPriority(LockableTarget target)
        {
            if (target == null)
            {
                return int.MaxValue;
            }

            switch (ActiveWeapon)
            {
                case SelectedWeapon.Agm88jSiaw:
                case SelectedWeapon.Agm114Hellfire:
                    if (target.IsGroundVehicle)
                    {
                        return 0;
                    }

                    if (target.IsInfantry)
                    {
                        return 1;
                    }

                    if (target.IsBuilding || target.GetComponent<OutpostBuilding>() != null)
                    {
                        return 2;
                    }

                    return 3;
                case SelectedWeapon.Gbu12Paveway:
                    if (target.IsBuilding || target.GetComponent<OutpostBuilding>() != null)
                    {
                        return 0;
                    }

                    return 1;
                default:
                    return 0;
            }
        }

        private bool IsCycleCandidate(LockableTarget target)
        {
            if (target == null
                || !target.IsAlive
                || target.IsFlareDecoy
                || target.IsPlayerAircraft
                || target.IsFriendly
                || target.IsNeutral)
            {
                return false;
            }

            var outpostBuilding = target.GetComponent<OutpostBuilding>();
            if (outpostBuilding != null && !OutpostPrimaryObjective.IsMissionHostileTarget(target))
            {
                return false;
            }

            var baseSite = target.GetComponent<AntarcticaBase>();
            if (baseSite != null && baseSite.SiteKind == BaseSiteKind.Land)
            {
                return baseSite.IsActive && !baseSite.IsDestroyed;
            }

            if (baseSite != null && baseSite.SiteKind == BaseSiteKind.Carrier)
            {
                return false;
            }

            if (!IsWithinShortRadarRange(target))
            {
                return false;
            }

            switch (ActiveWeapon)
            {
                case SelectedWeapon.Aim9z:
                    return target.IsAirTarget;
                case SelectedWeapon.Agm88jSiaw:
                case SelectedWeapon.Agm114Hellfire:
                    return target.TargetKind == LockableTargetKind.Ground
                        && (target.IsGroundVehicle || target.IsInfantry);
                case SelectedWeapon.Gbu12Paveway:
                    return target.TargetKind == LockableTargetKind.Ground;
                default:
                    return true;
            }
        }

        private bool IsWithinShortRadarRange(LockableTarget target)
        {
            if (target == null || aircraft == null)
            {
                return false;
            }

            var profile = aircraft.Profile;
            var worldMap = aircraft.WorldMap;
            if (profile == null || worldMap == null)
            {
                return false;
            }

            return WeaponLockRange.IsWithinRange(
                aircraft.transform.position,
                target.transform.position,
                RadarContactScanner.ShortRangeMiles,
                worldMap,
                profile.ticSizeWorldUnits);
        }

        private void FireActiveWeapon(Vector2 aimScreen)
        {
            switch (ActiveWeapon)
            {
                case SelectedWeapon.Aim9z:
                    TryFireMissile(aim9zConfig, ref aim9zRemaining, Aim9zMissileColor, aimScreen);
                    break;
                case SelectedWeapon.Agm88jSiaw:
                    TryFireMissile(agm88jConfig, ref agm88jRemaining, Agm88jMissileColor, aimScreen);
                    break;
                case SelectedWeapon.Gbu12Paveway:
                    TryDropGbu12(aimScreen);
                    break;
                case SelectedWeapon.Agm114Hellfire:
                    TryFireMissile(agm114Config, ref agm114Remaining, Agm114MissileColor, aimScreen);
                    break;
            }
        }

        private void HandleWeaponSelect(AircraftControlInput input)
        {
            if (input.selectAim9zPressed)
            {
                ActiveWeapon = ActiveWeapon == SelectedWeapon.Aim9z
                    ? SelectedWeapon.None
                    : SelectedWeapon.Aim9z;
            }

            if (input.selectAgm88jPressed)
            {
                ActiveWeapon = ActiveWeapon == SelectedWeapon.Agm88jSiaw
                    ? SelectedWeapon.None
                    : SelectedWeapon.Agm88jSiaw;
            }

            if (input.selectGbu12Pressed)
            {
                ActiveWeapon = ActiveWeapon == SelectedWeapon.Gbu12Paveway
                    ? SelectedWeapon.None
                    : SelectedWeapon.Gbu12Paveway;
            }

            if (input.selectAgm114Pressed)
            {
                ActiveWeapon = ActiveWeapon == SelectedWeapon.Agm114Hellfire
                    ? SelectedWeapon.None
                    : SelectedWeapon.Agm114Hellfire;
            }

            if (input.selectGau27aPressed)
            {
                var selecting = ActiveWeapon != SelectedWeapon.Gau27a;
                ActiveWeapon = ActiveWeapon == SelectedWeapon.Gau27a
                    ? SelectedWeapon.None
                    : SelectedWeapon.Gau27a;
                GetComponent<Gau27FireSound>()?.NotifyFireReleased();
                if (selecting && gau27aGun != null)
                {
                    gau27aGun.ResetCrosshairDistance();
                }
            }
        }

        private ILockCapableWeapon GetActiveLockWeapon()
        {
            return ActiveWeapon switch
            {
                SelectedWeapon.Aim9z => aim9zConfig,
                SelectedWeapon.Agm88jSiaw => agm88jConfig,
                SelectedWeapon.Agm114Hellfire => agm114Config,
                SelectedWeapon.Gbu12Paveway => gbu12Config,
                _ => null
            };
        }

        private Vector3 ResolveLaunchDirection(Vector3 spawnPoint, Vector2 aimScreen, LockableTarget lockedTarget, bool lockedShot)
        {
            if (lockedShot && lockedTarget != null && lockedTarget.IsAlive)
            {
                var toTarget = lockedTarget.transform.position - spawnPoint;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    return toTarget.normalized;
                }
            }

            return GetAircraftForward();
        }

        private Vector3 GetLaunchVelocity()
        {
            if (aircraft == null)
            {
                return Vector3.zero;
            }

            var velocity = aircraft.transform.forward * aircraft.CurrentSpeed;
            velocity.y = 0f;
            return velocity;
        }

        private Vector3 GetAircraftForward()
        {
            if (aircraft == null)
            {
                return Vector3.forward;
            }

            var forward = aircraft.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private bool TryResolveAimWorldPoint(Vector2 aimScreen, out Vector3 aimPoint)
        {
            aimPoint = default;
            if (aircraft == null)
            {
                return false;
            }

            var camera = Camera.main;
            var profile = aircraft.Profile;
            return WeaponAimRange.TryResolveAimWorldPoint(
                camera,
                aimScreen,
                aircraft.transform.position,
                ActiveWeaponRangeMiles,
                aircraft.WorldMap,
                profile != null ? profile.ticSizeWorldUnits : 1f,
                out aimPoint);
        }

        private void TryFireMissile(IMissileWeaponConfig config, ref int remaining, Color missileColor, Vector2 aimScreen)
        {
            if (config == null || remaining <= 0)
            {
                return;
            }

            var lockedTarget = lockController.GetLockedTarget();
            if (lockedTarget == null)
            {
                return;
            }

            var lockedShot = true;
            var spawnPoint = aircraft.transform.position + aircraft.transform.forward * 1.2f;
            spawnPoint.y = 0.5f;
            var accuracy = aircraft.GetWeaponAccuracyMultiplier();
            var launchDirection = ResolveLaunchDirection(spawnPoint, aimScreen, lockedTarget, lockedShot);
            var launchVelocity = GetLaunchVelocity();

            HomingMissile.Launch(
                config,
                aircraft.WorldMap,
                aircraft.Profile,
                spawnPoint,
                launchDirection,
                lockedShot ? lockedTarget : null,
                lockedShot,
                missileColor,
                accuracy,
                launchVelocity);

            remaining--;
            MissileFireSound.Play(this);
            lockController.ClearLockAfterFire();
            Debug.Log(
                $"{config.WeaponName} fired. Remaining: {remaining}. {(lockedShot ? "LOCKED" : "UNLOCKED")}. Accuracy: {accuracy:P0}");
        }

        private void TryDropGbu12(Vector2 aimScreen)
        {
            if (gbu12Config == null || gbu12Remaining <= 0)
            {
                return;
            }

            var lockedTarget = lockController.GetLockedTarget();
            if (lockedTarget == null)
            {
                return;
            }

            var lockedShot = true;
            var spawnPoint = aircraft.transform.position + aircraft.transform.forward * 1.2f;
            spawnPoint.y = 0.5f;
            var accuracy = aircraft.GetWeaponAccuracyMultiplier();
            var launchDirection = ResolveLaunchDirection(spawnPoint, aimScreen, lockedTarget, lockedShot);
            var launchVelocity = GetLaunchVelocity();

            Gbu12Bomb.Drop(
                gbu12Config,
                aircraft.WorldMap,
                aircraft.Profile,
                lockedShot ? lockedTarget : null,
                spawnPoint,
                launchDirection,
                accuracy,
                launchVelocity);

            gbu12Remaining--;
            MissileFireSound.Play(this);
            lockController.ClearLockAfterFire();
            var targetLabel = lockedShot ? lockedTarget.TargetLabel : "unguided";
            Debug.Log(
                $"{gbu12Config.WeaponName} released ({targetLabel}). Remaining: {gbu12Remaining}. Accuracy: {accuracy:P0}. {(lockedShot ? "LOCKED" : "UNLOCKED")}");
        }
    }
}
