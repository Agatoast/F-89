using UnityEngine;

namespace F89.Flight
{
    public enum CrashLandingOutcome
    {
        None = 0,
        NotRescued = 1,
        Rescued = 2,
        Wounded = 3
    }

    /// <summary>
    /// Resolves crash landing rescue outcome.
    /// 90% rescued overall; of rescued, 80% uninjured rescue and 20% wounded (Purple Heart).
    /// </summary>
    public static class CrashLandingResolver
    {
        public const int RescueWeightPercent = 90;
        public const int WoundedAmongRescuedPercent = 20;

        public static CrashLandingOutcome RollOutcome()
        {
            if (Random.Range(0, 100) >= RescueWeightPercent)
            {
                return CrashLandingOutcome.NotRescued;
            }

            return Random.Range(0, 100) < WoundedAmongRescuedPercent
                ? CrashLandingOutcome.Wounded
                : CrashLandingOutcome.Rescued;
        }
    }
}
