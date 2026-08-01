using F89.Audio;
using F89.CameraSystems;
using F89.Controls;
using F89.Core;
using F89.Enemies;
using F89.Flight;
using F89.UI;
using F89.Weapons;
using UnityEngine;

namespace F89.Testing
{
    public static class FlightTestRuntimeBuilder
    {
        public static GameObject BuildIfNeeded()
        {
            var existingPlayer = Object.FindAnyObjectByType<AircraftController>();
            if (existingPlayer != null)
            {
                FlightAudioBootstrap.EnsureReady();
                FlightMissionStartBootstrap.ResetForSceneLoad();
                EnsurePlayerConfigured(existingPlayer);

                EnsureMapSystems(existingPlayer);
                EnsurePlayerVisuals(existingPlayer);
                AntarcticaBaseSpawner.SpawnIfNeeded();
                EnsureWeaponSystems(existingPlayer.gameObject);
                JetEngineSound.EnsureOn(existingPlayer.gameObject);
                Gau27FireSound.EnsureOn(existingPlayer.gameObject);
                WeaponTestTargetSpawner.RemoveIfPresent();
                FlightHudHost.EnsureOn(existingPlayer.gameObject);

                return null;
            }

            return Build();
        }

        private static void EnsurePlayerVisuals(AircraftController player)
        {
            if (player == null)
            {
                return;
            }

            var visualPivot = player.transform.Find("VisualPivot");
            if (visualPivot == null)
            {
                return;
            }

            var bankVisual = visualPivot.GetComponent<AircraftBankVisual>();
            var aircraftVisual = visualPivot.Find("AircraftVisual");
            if (bankVisual != null && aircraftVisual != null)
            {
                bankVisual.Configure(player, visualPivot, aircraftVisual);
            }
        }

        private static void EnsureMapSystems(AircraftController player)
        {
            var profile = player.Profile ?? LoadFlightProfile();
            var worldMap = player.WorldMap ?? LoadWorldMapConfig();
            var mapRoot = CreateMapRoot(worldMap, profile);
            mapRoot.GetComponent<InfiniteWhiteMap>().SetTarget(player.transform);

            var grid = mapRoot.GetComponent<MotionGridOverlay>();
            if (grid == null)
            {
                grid = mapRoot.AddComponent<MotionGridOverlay>();
            }

            grid.Configure(player.transform, profile.ticSizeWorldUnits, worldMap);
            CreateAntarcticaMapOverlay(player.gameObject);
            EnsureAutopilotController(player.gameObject);
            EnsureLandingController(player.gameObject);
            EnsureFlightCamera(player);
        }

        private static void EnsureFlightCamera(AircraftController player)
        {
            if (player == null)
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.white;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 5000f;
            }

            var follow = camera.GetComponent<TopDownFollowCamera>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<TopDownFollowCamera>();
            }

            follow.Configure(player.transform, player);
        }

        public static GameObject Build()
        {
            FlightAudioBootstrap.EnsureReady();
            var profile = LoadFlightProfile();
            var worldMap = LoadWorldMapConfig();
            var mapRoot = CreateMapRoot(worldMap, profile);
            AntarcticaBaseSpawner.SpawnIfNeeded();
            var player = CreatePlayer(profile, worldMap);
            SetupCamera(player);
            CreateWeaponSystems(player);
            EnsureCountermeasureSystems(player);
            RemoveInvisibleOutpostSamLaunchers();
            WeaponTestTargetSpawner.RemoveIfPresent();
            mapRoot.GetComponent<InfiniteWhiteMap>().SetTarget(player.transform);

            var grid = mapRoot.GetComponent<MotionGridOverlay>();
            if (grid == null)
            {
                grid = mapRoot.AddComponent<MotionGridOverlay>();
            }

            grid.Configure(player.transform, profile.ticSizeWorldUnits, worldMap);

            CreateAntarcticaMapOverlay(player);
            FlightHudHost.EnsureOn(player);

            Debug.Log("F-89 flight test ready. Launch from USS Martin Van Buren. Mission 1: capture Palmer Station.");
            return player;
        }

        private static Gau27aWeaponConfig LoadGau27aConfig()
        {
            var config = Resources.Load<Gau27aWeaponConfig>("F89_Gau27aWeaponConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<Gau27aWeaponConfig>();
            Debug.LogWarning("F-89: Using runtime GAU-27A defaults.");
            return config;
        }

        private static Gbu12PavewayConfig LoadGbu12Config()
        {
            var config = Resources.Load<Gbu12PavewayConfig>("F89_Gbu12PavewayConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<Gbu12PavewayConfig>();
            Debug.LogWarning("F-89: Using runtime GBU-12 defaults.");
            return config;
        }

        private static Agm114HellfireWeaponConfig LoadAgm114Config()
        {
            var config = Resources.Load<Agm114HellfireWeaponConfig>("F89_Agm114HellfireWeaponConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<Agm114HellfireWeaponConfig>();
            Debug.LogWarning("F-89: Using runtime AGM-114 Hellfire defaults.");
            return config;
        }

        private static Aim9zWeaponConfig LoadAim9zConfig()
        {
            var config = Resources.Load<Aim9zWeaponConfig>("F89_Aim9zWeaponConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<Aim9zWeaponConfig>();
            Debug.LogWarning("F-89: Using runtime AIM-9z defaults.");
            return config;
        }

        private static Agm88jSiawWeaponConfig LoadAgm88jConfig()
        {
            var config = Resources.Load<Agm88jSiawWeaponConfig>("F89_Agm88jSiawWeaponConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<Agm88jSiawWeaponConfig>();
            Debug.LogWarning("F-89: Using runtime AGM-88J SiAW defaults.");
            return config;
        }

        private static FlareLoadoutConfig LoadFlareLoadoutConfig()
        {
            var config = Resources.Load<FlareLoadoutConfig>("F89_FlareLoadoutConfig");
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<FlareLoadoutConfig>();
            Debug.LogWarning("F-89: Using runtime flare loadout defaults (48 flares).");
            return config;
        }

        private static FlightProfile LoadFlightProfile()
        {
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<FlightProfile>();
            Debug.LogWarning("F-89: Using runtime flight profile defaults.");
            return profile;
        }

        private static WorldMapConfig LoadWorldMapConfig()
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            if (worldMap != null)
            {
                return worldMap;
            }

            worldMap = ScriptableObject.CreateInstance<WorldMapConfig>();
            Debug.LogWarning("F-89: Using runtime world map defaults.");
            return worldMap;
        }

        private static GameObject CreateMapRoot(WorldMapConfig worldMap, FlightProfile profile)
        {
            var existing = Object.FindAnyObjectByType<InfiniteWhiteMap>();
            if (existing != null)
            {
                existing.Configure(worldMap, profile != null ? profile.ticSizeWorldUnits : 1f);
                if (existing.GetComponent<MotionGridOverlay>() == null)
                {
                    existing.gameObject.AddComponent<MotionGridOverlay>();
                }

                return existing.gameObject;
            }

            var mapRoot = new GameObject("Map");
            var ground = mapRoot.AddComponent<InfiniteWhiteMap>();
            ground.Configure(worldMap, profile != null ? profile.ticSizeWorldUnits : 1f);
            mapRoot.AddComponent<MotionGridOverlay>();
            return mapRoot;
        }

        private static GameObject CreatePlayer(FlightProfile profile, WorldMapConfig worldMap)
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            var spawnPosition = Vector3.zero;
            var spawnRotation = Quaternion.identity;
            var hasSpawn = FlightGroundReturnService.TryGetPendingReturnSpawn(out spawnPosition, out spawnRotation)
                || AntarcticaBaseSpawner.TryGetPlayerSpawn(worldMap, profile, out spawnPosition, out spawnRotation);
            if (hasSpawn)
            {
                player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (hasSpawn)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            var input = player.AddComponent<PlayerAircraftInput>();
            var controller = player.AddComponent<AircraftController>();
            controller.Configure(profile, input, worldMap);
            EnsureAutopilotController(player);
            EnsureLandingController(player);

            var visualPivot = new GameObject("VisualPivot");
            visualPivot.transform.SetParent(player.transform, false);

            var bankVisual = visualPivot.AddComponent<AircraftBankVisual>();

            var aircraftVisual = AircraftVisualFactory.CreateVisual(visualPivot.transform, controller);
            bankVisual.Configure(controller, visualPivot.transform, aircraftVisual.transform);

            EnsurePlayerLockableTarget(player);
            JetEngineSound.EnsureOn(player);
            Gau27FireSound.EnsureOn(player);

            return player;
        }

        private static void EnsurePlayerLockableTarget(GameObject player)
        {
            var target = player.GetComponent<LockableTarget>();
            if (target == null)
            {
                target = player.AddComponent<LockableTarget>();
            }

            target.Configure(
                "F-89",
                LockableTargetKind.Air,
                TargetAffiliation.Friendly,
                TargetUnitClass.PlayerAircraft);
            target.SetMaxGroundHitPoints(PlayerAircraftGhp.Max);
        }

        private static void CreateWeaponSystems(GameObject player)
        {
            if (player.GetComponent<MissileLockController>() == null)
            {
                player.AddComponent<MissileLockController>();
            }

            if (player.GetComponent<WeaponTargetPaint>() == null)
            {
                player.AddComponent<WeaponTargetPaint>();
            }

            if (player.GetComponent<Gau27aGunController>() == null)
            {
                player.AddComponent<Gau27aGunController>();
            }

            if (player.GetComponent<PlayerWeaponController>() == null)
            {
                player.AddComponent<PlayerWeaponController>();
            }

            if (player.GetComponent<FlareCountermeasureController>() == null)
            {
                player.AddComponent<FlareCountermeasureController>();
            }

            var input = player.GetComponent<PlayerAircraftInput>();
            var aim9zConfig = LoadAim9zConfig();
            var agm88jConfig = LoadAgm88jConfig();
            var gbu12Config = LoadGbu12Config();
            var agm114Config = LoadAgm114Config();
            var gau27aConfig = LoadGau27aConfig();
            EnsureWeaponConfigs(
                ref aim9zConfig,
                ref agm88jConfig,
                ref gbu12Config,
                ref agm114Config,
                ref gau27aConfig);
            var controller = player.GetComponent<AircraftController>();
            var lockController = player.GetComponent<MissileLockController>();
            if (lockController == null)
            {
                lockController = player.AddComponent<MissileLockController>();
            }

            var targetPaint = player.GetComponent<WeaponTargetPaint>();
            if (targetPaint == null)
            {
                targetPaint = player.AddComponent<WeaponTargetPaint>();
            }

            var gau27aGun = player.GetComponent<Gau27aGunController>();
            var weaponController = player.GetComponent<PlayerWeaponController>();
            var flareController = player.GetComponent<FlareCountermeasureController>();
            var camera = Camera.main;

            flareController?.Configure(controller, input, LoadFlareLoadoutConfig());
            lockController.Configure(controller, camera);
            targetPaint.Configure(controller, camera);
            weaponController.Configure(
                aim9zConfig,
                agm88jConfig,
                gbu12Config,
                agm114Config,
                gau27aConfig,
                controller,
                input,
                lockController,
                targetPaint,
                gau27aGun);

            AircraftLoadoutState.ApplyToWeaponController(weaponController);
        }

        private static void EnsureWeaponSystems(GameObject player)
        {
            CreateWeaponSystems(player);
            EnsureCountermeasureSystems(player);
            EnsureFuelCrashMonitor(player);
            RemoveInvisibleOutpostSamLaunchers();
            CreateAntarcticaMapOverlay(player);
        }

        private static void EnsureCountermeasureSystems(GameObject player)
        {
            EnsurePlayerLockableTarget(player);

            var controller = player.GetComponent<AircraftController>();
            var input = player.GetComponent<PlayerAircraftInput>();
            var flareController = player.GetComponent<FlareCountermeasureController>();
            if (flareController == null)
            {
                flareController = player.AddComponent<FlareCountermeasureController>();
            }

            flareController.Configure(controller, input, LoadFlareLoadoutConfig());
        }

        private static void RemoveInvisibleOutpostSamLaunchers()
        {
            EnemySamLauncher.RemoveFromOutpostBases();
        }

        private static void EnsureFuelCrashMonitor(GameObject player)
        {
            if (player.GetComponent<PlayerAircraftFuelCrashMonitor>() == null)
            {
                player.AddComponent<PlayerAircraftFuelCrashMonitor>();
            }
        }

        private static void EnsureAutopilotController(GameObject player)
        {
            if (player.GetComponent<AutopilotController>() == null)
            {
                player.AddComponent<AutopilotController>();
            }
        }

        private static void EnsureLandingController(GameObject player)
        {
            if (player.GetComponent<AircraftLandingController>() == null)
            {
                player.AddComponent<AircraftLandingController>();
            }
        }

        private static void CreateAntarcticaMapOverlay(GameObject player)
        {
            var mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            if (mapOverlay == null)
            {
                var mapObject = new GameObject("AntarcticaMapOverlay");
                mapOverlay = mapObject.AddComponent<AntarcticaMapOverlay>();
            }

            var controller = player.GetComponent<AircraftController>();
            var autopilot = player.GetComponent<AutopilotController>();
            mapOverlay.Configure(
                controller,
                controller != null ? controller.WorldMap : LoadWorldMapConfig(),
                autopilot);
            autopilot?.Configure(mapOverlay);
        }

        private static void SetupCamera(GameObject player)
        {
            EnsureFlightCamera(player.GetComponent<AircraftController>());
        }

        private static void EnsurePlayerConfigured(AircraftController player)
        {
            if (player == null)
            {
                return;
            }

            if (player.GetComponent<PlayerAircraftInput>() == null)
            {
                player.gameObject.AddComponent<PlayerAircraftInput>();
            }

            var profile = player.Profile ?? LoadFlightProfile();
            var worldMap = player.WorldMap ?? LoadWorldMapConfig();
            var input = player.GetComponent<PlayerAircraftInput>();
            player.Configure(profile, input, worldMap);
        }

        private static void EnsureWeaponConfigs(
            ref Aim9zWeaponConfig aim9z,
            ref Agm88jSiawWeaponConfig agm88j,
            ref Gbu12PavewayConfig gbu12,
            ref Agm114HellfireWeaponConfig agm114,
            ref Gau27aWeaponConfig gau27a)
        {
            aim9z ??= LoadAim9zConfig();
            agm88j ??= LoadAgm88jConfig();
            gbu12 ??= LoadGbu12Config();
            agm114 ??= LoadAgm114Config();
            gau27a ??= LoadGau27aConfig();
        }
    }
}
