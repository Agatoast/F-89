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
            if (LandingMileFlagState.HasActiveFlag
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn
                || LandMissionHandoffState.HasPendingGroundReturn
                || AircraftLandingController.IsTakeoffActive
                || AircraftLandingController.IsParkedAtRunway)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(FlightMissionLaunchState.LaunchFromOutpostName)
                && !FlightMissionLaunchState.LaunchFromCarrier)
            {
                return true;
            }

            return MissionLaunchOrigin.TryResolveLaunchOutpost(
                CharacterSessionState.ActiveSave,
                out _);
        }

        public static Vector3 ResolveSortieReturnPosition(LandSortieSnapshot snapshot, WorldMapConfig worldMap, float ticSize)
        {
            FlightGroundReturnService.EnsureWaypointLandingFlag(snapshot);

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

            if (!string.IsNullOrWhiteSpace(snapshot.WaypointSiteCode)
                && CampaignWaypointSecondaryState.TryGetPadWorldPosition(snapshot.WaypointSiteCode, out var padPosition))
            {
                padPosition.y = 0f;
                return padPosition;
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

            if (MissionLaunchOrigin.TryResolveLaunchOutpost(
                    CharacterSessionState.ActiveSave,
                    out var launchOutpostName)
                && TryApplySavedLaunchOutpostSpawn(aircraft, launchOutpostName))
            {
                VtolTakeoffLaunch.BeginAt(aircraft, aircraft.transform.position);
                return true;
            }

            Debug.LogError(
                $"F-89: Invalid spawn {CampaignMapCoordinates.FormatMilesLabel(CampaignMapCoordinates.WorldToMiles(position, worldMap, ticSize))} — ocean CV relocation removed.");
            return false;
        }

        private static bool TryApplySavedLaunchOutpostSpawn(
            AircraftController aircraft,
            string outpostName)
        {
            if (aircraft == null || string.IsNullOrWhiteSpace(outpostName))
            {
                return false;
            }

            if (!OutpostRunwayLanding.TryGetRunwaySpawn(
                    outpostName,
                    out var spawnPosition,
                    out var spawnRotation)
                && !OutpostRunwayLanding.TryGetLayoutMilesSpawn(
                    outpostName,
                    out spawnPosition,
                    out spawnRotation))
            {
                return false;
            }

            spawnPosition.y = 0f;
            aircraft.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var body = aircraft.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = spawnPosition;
                body.rotation = spawnRotation;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            Debug.LogWarning(
                $"F-89: Recovered saved launch outpost '{outpostName}' at "
                + $"{CampaignMapCoordinates.FormatMilesLabel(CampaignMapCoordinates.WorldToMiles(spawnPosition, aircraft.WorldMap, aircraft.Profile != null ? aircraft.Profile.ticSizeWorldUnits : 1f))} "
                + "(avoided ocean CV fallback).");
            return true;
        }
    }
}
