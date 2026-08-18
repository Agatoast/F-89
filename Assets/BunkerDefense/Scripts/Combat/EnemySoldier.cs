using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class EnemySoldier : MonoBehaviour
    {
        private enum State
        {
            Charge,
            Throwing,
            Retreating,
            Dead
        }

        private SpriteRenderer _renderer;
        private CircleCollider2D _hitbox;
        private State _state = State.Charge;
        private float _speed;
        private int _hitPoints;
        private bool _isRusher;
        private float _lane;
        private float _depth;
        private float _animTime;
        private float _throwTime;
        private bool _grenadeReleased;
        private float _corpseAge;
        private Color _baseColor = Color.white;

        public bool IsAlive => _state != State.Dead;
        public static int AliveCount { get; private set; }

        public void ForceKillImmediate()
        {
            if (!IsAlive)
            {
                return;
            }

            Die();
            Destroy(gameObject);
        }

        public static void ResetAliveCount()
        {
            AliveCount = 0;
        }

        public static EnemySoldier Spawn(float lane, float depthSpeed, int hitPoints, bool rusher)
        {
            var go = new GameObject(rusher ? "URRusher" : "URSoldier");
            go.tag = GameConstants.EnemyTag;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Enemies";

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.38f;
            collider.isTrigger = true;

            var soldier = go.AddComponent<EnemySoldier>();
            soldier._renderer = renderer;
            soldier._hitbox = collider;
            soldier._speed = depthSpeed;
            soldier._hitPoints = hitPoints;
            soldier._isRusher = rusher;
            soldier._lane = lane;
            soldier._depth = 0f;
            AliveCount++;
            soldier.ApplyPose();
            soldier.ApplyFrame(UrArcticSoldierSpriteSheet.Clip.Run, 0);
            return soldier;
        }

        public bool TryHit(int damage)
        {
            if (!IsAlive)
            {
                return false;
            }

            _hitPoints -= damage;
            _baseColor = new Color(1f, 0.72f, 0.72f, 1f);
            if (_hitPoints <= 0)
            {
                Die();
                return true;
            }

            return true;
        }

        private void Update()
        {
            if (_baseColor != Color.white)
            {
                _baseColor = Color.Lerp(_baseColor, Color.white, Time.deltaTime * 8f);
                if (_renderer != null)
                {
                    _renderer.color = _baseColor;
                }
            }

            switch (_state)
            {
                case State.Charge:
                    Charge();
                    break;
                case State.Throwing:
                    Throw();
                    break;
                case State.Retreating:
                    Retreat();
                    break;
                case State.Dead:
                    LingerCorpse();
                    break;
            }
        }

        private void Charge()
        {
            _depth = Mathf.MoveTowards(_depth, 1f, _speed * Time.deltaTime);
            _lane += Mathf.Sin(Time.time * 2.1f + _lane * 4f) * 0.08f * Time.deltaTime;
            _lane = Mathf.Clamp(_lane, -1f, 1f);
            ApplyPose();
            PlayLoop(UrArcticSoldierSpriteSheet.Clip.Run, 12f);

            if (_depth >= BattlefieldLayout.ThrowDepth)
            {
                _state = State.Throwing;
                _throwTime = 0f;
                _animTime = 0f;
                _grenadeReleased = false;
            }
        }

        private void Throw()
        {
            _throwTime += Time.deltaTime;
            ApplyPose();
            PlayOnce(UrArcticSoldierSpriteSheet.Clip.Grenade, 10f);

            if (!_grenadeReleased && _throwTime >= 0.48f)
            {
                _grenadeReleased = true;
                Grenade.Throw((Vector2)transform.position + new Vector2(0f, 0.25f), BattlefieldLayout.HangarImpact, GameConstants.GrenadeHangarDamage);
            }

            if (_throwTime >= 0.82f)
            {
                _state = State.Retreating;
                _animTime = 0f;
            }
        }

        private void Retreat()
        {
            _depth = Mathf.MoveTowards(_depth, 0f, _speed * 1.2f * Time.deltaTime);
            ApplyPose();
            if (_renderer != null)
            {
                _renderer.flipX = true;
            }

            PlayLoop(UrArcticSoldierSpriteSheet.Clip.Run, 12f);
            if (_depth <= 0.02f)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyPose()
        {
            transform.position = BattlefieldLayout.FieldToWorld(_lane, _depth);
            var scale = BattlefieldLayout.ScaleForDepth(_depth, _isRusher);
            transform.localScale = Vector3.one * scale;
            if (_renderer != null)
            {
                _renderer.sortingOrder = Mathf.RoundToInt(_depth * 40f);
                _renderer.flipX = _lane > 0f;
            }

            if (_hitbox != null)
            {
                _hitbox.radius = 0.38f;
            }
        }

        private void Die()
        {
            if (_state == State.Dead)
            {
                return;
            }

            _state = State.Dead;
            AliveCount = Mathf.Max(0, AliveCount - 1);
            _animTime = 0f;
            _corpseAge = 0f;
            if (_hitbox != null)
            {
                _hitbox.enabled = false;
            }
        }

        private void LingerCorpse()
        {
            PlayOnce(UrArcticSoldierSpriteSheet.Clip.Dead, 8f);
            _corpseAge += Time.deltaTime;
            if (_corpseAge > 2.4f)
            {
                var color = _renderer.color;
                color.a = Mathf.MoveTowards(color.a, 0f, Time.deltaTime);
                _renderer.color = color;
                if (color.a <= 0.02f)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void PlayLoop(UrArcticSoldierSpriteSheet.Clip clip, float fps)
        {
            _animTime += Time.deltaTime;
            var frames = UrArcticSoldierSpriteSheet.GetClip(clip, ResolveCamo());
            if (frames.Length == 0)
            {
                return;
            }

            ApplyFrame(clip, Mathf.FloorToInt(_animTime * fps) % frames.Length);
        }

        private void PlayOnce(UrArcticSoldierSpriteSheet.Clip clip, float fps)
        {
            _animTime += Time.deltaTime;
            var frames = UrArcticSoldierSpriteSheet.GetClip(clip, ResolveCamo());
            if (frames.Length == 0)
            {
                return;
            }

            ApplyFrame(clip, Mathf.Min(frames.Length - 1, Mathf.FloorToInt(_animTime * fps)));
        }

        private void ApplyFrame(UrArcticSoldierSpriteSheet.Clip clip, int index)
        {
            var frames = UrArcticSoldierSpriteSheet.GetClip(clip, ResolveCamo());
            if (frames.Length == 0 || _renderer == null)
            {
                return;
            }

            _renderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            _renderer.color = _baseColor;
        }

        private UrArcticSoldierSpriteSheet.Camouflage ResolveCamo()
        {
            return UrArcticSoldierSpriteSheet.Camouflage.Black;
        }

        private void OnDestroy()
        {
            if (_state != State.Dead)
            {
                AliveCount = Mathf.Max(0, AliveCount - 1);
            }
        }
    }
}
