using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Boss bunker floor — command room scaled from 200 ft design art. Gray floor tiles are walkable;
    /// terminal consoles are solid obstacles derived from bunker_floor.png.
    /// </summary>
    public static class LandBunkerFloorLayout
    {
        public const string FloorResourcePath = "LandCombat/bunker_floor";
        public const float RoomDesignFeet = 200f;
        public const float RoomScale = 0.25f;
        public const float RoomFeet = RoomDesignFeet * RoomScale;
        public const float FeetPerWorldUnit = 1f;
        public const float RoomWorldSize = RoomFeet / FeetPerWorldUnit;
        public const float RoomHalfWorld = RoomWorldSize * 0.5f;
        public const float FloorPixelsPerUnit = 100f;

        /// <summary>Source art size (px) used to derive terminal collision rects.</summary>
        public const float SourceWidthPx = 986f;
        public const float SourceHeightPx = 842f;

        /// <summary>Player enters slightly south of center in the open bay.</summary>
        public static Vector2 PlayerSpawn => new Vector2(0f, -18f) * RoomScale;

        /// <summary>Blue exit pad near the south wall, below the lower consoles.</summary>
        public static Vector2 ExitPadPosition => new Vector2(0f, -86f) * RoomScale;

        private const float WallThicknessDesign = 2.5f;

        // World-space center (x, y) and size (width, height) for each terminal block.
        private static readonly Vector4[] TerminalSolids =
        {
            new Vector4(-0.61f, 57.01f, 96.96f, 57.48f),
            new Vector4(62.98f, -39.67f, 44.42f, 75.06f),
            new Vector4(-68.66f, -41.21f, 44.02f, 73.87f),
            new Vector4(69.68f, 73.40f, 31.03f, 24.70f),
            new Vector4(6.19f, -96.56f, 72.01f, 6.89f),
            new Vector4(70.59f, 27.79f, 29.21f, 25.18f),
            new Vector4(-76.88f, 29.81f, 27.59f, 20.19f),
            new Vector4(-74.14f, 77.79f, 24.14f, 11.16f),
            new Vector4(76.17f, -71.50f, 18.05f, 11.40f),
            new Vector4(-66.13f, 25.18f, 9.74f, 24.23f),
            new Vector4(-65.11f, 68.53f, 8.92f, 21.62f),
            new Vector4(-39.86f, -97.15f, 15.21f, 5.70f),
            new Vector4(-31.95f, 34.44f, 12.78f, 12.35f)
        };

        public static void BuildFloorVisual(Transform parent)
        {
            var source = Resources.Load<Texture2D>(FloorResourcePath);
            if (source == null)
            {
                Debug.LogError($"F-89 Bunker: Missing floor art at Resources/{FloorResourcePath}.");
                return;
            }

            var rect = new Rect(0f, 0f, source.width, source.height);
            var sprite = Sprite.Create(
                source,
                rect,
                new Vector2(0.5f, 0.5f),
                FloorPixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "BunkerFloor";

            var floor = new GameObject("BunkerFloor");
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, 0f, 1f);

            var bounds = sprite.bounds.size;
            var scaleX = bounds.x > 0.0001f ? RoomWorldSize / bounds.x : 1f;
            var scaleY = bounds.y > 0.0001f ? RoomWorldSize / bounds.y : 1f;
            floor.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = -100;

            var material = F89.Core.F89RenderMaterials.CreateSpriteMaterial();
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        public static void BuildSolids(Transform parent)
        {
            var wallThickness = WallThicknessDesign * RoomScale;
            AddSolid(parent, new Vector2(0f, RoomHalfWorld), new Vector2(RoomWorldSize, wallThickness), "BunkerWallNorth");
            AddSolid(parent, new Vector2(0f, -RoomHalfWorld), new Vector2(RoomWorldSize, wallThickness), "BunkerWallSouth");
            AddSolid(parent, new Vector2(RoomHalfWorld, 0f), new Vector2(wallThickness, RoomWorldSize), "BunkerWallEast");
            AddSolid(parent, new Vector2(-RoomHalfWorld, 0f), new Vector2(wallThickness, RoomWorldSize), "BunkerWallWest");

            for (var i = 0; i < TerminalSolids.Length; i++)
            {
                var block = TerminalSolids[i];
                AddSolid(
                    parent,
                    new Vector2(block.x, block.y) * RoomScale,
                    new Vector2(block.z, block.w) * RoomScale,
                    $"BunkerTerminal_{i + 1}");
            }
        }

        private static void AddSolid(Transform parent, Vector2 center, Vector2 size, string objectName)
        {
            var solid = new GameObject(objectName);
            solid.transform.SetParent(parent, false);
            solid.transform.position = new Vector3(center.x, center.y, 0.2f);

            var body = solid.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            body.gravityScale = 0f;

            var box = solid.AddComponent<BoxCollider2D>();
            box.size = size;
        }
    }
}
