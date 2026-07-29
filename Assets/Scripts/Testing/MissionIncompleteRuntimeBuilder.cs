using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class MissionIncompleteRuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<MissionIncompleteController>() != null)
            {
                return;
            }

            var root = new GameObject("MissionIncomplete");
            root.AddComponent<MissionIncompleteController>();
        }
    }
}
