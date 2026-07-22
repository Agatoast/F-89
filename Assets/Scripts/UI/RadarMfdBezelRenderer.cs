using UnityEngine;

namespace F89.UI
{
    public static class RadarMfdBezelRenderer
    {
        public const float LayoutScale = 0.75f;
        public const float DisplayDiameter = 320f * LayoutScale;
        public const float CornerRockerSize = 54f * LayoutScale;
        public const float EdgeOsbSize = 38f * LayoutScale;
        public const float SideColumnWidth = 46f * LayoutScale;
        public static float ScopeEdgeGap => (SideColumnWidth - EdgeOsbSize) * 0.5f;

        private static readonly Color FrameOuter = new Color(0.08f, 0.09f, 0.1f, 1f);
        private static readonly Color FrameMid = new Color(0.18f, 0.2f, 0.23f, 1f);
        private static readonly Color FrameInner = new Color(0.34f, 0.36f, 0.4f, 1f);
        private static readonly Color Chassis = new Color(0.16f, 0.17f, 0.19f, 1f);
        private static readonly Color BezelBand = new Color(0.11f, 0.12f, 0.14f, 1f);
        private static readonly Color DisplayWell = new Color(0.02f, 0.025f, 0.03f, 1f);
        private static readonly Color ScopeRing = new Color(0.45f, 0.48f, 0.52f, 1f);
        private static readonly Color ButtonFace = new Color(0.5f, 0.53f, 0.57f, 1f);
        private static readonly Color ButtonHighlight = new Color(0.72f, 0.75f, 0.8f, 1f);
        private static readonly Color ButtonShadow = new Color(0.1f, 0.11f, 0.13f, 1f);
        private static readonly Color ButtonInset = new Color(0.36f, 0.38f, 0.42f, 1f);
        private static readonly Color Recess = new Color(0.06f, 0.07f, 0.08f, 1f);

        private static Texture2D cachedTexture;
        private static Vector2Int cachedSize;

        public struct Layout
        {
            public Rect AssemblyRect;
            public Rect ScopeRect;
            public Vector2 ScopeCenter;
            public float ScopeRadius;
        }

        public static Layout ComputeLayout()
        {
            var scopeRadius = DisplayDiameter * 0.5f;
            var scopeBandHeight = ScopeEdgeGap + DisplayDiameter + ScopeEdgeGap;
            var assemblyWidth = CornerRockerSize * 2f + SideColumnWidth * 2f + DisplayDiameter;
            var assemblyHeight = CornerRockerSize * 2f + scopeBandHeight;
            var assemblyLeft = Screen.width - assemblyWidth;
            var assemblyTop = Screen.height - assemblyHeight;
            var scopeLeft = assemblyLeft + CornerRockerSize + SideColumnWidth;
            var scopeTop = assemblyTop + CornerRockerSize + ScopeEdgeGap;

            return new Layout
            {
                AssemblyRect = new Rect(assemblyLeft, assemblyTop, assemblyWidth, assemblyHeight),
                ScopeRect = new Rect(scopeLeft, scopeTop, DisplayDiameter, DisplayDiameter),
                ScopeCenter = new Vector2(scopeLeft + scopeRadius, scopeTop + scopeRadius),
                ScopeRadius = scopeRadius
            };
        }

        public static Layout ComputeBottomLeftLayout()
        {
            var scopeRadius = DisplayDiameter * 0.5f;
            var scopeBandHeight = ScopeEdgeGap + DisplayDiameter + ScopeEdgeGap;
            var assemblyWidth = CornerRockerSize * 2f + SideColumnWidth * 2f + DisplayDiameter;
            var assemblyHeight = CornerRockerSize * 2f + scopeBandHeight;
            var assemblyTop = Screen.height - assemblyHeight;
            var scopeLeft = CornerRockerSize + SideColumnWidth;
            var scopeTop = assemblyTop + CornerRockerSize + ScopeEdgeGap;

            return new Layout
            {
                AssemblyRect = new Rect(0f, assemblyTop, assemblyWidth, assemblyHeight),
                ScopeRect = new Rect(scopeLeft, scopeTop, DisplayDiameter, DisplayDiameter),
                ScopeCenter = new Vector2(scopeLeft + scopeRadius, scopeTop + scopeRadius),
                ScopeRadius = scopeRadius
            };
        }

        public static Layout ComputeFuelGaugeLayout()
        {
            var gaugeSize = GetMfdAssemblyHeight() / 2f;

            return new Layout
            {
                AssemblyRect = new Rect(0f, 0f, gaugeSize, gaugeSize),
                ScopeRect = new Rect(0f, 0f, gaugeSize, gaugeSize),
                ScopeCenter = new Vector2(gaugeSize * 0.5f, gaugeSize * 0.5f),
                ScopeRadius = gaugeSize * 0.5f
            };
        }

        public static float GetMfdAssemblyHeight()
        {
            return CornerRockerSize * 2f + ScopeEdgeGap + DisplayDiameter + ScopeEdgeGap;
        }

        public static int GetStoresPanelFontSize()
        {
            var stores = ComputeBottomLeftLayout();
            var scope = stores.ScopeRect;
            const float rowCount = 6f;
            const float rowFontScale = 0.72f * 0.7f;
            var contentHeight = scope.height - 6f * LayoutScale;
            var rowHeight = contentHeight / rowCount;
            return Mathf.Max(9, Mathf.RoundToInt(rowHeight * rowFontScale));
        }

        public static Texture2D GetBezelTexture(Layout layout)
        {
            var width = Mathf.RoundToInt(layout.AssemblyRect.width);
            var height = Mathf.RoundToInt(layout.AssemblyRect.height);
            if (cachedTexture != null && cachedSize.x == width && cachedSize.y == height)
            {
                return cachedTexture;
            }

            if (cachedTexture != null)
            {
                Object.Destroy(cachedTexture);
            }

            cachedTexture = BuildBezelTexture(width, height, circularDisplay: true);
            cachedSize = new Vector2Int(width, height);
            return cachedTexture;
        }

        private static Texture2D cachedStoresTexture;
        private static Vector2Int cachedStoresSize;

        public static Texture2D GetStoresBezelTexture(Layout layout)
        {
            var width = Mathf.RoundToInt(layout.AssemblyRect.width);
            var height = Mathf.RoundToInt(layout.AssemblyRect.height);
            if (cachedStoresTexture != null && cachedStoresSize.x == width && cachedStoresSize.y == height)
            {
                return cachedStoresTexture;
            }

            if (cachedStoresTexture != null)
            {
                Object.Destroy(cachedStoresTexture);
            }

            cachedStoresTexture = BuildBezelTexture(width, height, circularDisplay: false);
            cachedStoresSize = new Vector2Int(width, height);
            return cachedStoresTexture;
        }

        private static Texture2D BuildBezelTexture(int width, int height, bool circularDisplay)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Chassis;
            }

            DrawFrame(pixels, width, height);
            DrawBezelBands(pixels, width, height);

            var scopeX = Mathf.RoundToInt(CornerRockerSize + SideColumnWidth);
            var scopeY = Mathf.RoundToInt(CornerRockerSize + ScopeEdgeGap);
            var scopeSize = Mathf.RoundToInt(DisplayDiameter);
            var scopeCenterX = scopeX + scopeSize * 0.5f;
            var scopeCenterY = scopeY + scopeSize * 0.5f;
            var scopeRadius = scopeSize * 0.5f;

            FillRect(pixels, width, scopeX - 6, scopeY - 6, scopeSize + 12, scopeSize + 12, Recess);
            if (circularDisplay)
            {
                FillCircle(pixels, width, height, scopeCenterX, scopeCenterY, scopeRadius + 4f, DisplayWell);
                DrawCircleRing(pixels, width, height, scopeCenterX, scopeCenterY, scopeRadius + 3f, 2f, ScopeRing);
                DrawCircleRing(pixels, width, height, scopeCenterX, scopeCenterY, scopeRadius + 1f, 1f, FrameOuter);
                MaskCircleToChassis(pixels, width, height, scopeCenterX, scopeCenterY, scopeRadius + 5f);
            }
            else
            {
                FillRect(pixels, width, scopeX, scopeY, scopeSize, scopeSize, DisplayWell);
                FillRect(pixels, width, scopeX, scopeY, scopeSize, 2, ScopeRing);
                FillRect(pixels, width, scopeX, scopeY + scopeSize - 2, scopeSize, 2, FrameOuter);
                FillRect(pixels, width, scopeX, scopeY, 2, scopeSize, ScopeRing);
                FillRect(pixels, width, scopeX + scopeSize - 2, scopeY, 2, scopeSize, FrameOuter);
            }

            DrawScrew(pixels, width, 8, 8);
            DrawScrew(pixels, width, width - 8, 8);
            DrawScrew(pixels, width, 8, height - 8);
            DrawScrew(pixels, width, width - 8, height - 8);

            var innerLeft = Mathf.RoundToInt(CornerRockerSize);
            var innerWidth = width - Mathf.RoundToInt(CornerRockerSize * 2f);
            var topSlot = innerWidth / 5f;
            var buttonY = scopeY - Mathf.RoundToInt(ScopeEdgeGap + EdgeOsbSize);
            for (var i = 0; i < 5; i++)
            {
                var x = innerLeft + Mathf.RoundToInt(topSlot * i + (topSlot - EdgeOsbSize) * 0.5f);
                DrawSquareButton(pixels, width, x, buttonY, Mathf.RoundToInt(EdgeOsbSize));
            }

            var bottomY = scopeY + scopeSize + Mathf.RoundToInt(ScopeEdgeGap);
            for (var i = 0; i < 5; i++)
            {
                var x = innerLeft + Mathf.RoundToInt(topSlot * i + (topSlot - EdgeOsbSize) * 0.5f);
                DrawSquareButton(pixels, width, x, bottomY, Mathf.RoundToInt(EdgeOsbSize));
            }

            var columnXLeft = innerLeft + Mathf.RoundToInt((SideColumnWidth - EdgeOsbSize) * 0.5f);
            var columnXRight = width - innerLeft - Mathf.RoundToInt((SideColumnWidth + EdgeOsbSize) * 0.5f);
            var columnTop = scopeY;
            var columnHeight = scopeSize;
            var gap = (columnHeight - EdgeOsbSize * 5f) / 6f;
            for (var i = 0; i < 5; i++)
            {
                var y = columnTop + Mathf.RoundToInt(gap + i * (EdgeOsbSize + gap));
                DrawSquareButton(pixels, width, columnXLeft, y, Mathf.RoundToInt(EdgeOsbSize));
                DrawSquareButton(pixels, width, columnXRight, y, Mathf.RoundToInt(EdgeOsbSize));
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void DrawBezelBands(Color[] pixels, int width, int height)
        {
            var topBandY = Mathf.RoundToInt(CornerRockerSize);
            var bottomBandY = Mathf.RoundToInt(CornerRockerSize + ScopeEdgeGap + DisplayDiameter);
            FillRect(pixels, width, 0, topBandY, width, 2, BezelBand);
            FillRect(pixels, width, 0, bottomBandY, width, 2, BezelBand);
        }

        private static void DrawFrame(Color[] pixels, int width, int height)
        {
            FillRect(pixels, width, 0, 0, width, 4, FrameOuter);
            FillRect(pixels, width, 0, height - 4, width, 4, FrameOuter);
            FillRect(pixels, width, 0, 0, 4, height, FrameOuter);
            FillRect(pixels, width, width - 4, 0, 4, height, FrameOuter);

            FillRect(pixels, width, 4, 4, width - 8, 2, FrameMid);
            FillRect(pixels, width, 4, height - 6, width - 8, 2, FrameMid);
            FillRect(pixels, width, 4, 4, 2, height - 8, FrameMid);
            FillRect(pixels, width, width - 6, 4, 2, height - 8, FrameMid);

            FillRect(pixels, width, 7, 7, width - 14, 1, FrameInner);
            FillRect(pixels, width, 7, height - 8, width - 14, 1, FrameInner);
            FillRect(pixels, width, 7, 7, 1, height - 15, FrameInner);
            FillRect(pixels, width, width - 8, 7, 1, height - 15, FrameInner);
        }

        private static void DrawSquareButton(Color[] pixels, int texWidth, int x, int y, int size)
        {
            FillRect(pixels, texWidth, x, y, size, size, ButtonFace);
            FillRect(pixels, texWidth, x + 2, y + 2, size - 4, size - 4, ButtonInset);
            FillRect(pixels, texWidth, x, y, size, 3, ButtonHighlight);
            FillRect(pixels, texWidth, x, y, 3, size, ButtonHighlight);
            FillRect(pixels, texWidth, x, y + size - 3, size, 3, ButtonShadow);
            FillRect(pixels, texWidth, x + size - 3, y, 3, size, ButtonShadow);
        }

        private static void DrawScrew(Color[] pixels, int texWidth, int centerX, int centerY)
        {
            FillCircle(pixels, texWidth, pixels.Length / texWidth, centerX, centerY, 3f, FrameOuter);
            FillCircle(pixels, texWidth, pixels.Length / texWidth, centerX, centerY, 1.5f, FrameInner);
        }

        private static void MaskCircleToChassis(Color[] pixels, int texWidth, int texHeight, float cx, float cy, float radius)
        {
            var outerRadius = radius + 8f;
            for (var y = 0; y < texHeight; y++)
            {
                for (var x = 0; x < texWidth; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > outerRadius)
                    {
                        continue;
                    }

                    if (distance > radius)
                    {
                        pixels[y * texWidth + x] = Chassis;
                    }
                }
            }
        }

        private static void FillCircle(Color[] pixels, int texWidth, int texHeight, float cx, float cy, float radius, Color color)
        {
            var radiusSq = radius * radius;
            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            var maxX = Mathf.Min(texWidth - 1, Mathf.CeilToInt(cx + radius));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            var maxY = Mathf.Min(texHeight - 1, Mathf.CeilToInt(cy + radius));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        pixels[y * texWidth + x] = color;
                    }
                }
            }
        }

        private static void DrawCircleRing(
            Color[] pixels,
            int texWidth,
            int texHeight,
            float cx,
            float cy,
            float radius,
            float thickness,
            Color color)
        {
            var inner = radius - thickness;
            var outer = radius + thickness;
            var innerSq = inner * inner;
            var outerSq = outer * outer;

            var minX = Mathf.Max(0, Mathf.FloorToInt(cx - outer));
            var maxX = Mathf.Min(texWidth - 1, Mathf.CeilToInt(cx + outer));
            var minY = Mathf.Max(0, Mathf.FloorToInt(cy - outer));
            var maxY = Mathf.Min(texHeight - 1, Mathf.CeilToInt(cy + outer));

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var distSq = dx * dx + dy * dy;
                    if (distSq >= innerSq && distSq <= outerSq)
                    {
                        pixels[y * texWidth + x] = color;
                    }
                }
            }
        }

        private static void FillRect(Color[] pixels, int texWidth, int x, int y, int w, int h, Color color)
        {
            x = Mathf.Clamp(x, 0, texWidth);
            y = Mathf.Clamp(y, 0, pixels.Length / texWidth);
            var texHeight = pixels.Length / texWidth;
            for (var py = y; py < y + h && py < texHeight; py++)
            {
                for (var px = x; px < x + w && px < texWidth; px++)
                {
                    pixels[py * texWidth + px] = color;
                }
            }
        }
    }
}
