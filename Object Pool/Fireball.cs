using System.Collections;
using UnityEngine;
using GameData;
using Characters.Components;

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

    private StatChangeable targetStatChangeable;
    private int damageToTarget;

    private ParticleSystem muzzleParticleSystem, impactParticleSystem;

    private Vector3 origin, middlePosition, destination, currentPosition, nextPosition;
    private SphereCollider sphereCollider;
    private GameObject target;
    private CharacterController targetController;

    #region 코루틴
    private IEnumerator DisableAfterWaiting(float timeInSeconds, GameObject gameObject)
    {
        yield return new WaitForSeconds(timeInSeconds);
        gameObject.SetActive(false);
    }
    #endregion

    // 참고: When you call Instantiate on a prefab, the Awake() function is run immediately, but NOT the Start() function.
    // (https://forum.unity.com/threads/instantiate-prefabs-and-start-awake-functions.197811/)
    private void Awake()
    {
        spawner = FindObjectOfType<FireballSpawner>();

        if (!(muzzleParticle == null))
        {
            // muzzleParticle = Instantiate(muzzleParticle, spawner.transform.position, Quaternion.identity, gameObject.transform);
            muzzleParticle.SetActive(false);
        }

        // projectileParticle = Instantiate(projectileParticle, spawner.transform.position, Quaternion.identity, gameObject.transform);
        projectileParticle.SetActive(false);

        // impactParticle = Instantiate(impactParticle, spawner.transform.position, Quaternion.identity, gameObject.transform);
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
        this.target = target; // GameObject.Find("Enemy");
        targetController = target.GetComponent<CharacterController>();

        // targetStats = target.GetComponent<IActable>().GetStats();
        targetStatChangeable = target.GetComponent<StatChangeable>();

        if (targetController == null)
            Debug.LogError("Target CharacterController not found.");
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

        if (targetStatChangeable)
        {
            var effectiveDamage =
                targetStatChangeable.GetEffectiveDamage(damageToTarget, false,
                Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
            targetStatChangeable.DecreaseStat(Stat.HitPoints, effectiveDamage);
            targetStatChangeable.ShowHitPointsChange(effectiveDamage, true, actionName);
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