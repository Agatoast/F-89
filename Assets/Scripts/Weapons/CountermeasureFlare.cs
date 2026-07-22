using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public class CountermeasureFlare : MonoBehaviour
    {
        public const float EfficacyDurationSeconds = 10f;
        public const float VisualFadeDurationSeconds = 1.75f;

        private LockableTarget lockableTarget;
        private FlareBurnVisual burnVisual;
        private float efficacyRemaining;
        private float visualFadeRemaining;
        private bool inVisualFade;
        private bool expired;

        public LockableTarget Target => lockableTarget;
        public bool IsBurning => efficacyRemaining > 0f && lockableTarget != null && lockableTarget.IsAlive;

        public static CountermeasureFlare Deploy(
            Vector3 spawnPosition,
            float planeWidthWorld,
            float planeLengthWorld)
        {
            var flareObject = new GameObject("CountermeasureFlare");
            spawnPosition.y = Mathf.Max(spawnPosition.y, 0.02f);
            flareObject.transform.position = spawnPosition;

            var flare = flareObject.AddComponent<CountermeasureFlare>();
            flare.Initialize(planeWidthWorld, planeLengthWorld);
            return flare;
        }

        private void Initialize(float planeWidthWorld, float planeLengthWorld)
        {
            efficacyRemaining = EfficacyDurationSeconds;

            lockableTarget = gameObject.AddComponent<LockableTarget>();
            lockableTarget.Configure(
                "FLARE",
                LockableTargetKind.Air,
                TargetAffiliation.Friendly,
                TargetUnitClass.FlareDecoy);

            burnVisual = gameObject.AddComponent<FlareBurnVisual>();
            burnVisual.Configure(planeWidthWorld, planeLengthWorld);
        }

        private void Update()
        {
            if (inVisualFade)
            {
                visualFadeRemaining -= Time.deltaTime;
                burnVisual.SetBurnIntensity(Mathf.Clamp01(visualFadeRemaining / VisualFadeDurationSeconds));

                if (visualFadeRemaining <= 0f && !expired)
                {
                    expired = true;
                    Destroy(gameObject);
                }

                return;
            }

            efficacyRemaining -= Time.deltaTime;
            burnVisual.SetBurnIntensity(1f);

            if (efficacyRemaining <= 0f && !expired)
            {
                BeginVisualFade();
            }
        }

        private void BeginVisualFade()
        {
            inVisualFade = true;
            lockableTarget?.ExpireWithoutHit();
            visualFadeRemaining = VisualFadeDurationSeconds;
        }

        public void DetonateFromMissile(string weaponName)
        {
            if (expired)
            {
                return;
            }

            Debug.Log($"F-89: {weaponName} detonated on flare decoy.");
            DestroyFlare();
        }

        private void DestroyFlare()
        {
            expired = true;
            lockableTarget?.ExpireWithoutHit();
            Destroy(gameObject);
        }
    }
}
