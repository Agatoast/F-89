using System.Collections.Generic;
using F89.Core;
using UnityEngine;

namespace F89.UI
{
    public static class CharacterPageRibbonUi
    {
        public static void DrawRibbons(Rect panelRect, IReadOnlyList<string> earnedRibbonIds)
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

                GUI.DrawTexture(slotRect, texture, ScaleMode.StretchToFill, true);
                MilitaryAwardTooltipUi.RegisterHover(slotRect, slot.RibbonId);
            }
        }
    }
}
