using F89.Controls;
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

        private const int UnlimitedAmmoCount = 9999;

        private int aim9zRemaining;
        private int agm88jRemaining;
        private int gbu12Remaining;
        private int agm114Remaining;

        public SelectedWeapon ActiveWeapon { get; private set; } = SelectedWeapon.None;
        public int Aim9zRemaining => aim9zRemaining;
        public int Agm88jRemaining => agm88jRemaining;
        public int Gbu12Remaining => gbu12Remaining;
        public int Agm114Remaining => agm114Remaining;
        public Gau27aGunController Gau27aGun => gau27aGun;
        public MissileLockController LockController => lockController;
        public HudTargetFilter ActiveHudTargetFilter => ActiveWeapon switch
        {
            SelectedWeapon.Aim9z => HudTargetFilter.AirOnly,
            SelectedWeapon.Agm88jSiaw => HudTargetFilter.GroundOnly,
            SelectedWeapon.Agm114Hellfire => HudTargetFilter.GroundOnly,
            SelectedWeapon.Gbu12Paveway => HudTargetFilter.GroundOnly,
            SelectedWeapon.Gau27a => HudTargetFilter.AirAndGround,
            _ => HudTargetFilter.None
        };

        public float ActiveWeaponRangeMiles => ActiveWeapon switch
        {
            SelectedWeapon.Aim9z => aim9zConfig != null ? aim9zConfig.rangeMiles : 0f,
            SelectedWeapon.Agm88jSiaw => agm88jConfig != null ? agm88jConfig.rangeMiles : 0f,
            SelectedWeapon.Agm114Hellfire => agm114Config != null ? agm114Config.rangeMiles : 0f,
            SelectedWeapon.Gbu12Paveway => gbu12Config != null ? gbu12Config.rangeMiles : 0f,
            SelectedWeapon.Gau27a => gau27aConfig != null ? gau27aConfig.maxRangeMiles : 0f,
            _ => 0f
        };

        public bool ShouldShowHudMarkerFor(LockableTarget target)
        {
            if (target == null || !target.IsAlive)
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
            aim9zRemaining = UnlimitedAmmoCount;
            agm88jRemaining = UnlimitedAmmoCount;
            gbu12Remaining = UnlimitedAmmoCount;
            agm114Remaining = UnlimitedAmmoCount;
            gau27aGun?.Configure(gau27a, aircraftController, Camera.main);
            if (gau27aGun != null)
            {
                gau27aGun.SetUnlimitedAmmo(true);
            }
        }

        public void SetRadarOverlay(PlaneRadarOverlay overlay)
        {
            radarOverlay = overlay;
        }

        private void Update()
        {
            if (inputSource == null || aircraft == null || lockController == null)
            {
                return;
            }

            var input = inputSource.Current;
            var rawAimScreen = input.aimScreenPosition;
            var aimScreen = GetClampedAimScreenPosition();
            HandleWeaponSelect(input);

            if (ActiveWeapon == SelectedWeapon.Gau27a && gau27aGun != null)
            {
                lockController.SetActiveWeapon(null);
                lockController.UpdateLockProgress(false);
                gau27aGun.UpdateCrosshairFromMouse(rawAimScreen);
                gau27aGun.TryFire(
                    aircraft.GetWeaponAccuracyMultiplier(),
                    input.fireHeld || input.firePressed);
                HandleTargetSelectionClick(input, rawAimScreen);
                return;
            }

            var lockWeapon = GetActiveLockWeapon();
            if (lockWeapon != null)
            {
                lockController.SetActiveWeapon(lockWeapon);
            }
            else
            {
                lockController.SetActiveWeapon(null);
            }

            HandleTargetSelectionClick(input, rawAimScreen, lockWeapon != null ? FireActiveWeapon : null, aimScreen);

            if (lockWeapon != null)
            {
                lockController.UpdateLockProgress(true);
            }
            else
            {
                lockController.UpdateLockProgress(false);
            }
        }

        private void HandleTargetSelectionClick(
            AircraftControlInput input,
            Vector2 rawAimScreen,
            System.Action<Vector2> fireAction = null,
            Vector2 aimScreen = default)
        {
            if (!input.firePressed)
            {
                return;
            }

            var radar = ResolveRadarOverlay();
            if (radar != null && radar.TrySelectBlipAtScreenPosition(rawAimScreen))
            {
                return;
            }

            if (lockController.TrySelectTargetOnClick(rawAimScreen))
            {
                return;
            }

            if (lockController.ShouldBlockFireForSelection())
            {
                return;
            }

            if (lockController.ShouldBlockFireWithoutSelection())
            {
                return;
            }

            if (lockController.ShouldBlockFireWithoutLock())
            {
                return;
            }

            fireAction?.Invoke(aimScreen);
        }

        private PlaneRadarOverlay ResolveRadarOverlay()
        {
            if (radarOverlay == null)
            {
                radarOverlay = Object.FindAnyObjectByType<PlaneRadarOverlay>();
            }

            return radarOverlay;
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
            if (config == null)
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

            remaining = UnlimitedAmmoCount;
            lockController.ClearLockAfterFire();
            Debug.Log(
                $"{config.WeaponName} fired. Remaining: {remaining}. {(lockedShot ? "LOCKED" : "UNLOCKED")}. Accuracy: {accuracy:P0}");
        }

        private void TryDropGbu12(Vector2 aimScreen)
        {
            if (gbu12Config == null)
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

            gbu12Remaining = UnlimitedAmmoCount;
            lockController.ClearLockAfterFire();
            var targetLabel = lockedShot ? lockedTarget.TargetLabel : "unguided";
            Debug.Log(
                $"{gbu12Config.WeaponName} released ({targetLabel}). Remaining: {gbu12Remaining}. Accuracy: {accuracy:P0}. {(lockedShot ? "LOCKED" : "UNLOCKED")}");
        }
    }
}
