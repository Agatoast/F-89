using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SaveAntarctica.BunkerDefense.UI
{
    /// <summary>
    /// Ensures uGUI clicks work in Input-System-only player builds.
    /// </summary>
    public static class BunkerDefenseEventSystem
    {
        public static void Ensure()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    Object.Destroy(legacy);
                }

                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }

            eventSystem.sendNavigationEvents = true;
        }
    }
}
