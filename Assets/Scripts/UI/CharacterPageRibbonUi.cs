using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageRibbonUi
    {
        public static void DrawRibbons(Rect panelRect, CharacterSaveData save)
        {
            DrawRibbons(panelRect, save?.EarnedRibbonIds, save);
        }

        public static void DrawRibbons(Rect panelRect, IReadOnlyList<string> earnedRibbonIds)
        {
            DrawRibbons(panelRect, earnedRibbonIds, CharacterSessionState.ActiveSave);
        }

        public static void DrawRibbons(
            Rect panelRect,
            IReadOnlyList<string> earnedRibbonIds,
            CharacterSaveData save)
        {
            var slots = MilitaryRibbonLayout.BuildGridSlots(earnedRibbonIds);
            if (slots.Count == 0)
            {
                return;
            }

            foreach (var slot in slots)
            {
                var slotRect = CharacterPageLayout.GetRibbonSlotRect(panelRect, slot.Row, slot.Column);
                var texture = MilitaryRibbonService.GetRibbonTexture(slot.RibbonId);
                if (texture == null)
                {
                    continue;
                }

                var ribbonRect = GetRibbonDrawRect(slotRect, texture);
                UiTextureFit.DrawTextureExact(ribbonRect, texture);
                DrawAwardDevice(ribbonRect, save, slot.RibbonId);
                MilitaryAwardTooltipUi.RegisterHover(ribbonRect, slot.RibbonId);
            }
        }

        private static Rect GetRibbonDrawRect(Rect slotRect, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
            {
                return slotRect;
            }

            var drawWidth = slotRect.width;
            var drawHeight = drawWidth * (texture.height / (float)texture.width);
            if (drawHeight > slotRect.height)
            {
                drawHeight = slotRect.height;
                drawWidth = drawHeight * (texture.width / (float)texture.height);
            }

            return new Rect(slotRect.x, slotRect.y, drawWidth, drawHeight);
        }

        private static void DrawAwardDevice(Rect ribbonRect, CharacterSaveData save, string ribbonId)
        {
            if (!MilitaryRibbonCatalog.SupportsAwardDevices(ribbonId))
            {
                return;
            }

            var awardCount = CharacterSaveRepository.GetRibbonAwardCount(save, ribbonId);
            if (!MilitaryRibbonAwardDevices.TryResolve(awardCount, out var composition))
            {
                return;
            }

            MilitaryRibbonDeviceService.DrawComposition(ribbonRect, composition);
        }
    }
}
