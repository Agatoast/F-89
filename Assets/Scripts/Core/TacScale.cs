namespace F89.Core
{
    /// <summary>
    /// Canonical ground / map square unit for F-89.
    /// One tac is a square: 260 feet × 260 feet.
    /// Flight grid tics remain 264 feet (20 tics per mile).
    /// </summary>
    public static class TacScale
    {
        public const float FeetPerTac = 260f;
        public const float FeetPerMile = 5280f;

        /// <summary>Flight grid tic length in feet (5280 / 20).</summary>
        public const float FeetPerTic = FeetPerMile / 20f;

        /// <summary>How many tac sides fit in one mile (5280 / 260 ≈ 20.3077).</summary>
        public const float TacsPerMile = FeetPerMile / FeetPerTac;

        public static float FeetToTacs(float feet) => feet / FeetPerTac;

        public static float TacsToFeet(float tacs) => tacs * FeetPerTac;

        public static float MilesToTacs(float miles) => miles * TacsPerMile;

        public static float TacsToMiles(float tacs) => tacs / TacsPerMile;

        /// <summary>World units for one tac side when 1 tic = <paramref name="ticSizeWorldUnits"/>.</summary>
        public static float WorldUnitsPerTac(float ticSizeWorldUnits = 1f)
        {
            return ticSizeWorldUnits * (FeetPerTac / FeetPerTic);
        }

        public static float TacsToWorld(float tacs, float ticSizeWorldUnits = 1f)
        {
            return tacs * WorldUnitsPerTac(ticSizeWorldUnits);
        }

        public static float TicsToWorld(float tics, float ticSizeWorldUnits = 1f)
        {
            return tics * ticSizeWorldUnits;
        }
    }
}
