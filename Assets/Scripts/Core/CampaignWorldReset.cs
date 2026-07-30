using F89.Enemies;
using F89.Flight;
using F89.LandCombat;
using F89.Weapons;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Resets per-character outpost destruction, platoon kills, bunkers, and boss progress.</summary>
    public static class CampaignWorldReset
    {
        public static bool ShouldSkipDuringActiveSortie() =>
            LandMissionHandoffState.HasActiveSortieHandoff();

        /// <summary>Clears sortie handoff and resets all outpost/bunker/platoon/boss world progress.</summary>
        public static void ResetMapForFreshPlay()
        {
            LandMissionHandoffState.Clear();
            OutpostRunwayDeckState.Clear();
            CarrierResupplyState.Clear();
            FriendlyOutpostTakeoffState.Clear();
            LandMissionCompleteState.Clear();
            LandMissionHealthState.Clear();
            ResetAllOutpostsAndBunkers(force: true);
        }

        public static void ResetAllOutpostsAndBunkers(bool force = false)
        {
            if (!force && ShouldSkipDuringActiveSortie())
            {
                return;
            }

            var save = CharacterSessionState.ActiveSave;
            if (save == null)
            {
                Debug.LogWarning("F-89: Cannot reset outposts and bunkers — no active character save.");
                return;
            }

            ResetSaveState(save);
            ApplyRuntimeWorldReset();
            Debug.Log("F-89: Reset all outposts, bunkers, platoons, and boss progress.");
        }

        private static void ResetSaveState(CharacterSaveData save)
        {
            var destroyedOutposts = save.DestroyedOutpostNames?.Length ?? 0;
            var destroyedTargets = save.DestroyedWorldTargetIds?.Length ?? 0;

            save.DestroyedOutpostNames = System.Array.Empty<string>();
            save.DestroyedWorldTargetIds = System.Array.Empty<string>();
            save.FriendlyOccupiedOutpostNames = System.Array.Empty<string>();
            save.MissionLaunchOutpostName = string.Empty;
            save.RevealedBunkerMask = 0;
            save.UrVehicleKillsByLevel = new int[UrKillCredit.LevelCount];
            save.UrTroopKillsByLevel = new int[UrKillCredit.LevelCount];
            save.EnemyVehiclesKilled = 0;
            save.EnemyTroopsKilled = 0;

            LandBossEncounter.ResetAllBossesForTest();
            LandBossMissionAssignment.InitializeBossMissionOutpostLinks(save);
            LandBossMissionAssignment.PrepareNextAssignment(save);

            CharacterSaveRepository.WriteWorldProgress(save);
            CharacterSaveRepository.WriteBossProgress(save);

            if (destroyedOutposts > 0 || destroyedTargets > 0)
            {
                Debug.Log(
                    $"F-89: Cleared {destroyedOutposts} destroyed outpost(s) "
                    + $"and {destroyedTargets} destroyed world target(s).");
            }
        }

        private static void ApplyRuntimeWorldReset()
        {
            if (!ShouldPreserveFlightHandoff())
            {
                OutpostRunwayDeckState.Clear();
                LandMissionHandoffState.Clear();
            }

            OutpostVehicleSpawner.RemoveAllPlatoons();
            EnemySamLauncher.RemoveFromOutpostBases();

            var worldMap = Resources.Load<WorldMapConfig>("F89_WorldMapConfig");
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap, profile);
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var bases = Object.FindObjectsByType<AntarcticaBase>(FindObjectsSortMode.None);
            for (var i = 0; i < bases.Length; i++)
            {
                var baseSite = bases[i];
                if (baseSite == null || baseSite.SiteKind != BaseSiteKind.Land)
                {
                    continue;
                }

                DestroyRuntimeOutpostArtifacts(baseSite.transform);
                baseSite.RefreshPersistedWorldState(worldUnitsPerMile);
                OutpostBuildingClusterSpawner.EnsureCluster(baseSite, worldUnitsPerMile, ticSize);
            }
        }

        private static void DestroyRuntimeOutpostArtifacts(Transform outpostRoot)
        {
            if (outpostRoot == null)
            {
                return;
            }

            DestroyChild(outpostRoot, OutpostBuildingClusterSpawner.ClusterRootName);
            DestroyChild(outpostRoot, OutpostRunwayVisual.RunwayObjectName);
            DestroyChild(outpostRoot, "VehiclePlatoon");
            DestroyChild(outpostRoot, "SurfaceBunkerPad");
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                Object.Destroy(child.gameObject);
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

        private static bool ShouldPreserveFlightHandoff()
        {
            return LandMissionHandoffState.HasPendingGroundReturn
                || LandMissionHandoffState.HasPendingEnter
                || LandMissionHandoffState.ShouldSuppressCarrierRespawn
                || LandMissionHandoffState.GetStoredFlightSnapshot().IsValid;
        }
    }
}
