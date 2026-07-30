using F89.Core;
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
        [SerializeField] private int maxGroundHitPoints;
        [SerializeField] private int currentGroundHitPoints;

        public string TargetLabel => targetLabel;
        public LockableTargetKind TargetKind => targetKind;
        public TargetAffiliation Affiliation => affiliation;
        public TargetUnitClass UnitClass => unitClass;
        public bool IsFriendly => affiliation == TargetAffiliation.Friendly;
        public bool IsNeutral => affiliation == TargetAffiliation.Neutral;
        public bool IsInfantry => unitClass == TargetUnitClass.Infantry;
        public bool IsFlareDecoy => unitClass == TargetUnitClass.FlareDecoy;
        public bool IsPlayerAircraft => unitClass == TargetUnitClass.PlayerAircraft;
        public bool IsGroundVehicle => unitClass == TargetUnitClass.GroundVehicle;
        public bool IsFlier => unitClass == TargetUnitClass.Flier;
        public bool IsBuilding => unitClass == TargetUnitClass.Building;
        public bool IsAirTarget =>
            (targetKind == LockableTargetKind.Air || IsFlier)
            && !IsPlayerAircraft
            && !IsFlareDecoy;
        public bool RespondsWithIff => IsFriendly && !IsInfantry && !IsFlareDecoy;
        public bool IsAlive { get; private set; } = true;
        public int MaxGroundHitPoints => maxGroundHitPoints;
        public int CurrentGroundHitPoints => currentGroundHitPoints;
        public bool HasGroundHitPoints => maxGroundHitPoints > 0;

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

            // US and UR troops: 1 GHP. Affiliation does not change this.
            if (targetUnitClass == TargetUnitClass.Infantry)
            {
                SetMaxGroundHitPoints(GroundTargetGhp.Troop);
            }
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

        public void RestoreTargeting()
        {
            IsAlive = true;
            if (maxGroundHitPoints > 0)
            {
                currentGroundHitPoints = maxGroundHitPoints;
            }
        }

        /// <summary>
        /// Sets Max GHP for vehicles/troops/buildings. Until set (&gt; 0), GHP damage is logged but not lethal.
        /// </summary>
        public void SetMaxGroundHitPoints(int maxGhp)
        {
            maxGroundHitPoints = Mathf.Max(0, maxGhp);
            currentGroundHitPoints = maxGroundHitPoints;
        }

        public void ApplyGroundDamage(int ghpDamage, string weaponName, bool wasLockedShot = false)
        {
            if (!IsAlive || IsFlareDecoy || IsPlayerAircraft || ghpDamage <= 0)
            {
                return;
            }

            if (maxGroundHitPoints <= 0)
            {
                Debug.Log(
                    $"F-89: {targetLabel} took {ghpDamage} GHP from {weaponName} "
                    + $"(MaxGHP not set yet — no destroy).");
                return;
            }

            currentGroundHitPoints = Mathf.Max(0, currentGroundHitPoints - ghpDamage);
            Debug.Log(
                $"F-89: {targetLabel} took {ghpDamage} GHP from {weaponName} "
                + $"({currentGroundHitPoints}/{maxGroundHitPoints} remaining).");

            if (currentGroundHitPoints <= 0)
            {
                DestroyFromGroundDamage(weaponName, wasLockedShot);
                return;
            }

            GetComponent<OutpostBuilding>()?.SyncFromLockableTarget(this);
        }

        /// <summary>
        /// Air GHP from air-to-air weapons (e.g. AIM-9z).
        /// </summary>
        public void ApplyAirDamage(int airGhpDamage, string weaponName, bool wasLockedShot = false)
        {
            if (!IsAlive || IsFlareDecoy || airGhpDamage <= 0)
            {
                return;
            }

            if (IsPlayerAircraft)
            {
                ApplyPlayerAircraftDamage(airGhpDamage, weaponName, wasLockedShot);
                return;
            }

            ApplyGroundDamage(airGhpDamage, weaponName, wasLockedShot);
        }

        public void RegisterHit(string weaponName, bool wasLockedShot, float destroyChance = 1f)
        {
            if (!IsAlive || IsFlareDecoy)
            {
                return;
            }

            if (IsPlayerAircraft)
            {
                if (Random.value > destroyChance)
                {
                    Debug.LogWarning(
                        $"F-89: PLAYER AIRCRAFT HIT by {weaponName} ({(wasLockedShot ? "locked" : "direct")}) but survived ({destroyChance:P0} hit chance).");
                    return;
                }

                ApplyPlayerAircraftDamage(PlayerAircraftGhp.EnemySamHit, weaponName, wasLockedShot);
                return;
            }

            DestroyFromGroundDamage(weaponName, wasLockedShot);
        }

        private void ApplyPlayerAircraftDamage(int ghpDamage, string weaponName, bool wasLockedShot)
        {
            if (maxGroundHitPoints <= 0)
            {
                maxGroundHitPoints = PlayerAircraftGhp.Max;
                currentGroundHitPoints = maxGroundHitPoints;
            }

            currentGroundHitPoints = Mathf.Max(0, currentGroundHitPoints - ghpDamage);
            var hitKind = wasLockedShot ? "locked" : "direct";
            Debug.LogWarning(
                $"F-89: PLAYER AIRCRAFT took {ghpDamage} GHP from {weaponName} ({hitKind}) "
                + $"({currentGroundHitPoints}/{maxGroundHitPoints} remaining).");

            if (currentGroundHitPoints > 0)
            {
                return;
            }

            IsAlive = false;
            var crash = GetComponent<F89.Flight.PlayerAircraftCrashController>();
            if (crash == null)
            {
                crash = gameObject.AddComponent<F89.Flight.PlayerAircraftCrashController>();
            }

            crash.BeginCrash(weaponName);
        }

        private void DestroyFromGroundDamage(string weaponName, bool wasLockedShot)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            currentGroundHitPoints = 0;
            var hitKind = wasLockedShot ? "locked" : "direct collision";
            var worldPosition = transform.position;

            // Troops (US and UR) are removed without an explosion.
            if (IsInfantry)
            {
                var parentBase = GetComponentInParent<AntarcticaBase>();
                if (parentBase != null)
                {
                    AntarcticaOutpostState.MarkTargetDestroyed(parentBase.BaseName, targetLabel);
                    OutpostFlightPlatoonState.TryFinalizePlatoonClearance(parentBase);
                }

                F89.Flight.CombatThreatRange.InvalidateCaches();
                TryRegisterUrKillCredit();
                Debug.Log($"Troop {targetLabel} destroyed by {weaponName} ({hitKind}) — no explosion.");
                gameObject.SetActive(false);
                return;
            }

            if (GetComponent<OutpostBuilding>() is OutpostBuilding building)
            {
                IsAlive = false;
                currentGroundHitPoints = 0;
                building.SyncFromLockableTarget(this);
                var buildingBase = GetComponentInParent<AntarcticaBase>();
                if (buildingBase != null)
                {
                    AntarcticaOutpostState.MarkTargetDestroyed(buildingBase.BaseName, targetLabel);
                }

                Debug.Log($"Building {targetLabel} destroyed by {weaponName} ({hitKind}).");
                return;
            }

            var baseSite = GetComponent<AntarcticaBase>();
            if (baseSite != null)
            {
                GroundExplosionEffect.PlayBuildingExplosion(worldPosition, baseSite.BaseName);
                baseSite.Destroy();
                Debug.Log($"Outpost {baseSite.BaseName} destroyed by {weaponName} ({hitKind}).");
                return;
            }

            if (IsBuilding || GetComponent<OutpostBuilding>() != null)
            {
                GroundExplosionEffect.PlayBuildingExplosion(worldPosition, targetLabel);
            }
            else if (IsGroundVehicle || IsFlier || IsAirTarget)
            {
                GroundExplosionEffect.PlayVehicleExplosion(worldPosition, targetLabel);
            }

            var owningBase = GetComponentInParent<AntarcticaBase>();
            if (owningBase != null)
            {
                AntarcticaOutpostState.MarkTargetDestroyed(owningBase.BaseName, targetLabel);
                OutpostFlightPlatoonState.TryFinalizePlatoonClearance(owningBase);
            }

            F89.Flight.CombatThreatRange.InvalidateCaches();
            TryRegisterUrKillCredit();
            Debug.Log($"Target {targetLabel} destroyed by {weaponName} ({hitKind}).");
            gameObject.SetActive(false);
        }

        private void TryRegisterUrKillCredit()
        {
            if (affiliation != TargetAffiliation.Hostile)
            {
                return;
            }

            var vehicleUnit = GetComponent<F89.Enemies.VehicleUnitComponent>();
            if (vehicleUnit?.Definition != null)
            {
                UrKillCredit.RegisterDestroy(vehicleUnit.Definition);
            }
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
