using SaveAntarctica.BunkerDefense.Core;

using SaveAntarctica.BunkerDefense.World;

using UnityEngine;

using UnityEngine.InputSystem;



namespace SaveAntarctica.BunkerDefense.Combat

{

    public sealed class MachineGun : MonoBehaviour

    {

        private SandbagBunker _bunker;

        private AudioSource _fireSource;

        private float _heat;

        private float _nextShotTime;

        private bool _overheated;

        private bool _canFire = true;



        public float Heat01 => _heat;

        public bool IsOverheated => _overheated;

        public bool CanFire => _canFire;

        public Vector2 AimPoint { get; private set; }

        public Vector2 CrosshairScreen { get; private set; }



        public static MachineGun Create(SandbagBunker bunker)

        {

            var go = new GameObject("MachineGun");

            var gun = go.AddComponent<MachineGun>();

            gun._bunker = bunker;

            gun.SetupFireAudio();

            return gun;

        }



        public void SetCanFire(bool canFire)

        {

            _canFire = canFire;

            Cursor.visible = !canFire;

            if (!canFire)

            {

                StopFireSound();

            }

        }



        private void OnDestroy()

        {

            Cursor.visible = true;

            StopFireSound();

        }



        private void SetupFireAudio()

        {

            _fireSource = gameObject.AddComponent<AudioSource>();

            _fireSource.clip = Resources.Load<AudioClip>(GameConstants.MachineGunFireSoundResource);

            _fireSource.loop = true;

            _fireSource.playOnAwake = false;

            _fireSource.spatialBlend = 0f;

            _fireSource.volume = 0.213f;



            if (_fireSource.clip == null)

            {

                Debug.LogError(

                    $"F-89 Bunker Defense: missing MG audio at Resources/{GameConstants.MachineGunFireSoundResource}.");

            }

        }



        private void Update()

        {

            var camera = Camera.main;

            var screen = ReadMouseScreen();

            CrosshairScreen = BattlefieldLayout.ClampScreenToOpening(screen, camera);

            AimPoint = BattlefieldLayout.ScreenToOpeningAimWorld(screen, camera);

            _bunker?.AimAt(AimPoint);



            UpdateFireSound();

            CoolOrRecover();

            TryLaunchBoardClearGrenade();

            if (!_canFire || _overheated || !IsFireHeld())

            {

                return;

            }



            if (Time.time < _nextShotTime)

            {

                return;

            }



            Fire();

        }



        private void UpdateFireSound()

        {

            if (_fireSource == null || _fireSource.clip == null)

            {

                return;

            }



            var shouldPlay = _canFire && !_overheated && IsFireHeld();

            if (shouldPlay)

            {

                if (!_fireSource.isPlaying)

                {

                    _fireSource.Play();

                }



                return;

            }



            StopFireSound();

        }



        private void StopFireSound()

        {

            if (_fireSource != null && _fireSource.isPlaying)

            {

                _fireSource.Stop();

            }

        }



        private void Fire()

        {

            _nextShotTime = Time.time + 1f / GameConstants.MachineGunRoundsPerSecond;

            _heat = Mathf.Min(1f, _heat + GameConstants.HeatPerShot);

            if (_heat >= 1f)

            {

                _overheated = true;

                StopFireSound();

            }



            var aim = AimPoint + Random.insideUnitCircle * GameConstants.MachineGunSpread;

            _bunker?.Kick();
            SpawnBarrelFlames();

            var hit = FindTargetTransform(aim);

            var end = hit != null ? (Vector2)hit.position : aim;

            FxService.SpawnTracer(AimPoint, end);



            if (hit != null && TryDamageTarget(hit, 1))

            {

                FxService.SpawnHitSpark(hit.position);

            }

        }



        private void SpawnBarrelFlames()

        {

            if (_bunker == null || _bunker.Muzzles == null || _bunker.Muzzles.Length == 0)

            {

                return;

            }



            var muzzles = new Vector2[_bunker.Muzzles.Length];

            for (var i = 0; i < muzzles.Length; i++)

            {

                muzzles[i] = _bunker.Muzzles[i].position;

            }



            FxService.SpawnBarrelFlames(muzzles, AimPoint);

        }



        private static Transform FindTargetTransform(Vector2 aim)
        {
            var hits = Physics2D.OverlapCircleAll(aim, GameConstants.MachineGunHitRadius);
            Transform best = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < hits.Length; i++)
            {
                var soldier = hits[i].GetComponent<EnemySoldier>();
                if (soldier != null && soldier.IsAlive)
                {
                    var dist = ((Vector2)soldier.transform.position - aim).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = soldier.transform;
                    }

                    continue;
                }

                var vehicle = hits[i].GetComponent<EnemyVehicle>();
                if (vehicle != null && vehicle.IsAlive)
                {
                    var vehicleDist = ((Vector2)vehicle.transform.position - aim).sqrMagnitude;
                    if (vehicleDist < bestDist)
                    {
                        bestDist = vehicleDist;
                        best = vehicle.transform;
                    }

                    continue;
                }

                var saucer = hits[i].GetComponent<FlyingSaucer>();
                if (saucer != null && saucer.IsAlive)
                {
                    var saucerDist = ((Vector2)saucer.transform.position - aim).sqrMagnitude;
                    if (saucerDist < bestDist)
                    {
                        bestDist = saucerDist;
                        best = saucer.transform;
                    }
                }
            }

            return best;
        }

        private static bool TryDamageTarget(Transform target, int damage)
        {
            var soldier = target.GetComponent<EnemySoldier>();
            if (soldier != null)
            {
                return soldier.TryHit(damage);
            }

            var vehicle = target.GetComponent<EnemyVehicle>();
            if (vehicle != null)
            {
                return vehicle.TryHit(damage);
            }

            var saucer = target.GetComponent<FlyingSaucer>();
            return saucer != null && saucer.TryHit(damage);
        }



        private void CoolOrRecover()

        {

            if (_overheated)

            {

                _heat = Mathf.MoveTowards(_heat, 0f, GameConstants.HeatCoolPerSecond * 0.85f * Time.deltaTime);

                if (_heat <= GameConstants.OverheatLockUntil)

                {

                    _overheated = false;

                }



                return;

            }



            if (!IsFireHeld())

            {

                _heat = Mathf.MoveTowards(_heat, 0f, GameConstants.HeatCoolPerSecond * Time.deltaTime);

            }

        }



        private void TryLaunchBoardClearGrenade()
        {
            if (!_canFire || !WasRightClickPressed())
            {
                return;
            }

            BoardClearGrenade.TryUse();
        }

        private static bool WasRightClickPressed()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.rightButton.wasPressedThisFrame;
            }

            return Input.GetMouseButtonDown(1);
        }

        private static bool IsFireHeld()

        {

            var mouse = Mouse.current;

            if (mouse != null)

            {

                return mouse.leftButton.isPressed;

            }



            return Input.GetMouseButton(0);

        }



        private static Vector2 ReadMouseScreen()

        {

            var mouse = Mouse.current;

            if (mouse != null)

            {

                return mouse.position.ReadValue();

            }



            return Input.mousePosition;

        }

    }

}


