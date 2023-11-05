using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluentBuilderPattern;
using UnityEngine.AI;

public class Enemy : Character, IDamageable, IActable, ISelectable
{
    enum EnemyState
    {
        idling,
        locomoting,
        attacking,
        casting,
        returning,
        dying,
        dead
    }

    EnemyState state;

    const float maxSquareOfChaseDistance = 2500f;
    const float maxSquareOfMeleeAttackRange = 25f;
    const float squareOfStoppingDistance = 16f;
    const float squareOfPlayerDetectionRange = 100f;

    GameManager GAME;
    Statistics stats;
    Transform currentTargetTransform, enemyTransform;
    public Statistics Stats { get => stats; set { Stats = stats; } }

    readonly Actions actionCommands = new Actions();
    public Actions ActionCommands => actionCommands;

    private float sqrDistanceFromCurrentTarget = 0f;
    public float SqrDistanceFromCurrentTarget => sqrDistanceFromCurrentTarget;

    public int ID => id;

    EnemyHPDisplay enemyHPDisplay;
    public Dictionary<int, KeyValuePair<Stat, int>> ActiveBuffEffects { get; set; } = new Dictionary<int, KeyValuePair<Stat, int>>();
    public TargetIndicator TargetIndicator { get; set; }

    public bool IsDead { get; set; } = false;

    public bool IsCasting { get; set; }
    public int ActionToTake { get; set; } = 0;
    public int ActionBeingTaken { get; set; } = 0;
    public float VisibleGlobalCoolDownTime { get; set; }
    public float InvisibleGlobalCoolDownTime { get; set; }

    static public float globalCoolDownTime = 2f;
    public float GlobalCoolDownTime => globalCoolDownTime;

    private CastingBarDisplay castingBarDisplay;
    public CastingBarDisplay CastingBarDisplay { get { return castingBarDisplay; } }

    IStatChangeDisplay enemyIStatChangeDisplay;

    IDamageable currentTargetIDamageable;

    public IStatChangeDisplay EnemyIStatChangeDisplay { get { return enemyIStatChangeDisplay; } }

    bool hasEnmityListBeenUpdated = false;

    Dictionary<int, uint> enmitiesAgainstPlayers = new Dictionary<int, uint>(); // 키: 플레이어 ID; 값: 적개감 수치

    NavMeshAgent navMeshAgent;

    Vector3 originalPosition;
    Quaternion originalRotation;

    float timeToAttack = 3f;

    float timePassedSinceReset = 0f;

    override protected void Awake()
    {
        GAME = GameManager.Instance;

        id = 100;

        SetInitialStats(); // 초기 스탯 설정

        enemyIStatChangeDisplay = FindObjectOfType<EnemyStatChangeDisplay>();

        TargetIndicator = gameObject.GetComponentInChildren<TargetIndicator>(true);
        TargetIndicator.Disable();

        castingBarDisplay = null; // 리마인더: 지정 필요

        currentTarget = recentTarget = null;
        currentTargetTransform = null;
        enemyTransform = gameObject.transform;
        originalPosition = enemyTransform.position;
        originalRotation = enemyTransform.rotation;

        anim = gameObject.GetComponent<Animator>();

        navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
        //navMeshAgent.stoppingDistance = Mathf.Sqrt(squareOfStoppingDistance) * 0.84f;

        
        //locomotionSpeed = 6f;

        //cC = gameObject.GetComponent<CharacterController>();
        //cCTransform = cC.transform;
    }

    private void SetInitialStats()
    {
        stats = new StatisticsBuilder()
            .SetHP(5000).SetMaxHP(5000)
            .SetMP(1).SetMaxMP(1)
            .SetMeleeAttackPower(250)
            .SetMeleeDefensePower(160)
            .SetMagicAttackPower(10)
            .SetMagicDefensePower(10);
    }

    public void IncreaseEnmity(int playerID, uint amount)
    {
        if (!enmitiesAgainstPlayers.ContainsKey(playerID))
        {
            enmitiesAgainstPlayers.Add(playerID, amount);
        }
        else enmitiesAgainstPlayers[playerID] += amount;

        hasEnmityListBeenUpdated = true;
    }

    override protected void Start()
    {
        enemyHPDisplay = FindObjectOfType<EnemyHPDisplay>();
        id = GAME.AddEnemyAlive(id, enemyTransform);
        GAME.OnGameTick.AddListener(UpdateStat);
        GAME.OnGameTick2.AddListener(UpdateCurrentTarget);

        state = EnemyState.idling;
    }

    override protected void Update()
    {
        switch (state)
        {
            case EnemyState.idling:
                Idle();
                break;
            case EnemyState.locomoting:
                ChasePlayer();
                break;
            case EnemyState.attacking:
                AttackPlayer();
                break;
            case EnemyState.casting:

                break;
            case EnemyState.returning:
                ReturnToOriginalPosition();
                break;
            case EnemyState.dying:
                Die();
                break;
            default:

                break;
        }
    }

    float GetSqrDistance(Vector3 position1, Vector3 position2)
    {
        return Vector3.SqrMagnitude(Vector3.Scale(position1 - position2, new Vector3(1f, 0f, 1f)));
    }

    void LookAtCurrentTarget()
    {
        Vector3 direction = currentTargetTransform.position - enemyTransform.position;
        direction.y = 0f;
        enemyTransform.forward = direction;
    }

    private void Die()
    {
        anim.SetTrigger("Dead");
        GAME.EnemiesAlive.Remove(id);
        navMeshAgent.enabled = false;
        StopAllCoroutines();
        state = EnemyState.dead;
    }

    private void ReturnToOriginalPosition()
    {
        if (navMeshAgent.remainingDistance < 0.05f)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            navMeshAgent.velocity = Vector3.zero;

            ActiveBuffEffects.Clear();
            stats[Stat.hP] = stats[Stat.maxHP];

            state = EnemyState.idling;
            anim.SetInteger("MovementMode", 0);
        }
    }

    private void AttackPlayer()
    {
        if (GAME.PlayersAlive.Count == 0)
        {
            navMeshAgent.SetDestination(originalPosition);
            state = EnemyState.returning;
            return;
        }

        if (currentTargetIDamageable is null || currentTargetIDamageable.IsDead) return;

        sqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (sqrDistanceFromCurrentTarget <= maxSquareOfMeleeAttackRange)
        {
            LookAtCurrentTarget();

            if (sqrDistanceFromCurrentTarget > squareOfStoppingDistance)
                navMeshAgent.SetDestination(currentTargetTransform.position);
            else
                navMeshAgent.SetDestination(enemyTransform.position);

            timePassedSinceReset += Time.deltaTime;
            if (timePassedSinceReset >= timeToAttack)
            {
                timePassedSinceReset = 0f;
                anim.SetInteger("MovementMode", 0);
                anim.SetTrigger("Attack");
                currentTargetIDamageable.DecreaseStat(Stat.hP, stats[Stat.meleeAttackPower], true, false);
            }
        }
        else
        {
            state = EnemyState.locomoting;
            ////  currentTime = 0f;
        }
    }

    private void ChasePlayer()
    {
        if (GAME.PlayersAlive.Count == 0)
        {
            navMeshAgent.SetDestination(originalPosition);
            state = EnemyState.returning;
            return;
        }

        if (currentTargetIDamageable is null || currentTargetIDamageable.IsDead) return;

        sqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (sqrDistanceFromCurrentTarget > maxSquareOfChaseDistance)
        {
            // 복귀 상태로 변경하기
            state = EnemyState.returning;
        }
        else if (sqrDistanceFromCurrentTarget > maxSquareOfMeleeAttackRange)
        {
            // 이동 처리
            navMeshAgent.SetDestination(currentTargetTransform.position); // NavMeshAgent 변수를 사용하여 적 캐릭터를 플레이어 캐릭터 쪽으로 이동한다.
            LookAtCurrentTarget();
        }
        else
        {
            // 공격 상태로 변경하기
            navMeshAgent.SetDestination(enemyTransform.position);
            state = EnemyState.attacking;
            timeToAttack = 2f;
        }
    }

    void Idle()
    {
        if (enmitiesAgainstPlayers.Count > 0 && currentTarget != null)
        {
            sqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

            if (sqrDistanceFromCurrentTarget <= maxSquareOfChaseDistance)
            {
                if (sqrDistanceFromCurrentTarget > maxSquareOfMeleeAttackRange)
                {
                    // 이동 상태로 변경하기
                    state = EnemyState.locomoting;
                }
                else
                {
                    // 공격 상태로 변경하기
                    state = EnemyState.attacking;
                    timeToAttack = 2f;
                }
            }

            if (!GAME.IsInBattle) GAME.IsInBattle = true;
        }
        else if (GAME.PlayersAlive.Count > 0)
        {
            int foundKey = -1;
            float sqrDistanceFromPlayer = squareOfPlayerDetectionRange;

            foreach (int key in GAME.PlayersAlive.Keys)
            {
                float thisSqrDistanceFromPlayer = GetSqrDistance(enemyTransform.position, GAME.PlayersAlive[key].position);

                if (thisSqrDistanceFromPlayer <= squareOfPlayerDetectionRange)
                {
                    if (sqrDistanceFromPlayer >= thisSqrDistanceFromPlayer)
                    {
                        foundKey = key;
                        sqrDistanceFromPlayer = thisSqrDistanceFromPlayer;
                    }
                }
            }

            if (foundKey >= 0 && !enmitiesAgainstPlayers.ContainsKey(foundKey))
            {
                enmitiesAgainstPlayers.Add(foundKey, 1);
                hasEnmityListBeenUpdated = true;
                UpdateCurrentTarget();
            }
        }
    }

    override protected void FixedUpdate()
    {
        if (navMeshAgent.velocity.sqrMagnitude > 0f)
        {
            anim.SetInteger("MovementMode", 2);
        }
        else
        {
            anim.SetInteger("MovementMode", 0);
        }
    }

    void UpdateCurrentTarget()
    {
        if (!(currentTargetIDamageable is null) && currentTargetIDamageable.IsDead)
        {
            enmitiesAgainstPlayers.Remove(currentTargetIDamageable.ID);

            recentTarget = currentTarget;
            currentTarget = null;
            currentTargetTransform = null;
            currentTargetIDamageable = null;
        }

        if (hasEnmityListBeenUpdated && enmitiesAgainstPlayers.Count > 0)
        {
            currentTargetTransform = GAME.PlayersAlive[Utilities.GetMaxValuePair(enmitiesAgainstPlayers).Key];
            recentTarget = currentTarget;
            currentTarget = currentTargetTransform.gameObject;

            if (recentTarget == currentTarget)
                return;

            currentTargetIDamageable = currentTarget.GetComponent<IDamageable>();

            hasEnmityListBeenUpdated = false;
        }
    }

    void UpdateStat()
    {
        if (IsDead) return;

        foreach (KeyValuePair<Stat, int> activeBuffEffect in ActiveBuffEffects.Values)
        {
            DecreaseStat(activeBuffEffect.Key, activeBuffEffect.Value, true, true);
        }

        UpdateStatBars();
    }

    public Statistics GetStats()
    {
        return stats;
    }

    public void UpdateStatBars()
    {
        if (IsDead) return;

        if (stats[Stat.hP] > 0f)
        {
            enemyHPDisplay.UpdateHPBar(stats[Stat.hP], stats[Stat.maxHP]);
        }
        else
        {
            enemyHPDisplay.UpdateHPBar(0, 1);
            IsDead = true;
        }
    }

    public void IncreaseStat(Stat stat, int increment, bool shouldShowHPChangeDigits, bool isChangingOverTime)
    {
        stats[stat] += increment;

        switch (stat)
        {
            case Stat.hP:
                {
                    if (stats[stat] > stats[Stat.maxHP])
                        stats[stat] = stats[Stat.maxHP];
                }
                break;
            case Stat.mP:
                {
                    if (stats[stat] > stats[Stat.maxMP])
                        stats[stat] = stats[Stat.maxMP];
                }
                break;
            default:
                break;
        }
 
    }

    public void DecreaseStat(Stat stat, int decrement, bool shouldShowHPChangeDigits, bool isChangingOverTime)
    {
        stats[stat] -= decrement;

        switch (stat)
        {
            case Stat.hP:
                {
                    if (stats[stat] < 0)
                    {
                        stats[stat] = 0;
                        if (!IsDead)
                        {
                            UpdateStatBars();
                            IsDead = true;
                            GAME.EnemiesAlive.Remove(id);
                            GAME.OnGameTick.RemoveListener(UpdateStat);
                            // StartCoroutine(EndGame());
                            enemyIStatChangeDisplay.RemoveAllDisplayingBuffs();
                            state = EnemyState.dying;
                        }
                    }

                    if (shouldShowHPChangeDigits)
                    {
                        if (isChangingOverTime)
                            enemyIStatChangeDisplay.ShowHPChangeOverTime(decrement, true);
                        else
                            enemyIStatChangeDisplay.ShowHPChange(decrement, true, null);
                    }

                    UpdateStatBars();
                }
                break;
            case Stat.mP:
                {
                    if (stats[stat] < 0)
                        stats[stat] = 0;
                }
                break;
            default:
                break;
        }

    }
}
