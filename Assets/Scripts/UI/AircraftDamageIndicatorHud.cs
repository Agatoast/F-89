using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;

namespace F89.UI
{
    /// <summary>Wireframe airframe readout — black lines, red fill grows with sortie hits taken.</summary>
    public sealed class AircraftDamageIndicatorHud : MonoBehaviour
    {
        [SerializeField] private AircraftController aircraft;

        public void Configure(AircraftController aircraftController)
        {
            aircraft = aircraftController;
        }

        private void OnGUI()
        {
            if (Event.current == null
                || GamePauseController.IsPaused
                || AntarcticaMapOverlay.IsOpen
                || aircraft == null
                || Event.current.type != EventType.Repaint)
            {
                return;
            }

            var wireTexture = AircraftDamageWireLibrary.GetWireTexture();
            if (wireTexture == null)
            {
                return;
            }

            var layout = RadarMfdBezelRenderer.ComputeDamageIndicatorLayout();
            var drawRect = FitWireRect(layout.ScopeRect, wireTexture);
            var target = aircraft.GetComponent<LockableTarget>();
            var hitsTaken = GetHitsTaken(target);
            var sortieHitBudget = target != null ? target.SortieHitBudget : 0;

            var previousDepth = GUI.depth;
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -100;

            var damageFill = AircraftDamageWireLibrary.GetDamageFillTexture(hitsTaken, sortieHitBudget);
            if (damageFill != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(drawRect, damageFill, ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.black;
            GUI.DrawTexture(drawRect, wireTexture, ScaleMode.StretchToFill, true);

            if (sortieHitBudget >= PlayerAircraftGhp.MaxSortieHitBudget)
            {
                DrawBonusHitStar(drawRect);
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
            GUI.matrix = previousMatrix;
        }

        private static int GetHitsTaken(LockableTarget target)
        {
            if (target == null || !target.IsPlayerAircraft || target.MaxGroundHitPoints <= 0)
            {
                return 0;
            }

            return target.SortieHitsTaken;
        }

        private static void DrawBonusHitStar(Rect drawRect)
        {
            var starSize = drawRect.height * 0.24f;
            var starRect = new Rect(
                drawRect.xMax - drawRect.width * 0.11f - starSize,
                drawRect.y + drawRect.height * 0.36f,
                starSize,
                starSize);
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(starSize * 0.92f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.82f, 0.08f, 1f) }
            };
            GUI.Label(starRect, "\u2605", style);
        }

        private static Rect FitWireRect(Rect bounds, Texture2D texture)
        {
            if (texture == null || texture.height <= 0)
            {
                return bounds;
            }

            var aspect = texture.width / (float)texture.height;
            var width = bounds.width;
            var height = width / aspect;
            if (height > bounds.height)
            {
                height = bounds.height;
                width = height * aspect;
            }

            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }
    }
}
