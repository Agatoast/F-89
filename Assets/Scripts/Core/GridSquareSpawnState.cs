using System.Collections.Generic;
using UnityEngine;

namespace F89.Core
{
    /// <summary>Mission-scoped grid-square spawn rolls. Each GS is resolved once per campaign mission attempt.</summary>
    public static class GridSquareSpawnState
    {
        public readonly struct CellOutcome
        {
            public CellOutcome(bool spawned, string slotLabel, string vehicleAbbreviation, bool destroyed = false)
            {
                Spawned = spawned;
                SlotLabel = slotLabel ?? string.Empty;
                VehicleAbbreviation = vehicleAbbreviation ?? string.Empty;
                Destroyed = destroyed;
            }

            public bool Spawned { get; }
            public bool Destroyed { get; }
            public string SlotLabel { get; }
            public string VehicleAbbreviation { get; }

            public CellOutcome WithDestroyed() =>
                new CellOutcome(Spawned, SlotLabel, VehicleAbbreviation, destroyed: true);
        }

        private static readonly Dictionary<long, CellOutcome> MissionCells = new();
        private static int scopedMissionNumber = -1;
        private static string scopedSaveId;

        /// <summary>Each GS gets one spawn roll per campaign mission; re-briefing must not reset rolls mid-mission.</summary>
        public static void EnsureMissionScope(CharacterSaveData save)
        {
            if (save == null)
            {
                return;
            }

            CampaignMissionProgress.EnsureInitialized(save);
            var missionNumber = CampaignMissionProgress.GetCurrentMissionNumber(save);
            var saveId = save.Id ?? string.Empty;
            if (scopedMissionNumber == missionNumber && scopedSaveId == saveId)
            {
                return;
            }

            MissionCells.Clear();
            scopedMissionNumber = missionNumber;
            scopedSaveId = saveId;
        }

        public static bool HasBeenResolved(Vector2Int gridCell) =>
            MissionCells.ContainsKey(PackCell(gridCell));

        public static bool TryGetOutcome(Vector2Int gridCell, out CellOutcome outcome) =>
            MissionCells.TryGetValue(PackCell(gridCell), out outcome);

        public static void RecordOutcome(Vector2Int gridCell, CellOutcome outcome) =>
            MissionCells[PackCell(gridCell)] = outcome;

        public static void MarkDestroyed(Vector2Int gridCell)
        {
            var key = PackCell(gridCell);
            if (MissionCells.TryGetValue(key, out var outcome))
            {
                MissionCells[key] = outcome.WithDestroyed();
            }
        }

        public static void MarkDestroyedBySiteCode(string siteCode)
        {
            if (GridSquareSiteIds.TryParseSiteCode(siteCode, out var gridCell))
            {
                MarkDestroyed(gridCell);
            }
        }

        public static void ResetForNewMission()
        {
            MissionCells.Clear();
            scopedMissionNumber = -1;
            scopedSaveId = null;
        }

        public static long PackCell(Vector2Int gridCell) =>
            ((long)gridCell.x << 32) | (uint)gridCell.y;

        public static bool LandingCellHasHostileSpawn(Vector2 landingMiles)
        {
            if (!CampaignMapCoordinates.TryMilesToGridCell(landingMiles, out var gridCell))
            {
                return false;
            }

            if (!TryGetOutcome(gridCell, out var outcome))
            {
                return false;
            }

            return outcome.Spawned && !outcome.Destroyed;
        }
    }
}
