using UnityEngine;

namespace F89.Core
{
    [CreateAssetMenu(fileName = "AntarcticaMissionConfig", menuName = "F-89/Antarctica Mission Config")]
    public class AntarcticaMissionConfig : ScriptableObject
    {
        [Header("Home Base")]
        public string carrierName = AntarcticaWorldLocations.CarrierName;

        [Header("Mission 1")]
        public string missionTitle = "Mission 1: Secure Foothold";
        public string firstObjectiveBaseName = "Palmer Station";
        [TextArea(2, 4)]
        public string firstObjectiveBrief =
            "Launch from USS Martin Van Buren and capture the hostile base on the peninsula.";

        public static AntarcticaMissionConfig LoadOrDefault()
        {
            var config = Resources.Load<AntarcticaMissionConfig>("F89_AntarcticaMissionConfig");
            if (config != null)
            {
                return config;
            }

            config = CreateInstance<AntarcticaMissionConfig>();
            Debug.LogWarning("F-89: Using runtime Antarctica mission defaults.");
            return config;
        }
    }
}
