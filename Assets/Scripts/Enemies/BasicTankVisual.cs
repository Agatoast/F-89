using F89.Flight;
using UnityEngine;

namespace F89.Enemies
{
    public static class BasicTankVisual
    {
        public static GameObject AttachVisual(Transform parent, FlightProfile profile)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var planeLength = ticSize * AircraftVisualFactory.VisualSizeMultiplier;
            var tankSize = planeLength * 0.5f;

            var existing = parent.Find("BasicTankVisual");
            if (existing != null)
            {
                ApplyScale(existing, tankSize);
                return existing.gameObject;
            }

            var visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObject.name = "BasicTankVisual";
            visualObject.transform.SetParent(parent, false);

            var collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial.color = new Color(0.32f, 0.34f, 0.28f);
            }

            ApplyScale(visualObject.transform, tankSize);
            return visualObject;
        }

        private static void ApplyScale(Transform visualTransform, float tankSize)
        {
            visualTransform.localPosition = new Vector3(0f, tankSize * 0.12f, 0f);
            visualTransform.localRotation = Quaternion.identity;
            visualTransform.localScale = new Vector3(tankSize, tankSize * 0.24f, tankSize);
        }
    }
}
