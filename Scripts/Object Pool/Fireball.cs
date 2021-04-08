using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluentBuilderPattern;

public class Fireball : MonoBehaviour, IPoolable
{
    FireballSpawner spawner;
    public GameObject muzzleParticle;
    public GameObject projectileParticle;
    public GameObject impactParticle;
    string actionName;

    [Tooltip("발사체 총 이동 시간")] [SerializeField]
    float totalTravelTime = 0.7f;

    [Tooltip("도착 지점 위치 오프셋(y 축 방향)")] [SerializeField]
    float destinationOffsetY = 0.5f;

    float muzzleEffectLifetime;
    float impactEffectLifetime;
    float reciprocalTotalTravelTime;
    float currentTravelTime;
    bool hasArrived = false;

    // Statistics targetStats;
    IDamageable targetIDamageable;
    int damageToTarget;

    ParticleSystem muzzleParticleSystem, impactParticleSystem;

    Vector3 origin, middlePos, destination, currPos, nextPos;
    SphereCollider sphereCollider;
    GameObject target;
    CharacterController targetCC;

    #region 코루틴
    IEnumerator DisableAfterWaiting(float timeInSeconds, GameObject gameObject)
    {
        yield return new WaitForSeconds(timeInSeconds);
        gameObject.SetActive(false);
    }
    #endregion

    // 참고: When you call Instantiate on a prefab, the Awake() function is run immediately, but NOT the Start() function.
    // (https://forum.unity.com/threads/instantiate-prefabs-and-start-awake-functions.197811/)
    void Awake()
    {
        spawner = FindObjectOfType<FireballSpawner>();

        if (!(muzzleParticle is null))
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
        targetCC = this.target.GetComponent<CharacterController>();

        // targetStats = target.GetComponent<IActable>().GetStats();
        targetIDamageable = target.GetComponent<IDamageable>();

        if (targetCC is null)
            Debug.LogError("대상에 CharacterController가 없습니다.");
    }

    public void Fire(int damageToTarget, in string actionName)
    {
        projectileParticle.transform.position = spawner.transform.position;
        projectileParticle.SetActive(true);

        muzzleParticle.transform.position = spawner.transform.position;
        muzzleParticleSystem.Simulate(0.0f, true, true);
        muzzleParticle.SetActive(true);
        StartCoroutine(DisableAfterWaiting(muzzleEffectLifetime, muzzleParticle));

        impactParticle.transform.position = spawner.transform.position;

        hasArrived = false;
        currentTravelTime = 0f;

        reciprocalTotalTravelTime = 1 / totalTravelTime;

        origin = sphereCollider.transform.position + sphereCollider.center;

        this.damageToTarget = damageToTarget;

        this.actionName = actionName;
    }

    void FixedUpdate()
    {
        if (impactParticle.activeSelf) return;

        if (currentTravelTime >= totalTravelTime)
        {
            impactParticle.transform.position = projectileParticle.transform.position;
            impactParticleSystem.Simulate(0.0f, true, true);
            impactParticle.SetActive(true);

            //targetStats[Stat.hP] -= damageToTarget;
            //if (targetStats[Stat.hP] < 0)
            //    targetStats[Stat.hP] = 0;

            int damage = Mathf.RoundToInt(damageToTarget * Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
            targetIDamageable.DecreaseStat(Stat.hP, damage);
            targetIDamageable.UpdateStatBars();

            // 피해량 출력 처리(switch 패턴 매칭 사용)
            switch (targetIDamageable)
            {
                case Enemy en:
                    en.EnemyIStatChangeDisplay.ShowHPChange(damage, true, actionName);
                    break;
                case Player pl:
                    pl.PlayerIStatChangeDisplay.ShowHPChange(damage, true, actionName);
                    break;
                default:
                    break;
            }

            StartCoroutine(DisableAfterWaiting(impactEffectLifetime, impactParticle));

            projectileParticle.SetActive(false);

            StartCoroutine(DisableAfterWaiting(impactEffectLifetime, gameObject));
            return;
        }
        else
        {
            currentTravelTime += Time.deltaTime;
        }

        if (!hasArrived)
        {
            // 도착 지점을 갱신한다.(피격 대상이 이동하였을 수 있으므로)
            destination = targetCC.transform.position + Vector3.Scale(targetCC.transform.localScale, targetCC.center) + Vector3.up * destinationOffsetY;

            // 이차 베지에이 곡선 계산용 변수를 갱신한다.(임의 위치 적용)
            middlePos = (origin + destination) * 0.5f + Vector3.up;

            // 이차 베지에이 곡선에서 다음 위치를 받아와서 갱신한다.
            nextPos = Utilities.GetQuadraticBezierPoint(ref origin, ref middlePos, ref destination, currentTravelTime * reciprocalTotalTravelTime);

            if (targetCC.bounds.Contains(nextPos)) // 다음 위치가 targetCC 안이면
            {
                hasArrived = true;

                currPos = sphereCollider.transform.position + sphereCollider.center;

                projectileParticle.transform.position = nextPos;
                impactParticle.transform.position = projectileParticle.transform.position;
                impactParticle.transform.rotation = Quaternion.FromToRotation(Vector3.forward, currPos - nextPos);
            }
            else
            {
                projectileParticle.transform.position = nextPos;
            }
        }
    }
}