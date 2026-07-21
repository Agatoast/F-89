using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Testing
{
    public static class AntarcticaBaseSpawner
    {
        private const string RootName = "AntarcticaBases";
        private const int CatalogBaseCount = 12;
        private const int LandBaseCount = 50;
        private const int ExcludedLandBaseCount = 10;
        private const int FixedCarrierRelativeBaseCount = 1;
        private const int FixedAnchorRelativeBaseCount = 2;
        private const int CarrierCount = 1;
        private const int ExpectedBaseCount = CatalogBaseCount + LandBaseCount - ExcludedLandBaseCount
            + FixedCarrierRelativeBaseCount + FixedAnchorRelativeBaseCount + CarrierCount;
        private const int BasesLayoutVersion = 43;
        private const float FixedRelativeBaseOffsetMiles = 200f;
        private const float AnchorRelativeBaseSnapSearchMiles = 45f;
        private const float SolidIceSnapSearchMiles = AntarcticaLandMask.BasePlacementSnapSearchMiles;
        private const float CarrierPositionToleranceMiles = 1f;
        private const float CarrierOceanSearchNorthMiles = 1000f;
        private const float CarrierOceanSearchEastWestMiles = 48f;
        private const float CarrierNorthOffsetMiles = 400f;
        private const string CarrierSouthBaseName = "Outpost South";
        private const string Outpost13BaseName = "Outpost 13";
        private const string Outpost13SeedBaseName = "Outpost 13 SE";
        private const string Outpost01BaseName = "Outpost 01";
        private const float Outpost01BearingDegrees = 120f;
        private const float Outpost01DistanceMiles = 200f;
        private static readonly Vector2 Outpost01OffsetFromSouthMiles =
            CompassOffsetMiles(Outpost01DistanceMiles, Outpost01BearingDegrees);
        private const float Outpost13BearingDegrees = 120f;
        private const float Outpost13DistanceMiles = 20f;
        private static readonly Vector2 Outpost13PriorOffsetFromSeedMiles = new Vector2(-100f, -200f);
        private static readonly Vector2 Outpost13OffsetFromSeedMiles = (
            Outpost13PriorOffsetFromSeedMiles
            + CompassOffsetMiles(Outpost13DistanceMiles, Outpost13BearingDegrees)) * 0.5f;
        private static readonly Vector2 CarrierSouthOffsetFromCarrierMiles = new Vector2(-30f, 215f);
        private const float CarrierSouthSearchSouthMiles = 720f;
        private const float CarrierSouthSearchNorthMiles = 40f;
        private const float CarrierSouthSearchEastMiles = 720f;
        private const float CarrierSouthSearchWestMiles = 80f;

        private static readonly HashSet<string> ExcludedOutpostNames = new HashSet<string>
        {
            "Outpost 01",
            "Outpost 03",
            "Outpost 11",
            "Outpost 25",
            "Outpost 26",
            "Outpost 34",
            "Outpost 36",
            "Outpost 38",
            "Outpost 45",
            "Outpost 48"
        };

        public static void SpawnIfNeeded()
        {
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);

            var existing = GameObject.Find(RootName);
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            if (existing != null)
            {
                Object.Destroy(existing);
            }

            var catalog = Resources.Load<AntarcticaBaseCatalog>("F89_AntarcticaBaseCatalog");
            if (catalog == null || catalog.bases == null || catalog.bases.Length == 0)
            {
                catalog = CreateRuntimeDefaults();
            }

            var mission = AntarcticaMissionConfig.LoadOrDefault();

            var root = new GameObject(RootName);
            var layoutMarker = root.AddComponent<AntarcticaBasesRootMarker>();
            layoutMarker.layoutVersion = BasesLayoutVersion;

            var landBasePositions = new List<Vector2>();

            foreach (var definition in catalog.bases)
            {
                if (!definition.startsActive)
                {
                    continue;
                }

                var resolvedMiles = ResolveLandBasePosition(definition.positionMiles, mapSizeMiles);
                landBasePositions.Add(resolvedMiles);
                SpawnBase(root.transform, ResolveDefinitionPosition(definition, resolvedMiles), worldUnitsPerMile);
            }

            foreach (var definition in AntarcticaBaseLandFactory.GenerateLandBases(
                         mapSizeMiles,
                         landBasePositions,
                         LandBaseCount))
            {
                if (ExcludedOutpostNames.Contains(definition.baseName))
                {
                    continue;
                }

                var resolvedMiles = ResolveLandBasePosition(definition.positionMiles, mapSizeMiles);
                landBasePositions.Add(resolvedMiles);
                SpawnBase(
                    root.transform,
                    ResolveDefinitionPosition(definition, resolvedMiles),
                    worldUnitsPerMile);
            }

            var carrierSeedMiles = AntarcticaWorldLocations.DefaultCarrierPositionMiles;
            var carrierPositionMiles = ResolveCarrierPositionMiles(carrierSeedMiles, mapSizeMiles);
            AntarcticaWorldLocations.SetCarrierPositionMiles(carrierPositionMiles);
            layoutMarker.carrierPositionMiles = carrierPositionMiles;

            SpawnCarrier(root.transform, mission, carrierPositionMiles, worldUnitsPerMile);
            SpawnCarrierSouthBase(root.transform, carrierPositionMiles, mapSizeMiles, worldUnitsPerMile);
            SpawnAnchorRelativeBases(root.transform, mapSizeMiles, worldUnitsPerMile);
            MarkMissionObjective(root.transform, mission.firstObjectiveBaseName);
            EnsureCarrierAtLockedPosition(worldUnitsPerMile, carrierPositionMiles);
            SyncAllBasePositions(worldUnitsPerMile);
        }

        private static void SyncAllBasePositions(float worldUnitsPerMile)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null)
                {
                    continue;
                }

                baseSite.ApplyWorldPosition(worldUnitsPerMile);
            }
        }

        public static bool TryMovePlayerToCarrier(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return false;
            }

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            if (!TryGetPlayerSpawn(worldMap, profile, out var spawnPosition, out var spawnRotation))
            {
                return false;
            }

            playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            return true;
        }

        public static bool TryGetPlayerSpawn(
            WorldMapConfig worldMap,
            FlightProfile profile,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            var carrier = FindCarrierBase();
            if (carrier == null)
            {
                return false;
            }

            worldPosition = carrier.transform.position;
            worldRotation = ResolveSpawnRotation(carrier.transform.position);
            return true;
        }

        public static Vector2 GetLockedCarrierPositionMiles()
        {
            return AntarcticaWorldLocations.CarrierPositionMiles;
        }

        public static AntarcticaBase FindCarrierBase()
        {
            AntarcticaBase carrier = null;
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Carrier)
                {
                    continue;
                }

                if (carrier != null && carrier != baseSite)
                {
                    Object.Destroy(baseSite.gameObject);
                    continue;
                }

                carrier = baseSite;
            }

            return carrier;
        }

        public static AntarcticaBase FindPrimaryMissionObjective()
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite != null && baseSite.IsMissionObjective)
                {
                    return baseSite;
                }
            }

            return null;
        }

        private static void EnsureCarrierAtLockedPosition(float worldUnitsPerMile, Vector2 lockedCarrierMiles)
        {
            var carrier = FindCarrierBase();
            if (carrier == null)
            {
                return;
            }

            var expectedWorld = MilesToWorld(lockedCarrierMiles, worldUnitsPerMile);
            var currentMiles = WorldToMiles(carrier.transform.position, worldUnitsPerMile);
            if (Vector2.Distance(currentMiles, lockedCarrierMiles) <= CarrierPositionToleranceMiles)
            {
                return;
            }

            carrier.transform.position = expectedWorld;
            carrier.SetPositionMiles(lockedCarrierMiles, worldUnitsPerMile);

            var root = GameObject.Find(RootName);
            var marker = root != null ? root.GetComponent<AntarcticaBasesRootMarker>() : null;
            if (marker != null)
            {
                marker.carrierPositionMiles = lockedCarrierMiles;
            }

            Debug.Log(
                $"F-89: Moved carrier to locked position ({lockedCarrierMiles.x:0}, {lockedCarrierMiles.y:0}) MI.");
        }

        private static Vector2 ResolveCarrierPositionMiles(Vector2 seedMiles, float mapSizeMiles)
        {
            var resolvedMiles = seedMiles;
            if (AntarcticaLandMask.TryResolveVisibleOceanNorthOf(
                    seedMiles,
                    mapSizeMiles,
                    CarrierOceanSearchNorthMiles,
                    CarrierOceanSearchEastWestMiles,
                    out var oceanMiles))
            {
                resolvedMiles = oceanMiles;
            }
            else if (!AntarcticaLandMask.IsFlightOceanMiles(seedMiles, mapSizeMiles))
            {
                Debug.LogWarning(
                    $"F-89: Could not georef carrier at ({seedMiles.x:0}, {seedMiles.y:0}) MI onto flight ocean.");
            }

            resolvedMiles.y -= CarrierNorthOffsetMiles;
            if (!AntarcticaLandMask.IsFlightOceanMiles(resolvedMiles, mapSizeMiles)
                && !AntarcticaLandMask.TryFindNearestFlightOcean(
                    resolvedMiles,
                    mapSizeMiles,
                    CarrierOceanSearchEastWestMiles,
                    out resolvedMiles))
            {
                Debug.LogWarning(
                    $"F-89: Carrier offset landed on flight ice near ({resolvedMiles.x:0}, {resolvedMiles.y:0}) MI.");
            }

            var landBlend = AntarcticaLandMask.GetDisplayLandBlendMiles(resolvedMiles, mapSizeMiles);
            Debug.Log(
                $"F-89: Carrier georef at ({resolvedMiles.x:0}, {resolvedMiles.y:0}) MI " +
                $"(flight land blend {landBlend:0.00}, seed ({seedMiles.x:0}, {seedMiles.y:0}) MI).");
            return resolvedMiles;
        }

        private static void SpawnCarrier(
            Transform parent,
            AntarcticaMissionConfig mission,
            Vector2 positionMiles,
            float worldUnitsPerMile)
        {
            var carrierObject = new GameObject(mission.carrierName);
            carrierObject.transform.SetParent(parent, false);
            var carrier = carrierObject.AddComponent<AntarcticaBase>();
            carrier.Configure(
                mission != null ? mission.carrierName : AntarcticaWorldLocations.CarrierName,
                BaseControl.Friendly,
                positionMiles,
                worldUnitsPerMile,
                true,
                BaseSiteKind.Carrier);
            CarrierWorldVisual.Attach(carrierObject, worldUnitsPerMile);
            EnsureBaseLockableTarget(carrierObject, carrier.BaseName, BaseControl.Friendly);
        }

        private static void EnsureBaseLockableTarget(
            GameObject baseObject,
            string label,
            BaseControl control)
        {
            var lockable = baseObject.GetComponent<LockableTarget>();
            if (lockable == null)
            {
                lockable = baseObject.AddComponent<LockableTarget>();
            }

            var affiliation = control == BaseControl.Friendly
                ? TargetAffiliation.Friendly
                : TargetAffiliation.Hostile;
            lockable.Configure(label, LockableTargetKind.Ground, affiliation);
        }

        private static void MarkMissionObjective(Transform root, string objectiveBaseName)
        {
            if (string.IsNullOrWhiteSpace(objectiveBaseName))
            {
                return;
            }

            var bases = root.GetComponentsInChildren<AntarcticaBase>(true);
            foreach (var baseSite in bases)
            {
                if (baseSite != null && baseSite.BaseName == objectiveBaseName)
                {
                    baseSite.SetMissionObjective(true);
                    return;
                }
            }

            Debug.LogWarning($"F-89: Mission objective base not found: {objectiveBaseName}");
        }

        private static Quaternion ResolveSpawnRotation(Vector3 carrierPosition)
        {
            var objective = FindPrimaryMissionObjective();
            if (objective == null)
            {
                return Quaternion.identity;
            }

            var direction = objective.transform.position - carrierPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void SpawnCarrierSouthBase(
            Transform parent,
            Vector2 carrierPositionMiles,
            float mapSizeMiles,
            float worldUnitsPerMile)
        {
            var seedMiles = carrierPositionMiles + CarrierSouthOffsetFromCarrierMiles;
            var resolvedMiles = ResolveCarrierSouthBasePosition(seedMiles, mapSizeMiles);
            SpawnBase(
                parent,
                Entry(CarrierSouthBaseName, resolvedMiles, BaseControl.Hostile),
                worldUnitsPerMile);
        }

        private static Vector2 ResolveCarrierSouthBasePosition(Vector2 seedMiles, float mapSizeMiles)
        {
            if (AntarcticaLandMask.TryFindNearestCoastalDisplayLand(
                    seedMiles,
                    mapSizeMiles,
                    CarrierSouthSearchSouthMiles,
                    CarrierSouthSearchNorthMiles,
                    CarrierSouthSearchEastMiles,
                    CarrierSouthSearchWestMiles,
                    out var southLandMiles))
            {
                Debug.Log(
                    $"F-89: Outpost South placed at ({southLandMiles.x:0}, {southLandMiles.y:0}) MI " +
                    $"(seed ({seedMiles.x:0}, {seedMiles.y:0}) MI).");
                return southLandMiles;
            }

            if (AntarcticaLandMask.TryFindDisplayLandSouthOf(
                    seedMiles,
                    mapSizeMiles,
                    AntarcticaLandMask.BasePlacementInsetMiles,
                    CarrierSouthSearchSouthMiles,
                    CarrierSouthSearchEastMiles,
                    out southLandMiles))
            {
                Debug.Log(
                    $"F-89: Outpost South placed at ({southLandMiles.x:0}, {southLandMiles.y:0}) MI " +
                    $"(seed ({seedMiles.x:0}, {seedMiles.y:0}) MI).");
                return southLandMiles;
            }

            return ResolveLandBasePosition(seedMiles, mapSizeMiles);
        }

        private static void SpawnAnchorRelativeBases(
            Transform parent,
            float mapSizeMiles,
            float worldUnitsPerMile)
        {
            SpawnRelativeBase(
                parent,
                CarrierSouthBaseName,
                Outpost01OffsetFromSouthMiles,
                Outpost01BaseName,
                mapSizeMiles,
                worldUnitsPerMile);

            var outpost13Anchor = FindBaseByName(parent, Outpost13BaseName);
            if (outpost13Anchor != null)
            {
                RenameBaseSite(outpost13Anchor, Outpost13SeedBaseName);
            }

            SpawnRelativeBase(
                parent,
                Outpost13SeedBaseName,
                Outpost13OffsetFromSeedMiles,
                Outpost13BaseName,
                mapSizeMiles,
                worldUnitsPerMile);
        }

        private static void SpawnRelativeBase(
            Transform parent,
            string anchorBaseName,
            Vector2 offsetMiles,
            string newBaseName,
            float mapSizeMiles,
            float worldUnitsPerMile)
        {
            var anchor = FindBaseByName(parent, anchorBaseName);
            if (anchor == null)
            {
                Debug.LogWarning(
                    $"F-89: Could not place {newBaseName} — anchor {anchorBaseName} not found.");
                return;
            }

            var seedMiles = anchor.PositionMiles + offsetMiles;
            var resolvedMiles = ResolveAnchorRelativeBasePosition(
                anchor.PositionMiles,
                offsetMiles,
                mapSizeMiles);
            SpawnBase(
                parent,
                Entry(newBaseName, resolvedMiles, BaseControl.Hostile),
                worldUnitsPerMile);
            Debug.Log(
                $"F-89: {newBaseName} placed at ({resolvedMiles.x:0}, {resolvedMiles.y:0}) MI " +
                $"(anchor {anchorBaseName} at ({anchor.PositionMiles.x:0}, {anchor.PositionMiles.y:0}) MI, " +
                $"offset ({offsetMiles.x:0}, {offsetMiles.y:0}) MI).");
        }

        private static AntarcticaBase FindBaseByName(Transform parent, string baseName)
        {
            var bases = parent.GetComponentsInChildren<AntarcticaBase>(true);
            foreach (var baseSite in bases)
            {
                if (baseSite != null && baseSite.BaseName == baseName)
                {
                    return baseSite;
                }
            }

            return null;
        }

        private static void RenameBaseSite(AntarcticaBase baseSite, string newName)
        {
            if (baseSite == null || string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            baseSite.SetBaseName(newName);
            var lockable = baseSite.GetComponent<LockableTarget>();
            if (lockable != null)
            {
                var affiliation = baseSite.Control == BaseControl.Friendly
                    ? TargetAffiliation.Friendly
                    : TargetAffiliation.Hostile;
                lockable.Configure(newName, LockableTargetKind.Ground, affiliation);
            }
        }

        private static void SpawnBase(
            Transform parent,
            AntarcticaBaseCatalog.BaseDefinition definition,
            float worldUnitsPerMile)
        {
            var baseObject = new GameObject(definition.baseName);
            baseObject.transform.SetParent(parent, false);
            var baseSite = baseObject.AddComponent<AntarcticaBase>();
            baseSite.Configure(definition.baseName, definition.control, definition.positionMiles, worldUnitsPerMile, true);
            EnsureBaseLockableTarget(baseObject, definition.baseName, definition.control);
        }

        private static Vector3 MilesToWorld(Vector2 miles, float worldUnitsPerMile)
        {
            return WorldMapConfig.MileOffsetToWorld(miles, worldUnitsPerMile);
        }

        private static Vector2 WorldToMiles(Vector3 worldPosition, float worldUnitsPerMile)
        {
            return WorldMapConfig.WorldToMileOffset(worldPosition, worldUnitsPerMile);
        }

        private static float ResolveWorldUnitsPerMile(WorldMapConfig worldMap, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap == null)
            {
                return 20f * ticSize;
            }

            return worldMap.GridSpacingTics * ticSize / worldMap.milesPerGrid;
        }

        private static AntarcticaBaseCatalog CreateRuntimeDefaults()
        {
            var catalog = ScriptableObject.CreateInstance<AntarcticaBaseCatalog>();
            catalog.bases = new[]
            {
                Entry("McMurdo Station", new Vector2(420f, -180f), BaseControl.Hostile),
                Entry("Amundsen-Scott South Pole", new Vector2(40f, -60f), BaseControl.Hostile),
                Entry("Palmer Station", new Vector2(-520f, 280f), BaseControl.Hostile),
                Entry("Vostok Station", new Vector2(180f, 220f), BaseControl.Hostile),
                Entry("Concordia Station", new Vector2(-120f, 140f), BaseControl.Hostile),
                Entry("Halley VI", new Vector2(-280f, -120f), BaseControl.Hostile),
                Entry("Rothera Research", new Vector2(-460f, 120f), BaseControl.Hostile),
                Entry("Neumayer III", new Vector2(-340f, -40f), BaseControl.Hostile),
                Entry("Casey Station", new Vector2(620f, -80f), BaseControl.Hostile),
                Entry("Davis Station", new Vector2(580f, 40f), BaseControl.Hostile),
                Entry("Marambio Base", new Vector2(-380f, 320f), BaseControl.Hostile),
                Entry("Zhongshan Station", new Vector2(500f, 160f), BaseControl.Hostile)
            };
            return catalog;
        }

        private static AntarcticaBaseCatalog.BaseDefinition Entry(
            string name,
            Vector2 positionMiles,
            BaseControl control)
        {
            return new AntarcticaBaseCatalog.BaseDefinition
            {
                baseName = name,
                positionMiles = positionMiles,
                control = control,
                startsActive = true
            };
        }

        private static AntarcticaBaseCatalog.BaseDefinition ResolveDefinitionPosition(
            AntarcticaBaseCatalog.BaseDefinition definition,
            Vector2 positionMiles)
        {
            return new AntarcticaBaseCatalog.BaseDefinition
            {
                baseName = definition.baseName,
                positionMiles = positionMiles,
                control = definition.control,
                startsActive = definition.startsActive
            };
        }

        private static Vector2 ResolveAnchorRelativeBasePosition(
            Vector2 anchorMiles,
            Vector2 offsetMiles,
            float mapSizeMiles)
        {
            var seedMiles = anchorMiles + offsetMiles;
            if (AntarcticaLandMask.IsValidBasePlacementMiles(seedMiles, mapSizeMiles))
            {
                return seedMiles;
            }

            var desiredDirection = offsetMiles.sqrMagnitude > 0.01f
                ? offsetMiles.normalized
                : Vector2.down;
            var targetDistance = offsetMiles.magnitude;
            var minDistance = Mathf.Max(10f, targetDistance - AnchorRelativeBaseSnapSearchMiles);
            var maxDistance = targetDistance + AnchorRelativeBaseSnapSearchMiles;

            for (var distance = minDistance; distance <= maxDistance; distance += 5f)
            {
                var candidate = anchorMiles + desiredDirection * distance;
                if (AntarcticaLandMask.IsValidBasePlacementMiles(candidate, mapSizeMiles))
                {
                    return candidate;
                }
            }

            if (AntarcticaLandMask.TryFindNearestDisplayLand(
                    seedMiles,
                    mapSizeMiles,
                    AntarcticaLandMask.BasePlacementInsetMiles,
                    AnchorRelativeBaseSnapSearchMiles,
                    out var snappedMiles))
            {
                return snappedMiles;
            }

            Debug.LogWarning(
                $"F-89: Could not snap anchor-relative base near ({seedMiles.x:0}, {seedMiles.y:0}) MI " +
                $"onto inland ice within {AnchorRelativeBaseSnapSearchMiles:0} MI.");
            return seedMiles;
        }

        /// <summary>
        /// Map mile +X = east, mile +Y = visual south. Bearing is compass degrees clockwise from north.
        /// </summary>
        private static Vector2 CompassOffsetMiles(float distanceMiles, float bearingDegreesClockwiseFromNorth)
        {
            var radians = bearingDegreesClockwiseFromNorth * Mathf.Deg2Rad;
            return new Vector2(
                distanceMiles * Mathf.Sin(radians),
                -distanceMiles * Mathf.Cos(radians));
        }

        private static Vector2 ResolveLandBasePosition(Vector2 positionMiles, float mapSizeMiles)
        {
            if (AntarcticaLandMask.IsValidBasePlacementMiles(positionMiles, mapSizeMiles))
            {
                return positionMiles;
            }

            if (AntarcticaLandMask.TryFindNearestDisplayLand(
                    positionMiles,
                    mapSizeMiles,
                    AntarcticaLandMask.BasePlacementInsetMiles,
                    SolidIceSnapSearchMiles,
                    out var inlandMiles))
            {
                return inlandMiles;
            }

            Debug.LogWarning(
                $"F-89: Could not snap base at ({positionMiles.x:0}, {positionMiles.y:0}) MI onto inland ice.");
            return positionMiles;
        }
    }
}
