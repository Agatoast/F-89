using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Spawns building clusters and UR vehicle platoons at outposts by SiteCode.
    /// </summary>
    public static class OutpostVehicleSpawner
    {
        public const string OpSouthSiteCode = "OP-SOUTH";

        private const string PlatoonRootName = "VehiclePlatoon";
        private const float SpawnMinRadiusMiles = 0.38f;
        private const float SpawnMaxRadiusMiles = 0.95f;

        public static void EnsureOpSouthEnemyPlatoon(AircraftController player)
        {
            EnsureSiteEnemyPlatoon(OpSouthSiteCode, player);
        }

        /// <summary>
        /// Spawns at most one UR platoon per call — the nearest hostile outpost in range that lacks a platoon.
        /// </summary>
        public static void EnsureNearestHostilePlatoonWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            AircraftController player)
        {
            if (player == null || worldMap == null || rangeMiles <= 0f)
            {
                return;
            }

            var outposts = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            AntarcticaBase nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < outposts.Length; i++)
            {
                var outpost = outposts[i];
                if (outpost == null
                    || !outpost.IsActive
                    || outpost.IsDestroyed
                    || outpost.SiteKind != BaseSiteKind.Land
                    || outpost.Control != BaseControl.Hostile
                    || string.IsNullOrWhiteSpace(outpost.SiteCode))
                {
                    continue;
                }

                var platoon = outpost.transform.Find(PlatoonRootName);
                if (OutpostFlightPlatoonState.ShouldSkipPlatoonRespawn(outpost))
                {
                    if (platoon == null)
                    {
                        EnsureEmptyPlatoonMarker(outpost);
                    }

                    continue;
                }

                if (platoon != null)
                {
                    continue;
                }

                var distanceMiles = CombatThreatRange.DistanceMiles(
                    worldPosition,
                    outpost.transform.position,
                    worldMap,
                    ticSizeWorldUnits);
                if (distanceMiles > rangeMiles || distanceMiles >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distanceMiles;
                nearest = outpost;
            }

            if (nearest != null)
            {
                EnsureSiteEnemyPlatoon(nearest.SiteCode, player);
            }
        }

        public static void EnsureSiteEnemyPlatoon(string siteCode, AircraftController player)
        {
            var worldMap = player != null ? player.WorldMap : Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = player != null ? player.Profile : Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;

            var outpost = FindOutpostBySiteCode(siteCode);
            if (outpost == null)
            {
                Debug.LogWarning($"F-89: No outpost found for SiteCode {siteCode} — vehicle platoon skipped.");
                LogLandOutpostDiagnostics();
                return;
            }

            if (outpost.Control != BaseControl.Hostile
                || AntarcticaOutpostState.IsFriendlyOccupied(outpost.BaseName))
            {
                EnsureEmptyPlatoonMarker(outpost);
                return;
            }

            if (OutpostFlightPlatoonState.ShouldSkipPlatoonRespawn(outpost))
            {
                EnsureEmptyPlatoonMarker(outpost);
                return;
            }

            if (CampaignMapLayoutState.TryGetSiteByCode(siteCode, out var layoutSite))
            {
                var lockedMiles = CampaignMapLayoutState.GetLockedMiles(layoutSite);
                var milesDelta = outpost.PositionMiles - lockedMiles;
                if (milesDelta.sqrMagnitude > 0.25f)
                {
                    Debug.LogWarning(
                        $"F-89: {outpost.SiteCode} ({outpost.BaseName}) world miles "
                        + $"({outpost.PositionMiles.x:0.0}, {outpost.PositionMiles.y:0.0}) differ from "
                        + $"CampaignMapLayout ({lockedMiles.x:0.0}, {lockedMiles.y:0.0}). Resyncing.");
                    outpost.SetPositionMiles(lockedMiles, worldUnitsPerMile);
                }
            }

            OutpostBuildingClusterSpawner.EnsureCluster(outpost, worldUnitsPerMile, ticSize);

            var existingPlatoon = outpost.transform.Find(PlatoonRootName);
            if (existingPlatoon != null)
            {
                if (HasLivingPlatoonUnits(existingPlatoon))
                {
                    return;
                }

                if (AreAllPlatoonSlotsDestroyed(outpost))
                {
                    return;
                }

                MarkAllPlatoonSlotsDestroyed(outpost);
                Object.Destroy(existingPlatoon.gameObject);
            }

            if (AreAllPlatoonSlotsDestroyed(outpost))
            {
                EnsureEmptyPlatoonMarker(outpost);
                Debug.Log(
                    $"F-89: {outpost.SiteCode} ({outpost.BaseName}) platoon remains cleared — no respawn.");
                return;
            }

            var urUnits = CollectCatalogUnits(VehicleUnitDesignation.UR, includeTroops: false);
            if (urUnits.Count == 0)
            {
                Debug.LogWarning($"F-89: No UR vehicles in catalog — platoon skipped for {siteCode}.");
                return;
            }

            var usUnits = CollectCatalogUnits(VehicleUnitDesignation.US, includeTroops: false);
            var urTroops = CollectCatalogUnits(VehicleUnitDesignation.UR, includeTroops: true, troopsOnly: true);
            var urVehicleCount = ResolveSpawnCount(
                outpost,
                OutpostGroundRules.MinVehicleCountPerSide,
                OutpostGroundRules.MaxVehicleCountPerSide);
            var usVehicleCount = usUnits.Count > 0
                ? ResolveSpawnCount(
                    outpost,
                    OutpostGroundRules.MinVehicleCountPerSide,
                    OutpostGroundRules.MaxVehicleCountPerSide,
                    salt: 5101)
                : 0;
            var urTroopCount = urTroops.Count > 0
                ? ResolveSpawnCount(
                    outpost,
                    OutpostGroundRules.MinTroopCount,
                    OutpostGroundRules.MaxTroopCount,
                    salt: 7919)
                : 0;
            var playerTarget = player != null ? player.GetComponent<LockableTarget>() : null;
            var platoonRoot = new GameObject(PlatoonRootName);
            platoonRoot.transform.SetParent(outpost.transform, false);
            platoonRoot.AddComponent<OutpostPlatoonRoot>();

            var totalSlots = urVehicleCount + usVehicleCount + urTroopCount;
            var spawned = 0;
            spawned += SpawnPlatoonSide(
                platoonRoot.transform,
                urUnits,
                urVehicleCount,
                angleOffset: 0f,
                startIndex: 0,
                totalSlots: totalSlots,
                outpost,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget);
            spawned += SpawnPlatoonSide(
                platoonRoot.transform,
                usUnits,
                usVehicleCount,
                angleOffset: Mathf.PI,
                startIndex: urVehicleCount,
                totalSlots: totalSlots,
                outpost,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget);
            spawned += SpawnPlatoonSide(
                platoonRoot.transform,
                urTroops,
                urTroopCount,
                angleOffset: 0f,
                startIndex: urVehicleCount + usVehicleCount,
                totalSlots: totalSlots,
                outpost,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget);

            Debug.Log(
                $"F-89: Spawned {spawned} ground unit(s) at {outpost.SiteCode} ({outpost.BaseName}) — "
                + $"UR {urVehicleCount}v/{urTroopCount}t, US {usVehicleCount}v. Building cluster present.");

            if (spawned == 0 && AreAllPlatoonSlotsDestroyed(outpost))
            {
                Debug.Log(
                    $"F-89: {outpost.SiteCode} ({outpost.BaseName}) platoon remains cleared — no respawn.");
            }
        }

        private static bool HasLivingPlatoonUnits(Transform platoonRoot)
        {
            return PlatoonHasLivingUnits(platoonRoot);
        }

        public static bool PlatoonHasLivingUnits(Transform platoonRoot)
        {
            if (platoonRoot == null)
            {
                return false;
            }

            var targets = platoonRoot.GetComponentsInChildren<LockableTarget>(true);
            for (var i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                if (target != null && target.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static int SpawnPlatoonSide(
            Transform platoonRoot,
            List<VehicleUnitDefinition> catalog,
            int count,
            float angleOffset,
            int startIndex,
            int totalSlots,
            AntarcticaBase outpost,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float worldUnitsPerMile,
            float ticSize,
            LockableTarget playerTarget)
        {
            if (catalog == null || catalog.Count == 0 || count <= 0)
            {
                return 0;
            }

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                var definition = catalog[i % catalog.Count];
                var slotLabel = BuildUnitSlotLabel(definition, startIndex + i);
                if (AntarcticaOutpostState.IsTargetDestroyed(outpost.BaseName, slotLabel))
                {
                    continue;
                }

                var spawnPos = ResolveSpawnWorldPosition(
                    outpost.transform.position,
                    startIndex + i,
                    totalSlots,
                    worldUnitsPerMile,
                    worldMap,
                    definition,
                    ticSize,
                    angleOffset);
                if (!SpawnGroundUnit(
                        platoonRoot,
                        definition,
                        slotLabel,
                        spawnPos,
                        outpost,
                        worldMap,
                        profile,
                        worldUnitsPerMile,
                        ticSize,
                        playerTarget))
                {
                    continue;
                }

                spawned++;
            }

            return spawned;
        }

        public static void RemoveAllPlatoons()
        {
            var outposts = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < outposts.Length; i++)
            {
                var platoon = outposts[i].transform.Find(PlatoonRootName);
                if (platoon != null)
                {
                    Object.Destroy(platoon.gameObject);
                }
            }
        }

        /// <summary>
        /// True when every platoon slot for this outpost is marked destroyed in the save
        /// (no living units should respawn).
        /// </summary>
        public static bool AreAllPlatoonSlotsDestroyed(AntarcticaBase outpost)
        {
            if (outpost == null)
            {
                return false;
            }

            if (OutpostFlightPlatoonState.IsPlatoonClearedInSave(outpost.BaseName))
            {
                return true;
            }

            BuildExpectedPlatoonSlotLabels(
                outpost,
                out var labels,
                out var livingCount);
            if (livingCount <= 0)
            {
                return false;
            }

            for (var i = 0; i < labels.Count; i++)
            {
                if (!AntarcticaOutpostState.IsTargetDestroyed(outpost.BaseName, labels[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Persist platoon clearance so scene reloads never refill killed units.</summary>
        public static void MarkAllPlatoonSlotsDestroyed(AntarcticaBase outpost)
        {
            if (outpost == null)
            {
                return;
            }

            BuildExpectedPlatoonSlotLabels(
                outpost,
                out var labels,
                out _);
            for (var i = 0; i < labels.Count; i++)
            {
                AntarcticaOutpostState.MarkTargetDestroyed(outpost.BaseName, labels[i]);
            }

            OutpostFlightPlatoonState.MarkPlatoonCleared(outpost.BaseName);
        }

        public static void EnsureEmptyPlatoonMarker(AntarcticaBase outpost)
        {
            if (outpost == null)
            {
                return;
            }

            var existingPlatoon = outpost.transform.Find(PlatoonRootName);
            if (existingPlatoon != null)
            {
                return;
            }

            var clearedRoot = new GameObject(PlatoonRootName);
            clearedRoot.transform.SetParent(outpost.transform, false);
            clearedRoot.AddComponent<OutpostPlatoonRoot>();
        }

        private static void BuildExpectedPlatoonSlotLabels(
            AntarcticaBase outpost,
            out List<string> labels,
            out int livingCount)
        {
            labels = new List<string>(32);
            livingCount = 0;

            var urUnits = CollectCatalogUnits(VehicleUnitDesignation.UR, includeTroops: false);
            var usUnits = CollectCatalogUnits(VehicleUnitDesignation.US, includeTroops: false);
            var urTroops = CollectCatalogUnits(VehicleUnitDesignation.UR, includeTroops: true, troopsOnly: true);
            if (urUnits.Count == 0)
            {
                return;
            }

            var urVehicleCount = ResolveSpawnCount(
                outpost,
                OutpostGroundRules.MinVehicleCountPerSide,
                OutpostGroundRules.MaxVehicleCountPerSide);
            var usVehicleCount = usUnits.Count > 0
                ? ResolveSpawnCount(
                    outpost,
                    OutpostGroundRules.MinVehicleCountPerSide,
                    OutpostGroundRules.MaxVehicleCountPerSide,
                    salt: 5101)
                : 0;
            var urTroopCount = urTroops.Count > 0
                ? ResolveSpawnCount(
                    outpost,
                    OutpostGroundRules.MinTroopCount,
                    OutpostGroundRules.MaxTroopCount,
                    salt: 7919)
                : 0;

            AppendSlotLabels(labels, urUnits, urVehicleCount, 0);
            AppendSlotLabels(labels, usUnits, usVehicleCount, urVehicleCount);
            AppendSlotLabels(labels, urTroops, urTroopCount, urVehicleCount + usVehicleCount);
            livingCount = urVehicleCount + usVehicleCount + urTroopCount;
        }

        private static void AppendSlotLabels(
            List<string> labels,
            List<VehicleUnitDefinition> catalog,
            int count,
            int startIndex)
        {
            if (catalog == null || catalog.Count == 0 || count <= 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var definition = catalog[i % catalog.Count];
                labels.Add(BuildUnitSlotLabel(definition, startIndex + i));
            }
        }

        private static void RemovePlatoonsAtOtherSites(string keepSiteCode)
        {
            if (string.IsNullOrWhiteSpace(keepSiteCode))
            {
                return;
            }

            var outposts = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < outposts.Length; i++)
            {
                var outpost = outposts[i];
                if (outpost == null
                    || string.Equals(outpost.SiteCode, keepSiteCode.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var platoon = outpost.transform.Find(PlatoonRootName);
                if (platoon != null)
                {
                    Object.Destroy(platoon.gameObject);
                }
            }
        }

        private static string BuildUnitSlotLabel(VehicleUnitDefinition definition, int slotIndex)
        {
            var designation = definition != null ? definition.designation.ToString() : "UR";
            var abbrev = definition != null ? definition.abbreviation : "UNIT";
            return $"{designation}-{abbrev}-{slotIndex}";
        }

        private const float VehicleRoamRadiusMiles = 2f;

        private static bool SpawnGroundUnit(
            Transform parent,
            VehicleUnitDefinition definition,
            string slotLabel,
            Vector3 worldPosition,
            AntarcticaBase outpost,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float worldUnitsPerMile,
            float ticSizeWorldUnits,
            LockableTarget playerTarget)
        {
            if (definition == null)
            {
                return false;
            }

            var unitObject = new GameObject(slotLabel);
            unitObject.transform.SetParent(parent, true);
            unitObject.transform.position = worldPosition;

            var unit = unitObject.AddComponent<VehicleUnitComponent>();
            unit.Configure(definition);
            unit.SetPersistentTargetLabel(slotLabel);

            VehicleUnitVisual.Attach(unitObject.transform, definition, profile);

            var roamHome = ResolveBunkerRoamCenter(outpost);
            var behavior = unitObject.AddComponent<OutpostGroundUnitBehavior>();
            behavior.Configure(
                definition,
                worldMap,
                profile,
                worldUnitsPerMile,
                playerTarget,
                roamRadiusMilesOverride: VehicleRoamRadiusMiles,
                roamHomeWorld: roamHome);
            return true;
        }

        private static Vector3 ResolveBunkerRoamCenter(AntarcticaBase outpost)
        {
            if (outpost == null)
            {
                return Vector3.zero;
            }

            var buildings = outpost.GetComponentsInChildren<OutpostBuilding>(true);
            for (var i = 0; i < buildings.Length; i++)
            {
                var building = buildings[i];
                if (building != null && building.BuildingType == OutpostBuildingType.Bunker)
                {
                    var pos = building.transform.position;
                    pos.y = 0f;
                    return pos;
                }
            }

            var runwayBunker = outpost.transform.Find(OutpostRunwayVisual.RunwayBunkerObjectName);
            if (runwayBunker != null)
            {
                var pos = runwayBunker.position;
                pos.y = 0f;
                return pos;
            }

            var cluster = outpost.transform.Find(OutpostBuildingClusterSpawner.ClusterRootName);
            if (cluster != null)
            {
                var pos = cluster.position;
                pos.y = 0f;
                return pos;
            }

            var fallback = outpost.transform.position;
            fallback.y = 0f;
            return fallback;
        }

        private static List<VehicleUnitDefinition> CollectCatalogUnits(
            VehicleUnitDesignation designation,
            bool includeTroops = true,
            bool troopsOnly = false)
        {
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var results = new List<VehicleUnitDefinition>(16);
            if (catalog.units == null)
            {
                return results;
            }

            for (var i = 0; i < catalog.units.Length; i++)
            {
                var unit = catalog.units[i];
                if (unit == null || unit.designation != designation)
                {
                    continue;
                }

                if (troopsOnly && !unit.isTroop)
                {
                    continue;
                }

                if (!includeTroops && unit.isTroop)
                {
                    continue;
                }

                results.Add(unit);
            }

            results.Sort((a, b) => a.vehicleLevel.CompareTo(b.vehicleLevel));
            return results;
        }

        private static int ResolveSpawnCount(AntarcticaBase outpost, int min, int max, int salt = 0)
        {
            unchecked
            {
                var seed = StableSeed(outpost) + salt;
                var rng = new System.Random(seed);
                return rng.Next(min, max + 1);
            }
        }

        private static Vector3 ResolveSpawnWorldPosition(
            Vector3 outpostWorld,
            int index,
            int totalCount,
            float worldUnitsPerMile,
            WorldMapConfig worldMap,
            VehicleUnitDefinition definition,
            float ticSizeWorldUnits,
            float angleOffsetRadians = 0f)
        {
            var isFlier = definition != null && definition.isFlier;
            var fliesOverGround = definition != null && definition.fliesOverGroundUnits;
            outpostWorld.y = 0f;
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            var keepOutWorld = OutpostGroundRules.KeepOutRadiusWorld(ticSizeWorldUnits);
            var minRadiusMiles = fliesOverGround
                ? SpawnMinRadiusMiles
                : Mathf.Max(SpawnMinRadiusMiles, (keepOutWorld / worldUnitsPerMile) * 1.05f);
            var outpostMiles = CampaignMapCoordinates.WorldToMiles(outpostWorld, worldMap, ticSizeWorldUnits);

            var goldenAngle = 2.399963f;
            var baseAngle = angleOffsetRadians + index * goldenAngle;
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var angle = baseAngle + attempt * 0.31f;
                var t = totalCount > 1 ? index / (float)(totalCount - 1) : 0.5f;
                var radiusMiles = Mathf.Lerp(minRadiusMiles, SpawnMaxRadiusMiles, t) * (1f - attempt * 0.025f);
                var offsetMiles = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radiusMiles;
                var candidateMiles = outpostMiles + offsetMiles;
                var candidate = CampaignMapCoordinates.MilesToWorld(candidateMiles, worldMap, ticSizeWorldUnits);

                if (isFlier)
                {
                    candidate.y = fliesOverGround
                        ? TacScale.TacsToWorld(OutpostGroundRules.HelicopterCruiseAltitudeTacs, ticSizeWorldUnits)
                        : TacScale.TacsToWorld(1.5f, ticSizeWorldUnits);
                    if (fliesOverGround
                        || GroundUnitMovement.IsAllowedWorldPosition(
                            candidate,
                            worldMap,
                            ticSizeWorldUnits,
                            mapSizeMiles))
                    {
                        return candidate;
                    }

                    continue;
                }

                candidate.y = 0f;
                if (GroundUnitMovement.IsAllowedWorldPosition(
                        candidate,
                        worldMap,
                        ticSizeWorldUnits,
                        mapSizeMiles))
                {
                    return candidate;
                }
            }

            var fallbackAngle = baseAngle + index * 0.17f;
            var fallbackMiles = outpostMiles + new Vector2(Mathf.Cos(fallbackAngle), Mathf.Sin(fallbackAngle))
                * minRadiusMiles;
            var fallback = CampaignMapCoordinates.MilesToWorld(fallbackMiles, worldMap, ticSizeWorldUnits);
            fallback.y = isFlier
                ? TacScale.TacsToWorld(
                    fliesOverGround ? OutpostGroundRules.HelicopterCruiseAltitudeTacs : 1.5f,
                    ticSizeWorldUnits)
                : 0f;
            return fallback;
        }

        private static AntarcticaBase FindOutpostBySiteCode(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return null;
            }

            var normalizedCode = siteCode.Trim();
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            AntarcticaBase codeMatch = null;
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite != null
                    && baseSite.SiteKind == BaseSiteKind.Land
                    && string.Equals(baseSite.SiteCode, normalizedCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    codeMatch = baseSite;
                    break;
                }
            }

            if (codeMatch != null)
            {
                return codeMatch;
            }

            CampaignMapLayoutState.EnsureLoaded();
            if (!CampaignMapLayoutState.TryGetSiteByCode(normalizedCode, out var layoutSite))
            {
                return null;
            }

            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
                {
                    continue;
                }

                var normalizedName = CampaignMapLayoutState.NormalizeSiteName(baseSite.BaseName);
                if (!string.Equals(normalizedName, layoutSite.Label, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                baseSite.SetSiteCode(layoutSite.SiteCode);
                return baseSite;
            }

            return null;
        }

        private static void LogLandOutpostDiagnostics()
        {
            CampaignMapLayoutState.EnsureLoaded();
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
                {
                    continue;
                }

                var miles = baseSite.PositionMiles;
                Debug.Log(
                    $"F-89: Land outpost — {baseSite.SiteCode} / {baseSite.BaseName} "
                    + $"at ({miles.x:0.0}, {miles.y:0.0}) MI.");
            }

            if (CampaignMapLayoutState.TryGetSiteByCode(OpSouthSiteCode, out var south))
            {
                Debug.Log($"F-89: Campaign layout OP-SOUTH = {OutpostSiteIds.FormatIdentity(south)}");
            }
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

        private static int StableSeed(AntarcticaBase baseSite)
        {
            unchecked
            {
                var code = baseSite.SiteCode ?? baseSite.BaseName ?? "OUTPOST";
                var hash = 17;
                for (var i = 0; i < code.Length; i++)
                {
                    hash = (hash * 31) + char.ToUpperInvariant(code[i]);
                }

                return hash;
            }
        }
    }
}
