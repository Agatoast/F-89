using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    public class StoresPanelHud : MonoBehaviour
    {
        private const float RowFontScale = 0.72f * 0.7f;
        private const float WireIconScale = 0.2f;

        private enum StoreWireIcon
        {
            None,
            Agm114,
            Gbu12,
            Agm88j,
            Aim9z
        }

        private static readonly string[] StoreNameLabels =
        {
            "GAU-27A",
            "AGM-114",
            "GBU-12",
            "AGM-88J",
            "AIM-9Z",
            "Flare"
        };

        private struct RowLayout
        {
            public Rect RowRect;
            public Rect KeyRect;
            public Rect NameRect;
            public Rect WireRect;
            public Rect CountRect;
        }

        [SerializeField] private PlayerWeaponController weapons;
        [SerializeField] private FlareCountermeasureController flareController;

        private GUIStyle rowStyle;
        private GUIStyle countStyle;
        private int lastRowFontSize = -1;

        public void Configure(
            PlayerWeaponController weaponController,
            FlareCountermeasureController flares)
        {
            weapons = weaponController;
            flareController = flares;
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || weapons == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawStoresPanel(FlightHudColorPalette.Mfd);
        }

        private void DrawStoresPanel(Color hudColor)
        {
            var layout = RadarMfdBezelRenderer.ComputeBottomLeftLayout();
            var bezel = RadarMfdBezelRenderer.GetStoresBezelTexture(layout);
            var scope = layout.ScopeRect;
            var s = RadarMfdBezelRenderer.LayoutScale;
            const float rowCount = 6f;
            var contentHeight = scope.height - 6f * s;
            var rowHeight = contentHeight / rowCount;
            EnsureStyles(RadarMfdBezelRenderer.GetStoresPanelFontSize());

            var previousDepth = GUI.depth;
            GUI.depth = 100;
            GUI.color = Color.white;
            GUI.DrawTexture(layout.AssemblyRect, bezel);

            GUI.depth = -100;
            DrawStoresContent(scope, s, rowHeight, hudColor);

            GUI.depth = previousDepth;
            GUI.color = Color.white;
        }

        private void DrawStoresContent(Rect scope, float s, float rowHeight, Color hudColor)
        {
            var contentTop = scope.y + 3f * s;
            var contentWidth = scope.width - 10f * s;
            var contentX = scope.x + 5f * s;

            var gauRounds = weapons.Gau27aGun != null ? weapons.Gau27aGun.RoundsRemaining : 0;
            var flareCount = flareController != null ? flareController.FlaresRemaining : 0;
            var nameColumnWidth = MeasureNameColumnWidth(s);
            DrawStoreRow(contentX, contentTop + rowHeight * 0, contentWidth, rowHeight, 1, "GAU-27A", gauRounds.ToString("0"), SelectedWeapon.Gau27a, StoreWireIcon.None, hudColor, s, nameColumnWidth);
            DrawStoreRow(contentX, contentTop + rowHeight * 1, contentWidth, rowHeight, 2, "AGM-114", weapons.Agm114Remaining.ToString("0"), SelectedWeapon.Agm114Hellfire, StoreWireIcon.Agm114, hudColor, s, nameColumnWidth);
            DrawStoreRow(contentX, contentTop + rowHeight * 2, contentWidth, rowHeight, 3, "GBU-12", weapons.Gbu12Remaining.ToString("0"), SelectedWeapon.Gbu12Paveway, StoreWireIcon.Gbu12, hudColor, s, nameColumnWidth);
            DrawStoreRow(contentX, contentTop + rowHeight * 3, contentWidth, rowHeight, 4, "AGM-88J", weapons.Agm88jRemaining.ToString("0"), SelectedWeapon.Agm88jSiaw, StoreWireIcon.Agm88j, hudColor, s, nameColumnWidth);
            DrawStoreRow(contentX, contentTop + rowHeight * 4, contentWidth, rowHeight, 5, "AIM-9Z", weapons.Aim9zRemaining.ToString("0"), SelectedWeapon.Aim9z, StoreWireIcon.Aim9z, hudColor, s, nameColumnWidth);
            DrawInventoryRow(contentX, contentTop + rowHeight * 5, contentWidth, rowHeight, "F", "Flare", flareCount.ToString("0"), StoreWireIcon.None, hudColor, s, nameColumnWidth);
        }

        private float MeasureNameColumnWidth(float s)
        {
            var maxWidth = 0f;
            foreach (var name in StoreNameLabels)
            {
                var size = rowStyle.CalcSize(new GUIContent(name));
                maxWidth = Mathf.Max(maxWidth, size.x);
            }

            return maxWidth + 4f * s;
        }

        private static RowLayout ComputeRowLayout(float x, float y, float width, float height, float s, float nameColumnWidth)
        {
            var keyWidth = 14f * s;
            var countWidth = 26f * s;
            var gap = 3f * s;
            var nameToIconGap = 6f * s;
            var nameLeft = x + keyWidth + gap;
            var wireLeft = nameLeft + nameColumnWidth + nameToIconGap;
            var wireWidth = width - (wireLeft - x) - countWidth - gap;

            return new RowLayout
            {
                RowRect = new Rect(x, y, width, height),
                KeyRect = new Rect(x + 2f * s, y, keyWidth, height),
                NameRect = new Rect(nameLeft, y, nameColumnWidth, height),
                WireRect = new Rect(wireLeft, y + height * 0.12f, Mathf.Max(8f * s, wireWidth), height * 0.76f),
                CountRect = new Rect(x + width - countWidth - gap, y, countWidth, height)
            };
        }

        private void DrawInventoryRow(
            float x,
            float y,
            float width,
            float height,
            string keyLabel,
            string label,
            string count,
            StoreWireIcon icon,
            Color hudColor,
            float s,
            float nameColumnWidth)
        {
            var layout = ComputeRowLayout(x, y, width, height, s, nameColumnWidth);
            var labelColor = new Color(hudColor.r, hudColor.g, hudColor.b, 0.78f);
            DrawRowLabels(layout, keyLabel, label, count, labelColor);
            DrawWeaponIcon(icon, layout.WireRect, labelColor);
        }

        private void DrawStoreRow(
            float x,
            float y,
            float width,
            float height,
            int keyNumber,
            string label,
            string count,
            SelectedWeapon weapon,
            StoreWireIcon icon,
            Color hudColor,
            float s,
            float nameColumnWidth)
        {
            var layout = ComputeRowLayout(x, y, width, height, s, nameColumnWidth);
            var selected = weapons.ActiveWeapon == weapon;
            if (selected)
            {
                DrawSelectedLabelBox(layout.RowRect, string.Empty, hudColor, s, rowStyle);
            }

            var labelColor = selected
                ? hudColor
                : new Color(hudColor.r, hudColor.g, hudColor.b, 0.78f);
            DrawRowLabels(layout, keyNumber.ToString(), label, count, labelColor);
            DrawWeaponIcon(icon, layout.WireRect, labelColor);
        }

        private void DrawRowLabels(
            RowLayout layout,
            string keyLabel,
            string name,
            string count,
            Color labelColor)
        {
            rowStyle.normal.textColor = labelColor;
            countStyle.normal.textColor = labelColor;
            GUI.Label(layout.KeyRect, keyLabel, rowStyle);

            GUI.BeginGroup(layout.NameRect);
            GUI.Label(new Rect(0f, 0f, layout.NameRect.width, layout.NameRect.height), name, rowStyle);
            GUI.EndGroup();

            GUI.Label(layout.CountRect, count, countStyle);
        }

        private static void DrawWeaponIcon(StoreWireIcon icon, Rect rect, Color labelColor)
        {
            if (icon == StoreWireIcon.None || rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            var iconName = GetIconResourceName(icon);
            if (StoresWeaponIconLibrary.TryGetIcon(iconName, out var texture))
            {
                var drawRect = StoresWeaponIconLibrary.FitIconRect(rect, texture, iconName);
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, labelColor.a);
                GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill, true);
                GUI.color = previous;
                return;
            }

            DrawWeaponWireFallback(icon, rect, labelColor);
        }

        private static string GetIconResourceName(StoreWireIcon icon)
        {
            switch (icon)
            {
                case StoreWireIcon.Agm114:
                    return "agm114";
                case StoreWireIcon.Gbu12:
                    return "gbu12";
                case StoreWireIcon.Agm88j:
                    return "agm88j";
                case StoreWireIcon.Aim9z:
                    return "aim9z";
                default:
                    return null;
            }
        }

        private static void DrawWeaponWireFallback(StoreWireIcon icon, Rect rect, Color color)
        {
            rect = ScaleRectCentered(rect, WireIconScale);
            var thickness = 1f;
            switch (icon)
            {
                case StoreWireIcon.Gbu12:
                    DrawBombWire(rect, color, thickness);
                    break;
                case StoreWireIcon.Agm88j:
                    DrawMissileWire(rect, color, thickness, shortBody: false, cruciformTail: false);
                    break;
                case StoreWireIcon.Aim9z:
                    DrawMissileWire(rect, color, thickness, shortBody: true, cruciformTail: true);
                    break;
            }
        }

        private static Rect ScaleRectCentered(Rect rect, float scale)
        {
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private static void DrawBombWire(Rect rect, Color color, float thickness)
        {
            var body = InsetRect(rect, 0.12f, 0.22f, 0.52f, 0.56f);
            WireRectOutline(body, color, thickness);
            WireLine(new Vector2(body.xMax, body.yMin + body.height * 0.35f), new Vector2(rect.xMax - rect.width * 0.06f, body.yMin + body.height * 0.2f), color, thickness);
            WireLine(new Vector2(body.xMax, body.yMin + body.height * 0.35f), new Vector2(rect.xMax - rect.width * 0.06f, body.yMin + body.height * 0.5f), color, thickness);
            WireLine(new Vector2(body.xMax, body.yMax - body.height * 0.35f), new Vector2(rect.xMax - rect.width * 0.06f, body.yMax - body.height * 0.2f), color, thickness);
            WireLine(new Vector2(body.xMin + body.width * 0.35f, body.yMax), new Vector2(body.xMin + body.width * 0.35f, rect.yMax - rect.height * 0.08f), color, thickness);
        }

        private static void DrawMissileWire(Rect rect, Color color, float thickness, bool shortBody, bool cruciformTail)
        {
            var bodyLeft = shortBody ? 0.16f : 0.1f;
            var bodyRight = shortBody ? 0.72f : 0.82f;
            var body = InsetRect(rect, bodyLeft, 0.34f, bodyRight - bodyLeft, 0.32f);
            var nose = new Vector2(body.xMin - rect.width * 0.06f, body.yMin + body.height * 0.5f);
            WireLine(nose, new Vector2(body.xMin, body.yMin), color, thickness);
            WireLine(nose, new Vector2(body.xMin, body.yMax), color, thickness);
            WireRectOutline(body, color, thickness);

            var tailX = body.xMax;
            var tailMidY = body.yMin + body.height * 0.5f;
            if (cruciformTail)
            {
                var fin = rect.width * 0.12f;
                WireLine(new Vector2(tailX, tailMidY), new Vector2(rect.xMax - rect.width * 0.04f, tailMidY), color, thickness);
                WireLine(new Vector2(tailX, body.yMin), new Vector2(tailX + fin, body.yMin - rect.height * 0.18f), color, thickness);
                WireLine(new Vector2(tailX, body.yMax), new Vector2(tailX + fin, body.yMax + rect.height * 0.18f), color, thickness);
                WireLine(new Vector2(tailX, tailMidY - body.height * 0.35f), new Vector2(tailX + fin * 0.7f, tailMidY - body.height * 0.55f), color, thickness);
                WireLine(new Vector2(tailX, tailMidY + body.height * 0.35f), new Vector2(tailX + fin * 0.7f, tailMidY + body.height * 0.55f), color, thickness);
            }
            else
            {
                WireLine(new Vector2(tailX, body.yMin), new Vector2(rect.xMax - rect.width * 0.05f, body.yMin - rect.height * 0.12f), color, thickness);
                WireLine(new Vector2(tailX, body.yMax), new Vector2(rect.xMax - rect.width * 0.05f, body.yMax + rect.height * 0.12f), color, thickness);
                if (!shortBody)
                {
                    var wingY = body.yMin + body.height * 0.5f;
                    WireLine(new Vector2(body.xMin + body.width * 0.45f, wingY), new Vector2(body.xMin + body.width * 0.45f, wingY - rect.height * 0.22f), color, thickness);
                    WireLine(new Vector2(body.xMin + body.width * 0.45f, wingY), new Vector2(body.xMin + body.width * 0.45f, wingY + rect.height * 0.22f), color, thickness);
                }
            }
        }

        private static Rect InsetRect(Rect parent, float xNorm, float yNorm, float wNorm, float hNorm)
        {
            return new Rect(
                parent.x + parent.width * xNorm,
                parent.y + parent.height * yNorm,
                parent.width * wNorm,
                parent.height * hNorm);
        }

        private static void WireRectOutline(Rect rect, Color color, float thickness)
        {
            WireLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), color, thickness);
            WireLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), color, thickness);
            WireLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), color, thickness);
            WireLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), color, thickness);
        }

        private static void WireLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            HudGuiUtility.DrawScreenLine(start, end, color, thickness, Texture2D.whiteTexture);
        }

        private static void DrawSelectedLabelBox(Rect rect, string label, Color hudColor, float s, GUIStyle labelStyle)
        {
            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            DrawBorder(rect, 1f * s, Color.white);
            GUI.color = previous;

            if (!string.IsNullOrEmpty(label))
            {
                labelStyle.normal.textColor = hudColor;
                GUI.Label(rect, label, labelStyle);
            }
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles(int rowFontSize)
        {
            if (rowStyle != null && lastRowFontSize == rowFontSize)
            {
                return;
            }

            var mfdColor = FlightHudColorPalette.Mfd;
            rowStyle = HudStyleFactory.CreateLabel(rowFontSize, FontStyle.Bold, TextAnchor.MiddleLeft, mfdColor);
            countStyle = HudStyleFactory.CreateLabel(rowFontSize, FontStyle.Bold, TextAnchor.MiddleRight, mfdColor);
            lastRowFontSize = rowFontSize;
        }
    }
}
