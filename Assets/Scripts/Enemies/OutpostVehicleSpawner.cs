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
        private const int MinVehicleCount = 20;
        private const int MaxVehicleCount = 30;
        private const int MinTroopCount = 10;
        private const int MaxTroopCount = 15;
        private const float SpawnMinRadiusMiles = 0.38f;
        private const float SpawnMaxRadiusMiles = 0.95f;

        public static void EnsureOpSouthEnemyPlatoon(AircraftController player)
        {
            RemovePlatoonsAtOtherSites(OpSouthSiteCode);
            EnsureSiteEnemyPlatoon(OpSouthSiteCode, player);
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
                if (existingPlatoon.childCount > 0)
                {
                    return;
                }

                Object.Destroy(existingPlatoon.gameObject);
            }

            var urUnits = CollectHostileCatalogUnits(includeTroops: false);
            if (urUnits.Count == 0)
            {
                Debug.LogWarning($"F-89: No UR vehicles in catalog — platoon skipped for {siteCode}.");
                return;
            }

            var urTroops = CollectHostileCatalogUnits(includeTroops: true, troopsOnly: true);
            var vehicleCount = ResolveSpawnCount(outpost, MinVehicleCount, MaxVehicleCount);
            var troopCount = urTroops.Count > 0
                ? ResolveSpawnCount(outpost, MinTroopCount, MaxTroopCount, salt: 7919)
                : 0;
            var playerTarget = player != null ? player.GetComponent<LockableTarget>() : null;
            var platoonRoot = new GameObject(PlatoonRootName);
            platoonRoot.transform.SetParent(outpost.transform, false);

            var spawned = 0;
            for (var i = 0; i < vehicleCount; i++)
            {
                var definition = urUnits[i % urUnits.Count];
                var spawnPos = ResolveSpawnWorldPosition(
                    outpost.transform.position,
                    i,
                    vehicleCount,
                    worldUnitsPerMile,
                    worldMap,
                    definition,
                    ticSize);
                if (!SpawnGroundUnit(
                        platoonRoot.transform,
                        definition,
                        spawnPos,
                        worldMap,
                        profile,
                        worldUnitsPerMile,
                        playerTarget))
                {
                    continue;
                }

                spawned++;
            }

            for (var i = 0; i < troopCount; i++)
            {
                var definition = urTroops[i % urTroops.Count];
                var spawnPos = ResolveSpawnWorldPosition(
                    outpost.transform.position,
                    vehicleCount + i,
                    vehicleCount + troopCount,
                    worldUnitsPerMile,
                    worldMap,
                    definition,
                    ticSize);
                if (!SpawnGroundUnit(
                        platoonRoot.transform,
                        definition,
                        spawnPos,
                        worldMap,
                        profile,
                        worldUnitsPerMile,
                        playerTarget))
                {
                    continue;
                }

                spawned++;
            }

            Debug.Log(
                $"F-89: Spawned {spawned} UR ground unit(s) ({vehicleCount} vehicles, {troopCount} troops) "
                + $"at {outpost.SiteCode} ({outpost.BaseName}). Building cluster present.");
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

        private static bool SpawnGroundUnit(
            Transform parent,
            VehicleUnitDefinition definition,
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float worldUnitsPerMile,
            LockableTarget playerTarget)
        {
            if (definition == null)
            {
                return false;
            }

            var unitObject = new GameObject(definition.abbreviation);
            unitObject.transform.SetParent(parent, true);
            unitObject.transform.position = worldPosition;

            var unit = unitObject.AddComponent<VehicleUnitComponent>();
            unit.Configure(definition);

            VehicleUnitVisual.Attach(unitObject.transform, definition, profile);

            var behavior = unitObject.AddComponent<OutpostGroundUnitBehavior>();
            behavior.Configure(definition, worldMap, profile, worldUnitsPerMile, playerTarget);
            return true;
        }

        private static List<VehicleUnitDefinition> CollectHostileCatalogUnits(
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
                if (unit == null || unit.designation != VehicleUnitDesignation.UR)
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
            float ticSizeWorldUnits)
        {
            var isFlier = definition != null && definition.isFlier;
            var fliesOverGround = definition != null && definition.fliesOverGroundUnits;
            outpostWorld.y = 0f;
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            var keepOutWorld = OutpostGroundRules.KeepOutRadiusWorld(ticSizeWorldUnits);
            var minRadiusMiles = fliesOverGround
                ? SpawnMinRadiusMiles
                : Mathf.Max(SpawnMinRadiusMiles, (keepOutWorld / worldUnitsPerMile) * 1.05f);

            var goldenAngle = 2.399963f;
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var angle = (index * goldenAngle) + (attempt * 0.31f);
                var t = totalCount > 1 ? index / (float)(totalCount - 1) : 0.5f;
                var radiusMiles = Mathf.Lerp(minRadiusMiles, SpawnMaxRadiusMiles, t) * (1f - attempt * 0.025f);
                var candidate = outpostWorld + new Vector3(
                    Mathf.Cos(angle) * radiusMiles * worldUnitsPerMile,
                    0f,
                    Mathf.Sin(angle) * radiusMiles * worldUnitsPerMile);

                if (isFlier)
                {
                    candidate.y = fliesOverGround
                        ? TacScale.TacsToWorld(OutpostGroundRules.HelicopterCruiseAltitudeTacs, ticSizeWorldUnits)
                        : TacScale.TacsToWorld(1.5f, ticSizeWorldUnits);
                    if (fliesOverGround
                        || GroundUnitMovement.IsAllowedWorldPosition(
                            candidate,
                            worldMap,
                            worldUnitsPerMile,
                            mapSizeMiles))
                    {
                        return candidate;
                    }

                    continue;
                }

                if (GroundUnitMovement.IsAllowedWorldPosition(
                        candidate,
                        worldMap,
                        worldUnitsPerMile,
                        mapSizeMiles))
                {
                    return candidate;
                }
            }

            var fallback = outpostWorld + Vector3.right * (minRadiusMiles * worldUnitsPerMile);
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
