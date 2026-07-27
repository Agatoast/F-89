using F89.Core;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.LandCombat
{
    /// <summary>Builds one of several procedural bunker interiors.</summary>
    public static class LandBunkerSceneBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<LandPlayerController>() != null)
            {
                return;
            }

            var layout = LandBunkerHandoffState.ConsumeLayoutIndex(LandBunkerEntrance.LayoutCount);
            BuildFloor();
            BuildLayout(layout);
            BuildPlayer(Vector2.zero);
            BuildExitPad(ResolveExitPosition(layout));
            Debug.Log($"F-89 Bunker: Layout {layout + 1}/{LandBunkerEntrance.LayoutCount} ready.");
        }

        private static void BuildFloor()
        {
            var floor = new GameObject("BunkerFloor");
            floor.transform.position = new Vector3(0f, 0f, 1f);
            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = LandPlaceholderArt.GetPixelSquare();
            renderer.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            renderer.sortingOrder = -100;
            floor.transform.localScale = new Vector3(80f, 80f, 1f);
        }

        private static void BuildLayout(int layoutIndex)
        {
            switch (layoutIndex)
            {
                case 0:
                    // Long corridor north-south with side alcoves.
                    AddWall(new Vector2(-4f, 0f), new Vector2(1.2f, 28f));
                    AddWall(new Vector2(4f, 0f), new Vector2(1.2f, 28f));
                    AddWall(new Vector2(-7f, 8f), new Vector2(5f, 1.2f));
                    AddWall(new Vector2(7f, -6f), new Vector2(5f, 1.2f));
                    AddWall(new Vector2(0f, 15f), new Vector2(10f, 1.2f));
                    AddWall(new Vector2(0f, -15f), new Vector2(10f, 1.2f));
                    break;
                case 1:
                    // Open bay with central pillars.
                    AddWall(new Vector2(0f, 12f), new Vector2(24f, 1.2f));
                    AddWall(new Vector2(0f, -12f), new Vector2(24f, 1.2f));
                    AddWall(new Vector2(-12f, 0f), new Vector2(1.2f, 24f));
                    AddWall(new Vector2(12f, 0f), new Vector2(1.2f, 24f));
                    AddWall(new Vector2(-4f, 4f), new Vector2(2.2f, 2.2f));
                    AddWall(new Vector2(4f, -3f), new Vector2(2.2f, 2.2f));
                    AddWall(new Vector2(5f, 5f), new Vector2(2.2f, 2.2f));
                    AddWall(new Vector2(-5f, -5f), new Vector2(2.2f, 2.2f));
                    break;
                default:
                    // T-junction chambers.
                    AddWall(new Vector2(-2.5f, 4f), new Vector2(1.2f, 14f));
                    AddWall(new Vector2(2.5f, 4f), new Vector2(1.2f, 14f));
                    AddWall(new Vector2(-8f, -2f), new Vector2(12f, 1.2f));
                    AddWall(new Vector2(8f, -2f), new Vector2(12f, 1.2f));
                    AddWall(new Vector2(-14f, -8f), new Vector2(1.2f, 12f));
                    AddWall(new Vector2(14f, -8f), new Vector2(1.2f, 12f));
                    AddWall(new Vector2(0f, -14f), new Vector2(28f, 1.2f));
                    AddWall(new Vector2(0f, 12f), new Vector2(8f, 1.2f));
                    break;
            }
        }

        private static Vector2 ResolveExitPosition(int layoutIndex) =>
            layoutIndex switch
            {
                0 => new Vector2(0f, -13.5f),
                1 => new Vector2(0f, -10.5f),
                _ => new Vector2(0f, -12.5f)
            };

        private static void AddWall(Vector2 position, Vector2 size)
        {
            var wall = new GameObject("BunkerWall");
            wall.transform.position = new Vector3(position.x, position.y, 0.2f);
            var renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = LandPlaceholderArt.GetPixelSquare();
            renderer.color = new Color(0.05f, 0.05f, 0.06f, 1f);
            renderer.sortingOrder = 2;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);

            var body = wall.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;

            var box = wall.AddComponent<BoxCollider2D>();
            box.size = Vector2.one;
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

    /// <summary>Blue exit pad — returns to the surface ground map.</summary>
    public sealed class LandBunkerExit : MonoBehaviour
    {
        private bool transitioning;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (transitioning || other.GetComponentInParent<LandPlayerController>() == null)
            {
                return;
            }

            transitioning = true;
            CharacterGearSession.PersistActive();
            LandBossEncounter.CaptureActiveBossHealthFromBunker();
            LandBunkerHandoffState.Clear();
            LandSurfaceSession.BeginReturnAtBunker();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.GroundAttack);
        }
    }
}
