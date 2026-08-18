using System.Collections;
using SaveAntarctica.BunkerDefense.Core;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public sealed class FlyingSaucerSpawner : MonoBehaviour
    {
        private WaveDirector _waves;
        private int _remaining;

        public static FlyingSaucerSpawner Create(int mapNumber, WaveDirector waves)
        {
            var go = new GameObject("FlyingSaucerSpawner");
            var spawner = go.AddComponent<FlyingSaucerSpawner>();
            spawner._waves = waves;
            spawner._remaining = GetQuotaForMap(mapNumber);
            return spawner;
        }

        public static int GetQuotaForMap(int mapNumber)
        {
            mapNumber = Mathf.Clamp(mapNumber, 1, DefenseSiteCatalog.MissionCount);
            if (mapNumber <= 5)
            {
                return 1;
            }

            if (mapNumber <= 10)
            {
                return 2;
            }

            if (mapNumber <= 15)
            {
                return 3;
            }

            return 4;
        }

        public void Begin()
        {
            if (_remaining <= 0)
            {
                return;
            }

            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            var delay = Mathf.Lerp(10f, 6f, _remaining / 4f);
            yield return new WaitForSeconds(delay * 0.65f);

            while (_remaining > 0)
            {
                SpawnOne();
                _remaining--;
                if (_remaining <= 0)
                {
                    yield break;
                }

                yield return new WaitForSeconds(Random.Range(delay * 0.85f, delay * 1.35f));
            }
        }

        private void SpawnOne()
        {
            var saucer = FlyingSaucer.Spawn();
            if (saucer == null || _waves == null)
            {
                return;
            }

            var tracker = saucer.gameObject.AddComponent<FlyingSaucer.FlyingSaucerKillTracker>();
            tracker.Bind(_waves);
        }
    }
}
