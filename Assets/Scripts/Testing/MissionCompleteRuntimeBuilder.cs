using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class MissionCompleteRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MissionCompleteController>() != null)
            {
                return;
            }

            var root = new GameObject("MissionComplete");
            root.AddComponent<MissionCompleteController>();
        }
    }
}
