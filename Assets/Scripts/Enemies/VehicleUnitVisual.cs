using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Enemies
{
    public static class VehicleUnitVisual
    {
        public static readonly Color HostileUnitColor = new Color(0.92f, 0.15f, 0.1f);
        public static readonly Color FriendlyUnitColor = new Color(0.25f, 0.78f, 0.35f);

        public static void Attach(Transform parent, VehicleUnitDefinition definition, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var footprint = OutpostGroundRules.FootprintWorld(ticSize);
            var isFlier = definition != null && definition.isFlier;
            var isTroop = definition != null && definition.isTroop;

            GameObject visualObject;
            if (isTroop)
            {
                var troopSize = footprint * 0.5f;
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visualObject.name = "TroopVisual";
                visualObject.transform.SetParent(parent, false);
                visualObject.transform.localPosition = new Vector3(0f, troopSize * 0.5f, 0f);
                visualObject.transform.localScale = Vector3.one * troopSize;
            }
            else
            {
                var height = isFlier ? footprint * 0.45f : footprint * 0.35f;
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = "VehicleVisual";
                visualObject.transform.SetParent(parent, false);
                visualObject.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
                visualObject.transform.localScale = new Vector3(footprint, height, footprint);
            }

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
        }

        public static Color GetUnitColor(VehicleUnitDefinition definition)
        {
            if (definition == null)
            {
                return HostileUnitColor;
            }

            return definition.IsHostile ? HostileUnitColor : FriendlyUnitColor;
        }
    }
}
