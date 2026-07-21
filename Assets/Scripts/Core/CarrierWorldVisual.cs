using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Core
{
    public static class CarrierWorldVisual
    {
        private const string TextureResourcePath = "F89_UssMartinVanBurenCarrier";
        private const float UnityPlaneWorldSize = 10f;
        private const float CarrierLengthMiles = 0.65f;
        private const float GroundHeight = 0.002f;

        public static void Attach(GameObject carrierObject, float worldUnitsPerMile)
        {
            if (carrierObject == null || carrierObject.transform.Find("CarrierVisual") != null)
            {
                return;
            }

            var texture = LoadTexture();
            var visualObject = CreatePlaneMeshObject();
            visualObject.name = "CarrierVisual";
            visualObject.transform.SetParent(carrierObject.transform, false);

            var renderer = visualObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(texture);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ApplyTransform(visualObject.transform, texture, worldUnitsPerMile);
        }

        private static GameObject CreatePlaneMeshObject()
        {
            var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempPrimitive);

            var visualObject = new GameObject("CarrierVisual");
            visualObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            visualObject.AddComponent<MeshRenderer>();

            var collider = visualObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return visualObject;
        }

        private static Texture2D LoadTexture()
        {
            return Resources.Load<Texture2D>(TextureResourcePath);
        }

        private static Material CreateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Standard");
            var material = new Material(shader);

            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }

                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }

                ConfigureTransparentMaterial(material);
            }

            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent - 10;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
        }

        private static void ApplyTransform(Transform visualTransform, Texture2D texture, float worldUnitsPerMile)
        {
            var length = Mathf.Max(1f, CarrierLengthMiles * worldUnitsPerMile);
            var aspect = texture != null && texture.height > 0
                ? texture.width / (float)texture.height
                : 0.3f;
            var width = length * aspect;

            visualTransform.localPosition = new Vector3(0f, GroundHeight, 0f);
            visualTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visualTransform.localScale = new Vector3(
                width / UnityPlaneWorldSize,
                1f,
                length / UnityPlaneWorldSize);
        }
    }
}
