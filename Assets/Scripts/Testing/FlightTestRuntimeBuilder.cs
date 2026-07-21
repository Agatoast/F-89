using F89.CameraSystems;
using F89.Controls;
using F89.Core;
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
                EnsureMapSystems(existingPlayer);
                EnsureWeaponSystems(existingPlayer.gameObject);
                WeaponTestTargetSpawner.SpawnIfNeeded();
                AntarcticaBaseSpawner.SpawnIfNeeded();
                AntarcticaBaseSpawner.TryMovePlayerToCarrier(existingPlayer.transform);
                RemoveDecoyIfPresent();
                return null;
            }

            return Build();
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
        }

        public static GameObject Build()
        {
            var profile = LoadFlightProfile();
            var worldMap = LoadWorldMapConfig();
            var mapRoot = CreateMapRoot(worldMap, profile);
            AntarcticaBaseSpawner.SpawnIfNeeded();
            var player = CreatePlayer(profile, worldMap);
            SetupCamera(player);
            CreatePauseController();
            CreateWeaponSystems(player);
            CreateFlightHud(player);
            CreateAntarcticaMapOverlay(player);
            WeaponTestTargetSpawner.SpawnIfNeeded();
            RemoveDecoyIfPresent();
            mapRoot.GetComponent<InfiniteWhiteMap>().SetTarget(player.transform);

            var grid = mapRoot.GetComponent<MotionGridOverlay>();
            if (grid == null)
            {
                grid = mapRoot.AddComponent<MotionGridOverlay>();
            }

            grid.Configure(player.transform, profile.ticSizeWorldUnits, worldMap);

            Debug.Log("F-89 flight test ready. Launch from USS Martin Van Buren. Mission 1: capture Palmer Station.");
            return player;
        }

        private static void RemoveDecoyIfPresent()
        {
            var decoy = GameObject.Find("TestDecoyAirTarget");
            if (decoy != null)
            {
                Object.Destroy(decoy);
            }
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

            if (AntarcticaBaseSpawner.TryGetPlayerSpawn(worldMap, profile, out var spawnPosition, out var spawnRotation))
            {
                player.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var input = player.AddComponent<PlayerAircraftInput>();
            var controller = player.AddComponent<AircraftController>();
            controller.Configure(profile, input, worldMap);
            EnsureAutopilotController(player);

            var visualPivot = new GameObject("VisualPivot");
            visualPivot.transform.SetParent(player.transform, false);

            var bankVisual = visualPivot.AddComponent<AircraftBankVisual>();

            var aircraftVisual = AircraftVisualFactory.CreateVisual(visualPivot.transform, controller);
            bankVisual.Configure(controller, visualPivot.transform, aircraftVisual.transform);

            return player;
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

            var input = player.GetComponent<PlayerAircraftInput>();
            var aim9zConfig = LoadAim9zConfig();
            var agm88jConfig = LoadAgm88jConfig();
            var gbu12Config = LoadGbu12Config();
            var agm114Config = LoadAgm114Config();
            var gau27aConfig = LoadGau27aConfig();
            var controller = player.GetComponent<AircraftController>();
            var lockController = player.GetComponent<MissileLockController>();
            var targetPaint = player.GetComponent<WeaponTargetPaint>();
            var gau27aGun = player.GetComponent<Gau27aGunController>();
            var weaponController = player.GetComponent<PlayerWeaponController>();
            var camera = Camera.main;

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

            if (Object.FindAnyObjectByType<WeaponReticleHud>() == null)
            {
                var reticleObject = new GameObject("WeaponReticleHud");
                var reticle = reticleObject.AddComponent<WeaponReticleHud>();
                reticle.Configure(weaponController, input);
            }

            if (Object.FindAnyObjectByType<HudTargetDiamondOverlay>() == null)
            {
                var diamondObject = new GameObject("HudTargetDiamondOverlay");
                var diamonds = diamondObject.AddComponent<HudTargetDiamondOverlay>();
                diamonds.Configure(weaponController, controller, camera);
            }

            if (Object.FindAnyObjectByType<PlaneRadarOverlay>() == null)
            {
                var radarObject = new GameObject("PlaneRadarOverlay");
                var radar = radarObject.AddComponent<PlaneRadarOverlay>();
                radar.Configure(controller, lockController, weaponController);
                weaponController.SetRadarOverlay(radar);
            }
            else
            {
                var radar = Object.FindAnyObjectByType<PlaneRadarOverlay>();
                radar?.Configure(controller, lockController, weaponController);
                weaponController.SetRadarOverlay(radar);
            }

            var hud = Object.FindAnyObjectByType<FlightHud>();
            if (hud != null)
            {
                hud.Configure(controller, weaponController);
            }
        }

        private static void EnsureWeaponSystems(GameObject player)
        {
            CreateWeaponSystems(player);
            var controller = player.GetComponent<AircraftController>();
            var weaponController = player.GetComponent<PlayerWeaponController>();
            CreateFlightHud(player);
            if (weaponController != null && controller != null)
            {
                var hud = Object.FindAnyObjectByType<FlightHud>();
                hud?.Configure(controller, weaponController);
            }
        }

        private static void CreateFlightHud(GameObject player)
        {
            var hud = Object.FindAnyObjectByType<FlightHud>();
            if (hud == null)
            {
                var hudObject = new GameObject("FlightHud");
                hud = hudObject.AddComponent<FlightHud>();
            }

            var controller = player.GetComponent<AircraftController>();
            var weaponController = player.GetComponent<PlayerWeaponController>();
            hud.Configure(controller, weaponController);
        }

        private static void EnsureAutopilotController(GameObject player)
        {
            var autopilot = player.GetComponent<AutopilotController>();
            if (autopilot == null)
            {
                autopilot = player.AddComponent<AutopilotController>();
            }

            var mapOverlay = Object.FindAnyObjectByType<AntarcticaMapOverlay>();
            autopilot.Configure(mapOverlay);
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
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.white;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;

            var follow = camera.GetComponent<TopDownFollowCamera>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<TopDownFollowCamera>();
            }

            follow.Configure(player.transform, player.GetComponent<AircraftController>());
            camera.transform.position = new Vector3(0f, 38f, -12f);
            camera.transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        }

        private static void CreatePauseController()
        {
            if (Object.FindAnyObjectByType<GamePauseController>() != null)
            {
                return;
            }

            var pauseObject = new GameObject("GamePauseController");
            pauseObject.AddComponent<GamePauseController>();
        }
    }
}
