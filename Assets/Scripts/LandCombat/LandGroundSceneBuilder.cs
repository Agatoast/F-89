using UnityEngine;

namespace F89.LandCombat
{
    public static class LandGroundSceneBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindAnyObjectByType<LandPlayerController>() != null)
            {
                return;
            }

            LandGroundSceneController.ResetSession();
            LandGroundTerrainBuilder.BuildArena();
            BuildPlayer();
            SpawnEnemies();
        }

        private static void BuildPlayer()
        {
            var playerObject = new GameObject("LandPlayer");
            playerObject.transform.position = Vector3.zero;

            var sprite = playerObject.AddComponent<SpriteRenderer>();
            sprite.sprite = LandPlaceholderArt.GetPixelCircle();
            sprite.color = new Color(0.2f, 0.75f, 1f);
            sprite.sortingOrder = 10;

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var motor = playerObject.AddComponent<LandPlayerMotor>();
            var combat = playerObject.AddComponent<LandPlayerCombat>();
            playerObject.AddComponent<LandPlayerHealth>();
            playerObject.AddComponent<LandPlayerController>();

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
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.84f, 0.88f, 0.93f);

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

        private static void SpawnEnemies()
        {
            var spawnRadius = LandGameConstants.ArenaHalfSizeWorldUnits * 0.75f;
            var spawnPoints = new[]
            {
                new Vector2(spawnRadius * 0.45f, spawnRadius * 0.25f),
                new Vector2(-spawnRadius * 0.55f, spawnRadius * 0.15f),
                new Vector2(spawnRadius * 0.25f, -spawnRadius * 0.45f),
                new Vector2(-spawnRadius * 0.35f, -spawnRadius * 0.35f),
                new Vector2(spawnRadius * 0.65f, -spawnRadius * 0.1f)
            };

            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var enemyObject = new GameObject($"URTroop_{i + 1}");
                var enemy = enemyObject.AddComponent<LandGroundEnemy>();
                enemy.Initialize(spawnPoints[i], new Color(0.85f, 0.2f, 0.15f));
            }
        }
    }
}
