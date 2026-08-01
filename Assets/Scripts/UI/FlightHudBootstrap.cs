using F89.Controls;
using F89.Core;
using F89.Flight;
using F89.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.UI
{
    /// <summary>
    /// Ensures every flight HUD widget exists, is enabled, and is configured.
    /// </summary>
    public static class FlightHudBootstrap
    {
        private const string WidgetRootName = "FlightHudWidgets";

        public static void EnsureForPlayer(GameObject player)
        {
            if (player == null || !GameScenes.IsGameplayScene(SceneManager.GetActiveScene().name))
            {
                return;
            }

            var widgetRoot = EnsureWidgetRoot(player);
            var controller = player.GetComponent<AircraftController>();
            var weapons = player.GetComponent<PlayerWeaponController>();
            var input = player.GetComponent<PlayerAircraftInput>();
            var lockController = player.GetComponent<MissileLockController>();
            var flares = player.GetComponent<FlareCountermeasureController>();
            var camera = Camera.main;

            if (controller == null || weapons == null)
            {
                return;
            }

            EnsureFlightHud(widgetRoot, controller, weapons);
            EnsureAirspeedTape(widgetRoot, controller);
            EnsureStoresPanel(widgetRoot, weapons, flares);
            EnsureFuelGauge(widgetRoot, controller);
            EnsureWeaponReticle(widgetRoot, weapons, input);
            EnsureTargetDiamonds(widgetRoot, weapons, controller, camera);
            EnsureLongRangeRadar(widgetRoot, controller, lockController, weapons);
            EnsureShortRangeRadar(widgetRoot, controller, lockController, weapons);
        }

        private static Transform EnsureWidgetRoot(GameObject player)
        {
            var existing = player.transform.Find(WidgetRootName);
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            var root = new GameObject(WidgetRootName);
            root.transform.SetParent(player.transform, false);
            return root.transform;
        }

        private static T EnsureHudWidget<T>(Transform widgetRoot, string objectName) where T : Component
        {
            var widget = widgetRoot.GetComponentInChildren<T>(true);
            if (widget == null)
            {
                var widgets = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (var i = 0; i < widgets.Length; i++)
                {
                    var candidate = widgets[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    widget = candidate;
                    break;
                }
            }

            if (widget == null)
            {
                var host = new GameObject(objectName);
                host.transform.SetParent(widgetRoot, false);
                widget = host.AddComponent<T>();
            }
            else if (widget.transform.parent != widgetRoot)
            {
                widget.transform.SetParent(widgetRoot, false);
            }

            EnableWidget(widget);
            return widget;
        }

        private static void EnableWidget(Component widget)
        {
            if (widget == null)
            {
                return;
            }

            widget.gameObject.SetActive(true);
            if (widget is Behaviour behaviour)
            {
                behaviour.enabled = true;
            }
        }

        private static void EnsureFlightHud(Transform widgetRoot, AircraftController controller, PlayerWeaponController weapons)
        {
            var hud = EnsureHudWidget<FlightHud>(widgetRoot, "FlightHud");
            hud.Configure(controller, weapons);
        }

        private static void EnsureAirspeedTape(Transform widgetRoot, AircraftController controller)
        {
            var tape = EnsureHudWidget<AirspeedTapeHud>(widgetRoot, "AirspeedTapeHud");
            tape.Configure(controller);
        }

        private static void EnsureStoresPanel(
            Transform widgetRoot,
            PlayerWeaponController weapons,
            FlareCountermeasureController flares)
        {
            var stores = EnsureHudWidget<StoresPanelHud>(widgetRoot, "StoresPanelHud");
            stores.Configure(weapons, flares);
        }

        private static void EnsureFuelGauge(Transform widgetRoot, AircraftController controller)
        {
            var gauge = EnsureHudWidget<FuelGaugeHud>(widgetRoot, "FuelGaugeHud");
            gauge.Configure(controller);
        }

        private static void EnsureWeaponReticle(
            Transform widgetRoot,
            PlayerWeaponController weapons,
            PlayerAircraftInput input)
        {
            var reticle = EnsureHudWidget<WeaponReticleHud>(widgetRoot, "WeaponReticleHud");
            reticle.Configure(weapons, input);
        }

        private static void EnsureTargetDiamonds(
            Transform widgetRoot,
            PlayerWeaponController weapons,
            AircraftController controller,
            Camera camera)
        {
            var diamonds = EnsureHudWidget<HudTargetDiamondOverlay>(widgetRoot, "HudTargetDiamondOverlay");
            diamonds.Configure(weapons, controller, camera);
        }

        private static void EnsureLongRangeRadar(
            Transform widgetRoot,
            AircraftController controller,
            MissileLockController lockController,
            PlayerWeaponController weapons)
        {
            var radar = FindLongRangeRadarOverlay();
            if (radar == null)
            {
                var host = new GameObject("PlaneRadarOverlay");
                host.transform.SetParent(widgetRoot, false);
                radar = host.AddComponent<PlaneRadarOverlay>();
            }
            else if (radar.transform.parent != widgetRoot)
            {
                radar.transform.SetParent(widgetRoot, false);
            }

            EnableWidget(radar);
            radar.Configure(controller, lockController, weapons, PlaneRadarOverlay.RadarScopeKind.LongRange);
            weapons.SetRadarOverlay(radar);
        }

        private static void EnsureShortRangeRadar(
            Transform widgetRoot,
            AircraftController controller,
            MissileLockController lockController,
            PlayerWeaponController weapons)
        {
            var shortRadar = widgetRoot.GetComponentInChildren<ShortRangeRadarOverlay>(true);
            if (shortRadar == null)
            {
                var widgets = Object.FindObjectsByType<ShortRangeRadarOverlay>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                shortRadar = widgets.Length > 0 ? widgets[0] : null;
            }

            if (shortRadar == null)
            {
                var host = new GameObject("ShortRangeRadarOverlay");
                host.transform.SetParent(widgetRoot, false);
                shortRadar = host.AddComponent<ShortRangeRadarOverlay>();
            }
            else if (shortRadar.transform.parent != widgetRoot)
            {
                shortRadar.transform.SetParent(widgetRoot, false);
            }

            EnableWidget(shortRadar);
            shortRadar.ConfigureShortRange(controller, lockController, weapons);
        }

        private static PlaneRadarOverlay FindLongRangeRadarOverlay()
        {
            var overlays = Object.FindObjectsByType<PlaneRadarOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < overlays.Length; i++)
            {
                var overlay = overlays[i];
                if (overlay != null && overlay.ScopeKind == PlaneRadarOverlay.RadarScopeKind.LongRange)
                {
                    return overlay;
                }
            }

            return null;
        }
    }
}
