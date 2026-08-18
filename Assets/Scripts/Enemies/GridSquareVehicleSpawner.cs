using System.Collections.Generic;
using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Ambient enemy spawns: each 1×1 MI grid square (GS) has a 0.3% chance of one UR vehicle per mission.
    /// Squares are rolled once when the player first comes within 20 MI; rolls reset on END MISSION only.
    /// Ocean squares always spawn TDP when the roll succeeds; land squares pick a random UR vehicle.
    /// </summary>
    public static class GridSquareVehicleSpawner
    {
        public const float EnsureRangeMiles = 20f;
        public const float SpawnChancePercent = 0.3f;

        private const string PlatoonRootName = "VehiclePlatoon";
        private const string SpawnRootName = "GridSquareSpawns";
        private const float RoamRadiusMiles = 0.45f;
        private const int MaxCellsResolvedPerEnsure = 64;

        private static readonly List<VehicleUnitDefinition> UrCombatVehicleScratch = new(16);
        private static readonly HashSet<long> OccupiedBaseCellsScratch = new();
        private static Transform cachedSpawnRoot;

        public static void ResetForNewMission()
        {
            GridSquareSpawnState.ResetForNewMission();
            cachedSpawnRoot = null;
            DestroyAllInScene();
        }

        /// <summary>Returns true when a new ambient unit was spawned this pass.</summary>
        public static bool EnsureWithinMiles(
            Vector3 observerWorld,
            float rangeMiles,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            AircraftController player)
        {
            if (player == null || worldMap == null || rangeMiles <= 0f || ticSizeWorldUnits <= 0f)
            {
                return false;
            }

            var save = CharacterSessionState.ActiveSave;
            GridSquareSpawnState.EnsureMissionScope(save);

            if (!CampaignMapCoordinates.TryWorldToGridCell(
                    observerWorld,
                    worldMap,
                    ticSizeWorldUnits,
                    out var observerCell))
            {
                return false;
            }

            var rangeCells = Mathf.CeilToInt(rangeMiles);
            var profile = player.Profile ?? Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = worldMap.GetWorldUnitsPerMile(ticSizeWorldUnits);
            var playerTarget = player.GetComponent<LockableTarget>();
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            var dimensions = worldMap.GetMapGridDimensions();
            var resolvedThisPass = 0;
            var spawnedAnything = false;
            BuildBlockedAmbientSpawnCellSet(worldMap, ticSizeWorldUnits);

            for (var rowOffset = -rangeCells; rowOffset <= rangeCells && resolvedThisPass < MaxCellsResolvedPerEnsure; rowOffset++)
            {
                for (var colOffset = -rangeCells; colOffset <= rangeCells && resolvedThisPass < MaxCellsResolvedPerEnsure; colOffset++)
                {
                    var gridCell = new Vector2Int(observerCell.x + colOffset, observerCell.y + rowOffset);
                    if (gridCell.x < 1 || gridCell.y < 1
                        || gridCell.x > dimensions.x
                        || gridCell.y > dimensions.y)
                    {
                        continue;
                    }

                    var cellCenter = worldMap.GridCellToWorldCenter(gridCell, ticSizeWorldUnits);
                    var distanceMiles = CombatThreatRange.DistanceMiles(
                        observerWorld,
                        cellCenter,
                        worldMap,
                        ticSizeWorldUnits);
                    if (distanceMiles > rangeMiles)
                    {
                        continue;
                    }

                    // WP mission / secondary pad cells must never keep ambient red radar contacts.
                    if (IsBlockedAmbientSpawnCell(gridCell))
                    {
                        SuppressAmbientSpawnOnBlockedCell(gridCell);
                        continue;
                    }

                    if (GridSquareSpawnState.TryGetOutcome(gridCell, out var existing))
                    {
                        // Empty / destroyed cells need no per-tick scene work.
                        if (!existing.Spawned || existing.Destroyed)
                        {
                            continue;
                        }

                        if (RestoreFromOutcome(
                                existing,
                                gridCell,
                                catalog,
                                worldMap,
                                profile,
                                ticSizeWorldUnits,
                                worldUnitsPerMile,
                                playerTarget))
                        {
                            spawnedAnything = true;
                        }

                        continue;
                    }

                    resolvedThisPass++;
                    if (ResolveGridSquareOnce(
                            save,
                            gridCell,
                            catalog,
                            worldMap,
                            profile,
                            ticSizeWorldUnits,
                            worldUnitsPerMile,
                            playerTarget))
                    {
                        spawnedAnything = true;
                    }
                }
            }

            return spawnedAnything;
        }

        /// <summary>Resolves the ambient spawn roll for a landing cell when the player lands without flying within range first.</summary>
        public static void EnsureLandingCellResolved(Vector2 landingMiles)
        {
            if (!CampaignMapCoordinates.TryMilesToGridCell(landingMiles, out var gridCell))
            {
                return;
            }

            if (GridSquareSpawnState.HasBeenResolved(gridCell))
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            GridSquareSpawnState.EnsureMissionScope(save);

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            if (worldMap == null || profile == null || profile.ticSizeWorldUnits <= 0f)
            {
                return;
            }

            var ticSize = profile.ticSizeWorldUnits;
            var worldUnitsPerMile = worldMap.GetWorldUnitsPerMile(ticSize);
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            ResolveGridSquareOnce(
                save,
                gridCell,
                catalog,
                worldMap,
                profile,
                ticSize,
                worldUnitsPerMile,
                playerTarget: null);
        }

        public static void DestroyAllInScene()
        {
            cachedSpawnRoot = null;
            var roots = Object.FindObjectsByType<GridSquareSpawnRoot>(FindObjectsSortMode.None);
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null)
                {
                    Object.Destroy(root.gameObject);
                }
            }
        }

        private static bool ResolveGridSquareOnce(
            CharacterSaveData save,
            Vector2Int gridCell,
            VehicleUnitCatalog catalog,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float ticSizeWorldUnits,
            float worldUnitsPerMile,
            LockableTarget playerTarget)
        {
            if (GridSquareSpawnState.HasBeenResolved(gridCell))
            {
                return false;
            }

            var siteCode = GridSquareSiteIds.FormatSiteCode(gridCell);

            if (IsBlockedAmbientSpawnCell(gridCell))
            {
                GridSquareSpawnState.RecordOutcome(gridCell, new GridSquareSpawnState.CellOutcome(false, null, null));
                return false;
            }

            if (!RollShouldSpawn(save, gridCell))
            {
                GridSquareSpawnState.RecordOutcome(gridCell, new GridSquareSpawnState.CellOutcome(false, null, null));
                return false;
            }

            var cellMiles = CampaignMapCoordinates.GridCellToMiles(gridCell);
            var mapWidthMiles = CampaignMapCoordinates.GetMapWidthMiles(worldMap);
            var overWater = !AntarcticaLandMask.IsLandMiles(cellMiles, mapWidthMiles);
            var definition = ResolveSpawnDefinition(catalog, save, gridCell, overWater);
            if (definition == null)
            {
                GridSquareSpawnState.RecordOutcome(gridCell, new GridSquareSpawnState.CellOutcome(false, null, null));
                return false;
            }

            var slotLabel = BuildUnitSlotLabel(definition, 0);
            if (!TryResolveSpawnPosition(
                    gridCell,
                    definition,
                    worldMap,
                    ticSizeWorldUnits,
                    out var spawnPosition))
            {
                GridSquareSpawnState.RecordOutcome(gridCell, new GridSquareSpawnState.CellOutcome(false, null, null));
                return false;
            }

            GridSquareSpawnState.RecordOutcome(
                gridCell,
                new GridSquareSpawnState.CellOutcome(true, slotLabel, definition.abbreviation));

            var siteRoot = CreateSiteRoot(siteCode, gridCell, worldMap, ticSizeWorldUnits);
            var platoonRoot = EnsurePlatoonRoot(siteRoot.transform);
            SpawnUnit(
                platoonRoot,
                definition,
                slotLabel,
                spawnPosition,
                spawnPosition,
                worldMap,
                profile,
                worldUnitsPerMile,
                playerTarget);
            return true;
        }

        private static bool RestoreFromOutcome(
            GridSquareSpawnState.CellOutcome outcome,
            Vector2Int gridCell,
            VehicleUnitCatalog catalog,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float ticSizeWorldUnits,
            float worldUnitsPerMile,
            LockableTarget playerTarget)
        {
            if (!outcome.Spawned || outcome.Destroyed)
            {
                return false;
            }

            var siteCode = GridSquareSiteIds.FormatSiteCode(gridCell);
            var siteRoot = FindSiteRoot(siteCode) ?? CreateSiteRoot(siteCode, gridCell, worldMap, ticSizeWorldUnits);
            var platoonRoot = EnsurePlatoonRoot(siteRoot.transform);
            if (platoonRoot.childCount > 0)
            {
                return false;
            }

            if (catalog == null
                || !catalog.TryGetByAbbreviation(outcome.VehicleAbbreviation, VehicleUnitDesignation.UR, out var definition)
                || definition == null)
            {
                return false;
            }

            if (!TryResolveSpawnPosition(
                    gridCell,
                    definition,
                    worldMap,
                    ticSizeWorldUnits,
                    out var spawnPosition))
            {
                return false;
            }

            SpawnUnit(
                platoonRoot,
                definition,
                outcome.SlotLabel,
                spawnPosition,
                spawnPosition,
                worldMap,
                profile,
                worldUnitsPerMile,
                playerTarget);
            return true;
        }

        private static Transform EnsurePlatoonRoot(Transform siteRoot)
        {
            var existing = siteRoot.Find(PlatoonRootName);
            if (existing != null)
            {
                return existing;
            }

            var platoonRoot = new GameObject(PlatoonRootName);
            platoonRoot.transform.SetParent(siteRoot, false);
            platoonRoot.AddComponent<OutpostPlatoonRoot>();
            return platoonRoot.transform;
        }

        private static bool RollShouldSpawn(CharacterSaveData save, Vector2Int gridCell)
        {
            var rng = new System.Random(StableGridSeed(save, gridCell));
            return rng.Next(1000) < Mathf.RoundToInt(SpawnChancePercent * 10f);
        }

        private static VehicleUnitDefinition ResolveSpawnDefinition(
            VehicleUnitCatalog catalog,
            CharacterSaveData save,
            Vector2Int gridCell,
            bool overWater)
        {
            if (catalog == null)
            {
                return null;
            }

            if (overWater)
            {
                if (catalog.TryGetByAbbreviation("TDP", VehicleUnitDesignation.UR, out var tdp) && tdp != null)
                {
                    return tdp;
                }

                return catalog.GetUrVehicleByLevel(7);
            }

            CollectUrCombatVehicles(catalog, UrCombatVehicleScratch);
            if (UrCombatVehicleScratch.Count == 0)
            {
                return null;
            }

            var rng = new System.Random(StableGridSeed(save, gridCell) + 4157);
            return UrCombatVehicleScratch[rng.Next(UrCombatVehicleScratch.Count)];
        }

        private static void CollectUrCombatVehicles(VehicleUnitCatalog catalog, List<VehicleUnitDefinition> results)
        {
            results.Clear();
            if (catalog?.units == null)
            {
                return;
            }

            for (var i = 0; i < catalog.units.Length; i++)
            {
                var unit = catalog.units[i];
                if (unit == null
                    || unit.designation != VehicleUnitDesignation.UR
                    || unit.isTroop)
                {
                    continue;
                }

                results.Add(unit);
            }
        }

        private static bool TryResolveSpawnPosition(
            Vector2Int gridCell,
            VehicleUnitDefinition definition,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits,
            out Vector3 spawnPosition)
        {
            spawnPosition = worldMap.GridCellToWorldCenter(gridCell, ticSizeWorldUnits);
            var mapSizeMiles = worldMap.antarcticaSizeMiles;
            var isFlier = definition != null && definition.isFlier;
            var fliesOverGround = definition != null && definition.fliesOverGroundUnits;

            if (isFlier)
            {
                spawnPosition.y = fliesOverGround
                    ? TacScale.TacsToWorld(OutpostGroundRules.HelicopterCruiseAltitudeTacs, ticSizeWorldUnits)
                    : TacScale.TacsToWorld(1.5f, ticSizeWorldUnits);
                return true;
            }

            spawnPosition.y = 0f;
            return GroundUnitMovement.IsAllowedWorldPosition(
                spawnPosition,
                worldMap,
                ticSizeWorldUnits,
                mapSizeMiles);
        }

        private static void SpawnUnit(
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
                roamRadiusMilesOverride: RoamRadiusMiles,
                roamHomeWorld: roamHome);
        }

        private static GameObject FindSiteRoot(string siteCode)
        {
            var parent = GetOrCreateSpawnRoot();
            if (parent == null || string.IsNullOrWhiteSpace(siteCode))
            {
                return null;
            }

            var child = parent.Find(siteCode);
            return child != null ? child.gameObject : null;
        }

        private static Transform GetOrCreateSpawnRoot()
        {
            if (cachedSpawnRoot != null)
            {
                return cachedSpawnRoot;
            }

            var existing = Object.FindAnyObjectByType<GridSquareSpawnRoot>();
            if (existing != null)
            {
                cachedSpawnRoot = existing.transform;
                return cachedSpawnRoot;
            }

            var rootObject = new GameObject(SpawnRootName);
            rootObject.AddComponent<GridSquareSpawnRoot>();
            cachedSpawnRoot = rootObject.transform;
            return cachedSpawnRoot;
        }

        private static GameObject CreateSiteRoot(
            string siteCode,
            Vector2Int gridCell,
            WorldMapConfig worldMap,
            float ticSizeWorldUnits)
        {
            var parent = GetOrCreateSpawnRoot();
            var siteObject = new GameObject(siteCode);
            siteObject.transform.SetParent(parent, false);
            siteObject.transform.position = worldMap.GridCellToWorldCenter(gridCell, ticSizeWorldUnits);
            var tag = siteObject.AddComponent<CampaignWaypointMissionSite>();
            tag.SiteCode = siteCode;
            return siteObject;
        }

        private static void BuildBlockedAmbientSpawnCellSet(WorldMapConfig worldMap, float ticSizeWorldUnits)
        {
            OccupiedBaseCellsScratch.Clear();
            var bases = CombatThreatRange.GetCachedAntarcticaBases();
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || !baseSite.IsActive)
                {
                    continue;
                }

                if (CampaignMapCoordinates.TryWorldToGridCell(
                        baseSite.transform.position,
                        worldMap,
                        ticSizeWorldUnits,
                        out var baseCell))
                {
                    OccupiedBaseCellsScratch.Add(GridSquareSpawnState.PackCell(baseCell));
                }
            }

            // Block campaign waypoint cells and revealed secondary landing pads — WP secondary
            // has a wreck pad (not a bunker), so ambient GS red dots there look like false bunker contacts.
            CampaignWaypointLayoutState.EnsureLoaded();
            var waypoints = CampaignWaypointLayoutState.Waypoints;
            for (var i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                if (waypoint == null || string.IsNullOrWhiteSpace(waypoint.SiteCode))
                {
                    continue;
                }

                var miles = CampaignWaypointLayoutState.GetMiles(waypoint);
                var world = CampaignMapCoordinates.MilesToWorld(miles, worldMap, ticSizeWorldUnits);
                if (CampaignMapCoordinates.TryWorldToGridCell(world, worldMap, ticSizeWorldUnits, out var wpCell))
                {
                    OccupiedBaseCellsScratch.Add(GridSquareSpawnState.PackCell(wpCell));
                }
            }

            var save = CharacterSessionState.ActiveSave;
            var reveals = save?.WaypointSecondaryReveals;
            if (reveals == null)
            {
                return;
            }

            for (var i = 0; i < reveals.Length; i++)
            {
                var reveal = reveals[i];
                if (reveal == null || string.IsNullOrWhiteSpace(reveal.SiteCode))
                {
                    continue;
                }

                var padWorld = new Vector3(reveal.PadWorldX, 0f, reveal.PadWorldZ);
                if (CampaignMapCoordinates.TryWorldToGridCell(
                        padWorld,
                        worldMap,
                        ticSizeWorldUnits,
                        out var padCell))
                {
                    OccupiedBaseCellsScratch.Add(GridSquareSpawnState.PackCell(padCell));
                }
            }
        }

        private static bool IsBlockedAmbientSpawnCell(Vector2Int gridCell) =>
            OccupiedBaseCellsScratch.Contains(GridSquareSpawnState.PackCell(gridCell));

        private static void SuppressAmbientSpawnOnBlockedCell(Vector2Int gridCell)
        {
            if (GridSquareSpawnState.TryGetOutcome(gridCell, out var existing)
                && existing.Spawned
                && !existing.Destroyed)
            {
                GridSquareSpawnState.MarkDestroyed(gridCell);
                DestroyAmbientSiteRoot(GridSquareSiteIds.FormatSiteCode(gridCell));
                CombatThreatRange.InvalidateCaches();
                return;
            }

            if (!GridSquareSpawnState.TryGetOutcome(gridCell, out _))
            {
                GridSquareSpawnState.RecordOutcome(
                    gridCell,
                    new GridSquareSpawnState.CellOutcome(false, null, null));
            }
        }

        private static void DestroyAmbientSiteRoot(string siteCode)
        {
            if (string.IsNullOrWhiteSpace(siteCode))
            {
                return;
            }

            var siteRoot = FindSiteRoot(siteCode);
            if (siteRoot != null)
            {
                Object.Destroy(siteRoot.gameObject);
            }
        }

        private static string BuildUnitSlotLabel(VehicleUnitDefinition definition, int slotIndex)
        {
            var designation = definition != null ? definition.designation.ToString() : "UR";
            var abbrev = definition != null ? definition.abbreviation : "UNIT";
            return $"{designation}-{abbrev}-{slotIndex}";
        }

        private static int StableGridSeed(CharacterSaveData save, Vector2Int gridCell)
        {
            CampaignMissionProgress.EnsureInitialized(save);
            var missionNumber = save != null ? CampaignMissionProgress.GetCurrentMissionNumber(save) : 0;
            var saveSalt = save != null && !string.IsNullOrEmpty(save.Id) ? save.Id.GetHashCode() : 0;
            unchecked
            {
                return saveSalt
                    ^ missionNumber * 1315423911
                    ^ gridCell.x * 19349663
                    ^ gridCell.y * 83492791;
            }
        }
    }

    /// <summary>Scene marker for ambient grid-square spawn hierarchy.</summary>
    public sealed class GridSquareSpawnRoot : MonoBehaviour
    {
    }
}
