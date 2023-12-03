using System.Collections;
using UnityEngine;
using Characters.Handlers;
using Characters.StatisticsScripts;

namespace ObjectPool
{
    public class Fireball : MonoBehaviour, IPoolable
    {
        private FireballSpawner spawner;
        public GameObject muzzleParticle;
        public GameObject projectileParticle;
        public GameObject impactParticle;
        private string actionName;

        [SerializeField] private float totalTravelTime = 0.7f;

        [SerializeField] private float destinationOffsetY = 0.5f;

        private float muzzleEffectLifetime;
        private float impactEffectLifetime;
        private float reciprocalTotalTravelTime;
        private float currentTravelTime;
        private bool hasArrived = false;

        private StatChangeHandler targetStatChangeHandler;
        private int damageToTarget;

        private ParticleSystem muzzleParticleSystem, impactParticleSystem;

        private Vector3 origin, middlePosition, destination, currentPosition, nextPosition;
        private SphereCollider sphereCollider;
        private CharacterController targetController;

        private IEnumerator DisableAfterWaiting(float timeInSeconds, GameObject gameObject)
        {
            yield return new WaitForSeconds(timeInSeconds);
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            spawner = FindObjectOfType<FireballSpawner>();

            if (!(muzzleParticle == null))
            {
                muzzleParticle.SetActive(false);
            }

            projectileParticle.SetActive(false);

            impactParticle.SetActive(false);

            sphereCollider = projectileParticle.GetComponent<SphereCollider>();

            muzzleParticleSystem = muzzleParticle.GetComponent<ParticleSystem>();
            muzzleEffectLifetime = muzzleParticleSystem.main.duration + 0.05f;

            impactParticleSystem = impactParticle.GetComponent<ParticleSystem>();
            impactEffectLifetime = impactParticleSystem.main.duration + 0.05f;
        }

        public void OnObjectSpawn()
        {

        }

        public void SetTarget(GameObject target)
        {
            targetController = target.GetComponent<CharacterController>();
            targetStatChangeHandler = target.GetComponent<StatChangeHandler>();
        }

        public void Fire(int damageToTarget, in string actionName)
        {
            var spawnerPosition = spawner.transform.position;

            projectileParticle.transform.position = spawnerPosition;
            projectileParticle.SetActive(true);

            muzzleParticle.transform.position = spawnerPosition;
            muzzleParticleSystem.Simulate(0.0f, true, true);
            muzzleParticle.SetActive(true);
            StartCoroutine(DisableAfterWaiting(muzzleEffectLifetime, muzzleParticle));

            impactParticle.transform.position = spawnerPosition;

            hasArrived = false;
            currentTravelTime = 0f;

            reciprocalTotalTravelTime = 1 / totalTravelTime;

            origin = sphereCollider.transform.position + sphereCollider.center;

            this.damageToTarget = damageToTarget;

            this.actionName = actionName;
        }

        private void FixedUpdate()
        {
            if (impactParticle.activeSelf) return;

            if (currentTravelTime >= totalTravelTime)
            {
                OnTravelDone();
                return;
            }

            currentTravelTime += Time.deltaTime;

            if (hasArrived)
                return;

            UpdatePositionAndRotation();
        }

        private void OnTravelDone()
        {
            impactParticle.transform.position = projectileParticle.transform.position;
            impactParticleSystem.Simulate(0.0f, true, true);
            impactParticle.SetActive(true);

            if (targetStatChangeHandler)
            {
                var effectiveDamage =
                    targetStatChangeHandler.GetEffectiveDamage(damageToTarget, false,
                    Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
                targetStatChangeHandler.DecreaseStat(Stat.HitPoints, effectiveDamage);
                targetStatChangeHandler.ShowHitPointsChange(effectiveDamage, true, actionName);
            }

            StartCoroutine(DisableAfterWaiting(impactEffectLifetime, impactParticle));

            projectileParticle.SetActive(false);

            StartCoroutine(DisableAfterWaiting(impactEffectLifetime, gameObject));
        }

        private void UpdatePositionAndRotation()
        {
            var targetTransform = targetController.transform;
            destination = targetTransform.position
                + Vector3.Scale(targetTransform.localScale, targetController.center)
                + Vector3.up * destinationOffsetY;

            middlePosition = (origin + destination) * 0.5f + Vector3.up;

            nextPosition = Utilities.GetQuadraticBezierPoint(ref origin,
                ref middlePosition, ref destination,
                currentTravelTime * reciprocalTotalTravelTime);

            if (targetController.bounds.Contains(nextPosition))
            {
                hasArrived = true;

                currentPosition = sphereCollider.transform.position + sphereCollider.center;

                projectileParticle.transform.position = nextPosition;
                impactParticle.transform.position = nextPosition;
                impactParticle.transform.rotation =
                    Quaternion.FromToRotation(Vector3.forward, currentPosition - nextPosition);
            }
            else
            {
                projectileParticle.transform.position = nextPosition;
            }
        }
    }
}