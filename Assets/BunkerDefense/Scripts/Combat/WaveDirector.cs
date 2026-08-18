using System.Collections;
using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class WaveDirector : MonoBehaviour
    {
        private struct WaveSpec
        {
            public int Count;
            public float Interval;
            public float Speed;
            public float RusherChance;
            public float VehicleChance;
        }

        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }
        public int Kills { get; private set; }
        public bool IsSpawning { get; private set; }
        public bool WavesComplete { get; private set; }

        private WaveSpec[] _waves;
        private int _spawnedThisWave;

        public static WaveDirector Create(int mapNumber)
        {
            var go = new GameObject("WaveDirector");
            var director = go.AddComponent<WaveDirector>();
            director._waves = BuildWaves(mapNumber);
            director.TotalWaves = director._waves.Length;
            return director;
        }

        public void NotifyKill()
        {
            Kills++;
        }

        public void Begin()
        {
            StartCoroutine(RunWaves());
        }

        public bool FieldIsClear()
        {
            return !IsSpawning
                && EnemySoldier.AliveCount <= 0
                && EnemyVehicle.AliveCount <= 0
                && FlyingSaucer.AliveCount <= 0
                && FindObjectsByType<Grenade>(FindObjectsSortMode.None).Length == 0
                && FindObjectsByType<VehicleMissile>(FindObjectsSortMode.None).Length == 0;
        }

        private IEnumerator RunWaves()
        {
            for (var i = 0; i < _waves.Length; i++)
            {
                CurrentWave = i + 1;
                _spawnedThisWave = 0;
                IsSpawning = true;
                var spec = _waves[i];
                while (_spawnedThisWave < spec.Count)
                {
                    SpawnOne(spec);
                    _spawnedThisWave++;
                    yield return new WaitForSeconds(spec.Interval);
                }

                IsSpawning = false;
                while (EnemySoldier.AliveCount > 0 || EnemyVehicle.AliveCount > 0)
                {
                    yield return null;
                }

                if (i < _waves.Length - 1)
                {
                    yield return new WaitForSeconds(1.4f);
                }
            }

            WavesComplete = true;
        }

        private void SpawnOne(WaveSpec spec)
        {
            var lane = BattlefieldLayout.RandomLane();
            if (Random.value < spec.VehicleChance)
            {
                var vehicle = EnemyVehicle.Spawn(
                    lane,
                    spec.Speed * 0.038f,
                    UrArcticVehicleSpriteSheet.RandomType());
                vehicle.gameObject.AddComponent<EnemyKillTracker>().Bind(this);
                return;
            }

            var rusher = Random.value < spec.RusherChance;
            var hp = rusher ? GameConstants.RusherHitPoints : GameConstants.SoldierHitPoints;
            var depthSpeed = spec.Speed * 0.055f * (rusher ? 1.35f : 1f);
            var soldier = EnemySoldier.Spawn(lane, depthSpeed, hp, rusher);
            soldier.gameObject.AddComponent<EnemyKillTracker>().Bind(this);
        }

        private static WaveSpec[] BuildWaves(int mapNumber)
        {
            var t = Mathf.InverseLerp(1f, DefenseSiteCatalog.MissionCount, mapNumber);
            return new[]
            {
                new WaveSpec
                {
                    Count = BattlefieldBackgroundCatalog.ScaleSpawnCount(5, mapNumber),
                    Interval = Mathf.Lerp(0.85f, 0.55f, t),
                    Speed = Mathf.Lerp(1.7f, 2.2f, t),
                    RusherChance = Mathf.Lerp(0f, 0.15f, t),
                    VehicleChance = Mathf.Lerp(0.08f, 0.14f, t)
                },
                new WaveSpec
                {
                    Count = BattlefieldBackgroundCatalog.ScaleSpawnCount(8, mapNumber),
                    Interval = Mathf.Lerp(0.7f, 0.42f, t),
                    Speed = Mathf.Lerp(1.9f, 2.45f, t),
                    RusherChance = Mathf.Lerp(0.1f, 0.25f, t),
                    VehicleChance = Mathf.Lerp(0.14f, 0.22f, t)
                },
                new WaveSpec
                {
                    Count = BattlefieldBackgroundCatalog.ScaleSpawnCount(10, mapNumber),
                    Interval = Mathf.Lerp(0.6f, 0.32f, t),
                    Speed = Mathf.Lerp(2.05f, 2.7f, t),
                    RusherChance = Mathf.Lerp(0.15f, 0.35f, t),
                    VehicleChance = Mathf.Lerp(0.18f, 0.28f, t)
                }
            };
        }

        private sealed class EnemyKillTracker : MonoBehaviour
        {
            private WaveDirector _director;
            private bool _wasAlive = true;

            public void Bind(WaveDirector director)
            {
                _director = director;
            }

            private void Update()
            {
                if (_director == null || !_wasAlive)
                {
                    return;
                }

                var alive = false;
                var soldier = GetComponent<EnemySoldier>();
                if (soldier != null)
                {
                    alive = soldier.IsAlive;
                }
                else
                {
                    var vehicle = GetComponent<EnemyVehicle>();
                    if (vehicle != null)
                    {
                        alive = vehicle.IsAlive;
                    }
                }

                if (!alive)
                {
                    _wasAlive = false;
                    _director.NotifyKill();
                }
            }
        }
    }
}
