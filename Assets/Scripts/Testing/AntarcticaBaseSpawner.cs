using System.Collections.Generic;
using F89.Core;
using F89.Enemies;
using F89.Flight;
using F89.LandCombat;
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
        private const int BasesLayoutVersion = 62;
        private const float FixedRelativeBaseOffsetMiles = 200f;
        private const float AnchorRelativeBaseSnapSearchMiles = 45f;
        private const float SolidIceSnapSearchMiles = AntarcticaLandMask.BasePlacementSnapSearchMiles;
        private const float CarrierPositionToleranceMiles = 1f;
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

            if (LandCombatTestCheats.ResetDestroyedOutpostsOnDevEntry
                && !CampaignWorldReset.ShouldPreserveActiveSortieSpawn())
            {
                CampaignWorldReset.ResetMapForFreshPlay();
            }

            AntarcticaOutpostState.ApplyFriendlyControlToAllBases();

            var existing = GameObject.Find(RootName);
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            if (existing != null)
            {
                if (ShouldReuseExistingBases(existing))
                {
                    var marker = existing.GetComponent<AntarcticaBasesRootMarker>();
                    var lockedCarrierMiles = ResolveLockedCarrierMiles(marker);
                    EnsureSingleCarrierAtLockedSite(worldUnitsPerMile, lockedCarrierMiles);
                    ApplyLockedCampaignLayout(existing.transform, worldUnitsPerMile);
                    SyncAllLandBaseWorldState(worldUnitsPerMile);
                    EnsureOutpostBuildingClusters(worldUnitsPerMile, profile);
                    AntarcticaOutpostState.ApplyFriendlyControlToAllBases();
                    LogCarrierReadyState();
                    LogOutpostSiteIdSystem();
                    EnsureActiveCampaignMissionPlatoon();
                    return;
                }

                Object.DestroyImmediate(existing);
            }

            var mission = AntarcticaMissionConfig.LoadOrDefault();

            var root = new GameObject(RootName);
            var layoutMarker = root.AddComponent<AntarcticaBasesRootMarker>();
            layoutMarker.layoutVersion = BasesLayoutVersion;

            var carrierPositionMiles = ResolveLockedCarrierMiles(layoutMarker);
            layoutMarker.carrierPositionMiles = carrierPositionMiles;

            SpawnCarrier(root.transform, mission, carrierPositionMiles, worldUnitsPerMile);
            SpawnCampaignLayoutOutposts(root.transform, worldUnitsPerMile);
            MarkMissionObjective(root.transform, mission.firstObjectiveBaseName);
            EnsureSingleCarrierAtLockedSite(worldUnitsPerMile, carrierPositionMiles);
            SyncAllLandBaseWorldState(worldUnitsPerMile);
            EnsureOutpostBuildingClusters(worldUnitsPerMile, profile);
            AntarcticaOutpostState.ApplyFriendlyControlToAllBases();
            LogCarrierReadyState();
            LogOutpostSiteIdSystem();
            EnsureActiveCampaignMissionPlatoon();
        }

        public static void EnsureMissionPlatoonsIfNeeded(AircraftController player)
        {
            EnsureActiveCampaignMissionPlatoon(player);
        }

        private static void EnsureActiveCampaignMissionPlatoon(AircraftController preferredPlayer = null)
        {
            if (FlightGroundReturnService.ShouldSkipCarrierSpawn()
                || LandMissionHandoffState.IsRunwayDeckSortie())
            {
                return;
            }

            if (!GamePlayModeState.IsCampaign)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null
                || !CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var siteCode))
            {
                return;
            }

            var player = preferredPlayer ?? Object.FindAnyObjectByType<AircraftController>();
            if (player == null)
            {
                return;
            }

            if (CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                CampaignWaypointVehicleSpawner.EnsureActiveWaypointPlatoon(player, siteCode);
                return;
            }

            // Past outpost missions that are already cleared/friendly must never refill.
            if (CampaignMissionObjectiveState.TryResolveOutpostBaseName(siteCode, out var baseName)
                && (AntarcticaOutpostState.IsFriendlyOccupied(baseName)
                    || OutpostPrimaryObjective.AreAirObjectivesDestroyed(baseName)))
            {
                var outpost = FindLandOutpostBySiteCode(siteCode);
                if (outpost != null)
                {
                    OutpostVehicleSpawner.EnsureEmptyPlatoonMarker(outpost);
                }

                return;
            }

            OutpostVehicleSpawner.EnsureSiteEnemyPlatoon(siteCode, player);
        }

        private static AntarcticaBase FindLandOutpostBySiteCode(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return null;
            }

            var outposts = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < outposts.Length; i++)
            {
                var outpost = outposts[i];
                if (outpost != null
                    && outpost.SiteKind == BaseSiteKind.Land
                    && string.Equals(outpost.SiteCode, siteCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    return outpost;
                }
            }

            return null;
        }

        private static void EnsureOutpostBuildingClusters(float worldUnitsPerMile, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            OutpostBuildingClusterSpawner.EnsureAllLandOutpostClusters(worldUnitsPerMile, ticSize);
        }

        private static void SpawnCampaignLayoutOutposts(Transform parent, float worldUnitsPerMile)
        {
            CampaignMapLayoutState.EnsureLoaded();
            foreach (var site in CampaignMapLayoutState.Markers)
            {
                if (site == null || string.IsNullOrWhiteSpace(site.Label))
                {
                    continue;
                }

                SpawnBase(
                    parent,
                    Entry(site.Label, CampaignMapLayoutState.GetLockedMiles(site), BaseControl.Hostile),
                    worldUnitsPerMile);
            }
        }

        private static void EnsureCarrierSpawned(Transform root, float worldUnitsPerMile)
        {
            var lockedCarrierMiles = AntarcticaWorldLocations.LockedCarrierPositionMiles;
            var marker = root.GetComponent<AntarcticaBasesRootMarker>();
            if (marker != null)
            {
                marker.carrierPositionMiles = lockedCarrierMiles;
            }

            EnsureSingleCarrierAtLockedSite(worldUnitsPerMile, lockedCarrierMiles);
        }

        private static void ApplyLockedCampaignLayout(Transform parent, float worldUnitsPerMile)
        {
            CampaignMapLayoutState.EnsureLoaded();
            var layoutNames = new HashSet<string>();
            foreach (var site in CampaignMapLayoutState.Markers)
            {
                if (site == null || string.IsNullOrWhiteSpace(site.Label))
                {
                    continue;
                }

                layoutNames.Add(site.Label);
            }

            var bases = parent.GetComponentsInChildren<AntarcticaBase>(true);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    continue;
                }

                var siteName = CampaignMapLayoutState.NormalizeSiteName(baseSite.BaseName);
                if (!layoutNames.Contains(siteName)
                    || !CampaignMapLayoutState.TryGetSite(siteName, out var site))
                {
                    Object.Destroy(baseSite.gameObject);
                    continue;
                }

                baseSite.SetBaseName(siteName);
                baseSite.SetSiteCode(site.SiteCode);
                baseSite.SetPositionMiles(CampaignMapLayoutState.GetLockedMiles(site), worldUnitsPerMile);
            }

            var existingNames = new HashSet<string>();
            foreach (var baseSite in parent.GetComponentsInChildren<AntarcticaBase>(true))
            {
                if (baseSite == null || baseSite.SiteKind == BaseSiteKind.Carrier)
                {
                    continue;
                }

                existingNames.Add(CampaignMapLayoutState.NormalizeSiteName(baseSite.BaseName));
            }

            foreach (var site in CampaignMapLayoutState.Markers)
            {
                if (site == null || string.IsNullOrWhiteSpace(site.Label) || existingNames.Contains(site.Label))
                {
                    continue;
                }

                SpawnBase(
                    parent,
                    Entry(site.Label, CampaignMapLayoutState.GetLockedMiles(site), BaseControl.Hostile),
                    worldUnitsPerMile);
            }
        }

        private static void LogCarrierReadyState()
        {
            var carrier = FindCarrierBase();
            if (carrier == null)
            {
                Debug.LogError("F-89: Carrier failed to spawn.");
                return;
            }

            Debug.Log(
                $"F-89: Carrier ready at ({carrier.PositionMiles.x:0}, {carrier.PositionMiles.y:0}) MI.");
        }

        private static void LogOutpostSiteIdSystem()
        {
            CampaignMapLayoutState.EnsureLoaded();
            var count = CampaignMapLayoutState.Markers.Count;
            if (CampaignMapLayoutState.TryGetSiteByCode("OP-SOUTH", out var south))
            {
                Debug.Log(
                    $"F-89: Outpost IDs active ({count} sites). Example: {OutpostSiteIds.FormatIdentity(south)}");
                return;
            }

            Debug.Log($"F-89: Outpost IDs active ({count} sites). Map labels show SiteCode (OP-01, STN-PALMER, …).");
        }

        private static bool ShouldReuseExistingBases(GameObject root)
        {
            var marker = root.GetComponent<AntarcticaBasesRootMarker>();
            if (marker == null || marker.layoutVersion != BasesLayoutVersion)
            {
                return false;
            }

            return FindCarrierBase() != null;
        }

        private static void SyncAllLandBaseWorldState(float worldUnitsPerMile)
        {
            RefreshAllOutpostWorldState(worldUnitsPerMile);
        }

        public static void RefreshAllOutpostWorldState(float worldUnitsPerMile)
        {
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null)
                {
                    continue;
                }

                baseSite.RefreshPersistedWorldState(worldUnitsPerMile);
                if (baseSite.SiteKind == BaseSiteKind.Land)
                {
                    EnsureBaseLockableTarget(
                        baseSite.gameObject,
                        baseSite.BaseName,
                        baseSite.Control,
                        BaseSiteKind.Land);
                }
            }
        }

        public static bool TryMovePlayerToOutpostRunway(Transform playerTransform, string outpostName)
        {
            if (playerTransform == null
                || !OutpostRunwayLanding.TryGetRunwaySpawn(outpostName, out var spawnPosition, out var spawnRotation))
            {
                return false;
            }

            playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            return true;
        }

        public static bool TryMovePlayerToCarrier(
            Transform playerTransform,
            WorldMapConfig worldMap = null,
            FlightProfile profile = null)
        {
            if (playerTransform == null)
            {
                return false;
            }

            worldMap ??= Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            profile ??= Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            if (!TryResolveCarrierMiles(out var carrierMiles))
            {
                return false;
            }

            Vector3 spawnPosition;
            Quaternion spawnRotation;
            var carrier = FindCarrierBase();
            if (carrier != null)
            {
                carrier.SetPositionMiles(carrierMiles, worldUnitsPerMile);
                spawnPosition = carrier.transform.position;
                spawnRotation = ResolveSpawnRotation(spawnPosition);
            }
            else
            {
                spawnPosition = MilesToWorld(carrierMiles, worldMap, profile);
                spawnRotation = ResolveSpawnRotation(spawnPosition);
            }

            playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            return true;
        }

        public static bool IsNearCarrierDeck(
            Transform playerTransform,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            if (playerTransform == null)
            {
                return false;
            }

            worldMap ??= Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            profile ??= Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            return AntarcticaWorldLocations.IsWithinCarrierDeckRange(
                playerTransform.position,
                worldMap,
                ticSize,
                F89.Flight.AircraftLanding.CarrierLandingRangeMiles);
        }

        public static bool ForcePlayerToDefaultCarrier(
            Transform playerTransform,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            if (playerTransform == null)
            {
                return false;
            }

            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            if (worldUnitsPerMile <= 0f)
            {
                return false;
            }

            if (!TryResolveCarrierMiles(out var carrierMiles))
            {
                carrierMiles = AntarcticaWorldLocations.LockedCarrierPositionMiles;
            }

            Vector3 spawnPosition;
            Quaternion spawnRotation;
            var carrier = FindCarrierBase();
            if (carrier != null)
            {
                carrier.SetPositionMiles(carrierMiles, worldUnitsPerMile);
                spawnPosition = carrier.transform.position;
            }
            else
            {
                spawnPosition = MilesToWorld(carrierMiles, worldMap, profile);
            }

            spawnRotation = ResolveSpawnRotation(spawnPosition);
            playerTransform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            Debug.LogWarning(
                $"F-89: Forced player spawn to carrier miles ({carrierMiles.x:0}, {carrierMiles.y:0}).");
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

            if (FlightGroundReturnService.TryGetPendingReturnSpawn(out worldPosition, out worldRotation))
            {
                return true;
            }

            if (LandingMileFlagState.HasActiveFlag
                && LandingMileFlagState.TryResolveWorldPosition(out worldPosition, out worldRotation))
            {
                return true;
            }

            var pendingOutpost = FlightMissionLaunchState.LaunchFromOutpostName;
            if (!string.IsNullOrWhiteSpace(pendingOutpost)
                && TryGetOutpostSpawn(worldMap, profile, pendingOutpost, out worldPosition, out worldRotation))
            {
                return true;
            }

            if (MissionLaunchOrigin.TryResolveLaunchOutpost(CharacterSessionState.ActiveSave, out var savedOutpost)
                && TryGetOutpostSpawn(worldMap, profile, savedOutpost, out worldPosition, out worldRotation))
            {
                return true;
            }

            // Ocean CV deck spawn is handled only by FlightMissionStartBootstrap for explicit carrier sorties.
            return false;
        }

        private static bool TryGetOutpostSpawn(
            WorldMapConfig worldMap,
            FlightProfile profile,
            string outpostName,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;
            if (string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (OutpostRunwayLanding.TryGetRunwaySpawn(outpostName, out worldPosition, out worldRotation)
                || OutpostRunwayLanding.TryGetLayoutMilesSpawn(outpostName, out worldPosition, out worldRotation))
            {
                worldPosition.y = 0f;
                return true;
            }

            _ = worldMap;
            _ = profile;
            return false;
        }

        public static Vector2 GetLockedCarrierPositionMiles()
        {
            return AntarcticaWorldLocations.LockedCarrierPositionMiles;
        }

        public static AntarcticaBase FindCarrierBase()
        {
            PurgeStrayCarriers();
            AntarcticaBase carrier = null;
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            foreach (var baseSite in bases)
            {
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Carrier)
                {
                    continue;
                }

                if (carrier == null)
                {
                    carrier = baseSite;
                    continue;
                }

                var candidateOnRoot = IsUnderAntarcticaBasesRoot(baseSite);
                var currentOnRoot = IsUnderAntarcticaBasesRoot(carrier);
                if (candidateOnRoot && !currentOnRoot)
                {
                    Object.Destroy(carrier.gameObject);
                    carrier = baseSite;
                    continue;
                }

                if (!candidateOnRoot && currentOnRoot)
                {
                    Object.Destroy(carrier.gameObject);
                    carrier = baseSite;
                    continue;
                }

                Object.Destroy(baseSite.gameObject);
            }

            return carrier;
        }

        private static bool IsUnderAntarcticaBasesRoot(AntarcticaBase baseSite)
        {
            return baseSite != null && baseSite.transform.root.name == RootName;
        }

        private static Vector2 ResolveLockedCarrierMiles(AntarcticaBasesRootMarker marker)
        {
            var locked = AntarcticaWorldLocations.LockedCarrierPositionMiles;
            if (marker != null)
            {
                marker.carrierPositionMiles = locked;
            }

            return locked;
        }

        private static bool TryResolveCarrierMiles(out Vector2 carrierMiles)
        {
            carrierMiles = AntarcticaWorldLocations.LockedCarrierPositionMiles;
            return true;
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

        private static void EnsureSingleCarrierAtLockedSite(float worldUnitsPerMile, Vector2 lockedCarrierMiles)
        {
            PurgeStrayCarriers();

            var root = GameObject.Find(RootName);
            var mission = AntarcticaMissionConfig.LoadOrDefault();
            var carrier = FindCarrierBase();
            if (carrier == null && root != null)
            {
                SpawnCarrier(root.transform, mission, lockedCarrierMiles, worldUnitsPerMile);
                carrier = FindCarrierBase();
            }

            if (carrier == null)
            {
                return;
            }

            carrier.SetPositionMiles(lockedCarrierMiles, worldUnitsPerMile);
            CarrierWorldVisual.Attach(carrier.gameObject, worldUnitsPerMile);

            var marker = root != null ? root.GetComponent<AntarcticaBasesRootMarker>() : null;
            if (marker != null)
            {
                marker.carrierPositionMiles = lockedCarrierMiles;
            }
        }

        private static void PurgeStrayCarriers()
        {
            var locked = AntarcticaWorldLocations.LockedCarrierPositionMiles;
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Carrier)
                {
                    continue;
                }

                if (Vector2.Distance(baseSite.PositionMiles, locked) > CarrierPositionToleranceMiles)
                {
                    Debug.LogWarning(
                        $"F-89: Removing stray carrier at ({baseSite.PositionMiles.x:0}, {baseSite.PositionMiles.y:0}) MI.");
                    Object.Destroy(baseSite.gameObject);
                }
            }
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
            Debug.Log(
                $"F-89: Carrier spawned at ({positionMiles.x:0}, {positionMiles.y:0}) MI.");
            EnsureBaseLockableTarget(carrierObject, carrier.BaseName, BaseControl.Friendly, BaseSiteKind.Carrier);
        }

        private static void EnsureBaseLockableTarget(
            GameObject baseObject,
            string label,
            BaseControl control,
            BaseSiteKind siteKind)
        {
            var lockable = baseObject.GetComponent<LockableTarget>();
            if (lockable == null)
            {
                lockable = baseObject.AddComponent<LockableTarget>();
            }

            var affiliation = siteKind == BaseSiteKind.Land
                ? TargetAffiliation.Neutral
                : control == BaseControl.Friendly
                    ? TargetAffiliation.Friendly
                    : TargetAffiliation.Hostile;
            lockable.Configure(label, LockableTargetKind.Ground, affiliation);
            var baseSite = baseObject.GetComponent<AntarcticaBase>();
            if (baseSite != null && !baseSite.IsDestroyed)
            {
                lockable.RestoreTargeting();
            }
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
                var affiliation = baseSite.SiteKind == BaseSiteKind.Land
                    ? TargetAffiliation.Neutral
                    : baseSite.Control == BaseControl.Friendly
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
            var baseSite = baseObject.AddComponent<Outpost>();
            var siteCode = CampaignMapLayoutState.TryGetSite(definition.baseName, out var site)
                ? site.SiteCode
                : OutpostSiteIds.FromLabel(definition.baseName);
            baseSite.Configure(
                definition.baseName,
                definition.control,
                definition.positionMiles,
                worldUnitsPerMile,
                true,
                BaseSiteKind.Land,
                false,
                siteCode);
            EnsureBaseLockableTarget(baseObject, definition.baseName, definition.control, BaseSiteKind.Land);
        }

        private static Vector3 MilesToWorld(Vector2 miles, WorldMapConfig worldMap, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            return CampaignMapCoordinates.MilesToWorld(miles, worldMap, ticSize);
        }

        private static Vector2 WorldToMiles(Vector3 worldPosition, WorldMapConfig worldMap, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            return CampaignMapCoordinates.WorldToMiles(worldPosition, worldMap, ticSize);
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
