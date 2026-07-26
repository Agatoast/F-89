using F89.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace F89.LandCombat
{
    public static class LandGroundTerrainBuilder
    {
        private const string ShaderName = "F89/LandCombatGround";
        private const string LandMaskResourcePath = "F89_AntarcticaMap";
        private const string WorldMapResourcePath = "F89_WorldMapConfig";

        private static readonly int ArenaCenterMilesId = Shader.PropertyToID("_ArenaCenterMiles");
        private static readonly int MapSizeMilesId = Shader.PropertyToID("_MapSizeMiles");
        private static readonly int MapAspectWidthOverHeightId = Shader.PropertyToID("_MapAspectWidthOverHeight");
        private static readonly int SatelliteBlendId = Shader.PropertyToID("_SatelliteBlend");
        private static readonly int WorldUnitsPerMileId = Shader.PropertyToID("_WorldUnitsPerMile");
        private static readonly int ArenaHalfSizeWorldId = Shader.PropertyToID("_ArenaHalfSizeWorld");

        public static GameObject BuildArena()
        {
            var worldMap = Resources.Load<WorldMapConfig>(WorldMapResourcePath);
            var mapSizeMiles = worldMap != null ? worldMap.antarcticaSizeMiles : 3000f;
            var worldUnitsPerMile = ResolveWorldUnitsPerMile(worldMap);
            var arenaSize = LandGameConstants.ArenaSizeWorldUnits;
            var centerMiles = ResolveArenaCenterMiles(worldUnitsPerMile);

            var terrainObject = new GameObject("AntarcticaGroundTerrain");
            terrainObject.transform.position = new Vector3(0f, 0f, 1f);

            var meshFilter = terrainObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateQuadMesh(arenaSize, arenaSize);

            var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateTerrainMaterial(centerMiles, mapSizeMiles, worldUnitsPerMile);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = -100;

            return terrainObject;
        }

        private static float ResolveWorldUnitsPerMile(WorldMapConfig worldMap)
        {
            if (worldMap == null)
            {
                return LandGameConstants.WorldUnitsPerMile;
            }

            return worldMap.GridSpacingTics * LandGameConstants.WorldUnitsPerTile / worldMap.milesPerGrid;
        }

        private static Vector2 ResolveArenaCenterMiles(float worldUnitsPerMile)
        {
            var snapshot = LandMissionHandoffState.GetStoredFlightSnapshot();
            if (snapshot.IsValid)
            {
                return WorldMapConfig.WorldToMileOffset(snapshot.AircraftWorldPosition, worldUnitsPerMile);
            }

            return new Vector2(-420f, -780f);
        }

        private static Material CreateTerrainMaterial(Vector2 centerMiles, float mapSizeMiles, float worldUnitsPerMile)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[LandCombat] F89/LandCombatGround shader not found; using fallback color.");
                return new Material(Shader.Find("Sprites/Default"));
            }

            var material = new Material(shader);
            var landMask = AntarcticaLandMask.GetReadableMap();
            if (landMask != null)
            {
                material.SetTexture("_LandMask", landMask);
            }

            material.SetVector(ArenaCenterMilesId, new Vector4(centerMiles.x, centerMiles.y, 0f, 0f));
            material.SetFloat(MapSizeMilesId, mapSizeMiles);
            material.SetFloat(MapAspectWidthOverHeightId, AntarcticaLandMask.GetMapWidthOverHeight());
            material.SetFloat(SatelliteBlendId, 1f);
            material.SetFloat(WorldUnitsPerMileId, worldUnitsPerMile);
            material.SetFloat(ArenaHalfSizeWorldId, LandGameConstants.ArenaHalfSizeWorldUnits);
            return material;
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
