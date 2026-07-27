using F89.Core;
using F89.UI;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>MTAU-style ground combat HUD: minimap, HP, inventory, Bandages/Grenades, paperdoll.</summary>
    public static class LandCombatHud
    {
        private const string PaperdollResourcePath = "CharacterPage/paperdoll";

        private static Texture2D paperdollTexture;

        public static bool IsPointerOverHud()
        {
            var mouse = Event.current != null
                ? Event.current.mousePosition
                : new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            return LandCombatHudLayout.GetMinimapRect().Contains(mouse)
                   || LandCombatHudLayout.GetHpBarRect().Contains(mouse)
                   || LandCombatHudLayout.GetInventoryRect().Contains(mouse)
                   || LandCombatHudLayout.GetBandagesBoxRect().Contains(mouse)
                   || LandCombatHudLayout.GetGrenadeBoxRect().Contains(mouse)
                   || LandCombatHudLayout.GetItemTooltipRect().Contains(mouse)
                   || LandCombatHudLayout.GetPaperdollRect().Contains(mouse)
                   || LandCombatHudLayout.GetSettingsButtonRect().Contains(mouse)
                   || ContainsAnyEquipmentSlot(mouse);
        }

        public static void Draw(System.Action onReturnRequested)
        {
            CharacterGearSession.Bind(CharacterSessionState.ActiveSave);
            LandItemTooltipUi.BeginFrame();
            EnsurePaperdoll();

            var inventoryGrid = LandCombatHudLayout.GetInventoryGridRect();
            var unusedVault = new Rect(-4000f, -4000f, 8f, 8f);
            CharacterPageGearUi.HandleGearDragAndDrop(
                LandCombatHudLayout.GetEquipmentSlotRect,
                inventoryGrid,
                unusedVault,
                footlockerTopAlign: true);

            DrawStatusLine();
            DrawHpBar();
            DrawAvatarHpBar();
            DrawEnemyHpBars();
            DrawMinimap();
            DrawPaperdoll();
            CharacterPageGearUi.DrawEquipmentSlots(LandCombatHudLayout.GetEquipmentSlotRect);
            DrawPaperdollDr();
            DrawBandagesBox();
            DrawGrenadeBox();
            DrawInventoryPanel(inventoryGrid);
            CharacterPageGearUi.DrawDragOverlay();
            DrawFooterButtons(onReturnRequested);
            LandItemTooltipUi.Draw(LandItemTooltipUi.Placement.GroundHudPanel);
            DrawGearDeleteConfirmDialog();
            LandHudNotice.Draw();
        }

        private static void DrawGearDeleteConfirmDialog()
        {
            if (!CharacterPageGearUi.IsDeleteConfirmPending)
            {
                return;
            }

            var result = GearDeleteConfirmDialog.Draw(true);
            if (result == GearDeleteConfirmDialog.Result.Confirmed)
            {
                CharacterPageGearUi.ConfirmDeleteItem();
            }
            else if (result == GearDeleteConfirmDialog.Result.Cancelled)
            {
                CharacterPageGearUi.CancelDeleteConfirm();
            }
        }

        public static void HandleHotkeys()
        {
            if (GamePauseController.IsPaused)
            {
                return;
            }

            if (GameKeyBindings.WasPressed(GameKeyBindingIds.ThrowGrenade)
                && !LandCombatHud.IsPointerOverHud()
                && !LandLootBagUi.IsPointerOverPanel()
                && !CharacterPageGearUi.IsDraggingGear)
            {
                TryThrowGrenade();
            }

            if (GameKeyBindings.WasPressed(GameKeyBindingIds.UseBandage))
            {
                TryUseBandage();
            }
        }

        private static void TryUseBandage()
        {
            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            if (health == null || !health.IsAlive)
            {
                return;
            }

            if (health.CurrentHealth >= health.MaxHealth)
            {
                Debug.Log("F-89 Land: Already at full HP.");
                return;
            }

            if (!LandCombatConsumables.TryUseBandage())
            {
                Debug.Log("F-89 Land: No bandages remaining.");
                return;
            }

            health.Heal(LandGameConstants.BandageHealAmount);
            Debug.Log(
                $"F-89 Land: Bandage applied (+{LandGameConstants.BandageHealAmount} HP, "
                + $"{LandCombatConsumables.BandageCount} left).");
        }

        private static void TryThrowGrenade()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null)
            {
                return;
            }

            var health = player.GetComponent<LandPlayerHealth>();
            if (health != null && !health.IsAlive)
            {
                return;
            }

            if (!LandCombatConsumables.TryUseGrenade())
            {
                Debug.Log("F-89 Land: No grenades remaining.");
                return;
            }

            var camera = Camera.main;
            var origin = (Vector2)player.transform.position;
            Vector2 target;
            if (camera != null)
            {
                var mouse = camera.ScreenToWorldPoint(Input.mousePosition);
                target = new Vector2(mouse.x, mouse.y);
            }
            else
            {
                target = origin + Vector2.right * LandUnits.ToWorld(6f);
            }

            LandGrenade.Throw(origin, target);
            Debug.Log($"F-89 Land: Grenade thrown ({LandCombatConsumables.GrenadeCount} left).");
        }

        private static void DrawStatusLine()
        {
            var style = HudStyleFactory.CreateLabel(14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            var areaLabel = LandBossAreaState.HasActiveArea ? $"  |  {LandBossAreaState.SurfaceCode}" : string.Empty;
            GUI.Label(
                LandCombatHudLayout.GetStatusLineRect(),
                $"Ground Ops{areaLabel}  |  Kills: {LandGroundSceneController.SessionKills}  Score: {LandGroundSceneController.SessionScore}"
                + (AutoFireState.Enabled ? "  |  AUTO ON" : string.Empty)
                + $"  |  {FormatColdStatus()}",
                style);
        }

        private static string FormatColdStatus()
        {
            var cold = Object.FindAnyObjectByType<LandColdExposure>();
            if (cold == null)
            {
                return "Cold: —";
            }

            if (cold.IsSheltered)
            {
                return "Cold: SHELTERED";
            }

            var remaining = cold.RemainingSeconds;
            var hours = Mathf.FloorToInt(remaining / 3600f);
            var minutes = Mathf.FloorToInt((remaining % 3600f) / 60f);
            var seconds = Mathf.FloorToInt(remaining % 60f);
            return $"Cold: {hours:0}:{minutes:00}:{seconds:00}";
        }

        private static void DrawHpBar()
        {
            var health = Object.FindAnyObjectByType<LandPlayerHealth>();
            if (health == null)
            {
                return;
            }

            var barRect = LandCombatHudLayout.GetHpBarRect();
            var fillRect = new Rect(
                barRect.x + 1f,
                barRect.y + 1f,
                (barRect.width - 2f) * health.HealthNormalized,
                barRect.height - 2f);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            GUI.color = health.IsAlive
                ? new Color(0.18f, 0.82f, 0.28f, 0.95f)
                : new Color(0.75f, 0.12f, 0.12f, 0.95f);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var label = HudStyleFactory.CreateLabel(13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            var current = Mathf.CeilToInt(health.CurrentHealth);
            var max = Mathf.CeilToInt(health.MaxHealth);
            GUI.Label(barRect, $"HP  {current} / {max}", label);
        }

        private static void DrawAvatarHpBar()
        {
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            var health = player != null ? player.GetComponent<LandPlayerHealth>() : null;
            var camera = Camera.main;
            if (player == null || health == null || camera == null)
            {
                return;
            }

            if (!player.TryGetComponent<SpriteRenderer>(out var sprite) || sprite.sprite == null)
            {
                return;
            }

            DrawWorldHpBar(
                camera,
                sprite.bounds,
                health.HealthNormalized,
                health.IsAlive
                    ? new Color(0.2f, 0.9f, 0.3f, 0.95f)
                    : new Color(0.75f, 0.12f, 0.12f, 0.95f));
        }

        /// <summary>Player screen and world HP bars for minimal gameplay HUDs such as the bunker.</summary>
        public static void DrawPlayerHp()
        {
            DrawHpBar();
            DrawAvatarHpBar();
        }

        /// <summary>Floating HP bars over living UR soldiers / bosses (ground + bunker).</summary>
        public static void DrawEnemyHpBars()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                if (!enemy.TryGetComponent<SpriteRenderer>(out var sprite) || sprite.sprite == null)
                {
                    continue;
                }

                var fill = Mathf.Clamp01(enemy.CurrentHealth / Mathf.Max(0.01f, enemy.MaxHealth));
                DrawWorldHpBar(camera, sprite.bounds, fill, new Color(0.9f, 0.15f, 0.12f, 0.95f));
            }

            GUI.color = Color.white;
        }

        private static void DrawWorldHpBar(Camera camera, Bounds bounds, float fill01, Color fillColor)
        {
            var leftTop = camera.WorldToScreenPoint(new Vector3(bounds.min.x, bounds.max.y, 0f));
            var rightTop = camera.WorldToScreenPoint(new Vector3(bounds.max.x, bounds.max.y, 0f));
            if (leftTop.z < 0f || rightTop.z < 0f)
            {
                return;
            }

            var x = Mathf.Min(leftTop.x, rightTop.x);
            var width = Mathf.Max(12f, Mathf.Abs(rightTop.x - leftTop.x));
            const float height = 6f;
            var y = Screen.height - Mathf.Max(leftTop.y, rightTop.y) - height - 3f;
            var barRect = new Rect(x, y, width, height);
            var fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(fill01), barRect.height);

            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawMinimap()
        {
            var rect = LandCombatHudLayout.GetMinimapRect();
            GUI.color = new Color(0.06f, 0.1f, 0.12f, 0.88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.55f, 0.78f, 0.95f, 0.95f);
            DrawBorder(rect, 2f);
            GUI.color = Color.white;

            var title = HudStyleFactory.CreateLabel(11, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            GUI.Label(new Rect(rect.x, rect.y + 2f, rect.width, 16f), "MAP", title);

            var mapInner = new Rect(rect.x + 6f, rect.y + 20f, rect.width - 12f, rect.height - 26f);
            GUI.color = new Color(0.12f, 0.18f, 0.22f, 0.95f);
            GUI.DrawTexture(mapInner, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Player-centered view: radius = arena half × multiplier (4× area vs camera).
            var half = LandGameConstants.ArenaHalfSizeWorldUnits * LandGameConstants.MinimapViewHalfSizeMultiplier;
            var player = Object.FindAnyObjectByType<LandPlayerController>();
            var center = player != null ? player.transform.position : Vector3.zero;

            // Player stays fixed in the middle of the minimap.
            DrawMinimapDotAtCenter(mapInner, new Color(0.25f, 0.95f, 0.35f), 6f);

            DrawMinimapLandedPlane(mapInner, center, half);
            DrawMinimapBunker(mapInner, center, half);

            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                DrawMinimapDotRelative(mapInner, enemy.transform.position, center, half, new Color(0.95f, 0.25f, 0.2f), 5f);
            }
        }

        private static void DrawMinimapLandedPlane(Rect mapInner, Vector3 center, float viewHalf)
        {
            var plane = LandLandedPlane.Instance;
            if (plane == null)
            {
                return;
            }

            var planeColor = new Color(0.25f, 0.55f, 1f, 1f);
            var localX = plane.WorldPosition.x - center.x;
            var localY = plane.WorldPosition.y - center.y;
            var inRange = Mathf.Abs(localX) <= viewHalf && Mathf.Abs(localY) <= viewHalf;
            if (inRange)
            {
                DrawMinimapDotRelative(mapInner, plane.WorldPosition, center, viewHalf, planeColor, 7f);
                return;
            }

            // Off-view: blue arrow on the map edge pointing toward the plane.
            DrawMinimapEdgeArrow(mapInner, localX, localY, viewHalf, planeColor);
        }

        private static void DrawMinimapBunker(Rect mapInner, Vector3 center, float viewHalf)
        {
            var bunker = Object.FindAnyObjectByType<LandBunkerEntrance>();
            if (bunker == null)
            {
                return;
            }

            var bunkerPos = bunker.WorldPosition;
            var delta = bunkerPos - (Vector2)center;
            var landRange = LandUnits.ToLand(delta.magnitude);
            if (landRange > LandGameConstants.BunkerMinimapIndicatorMaxRangeLandUnits)
            {
                return;
            }

            var bunkerColor = new Color(0.95f, 0.15f, 0.12f, 1f);
            var localX = bunkerPos.x - center.x;
            var localY = bunkerPos.y - center.y;
            var onMap = Mathf.Abs(localX) <= viewHalf && Mathf.Abs(localY) <= viewHalf;

            if (onMap)
            {
                DrawMinimapDotRelative(mapInner, bunkerPos, center, viewHalf, bunkerColor, 7f);
                return;
            }

            DrawMinimapEdgeArrow(mapInner, localX, localY, viewHalf, bunkerColor);
        }

        private static void DrawMinimapDotAtCenter(Rect mapInner, Color color, float size)
        {
            var x = mapInner.x + mapInner.width * 0.5f;
            var y = mapInner.y + mapInner.height * 0.5f;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawMinimapDotRelative(
            Rect mapInner,
            Vector3 worldPos,
            Vector3 center,
            float viewHalf,
            Color color,
            float size)
        {
            var localX = worldPos.x - center.x;
            var localY = worldPos.y - center.y;
            if (Mathf.Abs(localX) > viewHalf || Mathf.Abs(localY) > viewHalf)
            {
                return;
            }

            var nx = Mathf.InverseLerp(-viewHalf, viewHalf, localX);
            var ny = Mathf.InverseLerp(viewHalf, -viewHalf, localY);
            var x = mapInner.x + nx * mapInner.width;
            var y = mapInner.y + ny * mapInner.height;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawMinimapEdgeArrow(
            Rect mapInner,
            float localX,
            float localY,
            float viewHalf,
            Color color)
        {
            // Normalize into map space (Y flipped for GUI). Clamp to the edge of the square.
            var nx = localX / viewHalf;
            var ny = -localY / viewHalf;
            var absMax = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
            if (absMax < 0.0001f)
            {
                return;
            }

            nx /= absMax;
            ny /= absMax;

            const float edgeInset = 8f;
            var x = mapInner.x + mapInner.width * 0.5f + nx * (mapInner.width * 0.5f - edgeInset);
            var y = mapInner.y + mapInner.height * 0.5f + ny * (mapInner.height * 0.5f - edgeInset);
            var angle = Mathf.Atan2(ny, nx) * Mathf.Rad2Deg;
            DrawArrowTriangle(x, y, angle, color);
        }

        private static Texture2D minimapArrowTexture;

        private static void DrawArrowTriangle(float x, float y, float angleDegrees, Color color)
        {
            EnsureMinimapArrowTexture();
            var previousMatrix = GUI.matrix;
            const float size = 14f;
            GUIUtility.RotateAroundPivot(angleDegrees, new Vector2(x, y));
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - size * 0.25f, y - size * 0.5f, size, size), minimapArrowTexture);
            GUI.color = Color.white;
            GUI.matrix = previousMatrix;
        }

        private static void EnsureMinimapArrowTexture()
        {
            if (minimapArrowTexture != null)
            {
                return;
            }

            const int size = 16;
            minimapArrowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            // Right-pointing triangle (tip at the right edge).
            var midY = (size - 1) * 0.5f;
            for (var py = 0; py < size; py++)
            {
                for (var px = 0; px < size; px++)
                {
                    var distFromTip = size - 1 - px;
                    var rowHalf = midY * (distFromTip / (float)(size - 1));
                    if (distFromTip >= 0 && Mathf.Abs(py - midY) <= rowHalf + 0.35f)
                    {
                        pixels[py * size + px] = Color.white;
                    }
                }
            }

            minimapArrowTexture.SetPixels(pixels);
            minimapArrowTexture.Apply(false, true);
        }

        private static void DrawPaperdoll()
        {
            var paperdoll = LandCombatHudLayout.GetPaperdollRect();
            GUI.color = new Color(0.08f, 0.1f, 0.12f, 0.55f);
            GUI.DrawTexture(paperdoll, Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (paperdollTexture != null)
            {
                GUI.DrawTexture(paperdoll, paperdollTexture, ScaleMode.ScaleToFit, true);
            }
        }

        private static void DrawPaperdollDr()
        {
            LandPaperdollDrUi.Draw(
                LandCombatHudLayout.GetPaperdollDrLabelRect(),
                LandCombatHudLayout.GetPaperdollDrValueRect());
        }

        private static void DrawBandagesBox()
        {
            var rect = LandCombatHudLayout.GetBandagesBoxRect();
            DrawConsumableBox(
                rect,
                "BANDAGES",
                LandCombatConsumables.BandageCount,
                LandGameConstants.BandageSlotCapacity,
                LandItemTileOverlay.GetBandageIcon());
        }

        private static void DrawGrenadeBox()
        {
            var rect = LandCombatHudLayout.GetGrenadeBoxRect();
            DrawConsumableBox(
                rect,
                "GRENADES",
                LandCombatConsumables.GrenadeCount,
                LandGameConstants.GrenadeSlotCapacity,
                LandItemTileOverlay.GetGrenadeIcon());
        }

        private static void DrawConsumableBox(
            Rect rect,
            string title,
            int count,
            int capacity,
            Texture2D icon = null)
        {
            GUI.color = new Color(0.08f, 0.1f, 0.12f, 0.88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.75f, 0.7f, 0.35f, 0.95f);
            DrawBorder(rect, 1.5f);
            GUI.color = Color.white;

            var titleStyle = HudStyleFactory.CreateLabel(11, FontStyle.Bold, TextAnchor.UpperCenter, Color.white);
            var countStyle = HudStyleFactory.CreateLabel(16, FontStyle.Bold, TextAnchor.LowerCenter, Color.white);
            GUI.Label(new Rect(rect.x, rect.y + 4f, rect.width, 18f), title, titleStyle);

            if (icon != null)
            {
                var iconSize = Mathf.Min(rect.width * 0.55f, rect.height * 0.45f);
                var iconRect = new Rect(
                    rect.x + (rect.width - iconSize) * 0.5f,
                    rect.y + 22f,
                    iconSize,
                    iconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
            }

            GUI.Label(
                new Rect(rect.x, rect.yMax - 22f, rect.width, 20f),
                $"{count}/{capacity}",
                countStyle);
        }

        private static void DrawInventoryPanel(Rect inventoryGrid)
        {
            var titleRect = LandCombatHudLayout.GetInventoryTitleRect();
            GUI.color = new Color(0.08f, 0.1f, 0.12f, 0.75f);
            GUI.DrawTexture(titleRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var title = HudStyleFactory.CreateLabel(12, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            GUI.Label(new Rect(titleRect.x + 4f, titleRect.y, titleRect.width, titleRect.height), "INVENTORY", title);
            CharacterPageGearUi.DrawInventory(inventoryGrid);
        }

        private static void DrawFooterButtons(System.Action _)
        {
            if (StartPageMenuStyles.DrawMenuButton(LandCombatHudLayout.GetSettingsButtonRect(), "SETTINGS", fontSize: 14))
            {
                GamePauseController.OpenSettingsMenu();
            }
        }

        private static bool ContainsAnyEquipmentSlot(Vector2 mouse)
        {
            for (var i = 0; i < LandCombatHudLayout.EquipmentSlotCount; i++)
            {
                if (LandCombatHudLayout.GetEquipmentSlotRect(i).Contains(mouse))
                {
                    return true;
                }
            }

            return false;
        }

        private static void DrawBorder(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        }

        private static void EnsurePaperdoll()
        {
            if (paperdollTexture != null)
            {
                return;
            }

            paperdollTexture = Resources.Load<Texture2D>(PaperdollResourcePath);
        }
    }
}
