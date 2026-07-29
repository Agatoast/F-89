using F89.Weapons;

using UnityEngine;



namespace F89.Core

{

    public sealed class OutpostBuilding : MonoBehaviour

    {

        [SerializeField] private OutpostBuildingType buildingType = OutpostBuildingType.Type1;

        [SerializeField] private int maxGroundHitPoints;

        [SerializeField] private int currentGroundHitPoints;



        public OutpostBuildingType BuildingType => buildingType;

        public int MaxGroundHitPoints => maxGroundHitPoints;

        public int CurrentGroundHitPoints => currentGroundHitPoints;

        public bool HasGroundHitPoints => maxGroundHitPoints > 0;

        public bool IsDestroyed { get; private set; }



        public void Configure(OutpostBuildingType type)

        {

            buildingType = type;

            SetMaxGroundHitPoints(OutpostBuildingGhp.ForType(type));

        }



        public void SetMaxGroundHitPoints(int maxGhp)

        {

            maxGroundHitPoints = Mathf.Max(0, maxGhp);

            currentGroundHitPoints = maxGroundHitPoints;

        }



        public float HeightTacs =>

            buildingType switch

            {

                OutpostBuildingType.Type2 => 0.5f,

                OutpostBuildingType.Type3 => 2f,

                _ => 1f

            };



        public void ApplyGroundDamage(int ghpDamage, string weaponName)

        {

            if (IsDestroyed || ghpDamage <= 0)

            {

                return;

            }



            var lockable = GetComponent<LockableTarget>();

            if (lockable != null)

            {

                lockable.ApplyGroundDamage(ghpDamage, weaponName);

                SyncFromLockableTarget(lockable);

                return;

            }



            ApplyGroundDamageLocal(ghpDamage, weaponName);

        }



        public void MarkDestroyed()

        {

            if (IsDestroyed)

            {

                return;

            }



            IsDestroyed = true;

            currentGroundHitPoints = 0;



            var lockable = GetComponent<LockableTarget>();

            if (lockable != null && lockable.IsAlive)

            {

                lockable.ApplyGroundDamage(Mathf.Max(1, maxGroundHitPoints), "Building Destroyed");

                return;

            }



            GroundExplosionEffect.PlayBuildingExplosion(transform.position, buildingType.ToString());

            gameObject.SetActive(false);

        }



        public void SyncFromLockableTarget(LockableTarget lockable)

        {

            if (lockable == null)

            {

                return;

            }



            if (!lockable.IsAlive)

            {

                if (!IsDestroyed)

                {

                    IsDestroyed = true;

                    currentGroundHitPoints = 0;

                    GroundExplosionEffect.PlayBuildingExplosion(transform.position, buildingType.ToString());

                    gameObject.SetActive(false);

                }



                return;

            }



            if (lockable.HasGroundHitPoints)

            {

                currentGroundHitPoints = lockable.CurrentGroundHitPoints;

                maxGroundHitPoints = lockable.MaxGroundHitPoints;

            }

        }



        private void ApplyGroundDamageLocal(int ghpDamage, string weaponName)

        {

            if (maxGroundHitPoints <= 0)

            {

                Debug.Log(

                    $"F-89: building {buildingType} took {ghpDamage} GHP from {weaponName} "

                    + "(MaxGHP not set yet — no destroy).");

                return;

            }



            currentGroundHitPoints = Mathf.Max(0, currentGroundHitPoints - ghpDamage);

            Debug.Log(

                $"F-89: building {buildingType} took {ghpDamage} GHP from {weaponName} "

                + $"({currentGroundHitPoints}/{maxGroundHitPoints} remaining).");



            if (currentGroundHitPoints <= 0)

            {

                MarkDestroyed();

            }

        }

    }

}

