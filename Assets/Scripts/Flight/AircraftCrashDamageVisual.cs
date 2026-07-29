using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Flight
{
    /// <summary>Smoke and fire billowing from a damaged aircraft during crash landing.</summary>
    public sealed class AircraftCrashDamageVisual : MonoBehaviour
    {
        private const float UnityPlaneWorldSize = 10f;

        private Transform aircraftVisual;
        private Material fireMaterial;
        private Material smokeMaterial;
        private Transform fireLeft;
        private Transform fireRight;
        private Transform smokeLeft;
        private Transform smokeRight;
        private float planeLength;
        private float planeWidth;
        private bool active;

        public void Begin()
        {
            if (active)
            {
                return;
            }

            active = true;
            EnsureConfigured();
        }

        private void Awake()
        {
            EnsureConfigured();
        }

        private void EnsureConfigured()
        {
            if (aircraftVisual != null)
            {
                return;
            }

            var visualPivot = transform.Find("VisualPivot");
            if (visualPivot == null || visualPivot.childCount == 0)
            {
                return;
            }

            aircraftVisual = visualPivot.GetChild(0);
            var controller = GetComponent<AircraftController>();
            var profile = controller != null ? controller.Profile : null;
            var ticSize = profile != null ? profile.ticSizeWorldUnits : 1f;
            var aspect = 1.67f;
            var hullFilter = aircraftVisual.GetComponent<MeshRenderer>();
            if (hullFilter != null && AircraftVisualFactory.LoadTexture() != null)
            {
                var texture = AircraftVisualFactory.LoadTexture();
                aspect = (float)texture.width / texture.height;
            }

            planeLength = ticSize * AircraftVisualFactory.VisualSizeMultiplier;
            planeWidth = ticSize * aspect * AircraftVisualFactory.VisualSizeMultiplier;

            fireMaterial = CreateFlameMaterial(new Color(1f, 0.45f, 0.08f, 1f), additive: true);
            smokeMaterial = CreateFlameMaterial(new Color(0.35f, 0.35f, 0.38f, 0.75f), additive: false);
            CreateEffect("CrashFireLeft", -0.18f, fireMaterial, planeLength * 0.55f, planeWidth * 0.14f, 0.02f, out fireLeft);
            CreateEffect("CrashFireRight", 0.18f, fireMaterial, planeLength * 0.55f, planeWidth * 0.14f, 0.02f, out fireRight);
            CreateEffect("CrashSmokeLeft", -0.24f, smokeMaterial, planeLength * 0.75f, planeWidth * 0.18f, 0.05f, out smokeLeft);
            CreateEffect("CrashSmokeRight", 0.24f, smokeMaterial, planeLength * 0.75f, planeWidth * 0.18f, 0.05f, out smokeRight);
        }

        private void LateUpdate()
        {
            if (!active || fireLeft == null)
            {
                return;
            }

            var flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 11f, 0.19f);
            var smokePulse = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 3.5f, 1.07f);
            SetEffectScale(fireLeft, planeLength * 0.55f * flicker, planeWidth * 0.14f);
            SetEffectScale(fireRight, planeLength * 0.55f * flicker, planeWidth * 0.14f);
            SetEffectScale(smokeLeft, planeLength * 0.75f * smokePulse, planeWidth * 0.18f);
            SetEffectScale(smokeRight, planeLength * 0.75f * smokePulse, planeWidth * 0.18f);
        }

        private void CreateEffect(
            string name,
            float lateralRatio,
            Material material,
            float length,
            float width,
            float heightOffset,
            out Transform effectTransform)
        {
            var effectObject = CreatePlaneObject(name);
            effectObject.transform.SetParent(aircraftVisual, false);
            var renderer = effectObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var tailZ = planeLength * 0.35f;
            effectObject.transform.localRotation = Quaternion.identity;
            effectObject.transform.localPosition = new Vector3(
                planeWidth * lateralRatio,
                heightOffset,
                tailZ + length * 0.5f);
            SetEffectScale(effectObject.transform, length, width);
            effectTransform = effectObject.transform;
        }

        private static void SetEffectScale(Transform effectTransform, float length, float width)
        {
            if (effectTransform == null)
            {
                return;
            }

            effectTransform.localScale = new Vector3(
                width / UnityPlaneWorldSize,
                1f,
                length / UnityPlaneWorldSize);
        }

        private static GameObject CreatePlaneObject(string name)
        {
            var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempPrimitive);

            var effectObject = new GameObject(name);
            effectObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            effectObject.AddComponent<MeshRenderer>();
            return effectObject;
        }

        private static Material CreateFlameMaterial(Color tint, bool additive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            var material = new Material(shader);
            var texture = ProceduralFlameTexture.Get();
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent + 1;
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            return material;
        }
    }
}
