using F89.Enemies;



namespace F89.Core

{

    /// <summary>Persisted flight-map platoon clearance — prevents respawning killed units.</summary>

    public static class OutpostFlightPlatoonState

    {

        public const string ClearedLabel = "FlightPlatoon-Cleared";



        public static bool IsPlatoonClearedInSave(string outpostName) =>

            !string.IsNullOrWhiteSpace(outpostName)

            && AntarcticaOutpostState.IsTargetDestroyed(outpostName, ClearedLabel);



        public static void MarkPlatoonCleared(string outpostName)

        {

            if (string.IsNullOrWhiteSpace(outpostName))

            {

                return;

            }



            AntarcticaOutpostState.MarkTargetDestroyed(outpostName, ClearedLabel);

        }



        public static bool HasPlatoonKillEvidence(string outpostName)

        {

            if (string.IsNullOrWhiteSpace(outpostName))

            {

                return false;

            }



            if (IsPlatoonClearedInSave(outpostName))

            {

                return true;

            }



            var save = CharacterSessionState.ActiveSave;

            if (save?.DestroyedWorldTargetIds == null)

            {

                return false;

            }



            var prefix = $"{outpostName.Trim()}::";

            for (var i = 0; i < save.DestroyedWorldTargetIds.Length; i++)

            {

                var targetId = save.DestroyedWorldTargetIds[i];

                if (string.IsNullOrEmpty(targetId) || !targetId.StartsWith(prefix, System.StringComparison.Ordinal))

                {

                    continue;

                }



                var label = targetId.Substring(prefix.Length);

                if (label == ClearedLabel

                    || label.StartsWith("UR-", System.StringComparison.Ordinal)

                    || label.StartsWith("US-", System.StringComparison.Ordinal))

                {

                    return true;

                }

            }



            return false;

        }



        public static bool IsPlatoonFullyCleared(string outpostName)

        {

            if (string.IsNullOrWhiteSpace(outpostName))

            {

                return false;

            }



            if (IsPlatoonClearedInSave(outpostName))

            {

                return true;

            }



            var outpost = FindOutpost(outpostName);

            if (outpost == null)

            {

                return HasPlatoonKillEvidence(outpostName);

            }



            return ShouldSkipPlatoonRespawn(outpost);

        }



        /// <summary>True when this outpost must never receive a new flight-map platoon.</summary>

        public static bool ShouldSkipPlatoonRespawn(AntarcticaBase outpost)

        {

            if (outpost == null)

            {

                return false;

            }



            if (AntarcticaOutpostState.IsFriendlyOccupied(outpost.BaseName)

                || outpost.Control != BaseControl.Hostile)

            {

                return true;

            }



            if (IsPlatoonClearedInSave(outpost.BaseName))

            {

                return true;

            }



            var platoon = outpost.transform.Find("VehiclePlatoon");

            if (platoon != null)

            {

                if (OutpostVehicleSpawner.PlatoonHasLivingUnits(platoon))

                {

                    return false;

                }



                OutpostVehicleSpawner.MarkAllPlatoonSlotsDestroyed(outpost);

                return true;

            }



            if (HasPlatoonKillEvidence(outpost.BaseName))

            {

                MarkPlatoonCleared(outpost.BaseName);

                return true;

            }



            return OutpostVehicleSpawner.AreAllPlatoonSlotsDestroyed(outpost);

        }



        /// <summary>When the last platoon unit dies, persist full clearance immediately.</summary>

        public static void TryFinalizePlatoonClearance(AntarcticaBase outpost)

        {

            if (outpost == null)

            {

                return;

            }



            var platoon = outpost.transform.Find("VehiclePlatoon");

            if (platoon != null && OutpostVehicleSpawner.PlatoonHasLivingUnits(platoon))

            {

                return;

            }



            if (platoon == null && !HasPlatoonKillEvidence(outpost.BaseName))

            {

                return;

            }



            OutpostVehicleSpawner.MarkAllPlatoonSlotsDestroyed(outpost);

            OutpostVehicleSpawner.EnsureEmptyPlatoonMarker(outpost);

        }



        public static void SyncOutpostFromScene(string outpostName)

        {

            if (string.IsNullOrWhiteSpace(outpostName))

            {

                return;

            }



            var outpost = FindOutpost(outpostName);

            if (outpost == null)

            {

                if (HasPlatoonKillEvidence(outpostName))

                {

                    MarkPlatoonCleared(outpostName);

                }



                return;

            }



            if (ShouldSkipPlatoonRespawn(outpost))

            {

                OutpostVehicleSpawner.EnsureEmptyPlatoonMarker(outpost);

            }

        }



        private static AntarcticaBase FindOutpost(string outpostName)

        {

            var bases = UnityEngine.Object.FindObjectsByType<AntarcticaBase>(UnityEngine.FindObjectsSortMode.None);

            for (var i = 0; i < bases.Length; i++)

            {

                var candidate = bases[i];

                if (candidate != null

                    && candidate.SiteKind == BaseSiteKind.Land

                    && string.Equals(candidate.BaseName, outpostName, System.StringComparison.Ordinal))

                {

                    return candidate;

                }

            }



            return null;

        }

    }

}


