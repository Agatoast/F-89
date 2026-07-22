using UnityEngine;

namespace F89.Weapons
{
    [CreateAssetMenu(fileName = "FlareLoadoutConfig", menuName = "F-89/Loadout/Flare Loadout Config")]
    public class FlareLoadoutConfig : ScriptableObject
    {
        [Header("Basic Complement")]
        [Tooltip("Standard onboard flare count before optional pods.")]
        public int baseComplementFlares = 48;

        [Header("Optional Pods (loadout screen)")]
        [Tooltip("Extra flares supplied by each equipped flare pod.")]
        public int flaresPerPod = 24;

        [Tooltip("Number of flare pods selected at loadout. Set by loadout UI later.")]
        public int equippedPodCount;

        public int TotalFlares => Mathf.Max(0, baseComplementFlares + equippedPodCount * flaresPerPod);
    }
}
