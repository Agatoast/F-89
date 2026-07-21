using F89.Core;
using UnityEngine;

namespace F89.Testing
{
    public class InfiniteWhiteMap : MonoBehaviour
    {
        private const string GroundShaderName = "F89/ProceduralFlightGround";
        private const string LandMaskResourcePath = "F89_AntarcticaMap";

        [SerializeField] private Transform target;
        [SerializeField] private Renderer groundRenderer;
        [SerializeField] private float groundSize = 10000f;

        [Header("Sky blend")]
        [SerializeField] private Color landSkyColor = new Color(0.88f, 0.91f, 0.94f, 1f);
        [SerializeField] private Color oceanSkyColor = new Color(0.62f, 0.78f, 0.90f, 1f);
        [SerializeField] private float skyBlendSpeed = 1.75f;

        private WorldMapConfig worldMap;
        private float ticSizeWorldUnits = 1f;
        private Material groundMaterial;
        private Camera followCamera;
        private float currentLandBlend;
        private float mapHalfSizeWorld = 30000f;
        private static readonly int MapHalfSizeWorldId = Shader.PropertyToID("_MapHalfSizeWorld");

        private void Awake()
        {
            EnsureGround();
        }

        public void Configure(WorldMapConfig mapConfig, float ticSize = 1f)
        {
            worldMap = mapConfig;
            ticSizeWorldUnits = Mathf.Max(0.01f, ticSize);
            mapHalfSizeWorld = ComputeMapHalfSizeWorld();
            EnsureGround();
            ApplyMaterial(groundRenderer);
        }

        private void LateUpdate()
        {
            if (target == null || groundRenderer == null)
            {
                return;
            }

            var groundTransform = groundRenderer.transform;
            var targetPosition = target.position;
            groundTransform.position = new Vector3(targetPosition.x, 0f, targetPosition.z);

            UpdateSkyColor(targetPosition);
        }

        private void UpdateSkyColor(Vector3 worldPosition)
        {
            if (followCamera == null)
            {
                followCamera = Camera.main;
            }

            if (followCamera == null || worldMap == null)
            {
                return;
            }

            var landBlend = SampleLandBlend(worldPosition);
            currentLandBlend = Mathf.MoveTowards(
                currentLandBlend,
                landBlend,
                skyBlendSpeed * Time.deltaTime);
            followCamera.backgroundColor = Color.Lerp(oceanSkyColor, landSkyColor, currentLandBlend);
        }

        private float SampleLandBlend(Vector3 worldPosition)
        {
            var milesPerWorldUnit = GetMilesPerWorldUnit();
            if (milesPerWorldUnit <= 0f)
            {
                return 1f;
            }

            var worldUnitsPerMile = GetMilesPerWorldUnit() > 0f ? 1f / GetMilesPerWorldUnit() : 20f;
            var positionMiles = WorldMapConfig.WorldToMileOffset(worldPosition, worldUnitsPerMile);
            return AntarcticaLandMask.GetDisplayLandBlendMiles(positionMiles, worldMap.antarcticaSizeMiles);
        }

        private float GetMilesPerWorldUnit()
        {
            if (worldMap == null)
            {
                return 1f / 20f;
            }

            var worldUnitsPerMile = worldMap.GridSpacingTics * ticSizeWorldUnits / worldMap.milesPerGrid;
            return worldUnitsPerMile > 0f ? 1f / worldUnitsPerMile : 1f / 20f;
        }

        private float ComputeMapHalfSizeWorld()
        {
            if (worldMap == null)
            {
                return 30000f;
            }

            var worldUnitsPerMile = worldMap.GridSpacingTics * ticSizeWorldUnits / worldMap.milesPerGrid;
            return worldMap.antarcticaSizeMiles * 0.5f * worldUnitsPerMile;
        }

        private void EnsureGround()
        {
            if (groundRenderer != null)
            {
                ApplyMaterial(groundRenderer);
                return;
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ProceduralFlightGround";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(groundSize / 10f, 1f, groundSize / 10f);
            ground.transform.position = Vector3.zero;

            var collider = ground.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            groundRenderer = ground.GetComponent<Renderer>();
            ApplyMaterial(groundRenderer);
        }

        private void ApplyMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            if (groundMaterial == null)
            {
                var shader = Shader.Find(GroundShaderName);
                if (shader == null)
                {
                    Debug.LogWarning("F-89: ProceduralFlightGround shader not found — using flat fallback.");
                    groundMaterial = CreateFallbackMaterial();
                }
                else
                {
                    groundMaterial = new Material(shader);
                }
            }

            if (groundMaterial.HasProperty(MapHalfSizeWorldId))
            {
                groundMaterial.SetFloat(MapHalfSizeWorldId, mapHalfSizeWorld);
            }

            var landMask = Resources.Load<Texture2D>(LandMaskResourcePath);
            if (landMask != null && groundMaterial.HasProperty("_LandMask"))
            {
                groundMaterial.SetTexture("_LandMask", landMask);
            }

            renderer.sharedMaterial = groundMaterial;
        }

        private static Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.78f, 0.85f, 0.92f));
            }
            else
            {
                material.color = new Color(0.78f, 0.85f, 0.92f);
            }

            return material;
        }

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            SnapGroundToTarget();
        }

        private void SnapGroundToTarget()
        {
            if (target == null || groundRenderer == null)
            {
                return;
            }

            var targetPosition = target.position;
            groundRenderer.transform.position = new Vector3(targetPosition.x, 0f, targetPosition.z);
        }

        private void OnDestroy()
        {
            if (groundMaterial != null)
            {
                Destroy(groundMaterial);
            }
        }
    }
}
