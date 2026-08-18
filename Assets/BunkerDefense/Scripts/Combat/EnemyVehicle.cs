using System.Collections.Generic;
using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class EnemyVehicle : MonoBehaviour
    {
        private enum State
        {
            Advance,
            Fire
        }

        private static readonly List<EnemyVehicle> Live = new();

        private const float DepthOverlapBand = 0.14f;
        private const float VehicleWidthPadding = 1.08f;

        private static int _nextSeparationId;

        private SpriteRenderer _renderer;
        private CircleCollider2D _hitbox;
        private State _state = State.Advance;
        private UrArcticVehicleSpriteSheet.VehicleType _vehicleType;
        private float _speed;
        private int _hitPoints;
        private float _lane;
        private float _depth;
        private float _nextMissileTime;
        private bool _alive = true;
        private int _separationId;
        private Color _baseColor = Color.white;

        public bool IsAlive => _alive;
        public static int AliveCount { get; private set; }

        public static void ResetAliveCount()
        {
            AliveCount = 0;
            Live.Clear();
            _nextSeparationId = 0;
        }

        public static float ReserveSpawnLane(float preferredLane)
        {
            preferredLane = Mathf.Clamp(preferredLane, -1f, 1f);
            if (!LaneConflicts(preferredLane, 0f))
            {
                return preferredLane;
            }

            for (var step = 1; step <= 16; step++)
            {
                var offset = step * 0.12f;
                var left = Mathf.Clamp(preferredLane - offset, -1f, 1f);
                if (!LaneConflicts(left, 0f))
                {
                    return left;
                }

                var right = Mathf.Clamp(preferredLane + offset, -1f, 1f);
                if (!LaneConflicts(right, 0f))
                {
                    return right;
                }
            }

            return preferredLane;
        }

        public static EnemyVehicle Spawn(float lane, float depthSpeed, UrArcticVehicleSpriteSheet.VehicleType type)
        {
            UrArcticVehicleSpriteSheet.EnsureLoaded();

            var go = new GameObject($"URVehicle_{type}");
            go.tag = GameConstants.EnemyTag;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Enemies";

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.58f;
            collider.isTrigger = true;

            var vehicle = go.AddComponent<EnemyVehicle>();
            vehicle._renderer = renderer;
            vehicle._hitbox = collider;
            vehicle._speed = depthSpeed;
            vehicle._hitPoints = GameConstants.VehicleHitPoints;
            vehicle._vehicleType = type;
            vehicle._lane = ReserveSpawnLane(lane);
            vehicle._depth = 0f;
            vehicle._separationId = _nextSeparationId++;
            AliveCount++;
            Live.Add(vehicle);
            vehicle.ApplyPose(UrArcticVehicleSpriteSheet.View.Front);
            return vehicle;
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

        public void ForceKill()
        {
            if (!IsAlive)
            {
                return;
            }

            Die();
        }

        private void Update()
        {
            if (!_alive)
            {
                return;
            }

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
                case State.Advance:
                    Advance();
                    break;
                case State.Fire:
                    Fire();
                    break;
            }
        }

        private void Advance()
        {
            _depth = Mathf.MoveTowards(_depth, BattlefieldLayout.VehicleStopDepth, _speed * Time.deltaTime);
            ApplyPose(UrArcticVehicleSpriteSheet.View.Front);

            if (_depth >= BattlefieldLayout.VehicleStopDepth - 0.01f)
            {
                _depth = BattlefieldLayout.VehicleStopDepth;
                _state = State.Fire;
                _nextMissileTime = Time.time + Random.Range(8f, 10f);
            }
        }

        private void Fire()
        {
            ApplyPose(ResolveFireView());

            if (Time.time < _nextMissileTime)
            {
                return;
            }

            VehicleMissile.Launch(
                (Vector2)transform.position + new Vector2(0f, 0.18f),
                BattlefieldLayout.HangarImpact,
                GameConstants.VehicleMissileHangarDamage);
            _nextMissileTime = Time.time + Random.Range(8f, 10f);
        }

        private UrArcticVehicleSpriteSheet.View ResolveFireView()
        {
            if (Mathf.Abs(_lane) > 0.55f)
            {
                return UrArcticVehicleSpriteSheet.View.Side;
            }

            return UrArcticVehicleSpriteSheet.View.Oblique;
        }

        private void ApplyPose(UrArcticVehicleSpriteSheet.View view)
        {
            SeparateFromOtherVehicles();
            transform.position = BattlefieldLayout.FieldToWorld(_lane, _depth);
            var scale = BattlefieldLayout.ScaleForVehicleDepth(_depth);
            transform.localScale = Vector3.one * scale;

            if (_renderer != null)
            {
                _renderer.sprite = UrArcticVehicleSpriteSheet.GetSprite(_vehicleType, view);
                _renderer.color = _baseColor;
                _renderer.sortingOrder = Mathf.RoundToInt(_depth * 40f) + 2;
                _renderer.flipX = view == UrArcticVehicleSpriteSheet.View.Side && _lane > 0f;
            }

            if (_hitbox != null)
            {
                _hitbox.radius = 0.58f;
            }
        }

        private void Die()
        {
            if (!_alive)
            {
                return;
            }

            _alive = false;
            AliveCount = Mathf.Max(0, AliveCount - 1);
            Live.Remove(this);
            if (_hitbox != null)
            {
                _hitbox.enabled = false;
            }

            var position = transform.position;
            var scale = transform.localScale.x * 1.1f;
            var sortOrder = _renderer != null ? _renderer.sortingOrder : 30;
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }

            FxService.SpawnExplosion(position, scale, sortOrder);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            Live.Remove(this);
            if (_alive)
            {
                AliveCount = Mathf.Max(0, AliveCount - 1);
            }
        }

        private void SeparateFromOtherVehicles()
        {
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < Live.Count; i++)
                {
                    var other = Live[i];
                    if (other == null || other == this || !other._alive)
                    {
                        continue;
                    }

                    if (Mathf.Abs(other._depth - _depth) > DepthOverlapBand)
                    {
                        continue;
                    }

                    var minLane = RequiredLaneSeparation(_depth) * 0.5f
                        + RequiredLaneSeparation(other._depth) * 0.5f;
                    var delta = _lane - other._lane;
                    if (Mathf.Abs(delta) >= minLane)
                    {
                        continue;
                    }

                    var push = (minLane - Mathf.Abs(delta)) * 0.5f;
                    var sign = Mathf.Abs(delta) < 0.0001f
                        ? (_separationId > other._separationId ? 1f : -1f)
                        : Mathf.Sign(delta);
                    _lane = Mathf.Clamp(_lane + sign * push, -1f, 1f);
                }
            }
        }

        private static bool LaneConflicts(float lane, float depth)
        {
            for (var i = 0; i < Live.Count; i++)
            {
                var other = Live[i];
                if (other == null || !other._alive)
                {
                    continue;
                }

                if (Mathf.Abs(other._depth - depth) > DepthOverlapBand)
                {
                    continue;
                }

                var minLane = RequiredLaneSeparation(depth) * 0.5f
                    + RequiredLaneSeparation(other._depth) * 0.5f;
                if (Mathf.Abs(other._lane - lane) < minLane)
                {
                    return true;
                }
            }

            return false;
        }

        private static float RequiredLaneSeparation(float depth)
        {
            var opening = BattlefieldLayout.OpeningWorld;
            if (opening.width < 0.01f)
            {
                return 0.34f;
            }

            var inset = Mathf.Max(0.08f, opening.width * BattlefieldLayout.SpawnHorizontalInsetFraction);
            var halfSpan = Mathf.Max(0.12f, opening.width * 0.5f - inset);
            var scale = BattlefieldLayout.ScaleForVehicleDepth(depth);
            var worldWidth = 0.58f * 2f * scale * VehicleWidthPadding;
            return worldWidth / halfSpan;
        }
    }
}
