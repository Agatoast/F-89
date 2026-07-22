using F89.Flight;
using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Weapons
{
    public class FlareBurnVisual : MonoBehaviour
    {
        private const float UnityPlaneWorldSize = 10f;

        private Material outerMaterial;
        private Material mainMaterial;
        private Material coreMaterial;
        private Material sparkMaterial;
        private Transform outerTransform;
        private Transform mainTransform;
        private Transform coreTransform;
        private Transform sparkTransform;
        private float burnIntensity = 1f;
        private float animationSeed;
        private float halfWidthWorld;
        private float halfLengthWorld;

        public void Configure(float planeWidthWorld, float planeLengthWorld)
        {
            halfWidthWorld = planeWidthWorld * 0.5f;
            halfLengthWorld = planeLengthWorld * 0.5f;
            animationSeed = Random.Range(0f, 100f);

            outerMaterial = CreateBloomMaterial(new Color(1.1f, 0.42f, 0.08f, 1f));
            mainMaterial = CreateBloomMaterial(new Color(1.45f, 0.62f, 0.12f, 1f));
            coreMaterial = CreateBloomMaterial(new Color(1.9f, 1.05f, 0.42f, 1f));
            sparkMaterial = CreateBloomMaterial(new Color(1.25f, 0.85f, 0.35f, 1f));

            outerTransform = CreateBloomLayer("FlareOuter", outerMaterial, 1.12f, 0.018f);
            mainTransform = CreateBloomLayer("FlareMain", mainMaterial, 1f, 0.024f);
            coreTransform = CreateBloomLayer("FlareCore", coreMaterial, 0.42f, 0.03f);
            sparkTransform = CreateBloomLayer("FlareSparks", sparkMaterial, 0.78f, 0.028f);
        }

        public void SetBurnIntensity(float intensity)
        {
            burnIntensity = Mathf.Clamp01(intensity);
        }

        private void LateUpdate()
        {
            if (mainTransform == null)
            {
                return;
            }

            var time = Time.time + animationSeed;
            var lifePulse = burnIntensity;
            var slowPulse = 0.92f + 0.08f * Mathf.Sin(time * 5.5f);
            var fastFlicker = 0.82f + 0.18f * Mathf.PerlinNoise(time * 14f, animationSeed);
            var coreFlicker = 0.75f + 0.25f * Mathf.PerlinNoise(time * 22f, animationSeed + 4.7f);

            AnimateLayer(outerTransform, outerMaterial, new Color(1.1f, 0.42f, 0.08f, 1f),
                lifePulse * slowPulse * 0.85f, 1.12f * slowPulse);
            AnimateLayer(mainTransform, mainMaterial, new Color(1.45f, 0.62f, 0.12f, 1f),
                lifePulse * fastFlicker, 1f * (0.96f + 0.06f * Mathf.Sin(time * 9f)));
            AnimateLayer(coreTransform, coreMaterial, new Color(1.9f, 1.05f, 0.42f, 1f),
                lifePulse * coreFlicker, 0.42f * (0.88f + 0.12f * fastFlicker));
            AnimateLayer(sparkTransform, sparkMaterial, new Color(1.25f, 0.85f, 0.35f, 1f),
                lifePulse * fastFlicker * 0.7f, 0.78f);

            if (sparkTransform != null)
            {
                sparkTransform.localRotation = Quaternion.Euler(0f, time * 95f, 0f);
            }
        }

        private void AnimateLayer(
            Transform layer,
            Material material,
            Color baseTint,
            float intensity,
            float sizeScale)
        {
            if (layer == null || material == null)
            {
                return;
            }

            layer.localScale = new Vector3(
                halfWidthWorld * sizeScale / UnityPlaneWorldSize,
                1f,
                halfLengthWorld * sizeScale / UnityPlaneWorldSize);

            var visible = intensity > 0.02f;
            layer.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var color = baseTint * intensity;
            color.a = intensity;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }
        }

        private Transform CreateBloomLayer(string name, Material material, float sizeScale, float height)
        {
            var layerObject = CreateBloomMeshObject(name);
            layerObject.transform.SetParent(transform, false);
            layerObject.transform.localPosition = new Vector3(0f, height, 0f);
            layerObject.transform.localRotation = Quaternion.identity;
            layerObject.transform.localScale = new Vector3(
                halfWidthWorld * sizeScale / UnityPlaneWorldSize,
                1f,
                halfLengthWorld * sizeScale / UnityPlaneWorldSize);

            var renderer = layerObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return layerObject.transform;
        }

        private static GameObject CreateBloomMeshObject(string name)
        {
            var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempPrimitive);

            var layerObject = new GameObject(name);
            layerObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            layerObject.AddComponent<MeshRenderer>();

            var collider = layerObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return layerObject;
        }

        private static Material CreateBloomMaterial(Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent");
            var material = new Material(shader);
            var texture = ProceduralFlareBloomTexture.Get();

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent + 3;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 0);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            return material;
        }
    }
}
