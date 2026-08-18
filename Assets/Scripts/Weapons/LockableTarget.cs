using F89.Core;
using F89.Flight;
using F89.LandCombat;
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
        [SerializeField] private int sortieHitBudget;

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
        public int SortieHitBudget => sortieHitBudget;
        public int SortieHitsTaken =>
            sortieHitBudget > 0 ? Mathf.Max(0, sortieHitBudget - currentGroundHitPoints) : 0;

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
            sortieHitBudget = 0;
        }

        /// <summary>One GHP per sortie hit — HUD and crash use discrete hit count.</summary>
        public void ConfigureSortieHitBudget(int hits)
        {
            sortieHitBudget = Mathf.Clamp(hits, 1, PlayerAircraftGhp.MaxSortieHitBudget);
            maxGroundHitPoints = sortieHitBudget;
            currentGroundHitPoints = sortieHitBudget;
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
                if (!PlayerAircraftCombatState.IsAirborneForEnemyEngagement(GetComponent<AircraftController>()))
                {
                    return;
                }

                ApplyPlayerAircraftDamage(weaponName, wasLockedShot, playHitFx: true);
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
                if (!PlayerAircraftCombatState.IsAirborneForEnemyEngagement(GetComponent<AircraftController>()))
                {
                    return;
                }

                GroundExplosionEffect.PlayPlayerAircraftHit(transform.position);
                if (Random.value > destroyChance)
                {
                    Debug.LogWarning(
                        $"F-89: PLAYER AIRCRAFT HIT by {weaponName} ({(wasLockedShot ? "locked" : "direct")}) but survived ({destroyChance:P0} hit chance).");
                    return;
                }

                ApplyPlayerAircraftDamage(weaponName, wasLockedShot, playHitFx: false);
                return;
            }

            DestroyFromGroundDamage(weaponName, wasLockedShot);
        }

        private void ApplyPlayerAircraftDamage(string weaponName, bool wasLockedShot, bool playHitFx)
        {
            if (sortieHitBudget <= 0)
            {
                ConfigureSortieHitBudget(PlayerAircraftGhp.SortieHitsToCrash);
            }

            currentGroundHitPoints = Mathf.Max(
                0,
                currentGroundHitPoints - PlayerAircraftGhp.DamagePerEnemyHit);

            var hitKind = wasLockedShot ? "locked" : "direct";
            if (playHitFx)
            {
                GroundExplosionEffect.PlayPlayerAircraftHit(transform.position);
            }

            Debug.LogWarning(
                $"F-89: PLAYER AIRCRAFT took {PlayerAircraftGhp.DamagePerEnemyHit} hit from {weaponName} ({hitKind}) "
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

                var waypointTroopSite = GetComponentInParent<CampaignWaypointMissionSite>();
                if (waypointTroopSite != null
                    && CampaignWaypointSiteIds.IsWaypointSiteCode(waypointTroopSite.SiteCode))
                {
                    AntarcticaOutpostState.MarkTargetDestroyed(waypointTroopSite.SiteCode, targetLabel);
                    CampaignWaypointPlatoonState.TryFinalizeClearance(waypointTroopSite, worldPosition);
                }
                else if (waypointTroopSite != null
                         && GridSquareSiteIds.IsGridSquareSiteCode(waypointTroopSite.SiteCode))
                {
                    GridSquareSpawnState.MarkDestroyedBySiteCode(waypointTroopSite.SiteCode);
                }

                F89.Flight.CombatThreatRange.InvalidateCaches();
                if (affiliation == TargetAffiliation.Hostile)
                {
                    var vehicleUnit = GetComponent<F89.Enemies.VehicleUnitComponent>();
                    var troopLevel = vehicleUnit?.Definition != null
                        ? vehicleUnit.Definition.vehicleLevel
                        : 1;
                    FlightInfantryLootState.RecordFlightKill(transform, troopLevel);
                }

                TryRegisterKillCredit();
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
                var wasPrimaryComplete = buildingBase != null
                    && OutpostPrimaryObjective.AreAirObjectivesDestroyed(buildingBase.BaseName);
                if (buildingBase != null)
                {
                    AntarcticaOutpostState.MarkTargetDestroyed(buildingBase.BaseName, targetLabel);
                    if (!string.IsNullOrWhiteSpace(buildingBase.SiteCode))
                    {
                        AntarcticaOutpostState.MarkTargetDestroyed(buildingBase.SiteCode, targetLabel);
                    }
                }

                if (buildingBase != null
                    && !wasPrimaryComplete
                    && OutpostPrimaryObjective.AreAirObjectivesDestroyed(buildingBase.BaseName))
                {
                    F89.UI.MissionObjectiveFlashNotifier.FlashPrimaryEliminated();
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
            var waypointSite = GetComponentInParent<CampaignWaypointMissionSite>();
            if (waypointSite != null && CampaignWaypointSiteIds.IsWaypointSiteCode(waypointSite.SiteCode))
            {
                var vehicleUnit = GetComponent<F89.Enemies.VehicleUnitComponent>();
                // First hostile ground vehicle kill drops the secondary wreck pad (not fliers/troops).
                if (IsGroundVehicle
                    && vehicleUnit?.Definition != null
                    && vehicleUnit.Definition.IsHostile
                    && !vehicleUnit.Definition.isTroop
                    && !vehicleUnit.Definition.isFlier)
                {
                    CampaignWaypointPlatoonState.RecordHostileVehicleDestroy(waypointSite.SiteCode, worldPosition);
                }

                AntarcticaOutpostState.MarkTargetDestroyed(waypointSite.SiteCode, targetLabel);
                CampaignWaypointPlatoonState.TryFinalizeClearance(waypointSite, worldPosition);
                F89.Flight.CombatThreatRange.InvalidateCaches();
                TryRegisterKillCredit();
                Debug.Log($"Waypoint target {waypointSite.SiteCode}/{targetLabel} destroyed by {weaponName} ({hitKind}).");
                gameObject.SetActive(false);
                return;
            }

            if (waypointSite != null && GridSquareSiteIds.IsGridSquareSiteCode(waypointSite.SiteCode))
            {
                GridSquareSpawnState.MarkDestroyedBySiteCode(waypointSite.SiteCode);
                F89.Flight.CombatThreatRange.InvalidateCaches();
                TryRegisterKillCredit();
                Debug.Log($"Grid square target {waypointSite.SiteCode}/{targetLabel} destroyed by {weaponName} ({hitKind}).");
                gameObject.SetActive(false);
                return;
            }

            if (owningBase != null)
            {
                AntarcticaOutpostState.MarkTargetDestroyed(owningBase.BaseName, targetLabel);
                OutpostFlightPlatoonState.TryFinalizePlatoonClearance(owningBase);
            }

            F89.Flight.CombatThreatRange.InvalidateCaches();
            TryRegisterKillCredit();
            Debug.Log($"Target {targetLabel} destroyed by {weaponName} ({hitKind}).");
            gameObject.SetActive(false);
        }

        private void TryRegisterKillCredit()
        {
            var vehicleUnit = GetComponent<F89.Enemies.VehicleUnitComponent>();
            if (vehicleUnit?.Definition != null)
            {
                if (vehicleUnit.Definition.IsHostile)
                {
                    UrKillCredit.RegisterEnemyDestroy(vehicleUnit.Definition);
                }
                else
                {
                    UrKillCredit.RegisterFriendlyDestroy(vehicleUnit.Definition);
                }

                return;
            }

            if (affiliation == TargetAffiliation.Hostile && IsInfantry)
            {
                UrKillCredit.RegisterEnemyTroopKillByLevel(1);
            }
            else if (affiliation == TargetAffiliation.Friendly && IsInfantry)
            {
                UrKillCredit.RegisterFriendlyTroopKillByLevel(1);
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
