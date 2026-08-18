using F89.Core;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>Tags a surface guard so its death persists for the active outpost landing.</summary>
    public sealed class LandOutpostGuardMarker : MonoBehaviour
    {
        private string outpostName = string.Empty;
        private int guardIndex = -1;
        private int bossNumber;

        public void Configure(string activeOutpostName, int slotIndex, int assignedBossNumber)
        {
            outpostName = activeOutpostName ?? string.Empty;
            guardIndex = slotIndex;
            bossNumber = assignedBossNumber;
        }

        public void NotifyDestroyed()
        {
            if (string.IsNullOrWhiteSpace(outpostName) || guardIndex < 0)
            {
                return;
            }

            OutpostGroundGuardState.MarkGuardDestroyed(outpostName, guardIndex);
            if (!OutpostGroundGuardState.HasLivingSurfaceGuardsInScene())
            {
                OutpostGroundGuardState.PersistFullGuardClearance(
                    outpostName,
                    OutpostRunwayDeckState.ResolveRunwayGuardCount(outpostName));
                F89.UI.MissionObjectiveFlashNotifier.FlashSecondaryEliminated();
            }

            OutpostRunwayDeckState.NotifyGuardDestroyed(outpostName);
            if (bossNumber > 0
                && LandBossAreaCatalog.TryGet(bossNumber, out var area))
            {
                OutpostGroundGuardState.TryMarkBossGuardsCleared(
                    outpostName,
                    bossNumber,
                    area.GuardCount);
            }
        }
    }
}
