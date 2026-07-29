using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Enemies
{
    public static class VehicleUnitVisual
    {
        public static void Attach(Transform parent, VehicleUnitDefinition definition, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var footprint = OutpostGroundRules.FootprintWorld(ticSize);
            var isFlier = definition != null && definition.isFlier;
            var isTroop = definition != null && definition.isTroop;
            var height = isFlier
                ? footprint * 0.45f
                : isTroop
                    ? footprint * 0.22f
                    : footprint * 0.35f;

            var visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObject.name = "VehicleVisual";
            visualObject.transform.SetParent(parent, false);

            var collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(renderer.sharedMaterial)
                {
                    color = GetUnitColor(definition)
                };
            }

            visualObject.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            visualObject.transform.localScale = new Vector3(footprint, height, footprint);
        }

        private static Color ResolveColor(VehicleUnitDefinition definition)
        {
            return GetUnitColor(definition);
        }

        public static Color GetUnitColor(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return new Color(0.32f, 0.34f, 0.28f);
            }

            if (definition.isFlier)
            {
                return new Color(0.42f, 0.28f, 0.30f);
            }

            if (definition.isTroop)
            {
                return definition.IsHostile
                    ? new Color(0.48f, 0.36f, 0.28f)
                    : new Color(0.34f, 0.38f, 0.32f);
            }

            var level = Mathf.Clamp(definition.vehicleLevel, 1, 10);
            var t = (level - 1) / 9f;
            return Color.Lerp(
                new Color(0.34f, 0.36f, 0.30f),
                new Color(0.22f, 0.24f, 0.20f),
                t);
        }
    }
}
