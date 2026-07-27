using F89.UI;
using UnityEngine;

namespace F89.Testing
{
    public static class Boss1RuntimeBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<Boss1Controller>() != null)
            {
                return;
            }

            var root = new GameObject("Boss1");
            root.AddComponent<Boss1Controller>();
        }
    }
}
