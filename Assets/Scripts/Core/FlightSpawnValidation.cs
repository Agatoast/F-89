using F89.Flight;
using F89.LandCombat;
using F89.Testing;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Validates carrier-deck spawns only. Sortie returns use tactical landing miles directly.</summary>
    public static class FlightSpawnValidation
    {
        public static bool ShouldBypassSpawnCorrection()
        {
            return LandingMileFlagState.HasActiveFlag
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn
                || LandMissionHandoffState.HasPendingGroundReturn
                || AircraftLandingController.IsTakeoffActive;
        }

        public static Vector3 ResolveSortieReturnPosition(LandSortieSnapshot snapshot, WorldMapConfig worldMap, float ticSize)
        {
            if (LandingMileFlagState.TryResolveFromSnapshot(snapshot, out var mileWorld, out _))
            {
                mileWorld.y = 0f;
                return mileWorld;
            }

            if (LandingMileFlagState.TryResolveWorldPosition(out mileWorld, out _))
            {
                mileWorld.y = 0f;
                return mileWorld;
            }

            if (snapshot.IsOpenFieldLanding && OpenFieldLandingState.LandingMiles.sqrMagnitude > 0.01f)
            {
                return CampaignMapCoordinates.MilesToWorld(OpenFieldLandingState.LandingMiles, worldMap, ticSize);
            }

            if (snapshot.HasLandingMiles)
            {
                return CampaignMapCoordinates.MilesToWorld(
                    new Vector2(snapshot.LandingMileX, snapshot.LandingMileY),
                    worldMap,
                    ticSize);
            }

            var aircraftPos = snapshot.AircraftWorldPosition;
            aircraftPos.y = 0f;
            return aircraftPos;
        }

        /// <summary>Land or explicit carrier deck only — never treat open ocean as valid except on the CV deck.</summary>
        public static bool IsValidSpawnPosition(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            FlightProfile profile,
            float ticSize)
        {
            if (worldMap == null || ticSize <= 0f)
            {
                return false;
            }

            worldPosition.y = 0f;
            if (!worldMap.IsWithinAntarcticaBounds(worldPosition, ticSize))
            {
                return false;
            }

            if (IsNearCarrierDeck(worldPosition, worldMap, profile))
            {
                return true;
            }

            return AntarcticaLandMask.GetLandBlendAtWorld(worldPosition, worldMap, ticSize) >= 0.5f;
        }

        public static bool IsNearCarrierDeck(
            Vector3 worldPosition,
            WorldMapConfig worldMap,
            FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            return AntarcticaWorldLocations.IsWithinCarrierDeckRange(
                worldPosition,
                worldMap,
                ticSize,
                F89.Flight.AircraftLanding.CarrierLandingRangeMiles);
        }

        public static bool TryResolveOutpostRunwayFallback(
            LandSortieSnapshot snapshot,
            out Vector3 spawnPosition,
            out Quaternion spawnRotation)
        {
            spawnPosition = Vector3.zero;
            spawnRotation = Quaternion.identity;
            if (string.IsNullOrWhiteSpace(snapshot.OutpostName))
            {
                return false;
            }

            return OutpostRunwayLanding.TryGetRunwaySpawn(
                snapshot.OutpostName,
                out spawnPosition,
                out spawnRotation);
        }

        /// <summary>
        /// Corrects only invalid fresh spawns. Never relocates a sortie return or landing-mile takeoff.
        /// </summary>
        public static bool EnsurePlausibleOrCarrier(AircraftController aircraft)
        {
            if (aircraft == null || ShouldBypassSpawnCorrection())
            {
                return true;
            }

            var worldMap = aircraft.WorldMap ?? Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = aircraft.Profile ?? Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var position = aircraft.transform.position;

            if (AircraftLandingController.IsParkedAtRunway)
            {
                return IsValidSpawnPosition(position, worldMap, profile, ticSize);
            }

            if (IsValidSpawnPosition(position, worldMap, profile, ticSize))
            {
                return true;
            }

            if (OutpostRunwayLanding.TryFindNearestRunway(aircraft, out var baseSite, out _, out _)
                && baseSite != null
                && AntarcticaOutpostState.IsFriendlyBase(baseSite.BaseName))
            {
                return true;
            }

            Debug.LogWarning(
                $"F-89: Aircraft at invalid spawn {CampaignMapCoordinates.FormatMilesLabel(CampaignMapCoordinates.WorldToMiles(position, worldMap, ticSize))} — moving to carrier deck.");
            LandMissionHandoffState.Clear();
            OutpostRunwayDeckState.Clear();
            LandingMileFlagState.Clear();

            AntarcticaBaseSpawner.TryMovePlayerToCarrier(aircraft.transform, worldMap, profile);
            if (!IsValidSpawnPosition(aircraft.transform.position, worldMap, profile, ticSize))
            {
                AntarcticaBaseSpawner.ForcePlayerToDefaultCarrier(aircraft.transform, worldMap, profile);
            }

            VtolTakeoffLaunch.BeginAt(aircraft, aircraft.transform.position);
            return false;
        }
    }
}
