using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.LandCombat
{
    /// <summary>Holds which bunker layout to build when the Bunker scene loads.</summary>
    public static class LandBunkerHandoffState
    {
        public static bool HasPendingLayout { get; private set; }
        public static int PendingLayoutIndex { get; private set; }
        /// <summary>After Boss1 dialogue: bunker starts a countdown, then the boss engages.</summary>
        public static bool PendingBossFight { get; private set; }

        public static void BeginEnter(int layoutIndex)
        {
            PendingLayoutIndex = layoutIndex;
            HasPendingLayout = true;
        }

        public static void BeginBossFightEnter(int layoutIndex = 1)
        {
            BeginEnter(layoutIndex);
            PendingBossFight = true;
        }

        /// <summary>Keep the pending layout; ensure the bunker will run the boss intro.</summary>
        public static void EnsureBossFightPending()
        {
            PendingBossFight = true;
        }

        public static int ConsumeLayoutIndex(int layoutCount)
        {
            var count = Mathf.Max(1, layoutCount);
            var index = HasPendingLayout
                ? Mathf.Clamp(PendingLayoutIndex, 0, count - 1)
                : Random.Range(0, count);
            HasPendingLayout = false;
            return index;
        }

        public static bool ConsumeBossFightPending()
        {
            var pending = PendingBossFight;
            PendingBossFight = false;
            return pending;
        }

        public static void Clear()
        {
            HasPendingLayout = false;
            PendingLayoutIndex = 0;
            PendingBossFight = false;
        }
    }

    /// <summary>Black pad on the ice. Stepping on it loads a random bunker map.</summary>
    public sealed class LandBunkerEntrance : MonoBehaviour
    {
        public const int LayoutCount = 3;

        private bool transitioning;

        public Vector2 WorldPosition => transform.position;

        public static LandBunkerEntrance Spawn(Vector2 worldPosition)
        {
            var go = new GameObject("BunkerEntrance");
            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, 0.5f);
            var entrance = go.AddComponent<LandBunkerEntrance>();
            entrance.BuildVisual();
            return entrance;
        }

        private void BuildVisual()
        {
            var size = LandGameConstants.BunkerEntranceWorldSize;
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LandPlaceholderArt.GetPixelSquare();
            renderer.color = Color.black;
            renderer.sortingOrder = -50;
            // 1×1 sprite at PPU 1 → scale to world size.
            transform.localScale = new Vector3(size, size, 1f);

            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var hitbox = gameObject.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.size = Vector2.one;

            var shelter = gameObject.AddComponent<LandShelterZone>();
            shelter.Configure(LandShelterZone.ShelterKind.Bunker, new Vector2(size, size));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (transitioning || other.GetComponentInParent<LandPlayerController>() == null)
            {
                return;
            }

            if (HasLivingSoldiers())
            {
                LandHudNotice.Show("Enemy Soldiers are still guarding the bunker.");
                return;
            }

            EnterBunker();
        }

        private static bool HasLivingSoldiers()
        {
            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnterBunker()
        {
            if (transitioning)
            {
                return;
            }

            transitioning = true;
            var layout = Random.Range(0, LayoutCount);
            Time.timeScale = 1f;
            LandSurfaceSession.CaptureFromWorld();

            // First bunker visit before Boss 1 is defeated → dialogue page, then fight.
            if (!LandBossEncounter.Boss1Defeated)
            {
                LandBunkerHandoffState.BeginBossFightEnter(layout);
                Debug.Log($"F-89 Land: Boss 1 briefing before bunker layout {layout + 1}/{LayoutCount}.");
                SceneManager.LoadScene(GameScenes.Boss1);
                return;
            }

            LandBunkerHandoffState.BeginEnter(layout);
            Debug.Log($"F-89 Land: Entering bunker layout {layout + 1}/{LayoutCount}.");
            SceneManager.LoadScene(GameScenes.Bunker);
        }
    }
}
