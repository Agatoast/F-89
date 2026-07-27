using UnityEngine;

namespace F89.LandCombat
{
    /// <summary>
    /// Hover corpse → preview loot at cursor.
    /// Click while hovering → pin bag open; click items to take (within range).
    /// </summary>
    public static class LandCorpseLootInput
    {
        public static void UpdateHover(Vector2 playerPosition)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                if (!LandLootBagSession.IsPinned)
                {
                    LandLootBagSession.Close();
                }

                return;
            }

            var worldMouse = (Vector2)camera.ScreenToWorldPoint(Input.mousePosition);
            var guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var hovered = FindCorpseUnderMouse(worldMouse);

            if (hovered != null)
            {
                hovered.EnsureLootReady();
                if (hovered.HasLootBag && hovered.HasLootRemaining)
                {
                    if (Input.GetMouseButtonDown(0) && !LandLootBagUi.IsPointerOverPanel())
                    {
                        LandLootBagSession.PinAtCursor(hovered, guiMouse);
                        return;
                    }

                    if (!LandLootBagSession.IsPinned)
                    {
                        LandLootBagSession.ShowHover(hovered);
                    }

                    return;
                }
            }

            if (LandLootBagSession.IsPinned)
            {
                if (LandLootBagUi.IsPointerOverPanel())
                {
                    LandLootBagSession.MarkCursorEnteredPanel();
                    return;
                }

                // Close only after the cursor has been on the bag, then left it.
                if (LandLootBagSession.HasCursorEnteredPanel)
                {
                    LandLootBagSession.Close();
                }

                return;
            }

            if (LandLootBagUi.IsPointerOverPanel())
            {
                return;
            }

            LandLootBagSession.Close();
        }

        public static bool IsPlayerInTakeRange(Vector2 playerPosition, LandGroundEnemy corpse)
        {
            if (corpse == null)
            {
                return false;
            }

            var distance = Vector2.Distance(playerPosition, corpse.transform.position);
            return distance <= LandUnits.ToWorld(LandEnemyLootRules.LootTakeRangeUnits);
        }

        private static LandGroundEnemy FindCorpseUnderMouse(Vector2 worldMouse)
        {
            LandGroundEnemy best = null;
            var bestDistance = float.MaxValue;
            var hoverRadius = LandUnits.ToWorld(LandEnemyLootRules.CorpseHoverRadiusUnits);
            var enemies = Object.FindObjectsByType<LandGroundEnemy>(FindObjectsSortMode.None);
            for (var i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsCorpse)
                {
                    continue;
                }

                var toClick = Vector2.Distance(worldMouse, enemy.transform.position);
                if (toClick > hoverRadius || toClick >= bestDistance)
                {
                    continue;
                }

                bestDistance = toClick;
                best = enemy;
            }

            return best;
        }
    }
}
