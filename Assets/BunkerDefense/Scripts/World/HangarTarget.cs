using UnityEngine;

namespace SaveAntarctica.BunkerDefense.World
{
    public sealed class HangarTarget : MonoBehaviour
    {
        public static HangarTarget Instance { get; private set; }

        public int MaxHitPoints { get; private set; }
        public int CurrentHitPoints { get; private set; }
        public bool IsDestroyed => CurrentHitPoints <= 0;

        public static HangarTarget Create(int hitPoints)
        {
            var go = new GameObject("Hangar");
            go.transform.position = BattlefieldLayout.HangarImpact;
            var target = go.AddComponent<HangarTarget>();
            target.MaxHitPoints = hitPoints;
            target.CurrentHitPoints = hitPoints;
            Instance = target;
            return target;
        }

        public void ApplyDamage(int amount)
        {
            if (IsDestroyed)
            {
                return;
            }

            CurrentHitPoints = Mathf.Max(0, CurrentHitPoints - Mathf.Max(0, amount));
        }
    }
}
