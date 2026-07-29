using System;
using UnityEngine;

namespace F89.Enemies
{
    [CreateAssetMenu(fileName = "VehicleUnitCatalog", menuName = "F-89/Enemies/Vehicle Unit Catalog")]
    public class VehicleUnitCatalog : ScriptableObject
    {
        private static VehicleUnitCatalog cached;

        public VehicleUnitDefinition[] units = Array.Empty<VehicleUnitDefinition>();

        public static VehicleUnitCatalog LoadOrDefault()
        {
            if (cached != null)
            {
                return cached;
            }

            cached = Resources.Load<VehicleUnitCatalog>("F89_VehicleUnitCatalog");
            if (cached == null)
            {
                cached = VehicleUnitCatalogFactory.CreateDefault();
            }

            return cached;
        }

        public bool TryGetByAbbreviation(string abbreviation, out VehicleUnitDefinition definition)
        {
            return TryGetByAbbreviation(abbreviation, designation: null, out definition);
        }

        public bool TryGetByAbbreviation(
            string abbreviation,
            VehicleUnitDesignation? designation,
            out VehicleUnitDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(abbreviation) || units == null)
            {
                return false;
            }

            VehicleUnitDefinition fallback = null;
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null
                    || !string.Equals(unit.abbreviation, abbreviation.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (designation.HasValue && unit.designation == designation.Value)
                {
                    definition = unit;
                    return true;
                }

                fallback ??= unit;
            }

            if (fallback == null)
            {
                return false;
            }

            definition = fallback;
            return true;
        }

        public VehicleUnitDefinition GetByLevel(VehicleUnitDesignation designation, int level)
        {
            if (units == null)
            {
                return null;
            }

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit != null
                    && unit.designation == designation
                    && unit.vehicleLevel == level)
                {
                    return unit;
                }
            }

            return null;
        }

        public VehicleUnitDefinition GetUrVehicleByLevel(int level)
        {
            return GetUrUnitByLevel(level, troopsOnly: false);
        }

        public VehicleUnitDefinition GetUrTroopByLevel(int level)
        {
            return GetUrUnitByLevel(level, troopsOnly: true);
        }

        private VehicleUnitDefinition GetUrUnitByLevel(int level, bool troopsOnly)
        {
            if (units == null)
            {
                return null;
            }

            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                if (unit == null
                    || unit.designation != VehicleUnitDesignation.UR
                    || unit.vehicleLevel != level
                    || unit.isTroop != troopsOnly)
                {
                    continue;
                }

                return unit;
            }

            return null;
        }
    }
}
