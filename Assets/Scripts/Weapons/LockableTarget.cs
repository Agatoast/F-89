using UnityEngine;

namespace F89.Weapons
{
    public class LockableTarget : MonoBehaviour
    {
        [SerializeField] private string targetLabel = "Hostile";
        [SerializeField] private LockableTargetKind targetKind = LockableTargetKind.Air;
        [SerializeField] private TargetAffiliation affiliation = TargetAffiliation.Hostile;
        [SerializeField] private TargetUnitClass unitClass = TargetUnitClass.Standard;

        public string TargetLabel => targetLabel;
        public LockableTargetKind TargetKind => targetKind;
        public TargetAffiliation Affiliation => affiliation;
        public TargetUnitClass UnitClass => unitClass;
        public bool IsFriendly => affiliation == TargetAffiliation.Friendly;
        public bool IsInfantry => unitClass == TargetUnitClass.Infantry;
        public bool RespondsWithIff => IsFriendly && !IsInfantry;
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

        public bool MatchesWeapon(LockableTargetKind weaponTargetKind)
        {
            return targetKind == weaponTargetKind;
        }

        public void RegisterHit(string weaponName, bool wasLockedShot)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Debug.Log(
                $"Target {targetLabel} hit by {weaponName} ({(wasLockedShot ? "locked" : "direct collision")}).");
            gameObject.SetActive(false);
        }
    }
}
