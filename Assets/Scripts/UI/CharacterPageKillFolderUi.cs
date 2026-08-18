using System.Collections.Generic;
using F89.Core;
using F89.Enemies;
using F89.LandCombat;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageKillFolderUi
    {
        private static readonly Dictionary<int, Texture2D> BossPortraitCache = new();

        private static GUIStyle slotNumberStyle;
        private static GUIStyle compactSlotNumberStyle;
        private static GUIStyle killCountStyle;
        private static GUIStyle compactKillCountStyle;

        private static Texture2D hoveredBossPortrait;
        private static Rect hoveredBossSlotRect;
        private static Rect hoveredFolderRect;

        public static void BeginFrame()
        {
            hoveredBossPortrait = null;
        }

        public static void DrawBossPortraitHoverPreview()
        {
            if (hoveredBossPortrait == null)
            {
                return;
            }

            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            const float previewScale = 1f;
            const float screenMargin = 8f;
            const float gapPx = 12f;

            var previewWidth = hoveredFolderRect.width * previewScale;
            var previewHeight = hoveredFolderRect.height * previewScale;
            var x = hoveredBossSlotRect.xMax + UiFitCanvas.Px(gapPx);
            var y = hoveredBossSlotRect.y + (hoveredBossSlotRect.height - previewHeight) * 0.5f;

            if (x + previewWidth > Screen.width - screenMargin)
            {
                x = hoveredBossSlotRect.xMin - previewWidth - UiFitCanvas.Px(gapPx);
            }

            x = Mathf.Clamp(x, screenMargin, Screen.width - previewWidth - screenMargin);
            y = Mathf.Clamp(y, screenMargin, Screen.height - previewHeight - screenMargin);

            var frameRect = new Rect(x, y, previewWidth, previewHeight);
            GUI.color = new Color(0.04f, 0.04f, 0.04f, 0.96f);
            GUI.DrawTexture(frameRect, Texture2D.whiteTexture);
            DrawSlotBorder(frameRect, UiFitCanvas.Px(2f), new Color(0.95f, 0.95f, 0.95f, 0.95f));

            var inset = InsetRect(frameRect, UiFitCanvas.Px(8f));
            GUI.color = Color.white;
            UiTextureFit.DrawTexture(inset, hoveredBossPortrait);
        }

        public static void DrawFolder(Rect folderRect, CharacterSaveData save, bool troopsFolder)
        {
            EnsureStyles();
            save = ResolveSave(save);
            if (troopsFolder)
            {
                DrawTroopBossFolder(folderRect, save);
                return;
            }

            DrawVehicleFolder(folderRect, save);
        }

        private static CharacterSaveData ResolveSave(CharacterSaveData save)
        {
            if (save == null)
            {
                return null;
            }

            var resolved = CharacterSaveRepository.FindById(save.Id) ?? save;
            CharacterSaveRepository.EnsureGearInitialized(resolved);
            CharacterSaveRepository.EnsureUrKillArrays(resolved);
            CharacterSaveRepository.SyncBossKillAwardMaskFromCampaignProgress(resolved);
            CharacterSaveRepository.EnsureBossKillAwardMaskInitialized(resolved);
            return resolved;
        }

        private static void DrawVehicleFolder(Rect folderRect, CharacterSaveData save)
        {
            var catalog = VehicleUnitCatalog.LoadOrDefault();

            for (var i = 0; i < CharacterPageLayout.VehicleKillFolderSlotCount; i++)
            {
                var slotRect = CharacterPageLayout.GetVehicleKillFolderSlotRect(folderRect, i);
                var level = i + 1;
                var kills = UrKillCredit.GetVehicleKillsAtLevel(save, level);
                var unit = catalog.GetUrVehicleByLevel(level);

                DrawSlotChrome(slotRect);
                DrawLevelKillSlot(slotRect, level, kills, unit, compact: false, vehiclesFolder: true);
            }

            GUI.color = Color.white;
        }

        private static void DrawTroopBossFolder(Rect folderRect, CharacterSaveData save)
        {
            var catalog = VehicleUnitCatalog.LoadOrDefault();
            BossPortraitCache.Clear();

            for (var i = 0; i < CharacterPageLayout.TroopKillFolderSlotCount; i++)
            {
                var slotRect = CharacterPageLayout.GetTroopKillFolderSlotRect(folderRect, i);
                DrawSlotChrome(slotRect);

                if (i == 0)
                {
                    DrawTroopSummarySlot(slotRect, save, catalog);
                    continue;
                }

                // Square 2 = boss 1 … square 20 = boss 19 (slot index matches boss number).
                DrawBossSlot(slotRect, folderRect, save, bossNumber: i);
            }

            GUI.color = Color.white;
        }

        private static void DrawTroopSummarySlot(
            Rect slotRect,
            CharacterSaveData save,
            VehicleUnitCatalog catalog)
        {
            const int troopLevel = 1;
            var kills = save != null ? UrKillCredit.Sum(save.UrTroopKillsByLevel) : 0;
            var unit = catalog.GetUrTroopByLevel(troopLevel);
            DrawLevelKillSlot(slotRect, troopLevel, kills, unit, compact: true);
        }

        private static void DrawLevelKillSlot(
            Rect slotRect,
            int level,
            int kills,
            VehicleUnitDefinition unit,
            bool compact,
            bool vehiclesFolder = false)
        {
            if (TryGetKillFolderSprite(unit, out var sprite, vehiclesFolder))
            {
                DrawUnitIcon(slotRect, sprite, compact, highlighted: kills > 0);
            }
            else
            {
                GUI.color = Color.white;
                GUI.Label(
                    slotRect,
                    level.ToString(),
                    compact ? compactSlotNumberStyle : slotNumberStyle);
            }

            if (kills > 0)
            {
                DrawKillCountBadge(slotRect, kills, compact);
            }
        }

        private static bool TryGetKillFolderSprite(
            VehicleUnitDefinition unit,
            out Sprite sprite,
            bool vehiclesFolder)
        {
            sprite = null;
            if (unit == null || string.IsNullOrWhiteSpace(unit.abbreviation))
            {
                return false;
            }

            if (unit.isFlier
                && string.Equals(
                    unit.abbreviation,
                    UrTdpSpriteSheet.Abbreviation,
                    System.StringComparison.OrdinalIgnoreCase)
                && UrTdpSpriteSheet.TryGetFrames(out var tdpFrames)
                && tdpFrames != null
                && tdpFrames.Length > 0
                && tdpFrames[0] != null)
            {
                sprite = tdpFrames[0];
                return true;
            }

            if (unit.isTroop
                && unit.designation == VehicleUnitDesignation.UR
                && TryGetUrTroopKillFolderSprite(out sprite))
            {
                return true;
            }

            // Vehicle kill boxes use left-facing side art from the UR sheet.
            if (vehiclesFolder)
            {
                if (string.Equals(
                        unit.abbreviation,
                        UrArwSpriteSheet.Abbreviation,
                        System.StringComparison.OrdinalIgnoreCase)
                    && UrArwSpriteSheet.TryGetVehicleKillFolderSprite(out sprite))
                {
                    return true;
                }

                return UrVehicleSpriteSheet.TryGetSideSprite(unit.abbreviation, out sprite);
            }

            if (UrVehicleSpriteSheet.TryGetNorthFacingSprite(unit.abbreviation, out sprite))
            {
                return true;
            }

            return UrVehicleSpriteSheet.TryGetSideSprite(unit.abbreviation, out sprite);
        }

        private static bool TryGetUrTroopKillFolderSprite(out Sprite sprite)
        {
            sprite = null;
            var idleFrames = LandEnemySpriteSheet.GetClip(LandEnemySpriteSheet.Clip.Idle);
            if (idleFrames == null || idleFrames.Length == 0 || idleFrames[0] == null)
            {
                return false;
            }

            sprite = idleFrames[0];
            return true;
        }

        private static void DrawBossSlot(Rect slotRect, Rect folderRect, CharacterSaveData save, int bossNumber)
        {
            if (bossNumber < LandBossEncounter.FirstBossNumber
                || bossNumber > LandBossEncounter.LastBossNumber
                || !IsBossDefeated(save, bossNumber))
            {
                return;
            }

            var portrait = LoadBossPortrait(bossNumber);
            if (portrait == null)
            {
                return;
            }

            var inset = InsetRect(slotRect, UiFitCanvas.Px(2f));
            GUI.color = Color.white;
            UiTextureFit.DrawTexture(inset, portrait);

            if (IsHovered(slotRect))
            {
                hoveredBossPortrait = portrait;
                hoveredBossSlotRect = slotRect;
                hoveredFolderRect = folderRect;
            }
        }

        private static bool IsBossDefeated(CharacterSaveData save, int bossNumber)
        {
            if (save == null || bossNumber <= 0)
            {
                return false;
            }

            return LandBossEncounter.IsBossKillAwarded(save, bossNumber);
        }

        private static Texture2D LoadBossPortrait(int bossNumber)
        {
            if (BossPortraitCache.TryGetValue(bossNumber, out var cached) && cached != null)
            {
                return cached;
            }

            var portrait = LandBossPortraitCatalog.LoadPortrait(bossNumber);
            if (portrait != null)
            {
                BossPortraitCache[bossNumber] = portrait;
            }

            return portrait;
        }

        private static void DrawSlotChrome(Rect slotRect)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 0.92f);
            GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
            DrawSlotBorder(slotRect, 1f, new Color(0.95f, 0.95f, 0.95f, 0.95f));
        }

        private static void DrawUnitIcon(Rect slotRect, Sprite sprite, bool compact, bool highlighted)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var inset = InsetRect(slotRect, UiFitCanvas.Px(compact ? 3f : 4f));
            GUI.color = highlighted ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            DrawSprite(inset, sprite, bottomAlign: !compact);
            GUI.color = Color.white;
        }

        private static void DrawSprite(Rect clipRect, Sprite sprite, bool bottomAlign = false)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var tr = sprite.textureRect;
            if (tr.width <= 0f || tr.height <= 0f)
            {
                return;
            }

            var spriteAspect = tr.width / tr.height;
            var fitWidth = clipRect.width;
            var fitHeight = fitWidth / spriteAspect;
            if (fitHeight > clipRect.height)
            {
                fitHeight = clipRect.height;
                fitWidth = fitHeight * spriteAspect;
            }

            var drawY = bottomAlign
                ? clipRect.yMax - fitHeight
                : clipRect.y + (clipRect.height - fitHeight) * 0.5f;
            var drawRect = new Rect(
                clipRect.x + (clipRect.width - fitWidth) * 0.5f,
                drawY,
                fitWidth,
                fitHeight);
            var texCoords = new Rect(
                tr.x / texture.width,
                tr.y / texture.height,
                tr.width / texture.width,
                tr.height / texture.height);
            GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords, alphaBlend: true);
        }

        private static void DrawKillCountBadge(Rect slotRect, int kills, bool compact)
        {
            var label = kills.ToString();
            var style = compact ? compactKillCountStyle : killCountStyle;
            var badgeWidth = Mathf.Max(
                UiFitCanvas.Px(compact ? 12f : 18f),
                style.CalcSize(new GUIContent(label)).x + UiFitCanvas.Px(compact ? 4f : 6f));
            var badgeHeight = UiFitCanvas.Px(compact ? 12f : 16f);
            var badgeRect = new Rect(
                slotRect.xMax - badgeWidth - UiFitCanvas.Px(2f),
                slotRect.yMax - badgeHeight - UiFitCanvas.Px(2f),
                badgeWidth,
                badgeHeight);

            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(badgeRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(badgeRect, label, style);
        }

        private static Rect InsetRect(Rect rect, float inset)
        {
            return new Rect(
                rect.x + inset,
                rect.y + inset,
                Mathf.Max(0f, rect.width - (inset * 2f)),
                Mathf.Max(0f, rect.height - (inset * 2f)));
        }

        private static void DrawSlotBorder(Rect rect, float thickness, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static bool IsHovered(Rect rect)
        {
            if (Event.current == null)
            {
                return false;
            }

            return rect.Contains(Event.current.mousePosition);
        }

        private static void EnsureStyles()
        {
            var slotFontSize = Mathf.Max(12, Mathf.RoundToInt(24f * UiFitCanvas.Scale));
            var compactSlotFontSize = Mathf.Max(10, Mathf.RoundToInt(18f * UiFitCanvas.Scale));
            var killFontSize = Mathf.Max(9, Mathf.RoundToInt(12f * UiFitCanvas.Scale));
            var compactKillFontSize = Mathf.Max(8, Mathf.RoundToInt(11f * UiFitCanvas.Scale));

            if (slotNumberStyle == null)
            {
                slotNumberStyle = HudStyleFactory.CreateLabel(
                    slotFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white);
                compactSlotNumberStyle = HudStyleFactory.CreateLabel(
                    compactSlotFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white);
                killCountStyle = HudStyleFactory.CreateLabel(
                    killFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white);
                compactKillCountStyle = HudStyleFactory.CreateLabel(
                    compactKillFontSize,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter,
                    Color.white);
                return;
            }

            slotNumberStyle.fontSize = slotFontSize;
            compactSlotNumberStyle.fontSize = compactSlotFontSize;
            killCountStyle.fontSize = killFontSize;
            compactKillCountStyle.fontSize = compactKillFontSize;
        }
    }
}
