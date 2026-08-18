using F89.Core;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.LandCombat
{
    /// <summary>Builds the boss bunker interior — 200 ft command room with terminal solids.</summary>
    public static class LandBunkerSceneBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<LandPlayerController>() != null)
            {
                return;
            }

            var root = new GameObject("BunkerRoom");
            LandBunkerFloorLayout.BuildFloorVisual(root.transform);
            LandBunkerFloorLayout.BuildSolids(root.transform);
            BuildPlayer(LandBunkerFloorLayout.PlayerSpawn);
            BuildExitPad(LandBunkerFloorLayout.ExitPadPosition);
            Debug.Log(
                $"F-89 Bunker: Command room ready ({LandBunkerFloorLayout.RoomFeet:0} ft square).");
        }

        private static void BuildExitPad(Vector2 position)
        {
            var go = new GameObject("BunkerExit");
            go.transform.position = new Vector3(position.x, position.y, 0.4f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LandPlaceholderArt.GetPixelSquare();
            renderer.color = new Color(0.25f, 0.55f, 0.95f, 1f);
            renderer.sortingOrder = -40;
            var size = LandGameConstants.BunkerEntranceWorldSize;
            go.transform.localScale = new Vector3(size, size, 1f);

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.one;

            go.AddComponent<LandBunkerExit>();
        }

        private static void BuildPlayer(Vector2 worldPosition)
        {
            var playerObject = new GameObject("LandPlayer");
            playerObject.transform.position = worldPosition;

            var sprite = playerObject.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 10;

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var hitbox = playerObject.AddComponent<CircleCollider2D>();
            hitbox.radius = 0.45f;

            var attributes = playerObject.AddComponent<LandPlayerAttributes>();
            attributes.ConfigureFromSave(CharacterSessionState.ActiveSave);
            attributes.ApplyEquippedGear(CharacterGearSession.ActiveLoadout, CharacterGearSession.Catalog);
            var motor = playerObject.AddComponent<LandPlayerMotor>();
            var combat = playerObject.AddComponent<LandPlayerCombat>();
            playerObject.AddComponent<LandPlayerHealth>();
            playerObject.AddComponent<LandPlayerController>();
            playerObject.AddComponent<LandPlayerVisual>();

            var poolObject = new GameObject("LandProjectilePool");
            var pool = poolObject.AddComponent<LandProjectilePool>();
            pool.Warm();
            combat.Initialize(motor, pool);
            combat.EquipWeapon(ResolveEquippedWeapon());

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = LandGameConstants.ArenaHalfSizeWorldUnits;
            camera.transform.position = new Vector3(worldPosition.x, worldPosition.y, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.06f);

            var follow = camera.gameObject.GetComponent<LandGroundCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<LandGroundCameraFollow>();
            }

            follow.SetTarget(playerObject.transform);
        }

        private static LandWeaponDefinition ResolveEquippedWeapon()
        {
            var catalog = CharacterGearSession.Catalog;
            var weaponItem = CharacterGearSession.ActiveLoadout?.Weapon;
            if (weaponItem != null && catalog.TryGetWeapon(weaponItem.DefinitionId, out var weapon))
            {
                return weapon;
            }

            return LandRuntimeContent.GetFallbackBlaster();
        }
    }

    /// <summary>Blue exit pad — returns to the surface ground map after confirmation.</summary>
    public sealed class LandBunkerExit : MonoBehaviour
    {
        private static LandBunkerExit activeExit;
        private static float timeScaleBeforePause = 1f;

        private bool leaveConfirmVisible;
        private bool transitioning;

        public static bool IsLeaveConfirmVisible =>
            activeExit != null && activeExit.leaveConfirmVisible;

        private void OnEnable()
        {
            activeExit = this;
        }

        private void OnDisable()
        {
            if (activeExit == this)
            {
                activeExit = null;
            }

            if (leaveConfirmVisible)
            {
                ResumeCombat();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (transitioning || leaveConfirmVisible || other.GetComponentInParent<LandPlayerController>() == null)
            {
                return;
            }

            BeginLeaveConfirm();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (transitioning || other.GetComponentInParent<LandPlayerController>() == null)
            {
                return;
            }

            CancelLeaveConfirm();
        }

        public static void ConfirmLeave()
        {
            if (activeExit == null || activeExit.transitioning)
            {
                return;
            }

            activeExit.ExecuteLeave();
        }

        public static void CancelLeave()
        {
            activeExit?.CancelLeaveConfirm();
        }

        private void BeginLeaveConfirm()
        {
            leaveConfirmVisible = true;
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        private void CancelLeaveConfirm()
        {
            if (!leaveConfirmVisible || transitioning)
            {
                return;
            }

            leaveConfirmVisible = false;
            ResumeCombat();
        }

        private static void ResumeCombat()
        {
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        }

        private void ExecuteLeave()
        {
            if (transitioning)
            {
                return;
            }

            transitioning = true;
            leaveConfirmVisible = false;
            CharacterGearSession.PersistActive();
            LandBossEncounter.CaptureActiveBossHealthFromBunker();
            LandBunkerHandoffState.Clear();
            LandSurfaceSession.BeginReturnAtBunker();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }
    }
}
