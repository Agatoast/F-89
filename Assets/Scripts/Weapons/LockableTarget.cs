using UnityEngine;

namespace F89.Weapons
{
    public class LockableTarget : MonoBehaviour
    {
        [SerializeField] private string targetLabel = "Hostile";
        [SerializeField] private LockableTargetKind targetKind = LockableTargetKind.Air;
        [SerializeField] private TargetAffiliation affiliation = TargetAffiliation.Hostile;
        [SerializeField] private TargetUnitClass unitClass = TargetUnitClass.Standard;
        [SerializeField] private float hitRadiusWorld;

        public string TargetLabel => targetLabel;
        public LockableTargetKind TargetKind => targetKind;
        public TargetAffiliation Affiliation => affiliation;
        public TargetUnitClass UnitClass => unitClass;
        public bool IsFriendly => affiliation == TargetAffiliation.Friendly;
        public bool IsInfantry => unitClass == TargetUnitClass.Infantry;
        public bool IsFlareDecoy => unitClass == TargetUnitClass.FlareDecoy;
        public bool IsPlayerAircraft => unitClass == TargetUnitClass.PlayerAircraft;
        public bool IsGroundVehicle => unitClass == TargetUnitClass.GroundVehicle;
        public bool RespondsWithIff => IsFriendly && !IsInfantry && !IsFlareDecoy;
        public bool IsAlive { get; private set; } = true;

        public void Configure(
            string label,
            LockableTargetKind kind = LockableTargetKind.Air,
            TargetAffiliation targetAffiliation = TargetAffiliation.Hostile,
            TargetUnitClass targetUnitClass = TargetUnitClass.Standard)
        {
            targetLabel = label;
            targetKind = kind;
            affiliation = targetAffiliation;
            unitClass = targetUnitClass;
        }

        public void SetHitRadiusWorld(float radius)
        {
            hitRadiusWorld = Mathf.Max(0f, radius);
        }

        public float GetHitRadiusWorld()
        {
            if (hitRadiusWorld > 0f)
            {
                return hitRadiusWorld;
            }

            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                var extents = collider.bounds.extents;
                return Mathf.Max(extents.x, extents.z);
            }

            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var extents = renderer.bounds.extents;
                return Mathf.Max(extents.x, extents.z);
            }

            return 1f;
        }

        public bool MatchesWeapon(LockableTargetKind weaponTargetKind)
        {
            return targetKind == weaponTargetKind;
        }

        public void RegisterHit(string weaponName, bool wasLockedShot, float destroyChance = 1f)
        {
            if (!IsAlive || IsFlareDecoy)
            {
                return;
            }

            if (IsPlayerAircraft)
            {
                if (Random.value <= destroyChance)
                {
                    IsAlive = false;
                    Debug.LogWarning(
                        $"F-89: PLAYER AIRCRAFT DESTROYED by {weaponName} ({(wasLockedShot ? "locked" : "direct")}).");
                }
                else
                {
                    Debug.LogWarning(
                        $"F-89: PLAYER AIRCRAFT HIT by {weaponName} ({(wasLockedShot ? "locked" : "direct")}) but survived ({destroyChance:P0} destroy chance).");
                }

                return;
            }

            IsAlive = false;
            Debug.Log(
                $"Target {targetLabel} destroyed by {weaponName} ({(wasLockedShot ? "locked" : "direct collision")}).");
            gameObject.SetActive(false);
        }

        public void ExpireWithoutHit()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
        }
    }
}
