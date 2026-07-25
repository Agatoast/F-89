using F89.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    public class CharacterLoadoutController : MonoBehaviour
    {
        private const float Margin = 20f;
        private const int InventoryRows = 3;
        private const int InventoryColumns = 10;
        private const float ButtonWidth = 190f;
        private const float ButtonHeight = 44f;
        private static readonly Color EquipmentSlotColor = new Color(0.15f, 0.65f, 0.2f);

        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        private bool showBailOutConfirm;

        private void OnGUI()
        {
            EnsureStyles();
            DrawPageBackground();

            var save = CharacterSessionState.ActiveSave;
            var leftWidth = Screen.width * 0.2f;
            var leftX = Margin;
            var topY = Margin;
            var rightX = leftX + leftWidth + Margin;
            var rightWidth = Screen.width - rightX - Margin;

            var profileHeight = Screen.height * 0.42f;
            DrawProfilePanel(new Rect(leftX, topY, leftWidth, profileHeight), save);
            DrawResearchButton(new Rect(leftX, topY + profileHeight + Margin, leftWidth, 40f));

            var inventoryHeight = Screen.height * 0.28f;
            DrawInventoryGrid(new Rect(rightX, topY, rightWidth, inventoryHeight));

            var figureTop = topY + inventoryHeight + Margin;
            var figureBottom = Screen.height - ButtonHeight - Margin * 2f;
            var figureRect = new Rect(
                rightX + rightWidth * 0.18f,
                figureTop,
                rightWidth * 0.64f,
                figureBottom - figureTop);
            DrawEquipmentPanel(figureRect);

            DrawActionButtons();
            if (showBailOutConfirm)
            {
                var dialogResult = BailOutConfirmDialog.Draw(true);
                if (dialogResult == BailOutConfirmDialog.Result.Confirmed)
                {
                    ConfirmBailOut();
                }
                else if (dialogResult == BailOutConfirmDialog.Result.Cancelled)
                {
                    showBailOutConfirm = false;
                }
            }
        }

        private void DrawProfilePanel(Rect rect, CharacterSaveData save)
        {
            var name = save != null ? save.DisplayRankAndName : "OPERATOR";
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 28f), name, headerStyle);

            var pictureRect = new Rect(rect.x, rect.y + 34f, rect.width, rect.height - 34f);
            DrawWireBox(pictureRect, 2f);
            GUI.Label(
                new Rect(pictureRect.x, pictureRect.y + pictureRect.height * 0.45f, pictureRect.width, 24f),
                "Picture",
                bodyStyle);
        }

        private void DrawResearchButton(Rect rect)
        {
            DrawRoundedHeader(rect, "Research and Development");
        }

        private void DrawInventoryGrid(Rect rect)
        {
            DrawWireBox(rect, 2f);

            var padding = 8f;
            var inner = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f);
            var cellWidth = inner.width / InventoryColumns;
            var cellHeight = inner.height / InventoryRows;

            for (var row = 0; row < InventoryRows; row++)
            {
                for (var col = 0; col < InventoryColumns; col++)
                {
                    var cell = new Rect(
                        inner.x + col * cellWidth + 2f,
                        inner.y + row * cellHeight + 2f,
                        cellWidth - 4f,
                        cellHeight - 4f);
                    DrawWireBox(cell, 2f);
                }
            }
        }

        private void DrawEquipmentPanel(Rect rect)
        {
            DrawWireBox(rect, 2f);
            DrawSilhouette(rect);
            DrawEquipmentSlots(rect);
        }

        private static void DrawSilhouette(Rect rect)
        {
            var centerX = rect.x + rect.width * 0.5f;
            var headY = rect.y + rect.height * 0.14f;
            var hipY = rect.y + rect.height * 0.58f;
            var footY = rect.y + rect.height * 0.88f;
            var shoulderY = rect.y + rect.height * 0.24f;
            var handY = rect.y + rect.height * 0.48f;
            var shoulderSpan = rect.width * 0.22f;
            var hipSpan = rect.width * 0.12f;

            HudGuiUtility.DrawScreenLine(new Vector2(centerX, headY + 16f), new Vector2(centerX, hipY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - shoulderSpan, shoulderY), new Vector2(centerX + shoulderSpan, shoulderY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - shoulderSpan, shoulderY), new Vector2(centerX - shoulderSpan - 8f, handY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX + shoulderSpan, shoulderY), new Vector2(centerX + shoulderSpan + 8f, handY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - hipSpan, hipY), new Vector2(centerX - hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX + hipSpan, hipY), new Vector2(centerX + hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);
            HudGuiUtility.DrawScreenLine(new Vector2(centerX - hipSpan, footY), new Vector2(centerX + hipSpan, footY), Color.black, 2f, Texture2D.whiteTexture);

            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(centerX - 12f, headY, 24f, 24f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawEquipmentSlots(Rect figureRect)
        {
            var slot = Mathf.Min(figureRect.width, figureRect.height) * 0.11f;
            var positions = new[]
            {
                new Vector2(0.5f, 0.08f),
                new Vector2(0.28f, 0.16f),
                new Vector2(0.72f, 0.16f),
                new Vector2(0.5f, 0.34f),
                new Vector2(0.18f, 0.44f),
                new Vector2(0.82f, 0.44f),
                new Vector2(0.5f, 0.82f)
            };

            foreach (var position in positions)
            {
                var center = new Vector2(
                    figureRect.x + figureRect.width * position.x,
                    figureRect.y + figureRect.height * position.y);
                var slotRect = new Rect(center.x - slot * 0.5f, center.y - slot * 0.5f, slot, slot);
                DrawColoredBox(slotRect, EquipmentSlotColor, 2f);
            }
        }

        private void DrawActionButtons()
        {
            const float gap = 16f;
            var totalWidth = ButtonWidth * 2f + gap;
            var startX = Screen.width - totalWidth - Margin;
            var y = Screen.height - ButtonHeight - Margin;

            var bailRect = new Rect(startX, y, ButtonWidth, ButtonHeight);
            var aircraftRect = new Rect(startX + ButtonWidth + gap, y, ButtonWidth, ButtonHeight);

            DrawActionButton(bailRect, "Bail Out?");
            DrawActionButton(aircraftRect, "AIRCRAFT LOADOUT");

            if (GUI.Button(bailRect, GUIContent.none, GUIStyle.none))
            {
                showBailOutConfirm = true;
            }

            if (!showBailOutConfirm && GUI.Button(aircraftRect, GUIContent.none, GUIStyle.none))
            {
                ProceedToAircraftLoadout();
            }
        }

        private void DrawActionButton(Rect rect, string label)
        {
            DrawWireBox(rect, 2f);
            GUI.Label(rect, label, buttonStyle);
        }

        private static void ProceedToAircraftLoadout()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.AircraftLoadout);
        }

        private static void ConfirmBailOut()
        {
            var save = CharacterSessionState.ActiveSave;
            if (save != null)
            {
                CharacterSaveRepository.ApplyScorePenalty(save, MissionBriefingState.BailOutScorePenalty);
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScenes.CharacterPage);
        }

        private static void DrawRoundedHeader(Rect rect, string text)
        {
            DrawWireBox(rect, 2f);
            var style = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
            GUI.Label(rect, text, style);
        }

        private static void DrawPageBackground()
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawWireBox(Rect rect, float thickness)
        {
            DrawColoredBox(rect, Color.black, thickness);
        }

        private static void DrawColoredBox(Rect rect, Color color, float thickness)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (headerStyle != null)
            {
                return;
            }

            headerStyle = HudStyleFactory.CreateLabel(20, FontStyle.Bold, TextAnchor.UpperLeft, Color.black);
            bodyStyle = HudStyleFactory.CreateLabel(14, FontStyle.Normal, TextAnchor.MiddleCenter, Color.black);
            buttonStyle = HudStyleFactory.CreateLabel(15, FontStyle.Bold, TextAnchor.MiddleCenter, Color.black);
        }
    }
}
