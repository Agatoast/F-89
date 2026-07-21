using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Flight
{
    public class AfterburnerVisual : MonoBehaviour
    {
        private const float UnityPlaneWorldSize = 10f;

        [SerializeField] private float engineSpanRatio = 0.22f;
        [SerializeField] private float tailInsetRatio = 0.06f;
        [SerializeField] private float flameWidthRatio = 0.11f;
        [SerializeField] private float maxGlowIntensity = 2.4f;
        [SerializeField] private float flameFlickerSpeed = 14f;

        private AircraftController controller;
        private Transform aircraftVisual;
        private MeshRenderer hullRenderer;
        private Material hullMaterial;
        private Material flameMaterial;
        private Transform flameLeft;
        private Transform flameRight;
        private float planeLength;
        private float planeWidth;
        private float flameLength;
        private float currentFlameIntensity;

        public void Configure(AircraftController aircraftController, Transform visualTransform)
        {
            controller = aircraftController;
            aircraftVisual = visualTransform;

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
            flameLength = planeLength * 0.5f;

            if (hullFilter != null)
            {
                hullRenderer = hullFilter;
                hullMaterial = hullRenderer.material;
            }

            flameMaterial = CreateFlameMaterial();
            CreateFlame("FlameLeft", -engineSpanRatio, out flameLeft);
            CreateFlame("FlameRight", engineSpanRatio, out flameRight);
            SetFlameIntensity(0f);
        }

        private void LateUpdate()
        {
            if (controller == null || flameLeft == null || flameRight == null)
            {
                return;
            }

            var targetIntensity = 0f;
            if (controller.IsAfterburning && controller.Profile != null)
            {
                var speedRatio = Mathf.InverseLerp(
                    controller.Profile.DefaultSpeedWorld,
                    controller.Profile.AfterburnerSpeedWorld,
                    controller.CurrentSpeed);
                targetIntensity = Mathf.Lerp(0.55f, 1f, speedRatio);
                var flicker = 0.82f + 0.18f * Mathf.PerlinNoise(Time.time * flameFlickerSpeed, 0.37f);
                targetIntensity *= flicker;
            }

            currentFlameIntensity = Mathf.MoveTowards(currentFlameIntensity, targetIntensity, Time.deltaTime * 4f);
            SetFlameIntensity(currentFlameIntensity);
        }

        private void CreateFlame(string name, float lateralRatio, out Transform flameTransform)
        {
            var flameObject = CreateFlameMeshObject();
            flameObject.name = name;
            flameObject.transform.SetParent(aircraftVisual, false);

            var renderer = flameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = flameMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var flameWidth = planeWidth * flameWidthRatio;
            var tailZ = planeLength * (0.5f - tailInsetRatio);
            flameObject.transform.localRotation = Quaternion.identity;
            flameObject.transform.localPosition = new Vector3(
                planeWidth * lateralRatio,
                0.015f,
                tailZ + flameLength * 0.5f);
            flameObject.transform.localScale = new Vector3(
                flameWidth / UnityPlaneWorldSize,
                1f,
                flameLength / UnityPlaneWorldSize);

            flameTransform = flameObject.transform;
        }

        private static GameObject CreateFlameMeshObject()
        {
            var tempPrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var sharedMesh = tempPrimitive.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempPrimitive);

            var flameObject = new GameObject("Flame");
            flameObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            flameObject.AddComponent<MeshRenderer>();
            return flameObject;
        }

        private static Material CreateFlameMaterial()
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

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent + 1;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.One);
            material.SetInt("_ZWrite", 0);
            return material;
        }

        private void SetFlameIntensity(float intensity)
        {
            var visible = intensity > 0.02f;
            if (flameLeft != null)
            {
                flameLeft.gameObject.SetActive(visible);
            }

            if (flameRight != null)
            {
                flameRight.gameObject.SetActive(visible);
            }

            if (flameMaterial != null)
            {
                var flameColor = new Color(0.55f, 0.82f, 1.35f, intensity);
                if (flameMaterial.HasProperty("_BaseColor"))
                {
                    flameMaterial.SetColor("_BaseColor", flameColor);
                }
                else
                {
                    flameMaterial.color = flameColor;
                }
            }

            if (hullMaterial == null)
            {
                return;
            }

            var hullTint = Color.Lerp(Color.white, new Color(1.05f, 1.12f, 1.35f), intensity * 0.45f);
            if (hullMaterial.HasProperty("_BaseColor"))
            {
                hullMaterial.SetColor("_BaseColor", hullTint);
            }

            if (hullMaterial.HasProperty("_EmissionColor"))
            {
                hullMaterial.EnableKeyword("_EMISSION");
                hullMaterial.SetColor("_EmissionColor", new Color(0.15f, 0.45f, 1.1f) * intensity * maxGlowIntensity);
            }
        }
    }
}
