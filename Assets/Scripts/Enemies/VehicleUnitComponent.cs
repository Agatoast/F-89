using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.Enemies
{
    /// <summary>
    /// Runtime link between a world unit and its <see cref="VehicleUnitDefinition"/>.
    /// </summary>
    public sealed class VehicleUnitComponent : MonoBehaviour
    {
        [SerializeField] private VehicleUnitDefinition definition;

        public VehicleUnitDefinition Definition => definition;

        public void Configure(VehicleUnitDefinition unitDefinition)
        {
            definition = unitDefinition;
            ApplyToLockableTarget();
        }

        public void ConfigureByAbbreviation(string abbreviation)
        {
            if (VehicleUnitCatalog.LoadOrDefault().TryGetByAbbreviation(abbreviation, out var unit))
            {
                Configure(unit);
            }
        }

        private void ApplyToLockableTarget()
        {
            if (definition == null)
            {
                return;
            }

            var target = GetComponent<LockableTarget>();
            if (target == null)
            {
                target = gameObject.AddComponent<LockableTarget>();
            }

            target.Configure(
                definition.abbreviation,
                definition.ToLockableTargetKind(),
                definition.ToTargetAffiliation(),
                definition.ToTargetUnitClass());

            if (!definition.isTroop)
            {
                target.SetMaxGroundHitPoints(definition.ghp);
            }

            var ticSize = ResolveTicSizeWorldUnits();
            var footprint = OutpostGroundRules.FootprintWorld(ticSize);
            var hitRadius = definition.isTroop ? footprint * 0.25f : footprint * 0.55f;
            target.SetHitRadiusWorld(hitRadius);
        }

        public void SetPersistentTargetLabel(string slotLabel)
        {
            if (definition == null || string.IsNullOrWhiteSpace(slotLabel))
            {
                return;
            }

            var target = GetComponent<LockableTarget>();
            if (target == null)
            {
                return;
            }

            target.Configure(
                slotLabel,
                definition.ToLockableTargetKind(),
                definition.ToTargetAffiliation(),
                definition.ToTargetUnitClass());

            if (!definition.isTroop)
            {
                target.SetMaxGroundHitPoints(definition.ghp);
            }

            var ticSize = ResolveTicSizeWorldUnits();
            var footprint = OutpostGroundRules.FootprintWorld(ticSize);
            var hitRadius = definition.isTroop ? footprint * 0.25f : footprint * 0.55f;
            target.SetHitRadiusWorld(hitRadius);
        }

        private static float ResolveTicSizeWorldUnits()
        {
            var profile = Resources.Load<FlightProfile>("F89_DefaultFlightProfile");
            return profile != null ? profile.ticSizeWorldUnits : 1f;
        }
    }
}
