using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class DownedOutcomeRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<DownedOutcomeController>() != null)
            {
                return;
            }

            var root = new GameObject("DownedOutcome");
            root.AddComponent<DownedOutcomeController>();
        }
    }
}
