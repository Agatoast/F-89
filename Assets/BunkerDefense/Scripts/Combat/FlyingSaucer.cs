using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class FlyingSaucer : MonoBehaviour
    {
        private enum State
        {
            Flying,
            Crashing,
            Done
        }

        private SpriteRenderer _renderer;
        private CircleCollider2D _hitbox;
        private State _state = State.Flying;
        private float _moveSign = 1f;
        private float _flySpeed;
        private float _fallSpeed;
        private int _hitPoints;
        private float _animTime;
        private int _frameIndex;
        private SideSaucerSpriteSheet.Clip _activeClip = SideSaucerSpriteSheet.Clip.Flight;
        private float _groundY;
        private bool _countsAsKill;
        private Color _flashColor = Color.white;
        private float _nextLightningTime;

        public bool IsAlive => _state != State.Done;

        public static int AliveCount { get; private set; }

        public static void ResetAliveCount()
        {
            AliveCount = 0;
        }

        public static FlyingSaucer Spawn()
        {
            SideSaucerSpriteSheet.EnsureLoaded();
            var fromLeft = Random.value < 0.5f;
            var spawn = BattlefieldLayout.GetSkySpawnPosition(fromLeft, out var moveSign);

            var go = new GameObject("FlyingSaucer");
            go.tag = GameConstants.EnemyTag;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Enemies";
            renderer.sortingOrder = 6;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;

            var saucer = go.AddComponent<FlyingSaucer>();
            saucer._renderer = renderer;
            saucer._hitbox = collider;
            saucer._moveSign = moveSign;
            saucer._flySpeed = Random.Range(
                GameConstants.FlyingSaucerMinSpeed,
                GameConstants.FlyingSaucerMaxSpeed);
            saucer._fallSpeed = GameConstants.FlyingSaucerCrashFallSpeed;
            saucer._hitPoints = GameConstants.FlyingSaucerHitPoints;
            saucer._groundY = BattlefieldLayout.GetSkyCrashGroundY();
            saucer.transform.position = spawn;
            saucer.transform.localScale = Vector3.one * GameConstants.FlyingSaucerScale;
            saucer._renderer.flipX = moveSign > 0f;
            saucer._nextLightningTime = Time.time + Random.Range(
                GameConstants.FlyingSaucerLightningMinInterval,
                GameConstants.FlyingSaucerLightningMaxInterval);
            AliveCount++;
            saucer.ApplyFlightFrame(0);
            return saucer;
        }

        public bool TryHit(int damage)
        {
            if (_state != State.Flying)
            {
                return false;
            }

            _hitPoints -= damage;
            _flashColor = new Color(1f, 0.72f, 0.72f, 1f);
            if (_hitPoints > 0)
            {
                return true;
            }

            BeginCrash();
            return true;
        }

        private void Update()
        {
            if (_flashColor != Color.white)
            {
                _flashColor = Color.Lerp(_flashColor, Color.white, Time.deltaTime * 8f);
                if (_renderer != null)
                {
                    _renderer.color = _flashColor;
                }
            }

            switch (_state)
            {
                case State.Flying:
                    FlyAcross();
                    TryFireLightning();
                    Animate(SideSaucerSpriteSheet.Clip.Flight, SideSaucerSpriteSheet.FlightFps);
                    break;
                case State.Crashing:
                    CrashFall();
                    Animate(SideSaucerSpriteSheet.Clip.Crash, SideSaucerSpriteSheet.CrashFps);
                    break;
            }
        }

        private void FlyAcross()
        {
            var pos = transform.position;
            pos.x += _moveSign * _flySpeed * Time.deltaTime;
            transform.position = pos;

            var opening = BattlefieldLayout.OpeningWorld;
            if (opening.width < 0.01f)
            {
                return;
            }

            var margin = opening.width * 0.06f;
            if (_moveSign > 0f && pos.x > opening.xMax + margin)
            {
                Despawn(scored: false);
            }
            else if (_moveSign < 0f && pos.x < opening.xMin - margin)
            {
                Despawn(scored: false);
            }
        }

        private void TryFireLightning()
        {
            if (Time.time < _nextLightningTime)
            {
                return;
            }

            if (HangarTarget.Instance != null && HangarTarget.Instance.IsDestroyed)
            {
                return;
            }

            _nextLightningTime = Time.time + Random.Range(
                GameConstants.FlyingSaucerLightningMinInterval,
                GameConstants.FlyingSaucerLightningMaxInterval);

            var strike = BattlefieldLayout.GetOpeningCenterWorld();
            SaucerLightningFx.Strike(transform.position, strike, GameConstants.FlyingSaucerLightningHangarDamage);
        }

        private void BeginCrash()
        {
            _state = State.Crashing;
            _countsAsKill = true;
            _activeClip = SideSaucerSpriteSheet.Clip.Crash;
            _animTime = 0f;
            _frameIndex = -1;
        }

        private void CrashFall()
        {
            var pos = transform.position;
            pos.x += _moveSign * _flySpeed * 0.35f * Time.deltaTime;
            pos.y -= _fallSpeed * Time.deltaTime;
            transform.position = pos;

            if (pos.y <= _groundY)
            {
                ExplodeOnGround(new Vector2(pos.x, _groundY));
            }
        }

        private void ExplodeOnGround(Vector2 position)
        {
            if (_countsAsKill)
            {
                var tracker = GetComponent<FlyingSaucerKillTracker>();
                tracker?.NotifyScored();
            }

            ExplosionFx.Play(position, GameConstants.FlyingSaucerExplosionScale, sortingOrder: 40);
            CombatAudio.PlayRocketExplosion();
            Despawn(scored: false);
        }

        private void Animate(SideSaucerSpriteSheet.Clip clip, float fps)
        {
            if (_activeClip != clip)
            {
                _activeClip = clip;
                _animTime = 0f;
                _frameIndex = -1;
            }

            _animTime += Time.deltaTime;
            var frames = SideSaucerSpriteSheet.GetClip(clip);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            var index = Mathf.FloorToInt(_animTime * fps) % frames.Length;
            if (index == _frameIndex)
            {
                return;
            }

            _frameIndex = index;
            ApplyFrame(frames[index]);
        }

        private void ApplyFlightFrame(int index)
        {
            var frames = SideSaucerSpriteSheet.GetClip(SideSaucerSpriteSheet.Clip.Flight);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, frames.Length - 1);
            ApplyFrame(frames[index]);
        }

        private void ApplyFrame(Sprite sprite)
        {
            if (_renderer != null && sprite != null)
            {
                _renderer.sprite = sprite;
            }
        }

        private void Despawn(bool scored)
        {
            if (_state == State.Done)
            {
                return;
            }

            _state = State.Done;
            AliveCount = Mathf.Max(0, AliveCount - 1);
            Destroy(gameObject);
        }

        public sealed class FlyingSaucerKillTracker : MonoBehaviour
        {
            private WaveDirector _director;
            private bool _notified;

            public void Bind(WaveDirector director)
            {
                _director = director;
            }

            public void NotifyScored()
            {
                if (_notified || _director == null)
                {
                    return;
                }

                _notified = true;
                _director.NotifyKill();
            }
        }
    }
}
