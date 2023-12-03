using System.Collections.Generic;
using UnityEngine;
using GameData;
using UnityEngine.AI;
using Characters.Handlers;

public class Enemy : Character, ISelectable
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

    private static readonly int MovementMode = Animator.StringToHash("MovementMode");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Dead = Animator.StringToHash("Dead");

    private const float MaxSquareOfChaseDistance = 2500f;
    private const float MaxSquareOfMeleeAttackRange = 25f;
    private const float SquareOfStoppingDistance = 16f;
    private const float SquareOfPlayerDetectionRange = 100f;

    private GameManager gameManagerInstance;
    private Transform currentTargetTransform, enemyTransform;

    [SerializeField] private HitAndManaPointsDisplay hitAndManaPointsDisplay;
    public TargetIndicator TargetIndicator { get; set; }

    private CastingBarDisplay castingBarDisplay;

    private StatChangeHandler currentTargetStatChangeHandler;
    public IStatChangeDisplay StatChangeDisplay { get; private set; }

    private bool hasEnmityListBeenUpdated = false;

    private readonly Dictionary<int, uint> enmitiesAgainstPlayers = new Dictionary<int, uint>(); // 키: 플레이어 ID; 값: 적개감 수치

    private NavMeshAgent navMeshAgent;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private float timeToAttack = 3f;

    private float timePassedSinceReset = 0f;

    [SerializeField] private CharacterActionHandler characterActionHandler;

    protected override void Awake()
    {
        gameManagerInstance = GameManager.Instance;

        Identifier = 100;

        StatChangeDisplay = FindObjectOfType<EnemyStatChangeDisplay>();

        SetStats();
        InitializeStatChangeHandler();

        TargetIndicator = gameObject.GetComponentInChildren<TargetIndicator>(true);
        TargetIndicator.Disable();

        castingBarDisplay = null;

        currentTargetTransform = null;
        enemyTransform = gameObject.transform;
        originalPosition = enemyTransform.position;
        originalRotation = enemyTransform.rotation;

        Animator = gameObject.GetComponent<Animator>();

        navMeshAgent = gameObject.GetComponent<NavMeshAgent>();

        InitializeCharacterActionHandler();
    }

    private void SetStats()
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
    }

    private void InitializeStatChangeHandler()
    {
        statChangeHandler.Initialize(new StatChangeHandler.InitializationContext
        {
            identifier = Identifier,
            stats = Stats,
            hitAndManaPointsDisplay = hitAndManaPointsDisplay,
            statChangeDisplay = StatChangeDisplay,
            onHitPointsBecomeZero = () => 
            {
                gameManagerInstance.EnemiesAlive.Remove(Identifier);
                gameManagerInstance.onGameTick.RemoveListener(UpdateStat);
                // StartCoroutine(EndGame());
                StatChangeDisplay.RemoveAllDisplayingBuffs();
                state = EnemyState.Dying;
            }
        });
    }

    private void InitializeCharacterActionHandler()
    {
        characterActionHandler.Initialize(
            new CharacterActionHandler.InitializationContext
            {
                actionToTake = 0,
                actionBeingTaken = 0,
                isCasting = false,
                globalCoolDownTime = 2f,
                visibleGlobalCoolDownTime = 0f,
                invisibleGlobalCoolDownTime = 0f,
                sqrDistanceFromCurrentTarget = 0f,
                castingBarDisplay = castingBarDisplay,
                characterActions = new CharacterActions(),
                stats = Stats
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

            statChangeHandler.ActiveStatChangingEffects.Clear();
            Stats[Stat.HitPoints] = Stats[Stat.MaximumHitPoints];
            statChangeHandler.UpdateHitPointsDisplay();

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

        if (currentTargetStatChangeHandler == null || currentTargetStatChangeHandler.HasZeroHitPoints) return;

        characterActionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (characterActionHandler.SqrDistanceFromCurrentTarget <= MaxSquareOfMeleeAttackRange)
        {
            LookAtCurrentTarget();

            if (characterActionHandler.SqrDistanceFromCurrentTarget > SquareOfStoppingDistance)
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
                    currentTargetStatChangeHandler.GetEffectiveDamage(Stats[Stat.MeleeAttack], true,
                    Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
                currentTargetStatChangeHandler.DecreaseStat(Stat.HitPoints, effectiveDamage);
                currentTargetStatChangeHandler.ShowHitPointsChange(effectiveDamage, true, null);
            }
        }
        else
        {
            state = EnemyState.Locomoting;
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

        if (currentTargetStatChangeHandler == null || currentTargetStatChangeHandler.HasZeroHitPoints) return;

        characterActionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

        if (characterActionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfChaseDistance)
        {
            state = EnemyState.Returning;
        }
        else if (characterActionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
        {
            navMeshAgent.SetDestination(currentTargetTransform.position);
            LookAtCurrentTarget();
        }
        else
        {
            navMeshAgent.SetDestination(enemyTransform.position);
            state = EnemyState.Attacking;
            timeToAttack = 2f;
        }
    }

    private void Idle()
    {
        if (enmitiesAgainstPlayers.Count > 0 && characterActionHandler.CurrentTarget != null)
        {
            characterActionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(enemyTransform.position, currentTargetTransform.position);

            if (characterActionHandler.SqrDistanceFromCurrentTarget <= MaxSquareOfChaseDistance)
            {
                if (characterActionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
                {
                    state = EnemyState.Locomoting;
                }
                else
                {
                    state = EnemyState.Attacking;
                    timeToAttack = 2f;
                }
            }

            if (!gameManagerInstance.IsInBattle)
                gameManagerInstance.IsInBattle = true;
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
        if (currentTargetStatChangeHandler.SelfOrNull() != null && currentTargetStatChangeHandler.HasZeroHitPoints)
        {
            enmitiesAgainstPlayers.Remove(currentTargetStatChangeHandler.Identifier);

            characterActionHandler.SetRecentTarget(characterActionHandler.CurrentTarget);
            characterActionHandler.SetCurrentTarget(null);
            currentTargetTransform = null;
            currentTargetStatChangeHandler = null;
        }

        if (!hasEnmityListBeenUpdated || enmitiesAgainstPlayers.Count <= 0)
            return;

        currentTargetTransform = gameManagerInstance.PlayersAlive[Utilities.GetMaxValuePair(enmitiesAgainstPlayers).Key];
        characterActionHandler.SetRecentTarget(characterActionHandler.CurrentTarget);
        characterActionHandler.SetCurrentTarget(currentTargetTransform.gameObject);

        if (characterActionHandler.RecentTarget == characterActionHandler.CurrentTarget)
            return;

        currentTargetStatChangeHandler = characterActionHandler.CurrentTarget.GetComponent<StatChangeHandler>();

        hasEnmityListBeenUpdated = false;
    }
}