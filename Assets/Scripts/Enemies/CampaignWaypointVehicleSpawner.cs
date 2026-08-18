using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Spawns primary UR forces plus half-count (floor) equal-level US forces at the active WP site.
    /// </summary>
    public static class CampaignWaypointVehicleSpawner
    {
        private const string PlatoonRootName = "VehiclePlatoon";
        private const float SpawnMinRadiusMiles = 0.25f;
        private const float SpawnMaxRadiusMiles = 0.85f;

        /// <summary>
        /// Spawns the active campaign waypoint platoon when the player flies within range.
        /// </summary>
        public static void EnsureActiveWaypointPlatoonWithinMiles(
            Vector3 worldPosition,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            AircraftController player)
        {
            if (player == null || worldMap == null || rangeMiles <= 0f || !GamePlayModeState.IsCampaign)
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null
                || !CampaignMissionSiteCatalog.TryGetCurrentSiteCode(save, out var siteCode)
                || !CampaignMissionSiteCatalog.IsWaypointSite(siteCode))
            {
                return;
            }

            if (!CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint))
            {
                return;
            }

            var siteWorld = CampaignMapCoordinates.MilesToWorld(
                CampaignWaypointLayoutState.GetMiles(waypoint),
                worldMap,
                ticSizeWorldUnits);
            var distanceMiles = CombatThreatRange.DistanceMiles(
                worldPosition,
                siteWorld,
                worldMap,
                ticSizeWorldUnits);
            if (distanceMiles > rangeMiles)
            {
                return;
            }

            EnsureActiveWaypointPlatoon(player, siteCode);
        }

        public static void EnsureActiveWaypointPlatoon(AircraftController player, string siteCode)
        {
            if (player == null || string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            siteCode = siteCode.Trim().ToUpperInvariant();
            if (!CampaignWaypointSiteIds.IsWaypointSiteCode(siteCode))
            {
                return;
            }

            if (!CampaignWaypointLayoutState.TryGetByCode(siteCode, out var waypoint))
            {
                Debug.LogWarning($"F-89: No waypoint layout for {siteCode} — platoon skipped.");
                return;
            }

            if (!CampaignMissionPrimarySpawnCatalog.TryGetPlan(waypoint.MissionNumber, out var plan))
            {
                Debug.LogWarning($"F-89: No primary spawn plan for mission {waypoint.MissionNumber}.");
                return;
            }

            var worldMap = player.WorldMap ?? Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = player.Profile ?? Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            var miles = CampaignWaypointLayoutState.GetMiles(waypoint);
            var siteWorld = CampaignMapCoordinates.MilesToWorld(miles, worldMap, ticSize);
            siteWorld.y = 0f;

            if (OutpostFlightPlatoonState.IsPlatoonClearedInSave(siteCode)
                || CampaignWaypointPlatoonState.IsPrimaryAirObjectivesComplete(siteCode)
                || AntarcticaOutpostState.IsTargetDestroyed(
                    siteCode,
                    CampaignWaypointPrimaryObjective.ClearedLabel))
            {
                var clearedSiteRoot = FindOrCreateSiteRoot(siteCode, siteWorld);
                ClearLeftoverPlatoon(clearedSiteRoot);
                CampaignWaypointPlatoonState.EnsureSecondaryPadRevealed(
                    siteCode,
                    siteWorld,
                    clearedSiteRoot.GetComponent<CampaignWaypointMissionSite>());
                return;
            }

            var siteRoot = FindOrCreateSiteRoot(siteCode, siteWorld);
            var siteComponent = siteRoot.GetComponent<CampaignWaypointMissionSite>();
            var existingPlatoon = siteRoot.transform.Find(PlatoonRootName);
            if (existingPlatoon != null && PlatoonHasLivingHostileUnits(existingPlatoon))
            {
                return;
            }

            if (existingPlatoon != null)
            {
                CampaignWaypointPlatoonState.TryFinalizeClearance(siteComponent, siteWorld);
                if (CampaignWaypointPlatoonState.IsPrimaryAirObjectivesComplete(siteCode)
                    || CampaignWaypointPlatoonState.ShouldShowSecondaryPad(siteCode, siteComponent))
                {
                    CampaignWaypointPlatoonState.EnsureSecondaryPadRevealed(siteCode, siteWorld, siteComponent);
                    Object.Destroy(existingPlatoon.gameObject);
                    return;
                }

                Object.Destroy(existingPlatoon.gameObject);
            }

            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var playerTarget = player.GetComponent<LockableTarget>();
            var platoonRoot = new GameObject(PlatoonRootName);
            platoonRoot.transform.SetParent(siteRoot.transform, false);
            platoonRoot.AddComponent<OutpostPlatoonRoot>();

            // US friendlies are half UR count (integer floor) at the same levels.
            var usInfantry = plan.InfantryCount / 2;
            var usVehicles = plan.VehicleCount / 2;
            var usLevel7 = plan.Level7Count / 2;
            var totalSlots = plan.InfantryCount + plan.VehicleCount + plan.Level7Count
                + usInfantry + usVehicles + usLevel7;
            var infantryLevel = CampaignMissionPrimarySpawnCatalog.GetInfantryLevelForMission(waypoint.MissionNumber);
            var spawned = 0;
            var slot = 0;

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                catalog.GetUrTroopByLevel(infantryLevel),
                plan.InfantryCount,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: 0f);

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                catalog.GetUrVehicleByLevel(plan.VehicleLevel),
                plan.VehicleCount,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: 0f);

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                catalog.GetUrVehicleByLevel(7),
                plan.Level7Count,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: 0f);

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                ResolveUsTroop(catalog, infantryLevel),
                usInfantry,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: Mathf.PI);

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                catalog.GetUsVehicleByLevel(plan.VehicleLevel),
                usVehicles,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: Mathf.PI);

            spawned += SpawnLevelGroup(
                platoonRoot.transform,
                catalog.GetUsVehicleByLevel(7),
                usLevel7,
                ref slot,
                totalSlots,
                siteWorld,
                siteCode,
                worldMap,
                profile,
                worldUnitsPerMile,
                ticSize,
                playerTarget,
                angleOffset: Mathf.PI);

            Debug.Log(
                $"F-89: Spawned {spawned} waypoint unit(s) at {siteCode} "
                + $"(mission {waypoint.MissionNumber}: UR {plan.InfantryCount}t L{infantryLevel}/"
                + $"{plan.VehicleCount}v L{plan.VehicleLevel}/{plan.Level7Count}v L7, "
                + $"US {usInfantry}t/{usVehicles}v L{plan.VehicleLevel}/{usLevel7}v L7).");
        }

        private static bool PlatoonHasLivingHostileUnits(Transform platoonRoot)
        {
            if (platoonRoot == null)
            {
                return false;
            }

            var units = platoonRoot.GetComponentsInChildren<VehicleUnitComponent>(true);
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null || unit.Definition == null || !unit.Definition.IsHostile)
                {
                    continue;
                }

                var target = unit.GetComponent<LockableTarget>();
                if (target != null && target.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private static VehicleUnitDefinition ResolveUsTroop(VehicleUnitCatalog catalog, int infantryLevel)
        {
            if (catalog != null
                && catalog.TryGetByAbbreviation("TRP", VehicleUnitDesignation.US, out var trp)
                && trp != null
                && trp.isTroop)
            {
                return trp;
            }

            return catalog != null ? catalog.GetUsTroopByLevel(infantryLevel) : null;
        }

        private static GameObject FindOrCreateSiteRoot(string siteCode, Vector3 worldPosition)
        {
            var sites = Object.FindObjectsByType<CampaignWaypointMissionSite>(FindObjectsSortMode.None);
            for (var i = 0; i < sites.Length; i++)
            {
                var site = sites[i];
                if (site != null
                    && string.Equals(site.SiteCode, siteCode, System.StringComparison.OrdinalIgnoreCase))
                {
                    site.transform.position = worldPosition;
                    return site.gameObject;
                }
            }

            var root = new GameObject(siteCode);
            root.transform.position = worldPosition;
            var tag = root.AddComponent<CampaignWaypointMissionSite>();
            tag.SiteCode = siteCode;
            return root;
        }

        private static void ClearLeftoverPlatoon(GameObject siteRoot)
        {
            if (siteRoot == null)
            {
                return;
            }

            var existingPlatoon = siteRoot.transform.Find(PlatoonRootName);
            if (existingPlatoon != null)
            {
                Object.Destroy(existingPlatoon.gameObject);
                CombatThreatRange.InvalidateCaches();
            }
        }

        private static int SpawnLevelGroup(
            Transform platoonRoot,
            VehicleUnitDefinition definition,
            int count,
            ref int slotIndex,
            int totalSlots,
            Vector3 siteWorld,
            string siteCode,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float worldUnitsPerMile,
            float ticSize,
            LockableTarget playerTarget,
            float angleOffset)
        {
            if (definition == null || count <= 0)
            {
                slotIndex += Mathf.Max(0, count);
                return 0;
            }

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                var slotLabel = BuildUnitSlotLabel(definition, slotIndex);
                slotIndex++;
                if (AntarcticaOutpostState.IsTargetDestroyed(siteCode, slotLabel))
                {
                    continue;
                }

                var spawnPos = ResolveSpawnWorldPosition(
                    siteWorld,
                    slotIndex - 1,
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
                        siteWorld,
                        worldMap,
                        profile,
                        worldUnitsPerMile,
                        playerTarget))
                {
                    continue;
                }

                spawned++;
            }

            return spawned;
        }

        private static bool SpawnGroundUnit(
            Transform parent,
            VehicleUnitDefinition definition,
            string slotLabel,
            Vector3 worldPosition,
            Vector3 roamHome,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float worldUnitsPerMile,
            LockableTarget playerTarget)
        {
            var unitObject = new GameObject(slotLabel);
            unitObject.transform.SetParent(parent, true);
            unitObject.transform.position = worldPosition;

            var unit = unitObject.AddComponent<VehicleUnitComponent>();
            unit.Configure(definition);
            unit.SetPersistentTargetLabel(slotLabel);
            VehicleUnitVisual.Attach(unitObject.transform, definition, profile);

            var behavior = unitObject.AddComponent<OutpostGroundUnitBehavior>();
            behavior.Configure(
                definition,
                worldMap,
                profile,
                worldUnitsPerMile,
                playerTarget,
                roamRadiusMilesOverride: 2f,
                roamHomeWorld: roamHome);
            return true;
        }

        private static string BuildUnitSlotLabel(VehicleUnitDefinition definition, int slotIndex)
        {
            var designation = definition != null ? definition.designation.ToString() : "UR";
            var abbrev = definition != null ? definition.abbreviation : "UNIT";
            return $"{designation}-{abbrev}-{slotIndex}";
        }

        private static Vector3 ResolveSpawnWorldPosition(
            Vector3 siteWorld,
            int index,
            int totalCount,
            float worldUnitsPerMile,
            WorldMapConfig worldMap,
            VehicleUnitDefinition definition,
            float ticSizeWorldUnits,
            float angleOffset)
        {
            var isFlier = definition != null && definition.isFlier;
            var fliesOverGround = definition != null && definition.fliesOverGroundUnits;
            siteWorld.y = 0f;
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            var siteMiles = CampaignMapCoordinates.WorldToMiles(siteWorld, worldMap, ticSizeWorldUnits);
            var goldenAngle = 2.399963f;
            var baseAngle = index * goldenAngle + angleOffset;

            for (var attempt = 0; attempt < 24; attempt++)
            {
                var angle = baseAngle + attempt * 0.31f;
                var t = totalCount > 1 ? index / (float)(totalCount - 1) : 0.5f;
                var radiusMiles = Mathf.Lerp(SpawnMinRadiusMiles, SpawnMaxRadiusMiles, t)
                    * (1f - attempt * 0.025f);
                var offsetMiles = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radiusMiles;
                var candidate = CampaignMapCoordinates.MilesToWorld(
                    siteMiles + offsetMiles,
                    worldMap,
                    ticSizeWorldUnits);

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

            var fallback = siteWorld;
            fallback.y = isFlier
                ? TacScale.TacsToWorld(1.5f, ticSizeWorldUnits)
                : 0f;
            return fallback;
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
    }
}
