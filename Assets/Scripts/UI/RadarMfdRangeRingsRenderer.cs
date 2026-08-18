using F89.Flight;
using UnityEngine;

namespace F89.UI
{
    /// <summary>
    /// Static range rings for radar MFD scopes (long/short), without contacts or ownship.
    /// </summary>
    public static class RadarMfdRangeRingsRenderer
    {
        public enum ScopeKind
        {
            LongRange,
            ShortRange
        }

        private static ScopeKind cachedKind = (ScopeKind)(-1);
        private static int cachedDiameter = -1;
        private static Texture2D outerRingTexture;
        private static Texture2D[] bandRingTextures;
        private static GUIStyle ringLabelStyle;

        public static void Draw(RadarMfdBezelRenderer.Layout layout, ScopeKind kind)
        {
            var center = layout.ScopeCenter;
            var displayRadius = layout.ScopeRadius;
            var diameter = Mathf.RoundToInt(RadarMfdBezelRenderer.DisplayDiameter);
            EnsureTextures(kind, diameter);

            var hudColor = FlightHudColorPalette.Mfd;
            var rangeMiles = GetRangeMiles(kind);
            var rangeScale = displayRadius / rangeMiles;
            var bandMiles = GetRangeBandMiles(kind);
            var outerSize = displayRadius * 2f;

            GUI.color = new Color(hudColor.r, hudColor.g, hudColor.b, 0.55f);
            GUI.DrawTexture(
                new Rect(center.x - displayRadius, center.y - displayRadius, outerSize, outerSize),
                outerRingTexture);

            for (var i = 0; i < bandMiles.Length && i < bandRingTextures.Length; i++)
            {
                var bandRadius = bandMiles[i] * rangeScale;
                var bandSize = bandRadius * 2f;
                GUI.DrawTexture(
                    new Rect(center.x - bandRadius, center.y - bandRadius, bandSize, bandSize),
                    bandRingTextures[i]);
            }

            GUI.color = Color.white;
            EnsureRingLabelStyle();
            ringLabelStyle.normal.textColor = hudColor;

            for (var i = 0; i < bandMiles.Length; i++)
            {
                var bandRadius = bandMiles[i] * rangeScale;
                var label = kind == ScopeKind.ShortRange
                    ? $"{bandMiles[i]:0} MI"
                    : i == 0
                        ? "50 MI ID"
                        : "100 MI ID";
                var labelSize = ringLabelStyle.CalcSize(new GUIContent(label));
                GUI.Label(
                    new Rect(
                        center.x - labelSize.x * 0.5f,
                        center.y + bandRadius + 4f,
                        labelSize.x,
                        labelSize.y),
                    label,
                    ringLabelStyle);
            }
        }

        private static void EnsureTextures(ScopeKind kind, int diameter)
        {
            if (cachedKind == kind && cachedDiameter == diameter
                && outerRingTexture != null
                && bandRingTextures != null)
            {
                return;
            }

            if (outerRingTexture != null)
            {
                Object.Destroy(outerRingTexture);
            }

            if (bandRingTextures != null)
            {
                for (var i = 0; i < bandRingTextures.Length; i++)
                {
                    if (bandRingTextures[i] != null)
                    {
                        Object.Destroy(bandRingTextures[i]);
                    }
                }
            }

            outerRingTexture = CreateDottedCircleTexture(diameter, 5);
            var bandMiles = GetRangeBandMiles(kind);
            bandRingTextures = new Texture2D[bandMiles.Length];
            for (var i = 0; i < bandMiles.Length; i++)
            {
                var bandDiameter = Mathf.RoundToInt(
                    RadarMfdBezelRenderer.DisplayDiameter * (bandMiles[i] / GetRangeMiles(kind)));
                bandRingTextures[i] = CreateDottedCircleTexture(bandDiameter, 4);
            }

            cachedKind = kind;
            cachedDiameter = diameter;
        }

        private static void EnsureRingLabelStyle()
        {
            if (ringLabelStyle != null)
            {
                return;
            }

            var s = RadarMfdBezelRenderer.LayoutScale;
            ringLabelStyle = HudStyleFactory.CreateLabel(
                Mathf.RoundToInt(10f * s),
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                FlightHudColorPalette.Mfd);
        }

        private static float GetRangeMiles(ScopeKind kind)
        {
            return kind == ScopeKind.ShortRange
                ? RadarContactScanner.ShortRangeMiles
                : RadarContactScanner.RangeMiles;
        }

        private static float[] GetRangeBandMiles(ScopeKind kind)
        {
            if (kind == ScopeKind.ShortRange)
            {
                return new[] { RadarContactScanner.ShortRangeBandMiles };
            }

            return new[]
            {
                RadarContactScanner.HostileDetectionMiles,
                RadarContactScanner.MidRangeBandMiles
            };
        }

        private static Texture2D CreateDottedCircleTexture(int diameter, int segmentPixels)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            var pixels = new Color[diameter * diameter];
            var center = (diameter - 1) * 0.5f;
            var radius = diameter * 0.5f;
            var dashPeriod = segmentPixels * 2;

            for (var y = 0; y < diameter; y++)
            {
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (Mathf.Abs(distance - radius) > 0.6f)
                    {
                        pixels[y * diameter + x] = Color.clear;
                        continue;
                    }

                    var angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f)
                    {
                        angle += Mathf.PI * 2f;
                    }

                    var arcLength = angle * radius;
                    pixels[y * diameter + x] = Mathf.FloorToInt(arcLength / dashPeriod) % 2 == 0
                        ? Color.white
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
