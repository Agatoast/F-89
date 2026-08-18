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
        /// <summary>After a boss dialogue page: bunker starts a countdown, then this boss engages.</summary>
        public static int PendingBossNumber { get; private set; }

        public static void BeginEnter(int layoutIndex)
        {
            PendingLayoutIndex = layoutIndex;
            HasPendingLayout = true;
        }

        public static void BeginBossFightEnter(int layoutIndex, int bossNumber)
        {
            BeginEnter(layoutIndex);
            PendingBossNumber = bossNumber;
        }

        /// <summary>Keep the pending layout; ensure the bunker will run the boss intro.</summary>
        public static void EnsureBossFightPending(int bossNumber)
        {
            PendingBossNumber = bossNumber;
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

        public static int ConsumeBossFightPending()
        {
            var pending = PendingBossNumber;
            PendingBossNumber = 0;
            return pending;
        }

        public static void Clear()
        {
            HasPendingLayout = false;
            PendingLayoutIndex = 0;
            PendingBossNumber = 0;
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

            if (IsBunkerEntryBlocked(out var blockMessage))
            {
                LandHudNotice.Show(blockMessage);
                return;
            }

            EnterBunker();
        }

        private static bool IsBunkerEntryBlocked(out string message)
        {
            message = string.Empty;
            var outpostName = LandOutpostLandingState.ActiveOutpostName;
            var save = CharacterSessionState.ActiveSave;
            if (string.IsNullOrWhiteSpace(outpostName) || save == null)
            {
                return false;
            }

            if (!LandBossMissionAssignment.TryGetBossForOutpost(save, outpostName, out var bossNumber)
                || bossNumber <= 0)
            {
                return false;
            }

            if (LandBossEncounter.IsDefeated(bossNumber))
            {
                return false;
            }

            if (!LandBossMissionAssignment.IsBunkerRevealedAtOutpost(save, outpostName))
            {
                message = "The door is damaged beyond use";
                return true;
            }

            return false;
        }

        private static bool HasLivingSoldiers()
        {
            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.GetComponent<LandOutpostGuardMarker>() != null)
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

            var bossNumber = LandBossAreaState.TryGetActiveArea(out var bossArea)
                ? bossArea.BossNumber
                : 0;
            if (bossNumber != 0 && LandBossEncounter.IsDefeated(bossNumber))
            {
                LandBunkerHandoffState.BeginEnter(layout);
                Debug.Log($"F-89 Land: Entering cleared {LandBossAreaState.BunkerCode} layout {layout + 1}/{LayoutCount}.");
                SceneManager.LoadScene(GameScenes.Bunker);
                return;
            }

            if (bossNumber != 0)
            {
                LandBunkerHandoffState.BeginBossFightEnter(layout, bossNumber);
                var bunkerName = LandBossAreaState.HasActiveArea ? LandBossAreaState.BunkerCode : "Bunker";
                Debug.Log($"F-89 Land: {bunkerName} briefing before bunker layout {layout + 1}/{LayoutCount}.");
                SceneManager.LoadScene(GameScenes.GetBossScene(bossNumber));
                return;
            }

            LandBunkerHandoffState.BeginEnter(layout);
            Debug.Log($"F-89 Land: Entering bunker layout {layout + 1}/{LayoutCount}.");
            SceneManager.LoadScene(GameScenes.Bunker);
        }
    }
}
