using F89.Flight;

using UnityEngine;



namespace F89.Core

{

    /// <summary>
    /// Persists the exact tactical-map mile coordinate where the aircraft landed (open field or sortie return).
    /// Supersedes the saved last-land base for spawn/VTOL while active. Cleared on VTOL takeoff.
    /// </summary>

    public static class LandingMileFlagState

    {

        private const string PrefsKey = "F89.LandingMileFlag";



        [System.Serializable]

        private struct LandingMileFlagData

        {

            public bool IsValid;

            public float MilesX;

            public float MilesY;

            public float RotationY;

        }



        private static LandingMileFlagData active;

        private static bool restoredFromPrefs;

        public static void ForceReloadFromPrefs()
        {
            restoredFromPrefs = false;
            EnsureRestored();
        }

        public static bool HasActiveFlag

        {

            get

            {

                EnsureRestored();

                return active.IsValid;

            }

        }



        public static Vector2 LandingMiles =>

            HasActiveFlag ? new Vector2(active.MilesX, active.MilesY) : Vector2.zero;



        public static void SetFromMiles(Vector2 miles, float rotationY)
        {
            EnsureRestored();
            active = new LandingMileFlagData
            {
                IsValid = true,
                MilesX = miles.x,
                MilesY = miles.y,
                RotationY = rotationY
            };
            Save();
            Debug.Log($"[F-89] Landing mile flag set at ({miles.x:0.0}, {miles.y:0.0}) MI.");
        }

        public static void SetFromWorldPosition(Vector3 worldPosition, Quaternion rotation)
        {
            SetFromLandingSquare(worldPosition, rotation);
        }

        /// <summary>
        /// Records the 1 MI grid square where the aircraft landed. All VTOL takeoffs use this until cleared.
        /// </summary>
        public static void SetFromLandingSquare(Vector3 worldPosition, Quaternion rotation)
        {
            EnsureRestored();
            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            if (worldMap != null
                && ticSize > 0f
                && CampaignMapCoordinates.TryWorldToGridCell(
                    worldPosition,
                    worldMap,
                    ticSize,
                    out var gridCell))
            {
                var center = CampaignMapCoordinates.GridCellToWorld(gridCell, worldMap, ticSize);
                var miles = CampaignMapCoordinates.WorldToMiles(center, worldMap, ticSize);
                active = new LandingMileFlagData
                {
                    IsValid = true,
                    MilesX = miles.x,
                    MilesY = miles.y,
                    RotationY = rotation.eulerAngles.y
                };
                Save();
                Debug.Log(
                    $"[F-89] Landing mile flag set at square {CampaignMapCoordinates.FormatMilesLabel(miles)}.");
                return;
            }

            var fallbackMiles = CampaignMapCoordinates.WorldToMiles(worldPosition, worldMap, ticSize);
            SetFromMiles(fallbackMiles, rotation.eulerAngles.y);
        }

        public static void ApplyToSnapshot(ref LandSortieSnapshot snapshot)

        {

            if (!HasActiveFlag)

            {

                return;

            }



            snapshot.HasLandingMiles = true;

            snapshot.LandingMileX = active.MilesX;

            snapshot.LandingMileY = active.MilesY;

            snapshot.LandingRotationY = active.RotationY;

            snapshot.AircraftWorldPosition = ResolveWorldPosition();

            snapshot.AircraftWorldRotation = ResolveRotation();

        }



        public static bool TryResolveWorldPosition(out Vector3 worldPosition, out Quaternion rotation)

        {

            EnsureRestored();

            worldPosition = Vector3.zero;

            rotation = Quaternion.identity;

            if (!active.IsValid)

            {

                return false;

            }



            worldPosition = ResolveWorldPosition();

            rotation = ResolveRotation();

            return true;

        }



        public static bool TryApplyToSnapshot(ref LandSortieSnapshot snapshot)

        {

            if (TryResolveFromSnapshot(snapshot, out var worldPosition, out var rotation))

            {

                snapshot.AircraftWorldPosition = worldPosition;

                snapshot.AircraftWorldRotation = rotation;

                return true;

            }



            if (!TryResolveWorldPosition(out worldPosition, out rotation))

            {

                return false;

            }



            snapshot.AircraftWorldPosition = worldPosition;

            snapshot.AircraftWorldRotation = rotation;

            snapshot.HasLandingMiles = true;

            snapshot.LandingMileX = active.MilesX;

            snapshot.LandingMileY = active.MilesY;

            snapshot.LandingRotationY = active.RotationY;

            return true;

        }



        public static bool TryResolveFromSnapshot(

            LandSortieSnapshot snapshot,

            out Vector3 worldPosition,

            out Quaternion rotation)

        {

            worldPosition = Vector3.zero;

            rotation = Quaternion.identity;

            if (!snapshot.HasLandingMiles)

            {

                return false;

            }



            worldPosition = CampaignMapCoordinates.MilesToWorld(new Vector2(snapshot.LandingMileX, snapshot.LandingMileY));

            rotation = Quaternion.Euler(0f, snapshot.LandingRotationY, 0f);

            return true;

        }



        public static void Clear()

        {

            active = default;

            restoredFromPrefs = true;

            PlayerPrefs.DeleteKey(PrefsKey);

            PlayerPrefs.Save();

        }



        public static void ClearAfterTakeoff()

        {

            if (!HasActiveFlag)

            {

                return;

            }



            Debug.Log(

                $"[F-89] Landing mile flag cleared after takeoff from "

                + $"({active.MilesX:0.0}, {active.MilesY:0.0}) MI.");

            Clear();

        }



        private static Vector3 ResolveWorldPosition()

        {

            return CampaignMapCoordinates.MilesToWorld(new Vector2(active.MilesX, active.MilesY));

        }



        private static Quaternion ResolveRotation()

        {

            return Quaternion.Euler(0f, active.RotationY, 0f);

        }



        private static void EnsureRestored()

        {

            if (restoredFromPrefs)

            {

                return;

            }



            restoredFromPrefs = true;

            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);

            if (string.IsNullOrEmpty(json))

            {

                return;

            }



            active = JsonUtility.FromJson<LandingMileFlagData>(json);

            if (!active.IsValid)

            {

                Clear();

                return;

            }



            MigrateLegacyCenterOriginMiles(ref active);

        }



        private static void MigrateLegacyCenterOriginMiles(ref LandingMileFlagData data)

        {

            if (!data.IsValid || (data.MilesX >= 0f && data.MilesY >= 0f))

            {

                return;

            }



            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");

            var mapWidth = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;

            var mapHeight = AntarcticaLandMask.GetMapHeightMiles(mapWidth);

            data.MilesX += mapWidth * 0.5f;

            data.MilesY = -data.MilesY + mapHeight * 0.5f;

            Save();

            Debug.Log(

                $"[F-89] Migrated landing mile flag to tactical coordinates "

                + $"({data.MilesX:0.0}, {data.MilesY:0.0}) MI.");

        }



        private static void Save()

        {

            restoredFromPrefs = true;

            if (!active.IsValid)

            {

                Clear();

                return;

            }



            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(active));

            PlayerPrefs.Save();

        }

    }

}

