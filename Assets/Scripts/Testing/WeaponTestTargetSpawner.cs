using F89.Weapons;
using UnityEngine;

namespace F89.Testing
{
    public static class WeaponTestTargetSpawner
    {
        private const string GridRootName = "TestGridTargets";
        private const string DecoyName = "TestDecoyAirTarget";

        public static void RemoveIfPresent()
        {
            var grid = GameObject.Find(GridRootName);
            if (grid != null)
            {
                Object.Destroy(grid);
            }

            var decoy = GameObject.Find(DecoyName);
            if (decoy != null)
            {
                Object.Destroy(decoy);
            }

            var targets = Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target == null)
                {
                    continue;
                }

                var name = target.gameObject.name;
                if (name.StartsWith("Grid-") || name == DecoyName)
                {
                    Object.Destroy(target.gameObject);
                }
            }
        }
    }
}
