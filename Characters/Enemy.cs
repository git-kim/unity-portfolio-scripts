using Characters.Handlers;
using Characters.StatisticsScripts;
using Managers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UserInterface;

namespace Characters
{
    public class Enemy : Character
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

        private const float normalAttackPeriod = 3f;
        private float lastAttackTime;

        private CastingBarDisplay castingBarDisplay; // not added yet.
        [SerializeField] private EnemyHitPointsDisplay hitPointsDisplay;
        [SerializeField] private EnemyStatChangeDisplay enemyStatChangeDisplay;
        public IStatChangeDisplay StatChangeDisplay => enemyStatChangeDisplay;

        [SerializeField] private EnemyActionHandler actionHandler;
        [SerializeField] private NavMeshAgent navMeshAgent;

        private Vector3 originalPosition;
        private Quaternion originalRotation;

        private Transform currentTargetTransform;
        private StatChangeHandler currentTargetStatChangeHandler;

        private readonly Dictionary<int, uint> enmitiesAgainstPlayers =
            new Dictionary<int, uint>(); // Key: Player Identifier
        private bool hasEnmityListBeenUpdated = false;

        protected override void Awake()
        {
            base.Awake();

            Identifier = 100;

            SetStats();
            InitializeStatChangeHandler();

            castingBarDisplay = null;

            currentTargetTransform = null;

            originalPosition = Transform.position;
            originalRotation = Transform.rotation;

            InitializeCharacterActionHandler();

            hitPointsDisplay.Initialize(Transform, CharacterController);
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
                hitAndManaPointsDisplay = hitPointsDisplay,
                statChangeDisplay = StatChangeDisplay,
                onHitPointsBecomeZero = () =>
                {
                    GameManager.Instance.EnemiesAlive.Remove(Identifier);
                    GameManager.Instance.onGameTick2.RemoveListener(UpdateCurrentTarget);
                    GameManager.Instance.onGameTick.RemoveListener(UpdateStat);
                    // StartCoroutine(EndGame());
                    StatChangeDisplay.RemoveAllDisplayingBuffs();
                    state = EnemyState.Dying;
                }
            });
        }

        private void InitializeCharacterActionHandler()
        {
            actionHandler.Initialize(
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

        private void Start()
        {
            Identifier = GameManager.Instance.AddEnemyAlive(Identifier, Transform);
            GameManager.Instance.onGameTick.AddListener(UpdateStat);
            GameManager.Instance.onGameTick2.AddListener(UpdateCurrentTarget);

            state = EnemyState.Idling;
        }

        private void Update()
        {
            if (state == EnemyState.Dead)
                return;

            switch (state)
            {
                case EnemyState.Dying:
                    Die();
                    break;
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
                    // Not implemented.
                    break;
                case EnemyState.Returning:
                    ResetAndReturn();
                    break;

            }
        }

        float GetSqrDistance(Vector3 position1, Vector3 position2)
        {
            return Vector3.SqrMagnitude(Vector3.Scale(position1 - position2, new Vector3(1f, 0f, 1f)));
        }

        void LookAtCurrentTarget()
        {
            Vector3 direction = currentTargetTransform.position - Transform.position;
            direction.y = 0f;
            Transform.forward = direction;
        }

        private void Die()
        {
            Animator.SetTrigger(Dead);
            GameManager.Instance.EnemiesAlive.Remove(Identifier);
            navMeshAgent.enabled = false;
            StopAllCoroutines();
            state = EnemyState.Dead;
        }

        private void ResetAndReturn()
        {
            if (navMeshAgent.remainingDistance < 0.05f)
            {
                Transform.position = originalPosition;
                Transform.rotation = originalRotation;
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
            if (GameManager.Instance.PlayersAlive.Count == 0)
            {
                navMeshAgent.SetDestination(originalPosition);
                state = EnemyState.Returning;
                return;
            }

            actionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(Transform.position, currentTargetTransform.position);

            if (actionHandler.SqrDistanceFromCurrentTarget <= MaxSquareOfMeleeAttackRange)
            {
                LookAtCurrentTarget();

                if (actionHandler.SqrDistanceFromCurrentTarget > SquareOfStoppingDistance)
                    navMeshAgent.SetDestination(currentTargetTransform.position);
                else
                    navMeshAgent.SetDestination(Transform.position);

                if (Time.time - lastAttackTime > normalAttackPeriod)
                {
                    lastAttackTime = Time.time;
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
            if (GameManager.Instance.PlayersAlive.Count == 0)
            {
                navMeshAgent.SetDestination(originalPosition);
                state = EnemyState.Returning;
                return;
            }

            actionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(Transform.position, currentTargetTransform.position);

            if (actionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfChaseDistance)
            {
                state = EnemyState.Returning;
            }
            else if (actionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
            {
                navMeshAgent.SetDestination(currentTargetTransform.position);
                LookAtCurrentTarget();
            }
            else
            {
                navMeshAgent.SetDestination(Transform.position);
                state = EnemyState.Attacking;
            }
        }

        private void Idle()
        {
            if (enmitiesAgainstPlayers.Count > 0 && actionHandler.CurrentTarget != null)
            {
                actionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(Transform.position, currentTargetTransform.position);

                if (actionHandler.SqrDistanceFromCurrentTarget <= MaxSquareOfChaseDistance)
                {
                    if (actionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange)
                    {
                        state = EnemyState.Locomoting;
                    }
                    else
                    {
                        state = EnemyState.Attacking;
                    }
                }

                if (!GameManager.Instance.IsInBattle)
                    GameManager.Instance.IsInBattle = true;
            }
            else if (GameManager.Instance.PlayersAlive.Count > 0)
            {
                var foundKey = -1;
                var sqrDistanceFromPlayer = SquareOfPlayerDetectionRange;

                foreach (var key in GameManager.Instance.PlayersAlive.Keys)
                {
                    var thisSqrDistanceFromPlayer = GetSqrDistance(Transform.position, GameManager.Instance.PlayersAlive[key].position);
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

        private void FixedUpdate()
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

                actionHandler.SetRecentTarget(actionHandler.CurrentTarget);
                actionHandler.SetCurrentTarget(null);
                currentTargetTransform = null;
                currentTargetStatChangeHandler = null;
            }

            if (!hasEnmityListBeenUpdated || enmitiesAgainstPlayers.Count <= 0)
                return;

            currentTargetTransform = GameManager.Instance.PlayersAlive[Utilities.GetMaxValuePair(enmitiesAgainstPlayers).Key];
            actionHandler.SetRecentTarget(actionHandler.CurrentTarget);
            actionHandler.SetCurrentTarget(currentTargetTransform.gameObject);

            if (actionHandler.RecentTarget == actionHandler.CurrentTarget)
                return;

            currentTargetStatChangeHandler = actionHandler.CurrentTarget.GetComponent<StatChangeHandler>();

            hasEnmityListBeenUpdated = false;
        }
    }
}