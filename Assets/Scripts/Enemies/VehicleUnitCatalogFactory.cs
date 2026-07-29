using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Master vehicle/troop table (UR + US). TRP troops: 1 GHP, 3-tic range, 1 GHP damage ground/air.
    /// </summary>
    public static class VehicleUnitCatalogFactory
    {
        public static VehicleUnitCatalog CreateDefault()
        {
            var catalog = ScriptableObject.CreateInstance<VehicleUnitCatalog>();
            catalog.units = new[]
            {
                // UR — Ultimate Reich
                Ur("PHT", 1, 11, 0.30f, 0.05f, 11f, 13f, 20, 0.30f, 3, 0.50f, 1, 1),
                Ur("MBT", 2, 12, 0.30f, 0.10f, 10f, 12f, 21, 0.30f, 4, 0.50f, 2, 2),
                Ur("FW", 3, 13, 0.30f, 0.20f, 10f, 12f, 22, 0.30f, 5, 0.50f, 3, 3),
                Ur("VHS", 4, 14, 0.30f, 0.30f, 10f, 12f, 23, 0.30f, 6, 0.50f, 4, 4),
                Ur("MC", 5, 15, 0.40f, 0.40f, 9f, 11f, 24, 0.40f, 7, 0.50f, 5, 5),
                Ur("HAR", 6, 16, 0.50f, 0.50f, 9f, 11f, 25, 0.50f, 8, 0.50f, 6, 6),
                UrFlier("TDP", 7, 17, 0.60f, 0.60f, 9f, 11f, 26, 0.60f, 9, 0.60f, 7, 7, speedMph: 300f),
                Ur("ARW", 8, 18, 0.70f, 0.70f, 8f, 10f, 27, 0.70f, 10, 0.70f, 8, 8),
                Ur("HCT", 9, 19, 0.80f, 0.80f, 8f, 10f, 28, 0.80f, 11, 0.80f, 9, 9),
                Ur("AH", 10, 20, 0.90f, 0.90f, 8f, 10f, 30, 0.90f, 11, 0.90f, 10, 10),
                UrTroop("TRP", 1, 3, 1.0f, 0f, 8f, 10f, 3, 1.0f, 1, 0.50f, 1, speedMph: 12f, groundHitDamage: 1),

                // US — friendly
                Us("ISV", 1, 10, 0.20f, 0.05f, 11f, 13f, 20, 0.20f, 3, -1, 1),
                Us("FMTV", 2, 11, 0.30f, 0.10f, 10f, 12f, 21, 0.30f, 4, -2, 2),
                Us("HEMTT", 3, 12, 0.30f, 0.20f, 10f, 12f, 22, 0.30f, 5, -3, 3),
                Us("MRAP", 4, 13, 0.30f, 0.30f, 10f, 12f, 23, 0.30f, 6, -4, 4),
                Us("JLTV", 5, 14, 0.30f, 0.40f, 9f, 11f, 24, 0.30f, 7, -5, 5),
                Us("ICV", 6, 15, 0.40f, 0.50f, 9f, 11f, 25, 0.40f, 8, -6, 6),
                Us("M109A7", 7, 16, 0.50f, 0.60f, 9f, 11f, 26, 0.50f, 9, -7, 7),
                Us("M2A4", 8, 17, 0.60f, 0.70f, 8f, 10f, 27, 0.60f, 10, -8, 8),
                UsFlier("AH-64", 9, 18, 0.70f, 0.80f, 8f, 10f, 28, 0.70f, 11, -9, 9, speedMph: 189f),
                Us("M1A2", 10, 19, 0.80f, 0.90f, 8f, 10f, 30, 0.80f, 11, -10, 10),
                UsTroop("INF", 1, 10, 0.20f, 0.05f, 11f, 13f, 20, 0.20f, 3, -1, speedMph: 12f),
                UsTroop("TRP", 1, 3, 1.0f, 0f, 8f, 10f, 3, 1.0f, 1, -1, speedMph: 12f, groundHitDamage: 1)
            };
            return catalog;
        }

        private static VehicleUnitDefinition Ur(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            float planeVsOther,
            int pointValue,
            int ghp)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.UR,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther,
                pointValue,
                ghp,
                isFlier: false);
        }

        private static VehicleUnitDefinition UrFlier(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            float planeVsOther,
            int pointValue,
            int ghp,
            float speedMph = 30f)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.UR,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther,
                pointValue,
                ghp,
                isFlier: true,
                speedMph: speedMph,
                fliesOverGroundUnits: true);
        }

        private static VehicleUnitDefinition UrTroop(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            float planeVsOther,
            int pointValue,
            float speedMph = 12f,
            int groundHitDamage = 0)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.UR,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther,
                pointValue,
                ghp: GroundTargetGhp.Troop,
                isFlier: false,
                isTroop: true,
                speedMph,
                groundHitDamage: groundHitDamage);
        }

        private static VehicleUnitDefinition UsTroop(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            int pointValue,
            float speedMph = 12f,
            int groundHitDamage = 0)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.US,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther: 0f,
                pointValue,
                ghp: GroundTargetGhp.Troop,
                isFlier: false,
                isTroop: true,
                speedMph,
                groundHitDamage: groundHitDamage);
        }

        private static VehicleUnitDefinition Us(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            int pointValue,
            int ghp)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.US,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther: 0f,
                pointValue,
                ghp,
                isFlier: false);
        }

        private static VehicleUnitDefinition UsFlier(
            string abbreviation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            int pointValue,
            int ghp,
            float speedMph = 30f)
        {
            return Build(
                abbreviation,
                VehicleUnitDesignation.US,
                level,
                groundRangeTics,
                chanceToHitGround,
                chanceToKill,
                fireRateMinSeconds,
                fireRateMaxSeconds,
                airRangeTics,
                chanceToHitAir,
                airDamage,
                planeVsOther: 0f,
                pointValue,
                ghp,
                isFlier: true,
                speedMph: speedMph,
                fliesOverGroundUnits: true);
        }

        private static VehicleUnitDefinition Build(
            string abbreviation,
            VehicleUnitDesignation designation,
            int level,
            float groundRangeTics,
            float chanceToHitGround,
            float chanceToKill,
            float fireRateMinSeconds,
            float fireRateMaxSeconds,
            float airRangeTics,
            float chanceToHitAir,
            int airDamage,
            float planeVsOther,
            int pointValue,
            int ghp,
            bool isFlier,
            bool isTroop = false,
            float speedMph = 30f,
            bool fliesOverGroundUnits = false,
            int groundHitDamage = 0)
        {
            var def = ScriptableObject.CreateInstance<VehicleUnitDefinition>();
            def.abbreviation = abbreviation;
            def.designation = designation;
            def.vehicleLevel = level;
            def.isFlier = isFlier;
            def.isTroop = isTroop;
            def.fliesOverGroundUnits = fliesOverGroundUnits;
            def.speedMph = speedMph;
            def.ghp = ghp;
            def.groundHitDamage = groundHitDamage;
            def.groundRangeTics = groundRangeTics;
            def.chanceToHitGround = chanceToHitGround;
            def.chanceToKill = chanceToKill;
            def.fireRateMinSeconds = fireRateMinSeconds;
            def.fireRateMaxSeconds = fireRateMaxSeconds;
            def.airRangeTics = airRangeTics;
            def.chanceToHitAir = chanceToHitAir;
            def.airDamage = airDamage;
            def.planeVsOtherTargetChance = planeVsOther;
            def.pointValue = pointValue;
            def.name = $"{designation}_{abbreviation}";
            return def;
        }
    }
}
