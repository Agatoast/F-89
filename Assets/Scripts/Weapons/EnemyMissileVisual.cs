using F89.Flight;
using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Weapons
{
    /// <summary>
    /// Lightweight incoming-enemy missile sprite — same world size as a flare main bloom layer.
    /// </summary>
    public sealed class EnemyMissileVisual : MonoBehaviour
    {
        private const float UnityPlaneWorldSize = 10f;
        private const float MainLayerSizeScale = 1f;
        private const float VisualHeight = 0.024f;

        private static Material sharedMaterial;
        private static Mesh sharedMesh;

        public static void AttachTo(Transform parent, FlightProfile profile)
        {
            if (parent == null || parent.GetComponent<EnemyMissileVisual>() != null)
            {
                return;
            }

            FlareBurnVisual.GetPlaneDimensions(profile, 1f, out var planeWidth, out var planeLength);
            var halfWidth = planeWidth * 0.5f;
            var halfLength = planeLength * 0.5f;

            EnsureSharedResources();

            var body = new GameObject("EnemyMissileBody");
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, VisualHeight, 0f);
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(
                halfWidth * MainLayerSizeScale / UnityPlaneWorldSize,
                1f,
                halfLength * MainLayerSizeScale / UnityPlaneWorldSize);

            var filter = body.AddComponent<MeshFilter>();
            filter.sharedMesh = sharedMesh;
            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            parent.gameObject.AddComponent<EnemyMissileVisual>();
        }

        private static void EnsureSharedResources()
        {
            if (sharedMesh == null)
            {
                var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
                sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tempPrimitive);
            }

            if (sharedMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent");
            sharedMaterial = new Material(shader);
            var texture = ProceduralFlareBloomTexture.Get();

            if (sharedMaterial.HasProperty("_BaseMap"))
            {
                sharedMaterial.SetTexture("_BaseMap", texture);
            }

            if (sharedMaterial.HasProperty("_MainTex"))
            {
                sharedMaterial.SetTexture("_MainTex", texture);
            }

            if (sharedMaterial.HasProperty("_Surface"))
            {
                sharedMaterial.SetFloat("_Surface", 1f);
            }

            sharedMaterial.SetOverrideTag("RenderType", "Transparent");
            sharedMaterial.renderQueue = (int)RenderQueue.Transparent + 3;
            sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            sharedMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            sharedMaterial.SetInt("_DstBlend", (int)BlendMode.One);
            sharedMaterial.SetInt("_ZWrite", 0);

            var tint = new Color(1.45f, 0.62f, 0.12f, 1f);
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                sharedMaterial.SetColor("_BaseColor", tint);
            }
            else
            {
                sharedMaterial.color = tint;
            }
        }
    }
}
