using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    public static class LandGroundSceneBuilder
    {
        /// <summary>Offset from bunker pad center so re-entry trigger does not fire immediately.</summary>
        private const float ExitSpawnOffsetWorld = 2.8f;

        public static void BuildIfNeeded()
        {
            // Bunker return takes priority over any stale scene objects left by an editor
            // domain/scene-reload configuration, otherwise the player falls back to the plane.
            if (LandSurfaceSession.TryConsumeReturnAtBunker(
                    out var planePos,
                    out var bunkerPos,
                    out var enemies))
            {
                LandGroundSceneController.ResetSession();
                LandGroundTerrainBuilder.BuildArena();
                RestoreSurfaceFromBunker(planePos, bunkerPos, enemies);
                return;
            }

            if (Object.FindAnyObjectByType<LandPlayerController>() != null)
            {
                // Scene already built (e.g. domain reload) — still ensure terrain follow, cold, plane.
                EnsureLandedPlane();
                EnsureColdExposure();
                EnsurePlayerHitbox();
                BindTerrainFollow();
                return;
            }

            LandGroundSceneController.ResetSession();
            LandGroundTerrainBuilder.BuildArena();

            // Fresh sortie: player at landed plane; objectives in one random bearing.
            var planePosition = Vector3.zero;
            LandLandedPlane.SpawnAt(planePosition);
            var playerPosition = planePosition
                + new Vector3(LandGameConstants.LandedPlaneOffsetWorldUnits, 0f, 0f);
            BuildPlayer(playerPosition);
            BindTerrainFollow();
            if (LandBossAreaState.TryGetActiveArea(out var bossArea))
            {
                var save = CharacterSessionState.ActiveSave;
                var spawnBunker = LandOutpostLandingState.HasActiveOutpost
                    && LandBossMissionAssignment.IsBunkerRevealedAtOutpost(
                        save,
                        LandOutpostLandingState.ActiveOutpostName);
                SpawnBossArea(planePosition, bossArea, spawnBunker);
            }
            else if (LandOutpostLandingState.HasActiveOutpost)
            {
                SpawnOutpostBunker(planePosition, LandOutpostLandingState.ActiveOutpostName);
            }
        }

        private static void RestoreSurfaceFromBunker(
            Vector2 planePos,
            Vector2 bunkerPos,
            System.Collections.Generic.IReadOnlyList<(Vector2 position, int level)> enemies)
        {
            LandLandedPlane.SpawnAt(planePos);
            LandBunkerEntrance.Spawn(bunkerPos);

            // Spawn just south of the pad so the entrance trigger does not re-fire.
            var playerPos = bunkerPos + Vector2.down * ExitSpawnOffsetWorld;
            BuildPlayer(playerPos);
            BindTerrainFollow();

            var player = Object.FindAnyObjectByType<LandPlayerController>();
            var faceTarget = player != null ? player.transform : null;
            if (enemies != null)
            {
                for (var i = 0; i < enemies.Count; i++)
                {
                    var record = enemies[i];
                    var enemyObject = new GameObject($"URTroop_{i + 1}");
                    var enemy = enemyObject.AddComponent<LandGroundEnemy>();
                    enemy.Initialize(record.position, record.level, faceTarget);
                }
            }

            Debug.Log(
                $"F-89 Land: Returned from bunker at {bunkerPos} "
                + $"(plane at {planePos}, {enemies?.Count ?? 0} enemies restored).");
        }

        private static void EnsureLandedPlane()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null)
            {
                LandLandedPlane.SpawnAt(Vector3.zero);
                return;
            }

            // Keep plane near the player on domain reload.
            LandLandedPlane.SpawnLeftOf(player.transform.position);
        }

        private static void EnsureColdExposure()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null || player.GetComponent<LandColdExposure>() != null)
            {
                return;
            }

            player.gameObject.AddComponent<LandColdExposure>();
        }

        private static void EnsurePlayerHitbox()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null)
            {
                return;
            }

            if (!player.TryGetComponent<CircleCollider2D>(out var hitbox))
            {
                hitbox = player.gameObject.AddComponent<CircleCollider2D>();
            }

            hitbox.isTrigger = true;
            hitbox.radius = 0.55f;
        }

        private static void BindTerrainFollow()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player != null)
            {
                LandGroundTerrainBuilder.BindFollowTarget(player.transform);
            }
        }

        private static void BuildPlayer(Vector3 worldPosition)
        {
            var playerObject = new GameObject("LandPlayer");
            playerObject.transform.position = worldPosition;

            var sprite = playerObject.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 10;

            var body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var hitbox = playerObject.AddComponent<CircleCollider2D>();
            hitbox.isTrigger = true;
            hitbox.radius = 0.55f;

            var attributes = playerObject.AddComponent<LandPlayerAttributes>();
            attributes.ConfigureFromSave(F89.Core.CharacterSessionState.ActiveSave);
            attributes.ApplyEquippedGear(CharacterGearSession.ActiveLoadout, CharacterGearSession.Catalog);
            var motor = playerObject.AddComponent<LandPlayerMotor>();
            var combat = playerObject.AddComponent<LandPlayerCombat>();
            playerObject.AddComponent<LandPlayerHealth>();
            playerObject.AddComponent<LandColdExposure>();
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

        private static void SpawnObjectiveCluster(Vector3 planeWorldPosition)
        {
            var bearing = Random.Range(0f, Mathf.PI * 2f);
            var bearingDir = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing));
            var plane = (Vector2)planeWorldPosition;

            var player = Object.FindAnyObjectByType<LandPlayerController>();
            var faceTarget = player != null ? player.transform : null;

            // Temporary spawn mix for ground test: all level 1.
            var tempLevels = new[] { 1, 1, 1, 1, 1 };
            var enemyCount = tempLevels.Length;

            for (var i = 0; i < enemyCount; i++)
            {
                // Keep the objective sector: same bearing ± cone, distance 50–100 range.
                var cone = Random.Range(-0.35f, 0.35f);
                var dir = new Vector2(
                    Mathf.Cos(bearing + cone),
                    Mathf.Sin(bearing + cone));
                var landRange = Random.Range(
                    LandGameConstants.EnemySpawnMinRangeLandUnits,
                    LandGameConstants.EnemySpawnMaxRangeLandUnits);
                var spawn = plane + dir * LandUnits.ToWorld(landRange);

                var enemyObject = new GameObject($"URTroop_{i + 1}");
                var enemy = enemyObject.AddComponent<LandGroundEnemy>();
                enemy.Initialize(spawn, tempLevels[i], faceTarget);
            }

            var bunkerLandRange = Random.Range(
                LandGameConstants.BunkerEntranceMinRangeLandUnits,
                LandGameConstants.BunkerEntranceMaxRangeLandUnits);
            var bunkerPos = plane + bearingDir * LandUnits.ToWorld(bunkerLandRange);
            LandBunkerEntrance.Spawn(bunkerPos);

            Debug.Log(
                $"F-89 Land: Objectives bearing {bearing * Mathf.Rad2Deg:0}° — "
                + $"{enemyCount} enemies at {LandGameConstants.EnemySpawnMinRangeLandUnits:0}-"
                + $"{LandGameConstants.EnemySpawnMaxRangeLandUnits:0} range, "
                + $"bunker at {bunkerLandRange:0.0} range.");
        }

        private static void SpawnOutpostBunker(Vector3 planeWorldPosition, string outpostName)
        {
            var bunkerPosition = (Vector2)planeWorldPosition + Vector2.up * LandUnits.ToWorld(70f);
            var bunker = LandBunkerEntrance.Spawn(bunkerPosition);
            bunker.name = $"{outpostName} Bunker";
            Debug.Log($"F-89 Land: Landed at {outpostName}; bunker access available, no surface enemies spawned.");
        }

        private static void SpawnBossArea(
            Vector3 planeWorldPosition,
            LandBossAreaCatalog.Definition area,
            bool spawnBunker)
        {
            var bearing = area.BearingDegrees * Mathf.Deg2Rad;
            var bearingDir = new Vector2(Mathf.Cos(bearing), Mathf.Sin(bearing));
            var plane = (Vector2)planeWorldPosition;
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            var faceTarget = player != null ? player.transform : null;

            var guardRange = (LandGameConstants.EnemySpawnMinRangeLandUnits
                              + LandGameConstants.EnemySpawnMaxRangeLandUnits) * 0.5f;
            var spawnGuards = !LandBossEncounter.IsGuardCleared(area.BossNumber);
            for (var i = 0; spawnGuards && i < area.GuardCount; i++)
            {
                var lateral = (i - (area.GuardCount - 1) * 0.5f) * 2.2f;
                var distanceOffset = (i % 2 == 0 ? -2f : 2f);
                var spawn = plane
                            + bearingDir * LandUnits.ToWorld(guardRange + distanceOffset)
                            + new Vector2(-bearingDir.y, bearingDir.x) * lateral;
                var enemyObject = new GameObject($"{area.SurfaceCode}_Guard_{i + 1}");
                var enemy = enemyObject.AddComponent<LandGroundEnemy>();
                enemy.Initialize(spawn, area.GuardLevel, faceTarget);
            }

            if (spawnBunker)
            {
                var bunkerRange = (LandGameConstants.BunkerEntranceMinRangeLandUnits
                                   + LandGameConstants.BunkerEntranceMaxRangeLandUnits) * 0.5f;
                var bunkerPos = plane + bearingDir * LandUnits.ToWorld(bunkerRange);
                var bunker = LandBunkerEntrance.Spawn(bunkerPos);
                bunker.name = area.BunkerCode;
            }

            Debug.Log(
                $"F-89 Land: {area.SurfaceCode} ready — {(spawnGuards ? area.GuardCount : 0)} UR level {area.GuardLevel} guards"
                + (spawnBunker
                    ? $"; {area.BunkerCode} bunker available."
                    : "; bunker locked until mission launch."));
        }
    }
}
