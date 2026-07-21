using F89.Core;
using F89.Flight;
using UnityEngine;

namespace F89.Weapons
{
    public interface ILaserPaintSource
    {
        LockableTarget GetPaintedTarget(
            float rangeMiles,
            LockableTargetKind targetKind,
            Vector3 rangeOrigin,
            Vector2 screenPosition);
    }

    public class WeaponTargetPaint : MonoBehaviour, ILaserPaintSource
    {
        [SerializeField] private AircraftController aircraft;
        [SerializeField] private Camera paintCamera;

        public void Configure(AircraftController aircraftController, Camera camera)
        {
            aircraft = aircraftController;
            paintCamera = camera;
        }

        public LockableTarget GetPaintedTarget(
            float rangeMiles,
            LockableTargetKind targetKind,
            Vector3 rangeOrigin,
            Vector2 screenPosition)
        {
            if (aircraft == null || paintCamera == null)
            {
                return null;
            }

            var ray = paintCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 100000f))
            {
                return null;
            }

            var target = hit.collider.GetComponentInParent<LockableTarget>();
            if (target == null || !target.IsAlive || !target.MatchesWeapon(targetKind))
            {
                return null;
            }

            var worldMap = aircraft.WorldMap;
            var profile = aircraft.Profile;
            if (worldMap == null || profile == null)
            {
                return null;
            }

            if (!WeaponLockRange.IsWithinRange(
                    rangeOrigin,
                    target.transform.position,
                    rangeMiles,
                    worldMap,
                    profile.ticSizeWorldUnits))
            {
                return null;
            }

            return target;
        }
    }
}
