using System.Collections.Generic;
using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Hover shows loot at the cursor. Click while hovering pins the bag open for looting.
    /// </summary>
    public static class LandLootBagSession
    {
        public static LandGroundEnemy OpenCorpse { get; private set; }
        public static bool IsPinned { get; private set; }
        public static bool HasCursorEnteredPanel { get; private set; }
        public static bool IsOpen => OpenCorpse != null && OpenCorpse.HasLootBag;
        public static Vector2 PinnedGuiPosition { get; private set; }

        public static void ShowHover(LandGroundEnemy corpse)
        {
            if (corpse == null || !corpse.HasLootBag)
            {
                return;
            }

            if (IsPinned && OpenCorpse == corpse)
            {
                return;
            }

            OpenCorpse = corpse;
            IsPinned = false;
            HasCursorEnteredPanel = false;
        }

        public static void PinAtCursor(LandGroundEnemy corpse, Vector2 guiPosition)
        {
            if (corpse == null || !corpse.HasLootBag)
            {
                return;
            }

            OpenCorpse = corpse;
            IsPinned = true;
            HasCursorEnteredPanel = false;
            PinnedGuiPosition = guiPosition;
        }

        public static void MarkCursorEnteredPanel()
        {
            if (IsPinned)
            {
                HasCursorEnteredPanel = true;
            }
        }

        public static void Close()
        {
            OpenCorpse = null;
            IsPinned = false;
            HasCursorEnteredPanel = false;
            PinnedGuiPosition = default;
        }

        public static IReadOnlyList<LandGearInstance> GetOpenItems()
        {
            return OpenCorpse != null ? OpenCorpse.LootItems : null;
        }

        public static bool TryTakeItem(int index)
        {
            if (OpenCorpse == null)
            {
                return false;
            }

            var player = Object.FindAnyObjectByType<LandPlayerController>();
            if (player == null
                || !LandCorpseLootInput.IsPlayerInTakeRange(player.transform.position, OpenCorpse))
            {
                Debug.Log("F-89 Land: Move closer (within 4 tiles) to take loot.");
                return false;
            }

            if (!OpenCorpse.TryTakeLootItem(index, out var item) || item == null)
            {
                return false;
            }

            if (LandConsumableIds.IsBandage(item))
            {
                if (!LandCombatConsumables.TryAddBandage())
                {
                    OpenCorpse.ReturnLootItem(index, item);
                    LandHudNotice.Show("BANDAGES FULL");
                    return false;
                }
            }
            else if (LandConsumableIds.IsGrenade(item))
            {
                if (!LandCombatConsumables.TryAddGrenade())
                {
                    OpenCorpse.ReturnLootItem(index, item);
                    LandHudNotice.Show("GRENADES FULL");
                    return false;
                }
            }
            else
            {
                var loadout = CharacterGearSession.ActiveLoadout;
                if (!LandLoadoutEquipService.TryAddToFirstEmptyInventory(loadout, item))
                {
                    OpenCorpse.ReturnLootItem(index, item);
                    Debug.Log("F-89 Land: Inventory full — cannot take loot.");
                    return false;
                }

                CharacterGearSession.PersistActive();
            }

            if (!OpenCorpse.HasLootRemaining)
            {
                OpenCorpse.NotifyLootBagEmptied();
                Close();
            }

            return true;
        }
    }
}
