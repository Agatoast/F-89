using F89.Flight;
using UnityEngine.SceneManagement;

namespace F89.Core
{
    public static class MissionProgressState
    {
        public static bool IsMissionInProgress()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == GameScenes.GroundAttack)
            {
                return true;
            }

            if (sceneName != GameScenes.FlightTest)
            {
                return false;
            }

            return !AircraftLandingController.IsLandingComplete;
        }
    }
}
