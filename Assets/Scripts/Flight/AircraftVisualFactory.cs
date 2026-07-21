using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Flight
{
    public static class AircraftVisualFactory
    {
        private const string TextureResourcePath = "F89_Placeholder";
        private const float UnityPlaneWorldSize = 10f;
        public const float VisualSizeMultiplier = 3f;

        public static GameObject CreateVisual(Transform parent, AircraftController controller)
        {
            var visualObject = CreatePlaneMeshObject();
            visualObject.name = "AircraftVisual";
            visualObject.transform.SetParent(parent, false);

            var texture = LoadTexture();
            var renderer = visualObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateMaterial(texture);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ApplyScale(visualObject.transform, controller != null ? controller.Profile : null, texture);
            ApplyOrientation(visualObject.transform);

            var afterburnerVisual = visualObject.AddComponent<AfterburnerVisual>();
            afterburnerVisual.Configure(controller, visualObject.transform);

            if (texture != null)
            {
                Debug.Log($"F-89: Aircraft texture loaded ({texture.width}x{texture.height}).");
            }
            else
            {
                Debug.LogError("F-89: Could not load aircraft texture from Resources/F89_Placeholder.");
            }

            return visualObject;
        }

        private static GameObject CreatePlaneMeshObject()
        {
            var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var meshFilter = tempPrimitive.GetComponent<MeshFilter>();
            var sharedMesh = meshFilter.sharedMesh;

            Object.DestroyImmediate(tempPrimitive);

            var visualObject = new GameObject("AircraftVisual");
            visualObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            visualObject.AddComponent<MeshRenderer>();
            return visualObject;
        }

        public static Texture2D LoadTexture()
        {
            var asset = Resources.Load(TextureResourcePath);
            switch (asset)
            {
                case Texture2D texture:
                    return texture;
                case Sprite sprite:
                    return sprite.texture;
            }

            var sprites = Resources.LoadAll<Sprite>(TextureResourcePath);
            if (sprites.Length > 0)
            {
                return sprites[0].texture;
            }

            return Resources.Load<Texture2D>(TextureResourcePath);
        }

        private static Material CreateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

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
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.25f));
            }
            else
            {
                material.color = new Color(0.2f, 0.2f, 0.25f);
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
            material.renderQueue = (int)RenderQueue.Transparent + 5;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
        }

        private static void ApplyScale(Transform visualTransform, FlightProfile profile, Texture2D texture)
        {
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;

            var aspect = 1f;
            if (texture != null)
            {
                aspect = (float)texture.width / texture.height;
            }

            var length = ticSize * VisualSizeMultiplier;
            var width = ticSize * aspect * VisualSizeMultiplier;
            visualTransform.localScale = new Vector3(
                width / UnityPlaneWorldSize,
                1f,
                length / UnityPlaneWorldSize);
        }

        private static void ApplyOrientation(Transform visualTransform)
        {
            visualTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visualTransform.localPosition = new Vector3(0f, 0.01f, 0f);
        }
    }
}
