using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class CrashLandingOutcomeRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<CrashLandingOutcomeController>() != null)
            {
                return;
            }

            var root = new GameObject("CrashLandingOutcome");
            root.AddComponent<CrashLandingOutcomeController>();
        }
    }
}
