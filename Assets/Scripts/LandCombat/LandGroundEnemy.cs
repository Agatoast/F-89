using System.Collections.Generic;
using F89.Core;
using F89.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F89.LandCombat
{
    public sealed class LandGroundEnemy : MonoBehaviour, ILandDamageable
    {
        private enum CorpsePhase
        {
            Alive,
            Dying,
            Corpse,
            Despawning
        }

        private const float ProjectileSpeed = 19f;
        private const float FireRate = 2.4f;
        private static readonly Color ProjectileColor = new(0.95f, 0.3f, 0.22f, 1f);

        private int level = LandUrEnemyStats.MinLevel;
        private float currentHealth;
        private SpriteRenderer spriteRenderer;
        private LandEnemyVisual visual;
        private Collider2D hitCollider;
        private Rigidbody2D body;
        private Transform chaseTarget;
        private LandProjectilePool projectilePool;
        private Vector2 roamPoint;
        private float nextRoamRetargetTime;
        private float roamLateralBias;
        private float roamSpeedScale = 1f;
        private float nextFireTime;
        private CorpsePhase phase = CorpsePhase.Alive;
        private float corpseSpawnTime;
        private float despawnAtTime = -1f;
        private bool checkedForLoot;
        private bool hasLootBag;
        private readonly List<LandGearInstance> lootItems = new();

        public int Level => level;
        public int DamageResistance => LandUrEnemyStats.GetDamageResistance(level);
        public int WeaponDamage => LandUrEnemyStats.GetWeaponDamage(level);
        public int Move => LandUrEnemyStats.GetMove(level);
        public float MoveSpeedWorldUnits => LandUrEnemyStats.GetMoveSpeedWorldUnits(level);
        public float MaxHealth => LandUrEnemyStats.MaxHitPoints;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => phase == CorpsePhase.Alive && currentHealth > 0f;
        public bool IsCorpse => phase == CorpsePhase.Corpse;
        public bool HasLootBag => hasLootBag;
        public bool HasLootRemaining
        {
            get
            {
                for (var i = 0; i < lootItems.Count; i++)
                {
                    if (LandLoadoutSlots.IsValidItem(lootItems[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public IReadOnlyList<LandGearInstance> LootItems => lootItems;

        public static LandGroundEnemy SpawnLootCorpseAt(
            Vector2 position,
            int enemyLevel,
            IReadOnlyList<LandGearInstance> items,
            Transform faceTarget = null)
        {
            var enemyObject = new GameObject($"URTroopCorpse_L{enemyLevel}");
            var enemy = enemyObject.AddComponent<LandGroundEnemy>();
            enemy.InitializeCorpseWithLoot(position, enemyLevel, items, faceTarget);
            return enemy;
        }

        public void Initialize(
            Vector2 position,
            int enemyLevel,
            Transform faceTarget = null,
            float startingHealth = -1f,
            LandEnemySpriteSheet.Camouflage camouflage = LandEnemySpriteSheet.Camouflage.White)
        {
            level = LandUrEnemyStats.ClampLevel(enemyLevel);
            transform.position = position;
            currentHealth = startingHealth < 0f ? MaxHealth : Mathf.Clamp(startingHealth, 0f, MaxHealth);
            phase = CorpsePhase.Alive;
            checkedForLoot = false;
            hasLootBag = false;
            lootItems.Clear();
            despawnAtTime = -1f;
            chaseTarget = faceTarget;
            nextFireTime = Time.time + Random.Range(0.2f, 0.9f);
            PickRoamPoint(force: true);
            gameObject.name = $"URTroop_L{level}";
            EnsureVisuals();
            visual?.SetCamouflage(camouflage);
            projectilePool = Object.FindAnyObjectByType<LandProjectilePool>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            if (faceTarget != null && visual != null)
            {
                visual.SetFaceTarget(faceTarget);
            }

            visual?.SetMoving(false);
        }

        public void InitializeCorpseWithLoot(
            Vector2 position,
            int enemyLevel,
            IReadOnlyList<LandGearInstance> items,
            Transform faceTarget = null)
        {
            level = LandUrEnemyStats.ClampLevel(enemyLevel);
            transform.position = position;
            currentHealth = 0f;
            phase = CorpsePhase.Corpse;
            checkedForLoot = true;
            hasLootBag = false;
            lootItems.Clear();
            despawnAtTime = -1f;
            chaseTarget = faceTarget;
            gameObject.name = $"URTroopCorpse_L{level}";
            EnsureVisuals();

            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (!LandLoadoutSlots.IsValidItem(items[i]))
                    {
                        continue;
                    }

                    var clone = LandLoadoutEquipService.CloneItem(items[i]);
                    if (LandLoadoutSlots.IsValidItem(clone))
                    {
                        lootItems.Add(clone);
                    }
                }
            }

            if (lootItems.Count > 0)
            {
                hasLootBag = true;
                while (lootItems.Count < LandEnemyLootRules.LootBagSlotCount)
                {
                    lootItems.Add(null);
                }

                corpseSpawnTime = Time.time;
                despawnAtTime = corpseSpawnTime + LandEnemyLootRules.EmptyCorpseLifetimeSeconds;
            }
            else
            {
                corpseSpawnTime = Time.time;
                ScheduleDespawn(LandEnemyLootRules.CheckedEmptyDespawnSeconds);
            }

            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            if (body != null)
            {
                FreezeCorpsePhysics();
            }

            visual?.ShowCorpseImmediate();
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            var afterDr = Mathf.Max(0f, amount - DamageResistance);
            if (afterDr <= 0f)
            {
                // Boss progression uses levels above starter-weapon damage. A landed hit still
                // chips bosses so every encounter remains winnable and visibly responds to fire.
                if (!LandBossEncounter.IsBossObjectName(gameObject.name))
                {
                    return;
                }

                afterDr = 1f;
            }

            currentHealth = Mathf.Max(0f, currentHealth - afterDr);
            if (currentHealth > 0f)
            {
                return;
            }

            BeginDeath();
        }

        /// <summary>Roll loot once when the corpse becomes available (or on first hover).</summary>
        public void EnsureLootReady()
        {
            if (phase != CorpsePhase.Corpse || checkedForLoot)
            {
                return;
            }

            checkedForLoot = true;
            if (Random.value <= LandEnemyLootRules.LootBagChance)
            {
                hasLootBag = true;
                if (LandBossEncounter.IsBossObjectName(gameObject.name))
                {
                    LandEnemyLootGenerator.FillBossLoot(level, lootItems);
                }
                else
                {
                    LandEnemyLootGenerator.FillInfantryDeathLoot(level, lootItems);
                }

                while (lootItems.Count < LandEnemyLootRules.LootBagSlotCount)
                {
                    lootItems.Add(null);
                }

                if (!HasLootRemaining)
                {
                    hasLootBag = false;
                    lootItems.Clear();
                    ScheduleDespawn(LandEnemyLootRules.CheckedEmptyDespawnSeconds);
                }
                else
                {
                    // Loot follows the same five-minute corpse lifetime as every other body.
                    despawnAtTime = corpseSpawnTime + LandEnemyLootRules.EmptyCorpseLifetimeSeconds;
                }
            }
            else
            {
                hasLootBag = false;
                lootItems.Clear();
                ScheduleDespawn(LandEnemyLootRules.CheckedEmptyDespawnSeconds);
            }
        }

        public bool TryTakeLootItem(int index, out LandGearInstance item)
        {
            item = null;
            if (index < 0 || index >= lootItems.Count)
            {
                return false;
            }

            item = lootItems[index];
            if (!LandLoadoutSlots.IsValidItem(item))
            {
                item = null;
                return false;
            }

            lootItems[index] = null;
            return true;
        }

        public void ReturnLootItem(int index, LandGearInstance item)
        {
            if (index < 0)
            {
                return;
            }

            while (lootItems.Count <= index)
            {
                lootItems.Add(null);
            }

            lootItems[index] = item;
        }

        public void NotifyLootBagEmptied()
        {
            hasLootBag = false;
            ScheduleDespawn(LandEnemyLootRules.CheckedEmptyDespawnSeconds);
        }

        private void BeginDeath()
        {
            phase = CorpsePhase.Dying;
            LandBossEncounter.TryMarkDefeated(gameObject.name);
            GetComponent<LandOutpostGuardMarker>()?.NotifyDestroyed();

            LandGroundSceneController.RegisterKill(level);
            UrKillCredit.RegisterEnemyTroopKillByLevel(level);
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            if (visual != null)
            {
                visual.SetMoving(false);
                visual.PlayDeath(EnterCorpseState);
            }
            else
            {
                EnterCorpseState();
            }
        }

        private void EnterCorpseState()
        {
            phase = CorpsePhase.Corpse;
            corpseSpawnTime = Time.time;
            // Default empty-corpse linger; cleared if a loot bag rolls with items.
            despawnAtTime = corpseSpawnTime + LandEnemyLootRules.EmptyCorpseLifetimeSeconds;
            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            FreezeCorpsePhysics();
            EnsureLootReady();
        }

        private void FreezeCorpsePhysics()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        private void ScheduleDespawn(float delaySeconds)
        {
            var at = Time.time + delaySeconds;
            if (despawnAtTime < 0f || at < despawnAtTime)
            {
                despawnAtTime = at;
            }
        }

        private void FixedUpdate()
        {
            if (!IsAlive || body == null)
            {
                return;
            }

            if (GamePauseController.IsPaused || chaseTarget == null)
            {
                StopMoving();
                return;
            }

            var playerPos = (Vector2)chaseTarget.position;
            var toPlayer = playerPos - body.position;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                visual?.SetFaceDirection(toPlayer);
            }

            if (Time.time >= nextRoamRetargetTime
                || ((Vector2)body.position - roamPoint).sqrMagnitude < 0.35f)
            {
                PickRoamPoint(force: false);
            }

            var toRoam = roamPoint - body.position;
            if (toRoam.sqrMagnitude < 0.04f)
            {
                var jitter = Random.insideUnitCircle;
                body.linearVelocity = jitter * (MoveSpeedWorldUnits * 0.35f * roamSpeedScale);
                visual?.SetMoving(jitter.sqrMagnitude > 0.05f);
                return;
            }

            var forward = toRoam.normalized;
            var lateral = new Vector2(-forward.y, forward.x) * roamLateralBias;
            var moveDir = (forward + lateral).normalized;
            body.linearVelocity = moveDir * (MoveSpeedWorldUnits * roamSpeedScale);
            visual?.SetMoving(true);
        }

        private void PickRoamPoint(bool force)
        {
            if (chaseTarget == null)
            {
                return;
            }

            var playerPos = (Vector2)chaseTarget.position;
            // Random radii inside and outside preferred gun range (~5), so they weave in/out.
            float landRadius;
            var roll = Random.value;
            if (roll < 0.35f)
            {
                landRadius = Random.Range(2.2f, 4.6f); // press in
            }
            else if (roll < 0.7f)
            {
                landRadius = Random.Range(4.6f, 7.2f); // around mid range
            }
            else
            {
                landRadius = Random.Range(7.2f, 11.5f); // fall back out
            }

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var radius = LandUnits.ToWorld(landRadius);
            roamPoint = playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            roamLateralBias = Random.Range(-0.65f, 0.65f);
            roamSpeedScale = Random.Range(0.65f, 1.15f);
            nextRoamRetargetTime = Time.time + (force
                ? Random.Range(0.15f, 0.45f)
                : Random.Range(0.35f, 1.55f));
        }

        private void TryShootPlayer()
        {
            if (!IsAlive
                || GamePauseController.IsPaused
                || chaseTarget == null
                || projectilePool == null
                || Time.time < nextFireTime)
            {
                return;
            }

            var toPlayer = (Vector2)chaseTarget.position - (Vector2)transform.position;
            var distance = toPlayer.magnitude;
            if (distance < 0.4f)
            {
                return;
            }

            var weaponRangeLand = 6f;
            if (LandUrWeaponCatalog.TryGetByTechLevel(level, out var weapon))
            {
                weaponRangeLand = weapon.Range;
            }

            var maxRange = LandUnits.ToWorld(weaponRangeLand);
            if (distance > maxRange * 1.05f)
            {
                return;
            }

            nextFireTime = Time.time + (1f / FireRate) * Random.Range(0.75f, 1.35f);
            var aim = toPlayer / distance;
            // Light inaccuracy so fire isn't laser-perfect while strafing.
            var spread = Random.Range(-7f, 7f) * Mathf.Deg2Rad;
            var sin = Mathf.Sin(spread);
            var cos = Mathf.Cos(spread);
            aim = new Vector2(aim.x * cos - aim.y * sin, aim.x * sin + aim.y * cos);

            var inherit = body != null ? body.linearVelocity : Vector2.zero;
            var alongAim = ProjectileSpeed + Vector2.Dot(inherit, aim);
            var lifetime = maxRange / Mathf.Max(0.1f, alongAim);
            projectilePool.Spawn(
                LandSpriteAnchor.GetMuzzle(this, aim),
                aim,
                ProjectileSpeed,
                WeaponDamage,
                lifetime,
                ProjectileColor,
                0.16f,
                LandProjectileTeam.Enemy,
                inherit);
            visual?.NotifyShot();
        }

        private void StopMoving()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }

            visual?.SetMoving(false);
        }

        private void Update()
        {
            if (IsAlive)
            {
                TryShootPlayer();
                return;
            }

            if (phase != CorpsePhase.Corpse)
            {
                return;
            }

            if (despawnAtTime < 0f || Time.time < despawnAtTime)
            {
                return;
            }

            if (LandLootBagSession.OpenCorpse == this)
            {
                LandLootBagSession.Close();
            }

            phase = CorpsePhase.Despawning;
            Destroy(gameObject);
        }

        private void EnsureVisuals()
        {
            if (!TryGetComponent(out spriteRenderer))
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sortingOrder = 5;
            }

            if (!TryGetComponent(out visual))
            {
                visual = gameObject.AddComponent<LandEnemyVisual>();
            }

            transform.localScale = Vector3.one;

            if (!TryGetComponent(out hitCollider))
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.55f;
                hitCollider = circle;
            }

            EnsureBunkerMovementBlocker();

            if (!TryGetComponent(out body))
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.linearVelocity = Vector2.zero;
        }

        private void EnsureBunkerMovementBlocker()
        {
            if (!string.Equals(
                    SceneManager.GetActiveScene().name,
                    GameScenes.Bunker,
                    System.StringComparison.Ordinal))
            {
                return;
            }

            var colliders = GetComponents<CircleCollider2D>();
            for (var i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].isTrigger)
                {
                    return;
                }
            }

            var blocker = gameObject.AddComponent<CircleCollider2D>();
            blocker.isTrigger = false;
            blocker.radius = 0.5f;
        }
    }
}
