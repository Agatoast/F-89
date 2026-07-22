using F89.Controls;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class FlareCountermeasureController : MonoBehaviour
    {
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private PlayerAircraftInput inputSource;
        [SerializeField] private FlareLoadoutConfig loadoutConfig;
        [SerializeField] private float deployCooldownSeconds = 0.35f;

        private float cooldownRemaining;
        private int flaresRemaining;

        public int FlaresRemaining => flaresRemaining;
        public int FlaresCapacity => loadoutConfig != null ? loadoutConfig.TotalFlares : 0;
        public bool HasFlaresRemaining => flaresRemaining > 0;

        public void Configure(
            AircraftController aircraftController,
            PlayerAircraftInput input,
            FlareLoadoutConfig loadout = null)
        {
            aircraft = aircraftController;
            inputSource = input;
            ApplyLoadout(loadout);
        }

        /// <summary>Called by the future loadout screen to set pod count and refill for a sortie.</summary>
        public void ApplyLoadout(FlareLoadoutConfig loadout)
        {
            loadoutConfig = loadout != null
                ? loadout
                : ScriptableObject.CreateInstance<FlareLoadoutConfig>();
            flaresRemaining = FlaresCapacity;
        }

        private void Update()
        {
            if (aircraft == null || inputSource == null)
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }

            if (cooldownRemaining > 0f || !inputSource.Current.flarePressed)
            {
                return;
            }

            if (!TryDeployFlare())
            {
                return;
            }

            cooldownRemaining = deployCooldownSeconds;
        }

        private bool TryDeployFlare()
        {
            if (flaresRemaining <= 0)
            {
                return false;
            }

            var profile = aircraft.Profile;
            var worldMap = aircraft.WorldMap;
            if (profile == null || worldMap == null)
            {
                return false;
            }

            var ticSize = profile.ticSizeWorldUnits;
            var planeLength = ticSize * AircraftVisualFactory.VisualSizeMultiplier;
            var planeAspect = 1.67f;
            var planeTexture = AircraftVisualFactory.LoadTexture();
            if (planeTexture != null)
            {
                planeAspect = (float)planeTexture.width / planeTexture.height;
            }

            var planeWidth = ticSize * planeAspect * AircraftVisualFactory.VisualSizeMultiplier;
            var forward = aircraft.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            var spawnPosition = aircraft.transform.position
                - forward * (planeLength * 0.35f)
                + right * Random.Range(-planeLength * 0.08f, planeLength * 0.08f);
            spawnPosition.y = aircraft.transform.position.y;

            CountermeasureFlare.Deploy(spawnPosition, planeWidth, planeLength);

            flaresRemaining--;
            Debug.Log($"F-89: Flare deployed. Remaining: {flaresRemaining}/{FlaresCapacity}.");
            return true;
        }
    }
}
