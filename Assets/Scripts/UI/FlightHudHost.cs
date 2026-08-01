using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Runs flight HUD wiring after the player and weapons finish Awake/Start.
    /// AfterSceneLoad bootstrap alone leaves many IMGUI widgets unconfigured in builds.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public sealed class FlightHudHost : MonoBehaviour
    {
        public static void EnsureOn(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<FlightHudHost>() == null)
            {
                player.AddComponent<FlightHudHost>();
            }
        }

        private void Start()
        {
            FlightHudBootstrap.EnsureForPlayer(gameObject);
        }
    }
}
