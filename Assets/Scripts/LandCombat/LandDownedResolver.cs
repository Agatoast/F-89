using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Resolves unconsciousness outcome when HP reaches 0.
    /// Weights: 50% Death, 49% POW, 1% Escape → DeathScreen / POWScreen / EscapedScreen.
    /// </summary>
    public static class LandDownedResolver
    {
        public const int DeathWeightPercent = 50;
        public const int PrisonerOfWarWeightPercent = 49;
        public const int EscapeWeightPercent = 1;

        public static LandDownedOutcome RollOutcome()
        {
            // 0–99 inclusive: 0–49 Death, 50–98 POW, 99 Escape.
            var roll = Random.Range(0, 100);
            if (roll < DeathWeightPercent)
            {
                return LandDownedOutcome.Death;
            }

            if (roll < DeathWeightPercent + PrisonerOfWarWeightPercent)
            {
                return LandDownedOutcome.PrisonerOfWar;
            }

            return LandDownedOutcome.Escaped;
        }

        public static string GetPendingScreenName(LandDownedOutcome outcome)
        {
            return outcome switch
            {
                LandDownedOutcome.Death => GameScenes.DeathScreen,
                LandDownedOutcome.FrozenToDeath => GameScenes.FrozenDeath,
                LandDownedOutcome.PrisonerOfWar => GameScenes.POWScreen,
                LandDownedOutcome.Escaped => GameScenes.EscapedScreen,
                _ => string.Empty
            };
        }
    }
}
