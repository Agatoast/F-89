using System.Collections.Generic;
using F89.CameraSystems;
using F89.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace F89.Testing
{
    public class MotionGridOverlay : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float ticSizeWorldUnits = 1f;
        [SerializeField] private int spacingTics = 20;
        [SerializeField] private int linesEachSide = 30;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private float gridHeight = 0.004f;
        [SerializeField] private Color lineColor = Color.black;

        private readonly List<LineRenderer> linePool = new List<LineRenderer>();
        private Material lineMaterial;
        private Transform lineContainer;
        private int lastCellX = int.MinValue;
        private int lastCellZ = int.MinValue;
        private int lastLinesEachSide = -1;

        public void Configure(Transform followTarget, float ticSize, WorldMapConfig worldMap = null)
        {
            target = followTarget;
            ticSizeWorldUnits = Mathf.Max(0.01f, ticSize);
            if (worldMap != null)
            {
                spacingTics = Mathf.RoundToInt(worldMap.GridSpacingTics);
            }

            lastCellX = int.MinValue;
            lastCellZ = int.MinValue;
            EnsureLineContainer();
            EnsureMaterial();
            RebuildGrid();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            UpdateGridExtentForCamera();

            var spacing = spacingTics * ticSizeWorldUnits;
            var cellX = Mathf.FloorToInt(target.position.x / spacing);
            var cellZ = Mathf.FloorToInt(target.position.z / spacing);
            if (cellX == lastCellX && cellZ == lastCellZ && linePool.Count > 0 && lastLinesEachSide == linesEachSide)
            {
                return;
            }

            lastCellX = cellX;
            lastCellZ = cellZ;
            lastLinesEachSide = linesEachSide;
            RebuildGrid();
        }

        private void UpdateGridExtentForCamera()
        {
            var followCamera = Object.FindAnyObjectByType<TopDownFollowCamera>();
            if (followCamera == null)
            {
                return;
            }

            var halfHorizontalMiles = followCamera.GetVisibleHorizontalMiles() * 0.5f;
            var halfVerticalMiles = followCamera.GetVisibleVerticalMiles() * 0.5f;
            var halfMilesNeeded = Mathf.Max(halfHorizontalMiles, halfVerticalMiles) + 1f;
            linesEachSide = Mathf.Clamp(Mathf.CeilToInt(halfMilesNeeded), 6, 40);
        }

        private void EnsureLineContainer()
        {
            if (lineContainer != null)
            {
                return;
            }

            var containerObject = new GameObject("GridLines");
            containerObject.transform.SetParent(transform, false);
            lineContainer = containerObject.transform;
        }

        private void EnsureMaterial()
        {
            if (lineMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            lineMaterial = new Material(shader);
            if (lineMaterial.HasProperty("_Color"))
            {
                lineMaterial.SetColor("_Color", lineColor);
            }

            if (lineMaterial.HasProperty("_BaseColor"))
            {
                lineMaterial.SetColor("_BaseColor", lineColor);
            }

            lineMaterial.renderQueue = (int)RenderQueue.Geometry;
        }

        private void RebuildGrid()
        {
            if (target == null)
            {
                return;
            }

            EnsureLineContainer();
            EnsureMaterial();

            var spacing = spacingTics * ticSizeWorldUnits;
            var extent = linesEachSide * spacing;
            var centerX = target.position.x;
            var centerZ = target.position.z;

            var startX = Mathf.Floor((centerX - extent) / spacing) * spacing;
            var endX = Mathf.Ceil((centerX + extent) / spacing) * spacing;
            var startZ = Mathf.Floor((centerZ - extent) / spacing) * spacing;
            var endZ = Mathf.Ceil((centerZ + extent) / spacing) * spacing;

            var lineIndex = 0;

            for (var x = startX; x <= endX + 0.001f; x += spacing)
            {
                SetLine(
                    lineIndex++,
                    new Vector3(x, gridHeight, startZ),
                    new Vector3(x, gridHeight, endZ));
            }

            for (var z = startZ; z <= endZ + 0.001f; z += spacing)
            {
                SetLine(
                    lineIndex++,
                    new Vector3(startX, gridHeight, z),
                    new Vector3(endX, gridHeight, z));
            }

            for (var i = lineIndex; i < linePool.Count; i++)
            {
                linePool[i].enabled = false;
            }
        }

        private void SetLine(int index, Vector3 start, Vector3 end)
        {
            while (linePool.Count <= index)
            {
                linePool.Add(CreateLineRenderer());
            }

            var line = linePool[index];
            line.enabled = true;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private LineRenderer CreateLineRenderer()
        {
            var lineObject = new GameObject($"GridLine_{linePool.Count}");
            lineObject.transform.SetParent(lineContainer, false);

            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 0;
            line.numCornerVertices = 0;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.textureMode = LineTextureMode.Stretch;
            line.material = lineMaterial;
            line.startColor = lineColor;
            line.endColor = lineColor;

            return line;
        }
    }
}
