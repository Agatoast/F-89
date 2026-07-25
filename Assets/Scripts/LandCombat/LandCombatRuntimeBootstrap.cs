using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Builds the land combat scene. Called only from GroundAttackRuntimeBuilder.
    /// </summary>
    public static class LandCombatRuntimeBootstrap
    {
        public static void BootstrapScene()
        {
            Time.timeScale = 1f;
            LandCombatModule.EnterFromHandoff();
        }
    }
}
