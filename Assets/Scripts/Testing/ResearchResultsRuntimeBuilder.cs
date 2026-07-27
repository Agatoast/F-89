using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class ResearchResultsRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<ResearchResultsController>() != null)
            {
                return;
            }

            var root = new GameObject("ResearchResults");
            root.AddComponent<ResearchResultsController>();
        }
    }
}
