using F89.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace F89.LandCombat
{
    public static class LandGroundTerrainBuilder
    {
        private const string MaterialResourcePath = "F89_LandCombatGround";

        public static GameObject BuildArena()
        {
            var terrainSize = LandGameConstants.InfiniteTerrainWorldSize;
            var terrainObject = new GameObject("AntarcticaGroundTerrain");
            terrainObject.transform.position = new Vector3(0f, 0f, 1f);

            var meshFilter = terrainObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateQuadMesh(terrainSize, terrainSize);

            var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateTerrainMaterial();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = -100;

            terrainObject.AddComponent<LandGroundTerrainFollow>();
            return terrainObject;
        }

        public static void BindFollowTarget(Transform target)
        {
            var follow = Object.FindAnyObjectByType<LandGroundTerrainFollow>();
            if (follow != null)
            {
                follow.SetTarget(target);
            }
        }

        private static Material CreateTerrainMaterial()
        {
            var template = Resources.Load<Material>(MaterialResourcePath);
            if (template != null && F89RenderMaterials.HasWorkingShader(template))
            {
                var material = new Material(template);
                material.SetFloat("_SatelliteBlend", 0f);
                material.SetFloat("_LandNoiseScale", 0.09f);
                return material;
            }

            Debug.LogWarning("[LandCombat] F89_LandCombatGround material missing; using flat ice fallback.");
            return F89RenderMaterials.CreateUnlit(new Color(0.86f, 0.90f, 0.94f));
        }

        private static Mesh CreateQuadMesh(float width, float height)
        {
            var mesh = new Mesh { name = "LandCombatGroundQuad" };

            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, -halfHeight, 0f),
                new Vector3(halfWidth, -halfHeight, 0f),
                new Vector3(-halfWidth, halfHeight, 0f),
                new Vector3(halfWidth, halfHeight, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
