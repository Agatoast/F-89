using UnityEngine;

namespace F89.Weapons
{
    /// <summary>
    /// Integer tic cells for weapon blast footprints (not mile-grid cells).
    /// Chebyshev distance includes diagonals: distance 1 = 3×3, distance 2 = 5×5.
    /// </summary>
    public static class TicGrid
    {
        public static Vector2Int WorldToTicCell(Vector3 worldPosition, float ticSizeWorldUnits)
        {
            var ticSize = Mathf.Max(0.0001f, ticSizeWorldUnits);
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / ticSize),
                Mathf.FloorToInt(worldPosition.z / ticSize));
        }

        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        public static bool IsWithinChebyshev(
            Vector3 centerWorld,
            Vector3 targetWorld,
            float ticSizeWorldUnits,
            int maxChebyshevDistance)
        {
            var centerCell = WorldToTicCell(centerWorld, ticSizeWorldUnits);
            var targetCell = WorldToTicCell(targetWorld, ticSizeWorldUnits);
            return ChebyshevDistance(centerCell, targetCell) <= maxChebyshevDistance;
        }
    }
}
