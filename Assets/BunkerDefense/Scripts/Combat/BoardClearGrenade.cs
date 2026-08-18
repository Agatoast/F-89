using SaveAntarctica.BunkerDefense.Core;
using SaveAntarctica.BunkerDefense.World;
using UnityEngine;

namespace SaveAntarctica.BunkerDefense.Combat
{
    public static class BoardClearGrenade
    {
        public const int MaxChargesPerMatch = 3;

        public static int MaxCharges { get; private set; }

        public static int Remaining { get; private set; }

        public static int GetMaxChargesForMap(int mapNumber)
        {
            return MaxChargesPerMatch;
        }

        public static void ResetForMatch(int mapNumber)
        {
            MaxCharges = GetMaxChargesForMap(mapNumber);
            Remaining = MaxCharges;
        }

        public static bool CanUse()
        {
            return Remaining > 0
                && MatchController.Instance != null
                && MatchController.Instance.IsFighting;
        }

        public static bool TryUse()
        {
            if (!CanUse())
            {
                return false;
            }

            Remaining--;
            var opening = BattlefieldLayout.OpeningWorld;
            var blastCenter = opening.width > 0.01f
                ? (Vector2)opening.center
                : Vector2.zero;
            FxService.SpawnExplosion(blastCenter, 2.2f);
            ScreenShake.Play(0.32f, 0.48f);
            CombatAudio.PlayRocketExplosion();

            var waves = MatchController.Instance.Waves;
            var soldiers = Object.FindObjectsByType<EnemySoldier>(FindObjectsSortMode.None);
            for (var i = 0; i < soldiers.Length; i++)
            {
                var soldier = soldiers[i];
                if (soldier == null || !soldier.IsAlive)
                {
                    continue;
                }

                soldier.ForceKillImmediate();
                waves?.NotifyKill();
            }

            var vehicles = Object.FindObjectsByType<EnemyVehicle>(FindObjectsSortMode.None);
            for (var i = 0; i < vehicles.Length; i++)
            {
                var vehicle = vehicles[i];
                if (vehicle == null || !vehicle.IsAlive)
                {
                    continue;
                }

                vehicle.ForceKill();
                waves?.NotifyKill();
            }

            return true;
        }
    }
}
