using System.Collections.Generic;
using UnityEngine;
using GameData;
using UnityEngine.AI;
using Characters.Components;
using UnityEditor.Experimental.GraphView;

public class Enemy : Character, IActable, ISelectable
{
    private enum EnemyState
    {
        Idling,
        Locomoting,
        Attacking,
        Casting,
        Returning,
        Dying,
        Dead
    }

    private EnemyState state;

    private const float MaxSquareOfChaseDistance = 2500f;
    private const float MaxSquareOfMeleeAttackRange = 25f;
    private const float SquareOfStoppingDistance = 16f;
    private const float SquareOfPlayerDetectionRange = 100f;

    private GameManager gameManagerInstance;
    private Transform currentTargetTransform, enemyTransform;
    public Statistics Stats { get; private set; }

    public Actions ActionCommands { get; } = new Actions();

    public float SqrDistanceFromCurrentTarget { get; private set; } = 0f;

    private EnemyHPDisplay enemyHPDisplay;
    public Dictionary<int, KeyValuePair<Stat, int>> ActiveBuffEffects { get; set; } = new Dictionary<int, KeyValuePair<Stat, int>>();
    public TargetIndicator TargetIndicator { get; set; }

    public bool IsDead { get; set; } = false;

    public bool IsCasting { get; set; }
    public int ActionToTake { get; set; } = 0;
    public int ActionBeingTaken { get; set; } = 0;
    public float VisibleGlobalCoolDownTime { get; set; }
    public float InvisibleGlobalCoolDownTime { get; set; }

    public float GlobalCoolDownTime => 2f;

    private CastingBarDisplay castingBarDisplay;
    public CastingBarDisplay CastingBarDisplay => castingBarDisplay;

    private IStatChangeDisplay enemyIStatChangeDisplay;

    private StatChangeable currentTargetStatChangeable;

    public IStatChangeDisplay EnemyIStatChangeDisplay => enemyIStatChangeDisplay;

    private bool hasEnmityListBeenUpdated = false;

    private readonly Dictionary<int, uint> enmitiesAgainstPlayers = new Dictionary<int, uint>(); // 키: 플레이어 ID; 값: 적개감 수치

    private NavMeshAgent navMeshAgent;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private float timeToAttack = 3f;

    private float timePassedSinceReset = 0f;

    private static readonly int MovementMode = Animator.StringToHash("MovementMode");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Dead = Animator.StringToHash("Dead");

    [SerializeField] private StatChangeable statChangeable;

    protected override void Awake()
    {
        gameManagerInstance = GameManager.Instance;

        Identifier = 100;

        SetInitialStats(); // 초기 스탯 설정

        enemyIStatChangeDisplay = FindObjectOfType<EnemyStatChangeDisplay>();

        TargetIndicator = gameObject.GetComponentInChildren<TargetIndicator>(true);
        TargetIndicator.Disable();

        castingBarDisplay = null; // todo: 지정 필요

        CurrentTarget = RecentTarget = null;
        currentTargetTransform = null;
        enemyTransform = gameObject.transform;
        originalPosition = enemyTransform.position;
        originalRotation = enemyTransform.rotation;

        Animator = gameObject.GetComponent<Animator>();

        navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
    }

    private void SetInitialStats()
    {
        Stats = new StatisticsBuilder()
            .SetBaseValue(Stat.HitPoints, 2500)
            .SetBaseValue(Stat.MaximumHitPoints, 2500)
            .SetBaseValue(Stat.ManaPoints, 1)
            .SetBaseValue(Stat.MaximumManaPoints, 1)
            .SetBaseValue(Stat.MeleeAttack, 270)
            .SetBaseValue(Stat.MeleeDefense, 160)
            .SetBaseValue(Stat.MagicAttack, 10)
            .SetBaseValue(Stat.MagicDefense, 10);

        statChangeable.Initialize(new StatChangeable.InitializationContext
        {
            Identifier = Identifier,
            stats = Stats,
            hitAndManaPointsDisplay = null,
            statChangeDisplay = enemyIStatChangeDisplay,
            onHitPointsBecomeZero = null
        });
    }

    public void IncreaseEnmity(int playerID, uint amount)
    {
        if (!enmitiesAgainstPlayers.ContainsKey(playerID))
        {
            enmitiesAgainstPlayers.Add(playerID, amount);
        }
        else
        {
            var currentEnmity = enmitiesAgainstPlayers[playerID];
            var remainingValue = uint.MaxValue - currentEnmity;
            enmitiesAgainstPlayers[playerID] =
                currentEnmity + (remainingValue > amount ? amount : remainingValue);
        }

        hasEnmityListBeenUpdated = true;
    }

    protected override void Start()
    {
        enemyHPDisplay = FindObjectOfType<EnemyHPDisplay>();
        Identifier = gameManagerInstance.AddEnemyAlive(Identifier, enemyTransform);
        gameManagerInstance.onGameTick.AddListener(UpdateStat);
        gameManagerInstance.onGameTick2.AddListener(UpdateCurrentTarget);

        state = EnemyState.Idling;
    }

    protected override void Update()
    {
        switch (state)
        {
            case EnemyState.Idling:
                Idle();
                break;
            case EnemyState.Locomoting:
                ChasePlayer();
                break;
            case EnemyState.Attacking:
                AttackPlayer();
                break;
            case EnemyState.Casting:

                break;
            case EnemyState.Returning:
                ReturnToOriginalPosition();
                break;
            case EnemyState.Dying:
                Die();
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
        Animator.SetTrigger(Dead);
        gameManagerInstance.EnemiesAlive.Remove(Identifier);
        navMeshAgent.enabled = false;
        StopAllCoroutines();
        state = EnemyState.Dead;
    }

    private void ReturnToOriginalPosition()
    {
        if (navMeshAgent.remainingDistance < 0.05f)
        {
            var thisTransform = transform;
            thisTransform.position = originalPosition;
            thisTransform.rotation = originalRotation;
            navMeshAgent.velocity = Vector3.zero;

            ActiveBuffEffects.Clear();
            Stats[Stat.HitPoints] = Stats[Stat.MaximumHitPoints];

            state = EnemyState.Idling;
            Animator.SetInteger(MovementMode, 0);
        }
    }

    private void AttackPlayer()
    {
        if (gameManagerInstance.PlayersAlive.Count == 0)
        {
            navMeshAgent.SetDestination(originalPosition);
            state = EnemyState.Returning;
            return;
        }

        if (currentTargetStatChangeable == null || currentTargetStatChangeable.HasZeroHitPoints) return;

        SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (SqrDistanceFromCurrentTarget <= MaxSquareOfMeleeAttackRange)
        {
            LookAtCurrentTarget();

            if (SqrDistanceFromCurrentTarget > SquareOfStoppingDistance)
                navMeshAgent.SetDestination(currentTargetTransform.position);
            else
                navMeshAgent.SetDestination(enemyTransform.position);

            timePassedSinceReset += Time.deltaTime;
            if (timePassedSinceReset >= timeToAttack)
            {
                timePassedSinceReset = 0f;
                Animator.SetInteger(MovementMode, 0);
                Animator.SetTrigger(Attack);

                var effectiveDamage =
                    currentTargetStatChangeable.GetEffectiveDamage(Stats[Stat.MeleeAttack], true,
                    Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
                currentTargetStatChangeable.DecreaseStat(Stat.HitPoints, effectiveDamage);
                currentTargetStatChangeable.ShowHitPointsChange(effectiveDamage, true, null);
            }
        }
        else
        {
            state = EnemyState.Locomoting;
            ////  currentTime = 0f;
        }
    }

    private void ChasePlayer()
    {
        if (gameManagerInstance.PlayersAlive.Count == 0)
        {
            navMeshAgent.SetDestination(originalPosition);
            state = EnemyState.Returning;
            return;
        }

        if (currentTargetStatChangeable == null || currentTargetStatChangeable.HasZeroHitPoints) return;

        SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (SqrDistanceFromCurrentTarget > MaxSquareOfChaseDistance)
        {
            // 복귀 상태로 변경하기
            state = EnemyState.Returning;
        }
        else if (SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
        {
            // 이동 처리
            navMeshAgent.SetDestination(currentTargetTransform.position); // NavMeshAgent 변수를 사용하여 적 캐릭터를 플레이어 캐릭터 쪽으로 이동한다.
            LookAtCurrentTarget();
        }
        else
        {
            // 공격 상태로 변경하기
            navMeshAgent.SetDestination(enemyTransform.position);
            state = EnemyState.Attacking;
            timeToAttack = 2f;
        }
    }

    private void Idle()
    {
        if (enmitiesAgainstPlayers.Count > 0 && CurrentTarget != null)
        {
            SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

            if (SqrDistanceFromCurrentTarget <= MaxSquareOfChaseDistance)
            {
                if (SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
                {
                    // 이동 상태로 변경하기
                    state = EnemyState.Locomoting;
                }
                else
                {
                    // 공격 상태로 변경하기
                    state = EnemyState.Attacking;
                    timeToAttack = 2f;
                }
            }

            if (!gameManagerInstance.IsInBattle) gameManagerInstance.IsInBattle = true;
        }
        else if (gameManagerInstance.PlayersAlive.Count > 0)
        {
            var foundKey = -1;
            var sqrDistanceFromPlayer = SquareOfPlayerDetectionRange;

            foreach (var key in gameManagerInstance.PlayersAlive.Keys)
            {
                var thisSqrDistanceFromPlayer = GetSqrDistance(enemyTransform.position, gameManagerInstance.PlayersAlive[key].position);
                if (thisSqrDistanceFromPlayer > SquareOfPlayerDetectionRange)
                    continue;
                if (sqrDistanceFromPlayer < thisSqrDistanceFromPlayer)
                    continue;
                foundKey = key;
                sqrDistanceFromPlayer = thisSqrDistanceFromPlayer;
            }

            if (foundKey < 0 || enmitiesAgainstPlayers.ContainsKey(foundKey))
                return;

            enmitiesAgainstPlayers.Add(foundKey, 1);
            hasEnmityListBeenUpdated = true;
            UpdateCurrentTarget();
        }
    }

    protected override void FixedUpdate()
    {
        if (navMeshAgent.velocity.sqrMagnitude > 0f)
        {
            Animator.SetInteger(MovementMode, 2);
        }
        else
        {
            Animator.SetInteger(MovementMode, 0);
        }
    }

    private void UpdateCurrentTarget()
    {
        if (currentTargetStatChangeable.SelfOrNull() != null && currentTargetStatChangeable.HasZeroHitPoints)
        {
            enmitiesAgainstPlayers.Remove(currentTargetStatChangeable.Identifier);

            RecentTarget = CurrentTarget;
            CurrentTarget = null;
            currentTargetTransform = null;
            currentTargetStatChangeable = null;
        }

        if (!hasEnmityListBeenUpdated || enmitiesAgainstPlayers.Count <= 0)
            return;

        currentTargetTransform = gameManagerInstance.PlayersAlive[Utilities.GetMaxValuePair(enmitiesAgainstPlayers).Key];
        RecentTarget = CurrentTarget;
        CurrentTarget = currentTargetTransform.gameObject;

        if (RecentTarget == CurrentTarget)
            return;

        currentTargetStatChangeable = CurrentTarget.GetComponent<StatChangeable>();

        hasEnmityListBeenUpdated = false;
    }

    private void UpdateStat()
    {
        if (IsDead) return;

        foreach (KeyValuePair<Stat, int> activeBuffEffect in ActiveBuffEffects.Values)
        {
            DecreaseStat(activeBuffEffect.Key, activeBuffEffect.Value, true, true);
        }

        UpdateStatBars();
    }

    public GameData.Statistics GetStats()
    {
        return Stats;
    }

    public void UpdateStatBars()
    {
        if (IsDead) return;

        if (Stats[Stat.HitPoints] > 0f)
        {
            enemyHPDisplay.UpdateHPBar(Stats[Stat.HitPoints], Stats[Stat.MaximumHitPoints]);
        }
        else
        {
            enemyHPDisplay.UpdateHPBar(0, 1);
            IsDead = true;
        }
    }

    public void IncreaseStat(Stat stat, int increment, bool shouldShowHPChangeDigits, bool isChangingOverTime)
    {
        Stats[stat] += increment;

        switch (stat)
        {
            case Stat.HitPoints:
                {
                    if (Stats[stat] > Stats[Stat.MaximumHitPoints])
                        Stats[stat] = Stats[Stat.MaximumHitPoints];
                }
                break;
            case Stat.ManaPoints:
                {
                    if (Stats[stat] > Stats[Stat.MaximumManaPoints])
                        Stats[stat] = Stats[Stat.MaximumManaPoints];
                }
                break;
        }
    }

    public void DecreaseStat(Stat stat, int decrement, bool shouldShowHPChangeDigits, bool isChangingOverTime)
    {
        Stats[stat] -= decrement;

        switch (stat)
        {
            case Stat.HitPoints:
                {
                    if (Stats[stat] < 0)
                    {
                        Stats[stat] = 0;
                        if (!IsDead)
                        {
                            UpdateStatBars();
                            IsDead = true;
                            gameManagerInstance.EnemiesAlive.Remove(Identifier);
                            gameManagerInstance.onGameTick.RemoveListener(UpdateStat);
                            // StartCoroutine(EndGame());
                            enemyIStatChangeDisplay.RemoveAllDisplayingBuffs();
                            state = EnemyState.Dying;
                        }
                    }

                    if (shouldShowHPChangeDigits)
                    {
                        if (isChangingOverTime)
                            enemyIStatChangeDisplay.ShowHitPointsChangeOverTime(decrement, true);
                        else
                            enemyIStatChangeDisplay.ShowHitPointsChange(decrement, true, null);
                    }

                    UpdateStatBars();
                }
                break;
            case Stat.ManaPoints:
                {
                    if (Stats[stat] < 0)
                        Stats[stat] = 0;
                }
                break;
        }
    }
}