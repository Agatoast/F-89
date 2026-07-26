using System;
using System.Collections.Generic;

namespace F89.Core
{
    public readonly struct MilitaryRibbonGridSlot
    {
        public MilitaryRibbonGridSlot(int row, int column, string ribbonId)
        {
            Row = row;
            Column = column;
            RibbonId = ribbonId;
        }

        public int Row { get; }
        public int Column { get; }
        public string RibbonId { get; }
    }

    public static class MilitaryRibbonLayout
    {
        public static int[] GetRowCounts(int ribbonCount)
        {
            if (ribbonCount <= 0)
            {
                return Array.Empty<int>();
            }

            if (ribbonCount <= MilitaryRibbonCatalog.MaxRibbonsPerRow)
            {
                return new[] { ribbonCount };
            }

            var remainder = ribbonCount % MilitaryRibbonCatalog.MaxRibbonsPerRow;
            var fullRowCount = ribbonCount / MilitaryRibbonCatalog.MaxRibbonsPerRow;
            var rowCounts = new List<int>();
            if (remainder > 0)
            {
                rowCounts.Add(remainder);
            }

            for (var i = 0; i < fullRowCount; i++)
            {
                rowCounts.Add(MilitaryRibbonCatalog.MaxRibbonsPerRow);
            }

            return rowCounts.ToArray();
        }

        public static IReadOnlyList<MilitaryRibbonGridSlot> BuildGridSlots(IReadOnlyList<string> earnedRibbonIds)
        {
            var slots = new List<MilitaryRibbonGridSlot>();
            if (earnedRibbonIds == null || earnedRibbonIds.Count == 0)
            {
                return slots;
            }

            var sortedRibbonIds = new List<string>(earnedRibbonIds);
            sortedRibbonIds.Sort((left, right) => MilitaryRibbonCatalog.ComparePrecedence(right, left));

            var rowCounts = GetRowCounts(sortedRibbonIds.Count);
            var ribbonIndex = 0;
            for (var row = rowCounts.Length - 1; row >= 0; row--)
            {
                for (var column = 0; column < rowCounts[row]; column++)
                {
                    slots.Add(new MilitaryRibbonGridSlot(row, column, sortedRibbonIds[ribbonIndex]));
                    ribbonIndex++;
                }
            }

            return slots;
        }
    }
}
