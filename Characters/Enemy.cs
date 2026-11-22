using Characters.EnemySkills;
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

        [Header("Identity")]
        [SerializeField] private string displayName = "언데드 기사";
        [SerializeField] private bool isBoss = true;
        public string DisplayName => displayName;
        public bool IsBoss => isBoss;

        [Header("Stats")]
        [SerializeField] private int maximumHitPoints = 2500;
        [SerializeField] private int meleeAttack = 270;
        [SerializeField] private int meleeDefense = 160;
        [SerializeField] private int magicAttack = 10;
        [SerializeField] private int magicDefense = 10;
        [SerializeField] private float normalAttackPeriod = 3f;
        private float lastAttackTime;

        [Header("Skill")]
        [SerializeField] private EnemySkill skill;

        [Header("Displays")]
        [SerializeField] private EnemyCastingBarDisplay castingBarDisplay;
        [SerializeField] private EnemyHitPointsDisplay hitPointsDisplay;
        [SerializeField] private EnemyStatChangeDisplay enemyStatChangeDisplay;
        public IStatChangeDisplay StatChangeDisplay => enemyStatChangeDisplay;
        public EnemyCastingBarDisplay CastingBar => castingBarDisplay;

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

            currentTargetTransform = null;

            originalPosition = Transform.position;
            originalRotation = Transform.rotation;

            InitializeCharacterActionHandler();

            hitPointsDisplay.Initialize(Transform, CharacterController);
            enemyStatChangeDisplay.Initialize(this, CharacterController);

            if (skill)
                skill.Initialize(this, Animator);
        }

        private void SetStats()
        {
            Stats = new StatisticsBuilder()
                .SetBaseValue(Stat.HitPoints, maximumHitPoints)
                .SetBaseValue(Stat.MaximumHitPoints, maximumHitPoints)
                .SetBaseValue(Stat.ManaPoints, 1)
                .SetBaseValue(Stat.MaximumManaPoints, 1)
                .SetBaseValue(Stat.MeleeAttack, meleeAttack)
                .SetBaseValue(Stat.MeleeDefense, meleeDefense)
                .SetBaseValue(Stat.MagicAttack, magicAttack)
                .SetBaseValue(Stat.MagicDefense, magicDefense);
        }

        private void InitializeStatChangeHandler()
        {
            statChangeHandler.Initialize(new StatChangeHandler.InitializationContext
            {
                identifier = Identifier,
                stats = Stats,
                hitAndManaPointsDisplay = hitPointsDisplay,
                statChangeDisplay = StatChangeDisplay,
                onHitPointsBecomeZero = OnHitPointsBecomeZero
            });
        }

        private void OnHitPointsBecomeZero()
        {
            GameManager.Instance.EnemiesAlive.Remove(Identifier);
            GameManager.Instance.onGameTick2.RemoveListener(UpdateCurrentTarget);
            GameManager.Instance.onGameTick.RemoveListener(UpdateStat);
            StatChangeDisplay.RemoveAllDisplayingBuffs();
            if (skill)
                skill.Cancel();
            state = EnemyState.Dying;
            GameManager.Instance.NotifyEnemyDied(this);
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

        /// <summary>Applies damage from this enemy to a target and shows it.</summary>
        public void DealDamage(StatChangeHandler target, int attackPower, bool isMelee, string actionName)
        {
            if (!target || target.HasZeroHitPoints)
                return;

            var effectiveDamage = target.GetEffectiveDamage(attackPower, isMelee,
                Utilities.GetRandomFloatFromSineDistribution(0.96f, 1.04f));
            target.DecreaseStat(Stat.HitPoints, effectiveDamage);
            target.ShowHitPointsChange(effectiveDamage, true, actionName);
            if (isMelee)
                AudioManager.Instance.Play(Sfx.MeleeHit, target.transform.position);
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
                    return;
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
                    Cast();
                    break;
                case EnemyState.Returning:
                    ResetAndReturn();
                    break;
            }

            UpdateMovementAnimation();
        }

        private void UpdateMovementAnimation()
        {
            Animator.SetInteger(MovementMode, navMeshAgent.velocity.sqrMagnitude > 0f ? 2 : 0);
        }

        float GetSqrDistance(Vector3 position1, Vector3 position2)
        {
            return Vector3.SqrMagnitude(Vector3.Scale(position1 - position2, new Vector3(1f, 0f, 1f)));
        }

        void LookAtCurrentTarget()
        {
            Vector3 direction = currentTargetTransform.position - Transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                Transform.forward = direction;
        }

        private void Die()
        {
            Animator.SetInteger(MovementMode, 0);
            Animator.SetBool(Dead, true); // a bool, not a trigger: a layer mid-transition must still see it
            AudioManager.Instance.Play(isBoss ? Sfx.BossDeath : Sfx.SkeletonCollapse, Transform.position);
            GameManager.Instance.EnemiesAlive.Remove(Identifier);
            navMeshAgent.enabled = false;
            hitPointsDisplay.SetActive(false);
            if (castingBarDisplay)
                castingBarDisplay.StopShowingCastingBar();
            StopAllCoroutines();
            state = EnemyState.Dead;
        }

        private void StartReturning()
        {
            if (skill)
                skill.Cancel();
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(originalPosition);
            GameManager.Instance.SetEngaged(this, false);
            state = EnemyState.Returning;
        }

        private void ResetAndReturn()
        {
            if (navMeshAgent.pathPending || navMeshAgent.remainingDistance >= 0.05f)
                return;

            Transform.position = originalPosition;
            Transform.rotation = originalRotation;
            navMeshAgent.velocity = Vector3.zero;

            statChangeHandler.ActiveStatChangingEffects.Clear();
            Stats[Stat.HitPoints] = Stats[Stat.MaximumHitPoints];
            statChangeHandler.UpdateHitPointsDisplay();

            enmitiesAgainstPlayers.Clear();
            actionHandler.SetRecentTarget(null);
            actionHandler.SetCurrentTarget(null);
            currentTargetTransform = null;
            currentTargetStatChangeHandler = null;

            state = EnemyState.Idling;
            Animator.SetInteger(MovementMode, 0);
        }

        private bool HasNoTargetLeft()
        {
            return GameManager.Instance.PlayersAlive.Count == 0 || currentTargetTransform == null;
        }

        private void Cast()
        {
            if (skill && skill.IsRunning)
                return;

            navMeshAgent.isStopped = false;
            state = EnemyState.Attacking;
        }

        private void AttackPlayer()
        {
            if (HasNoTargetLeft())
            {
                StartReturning();
                return;
            }

            actionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(Transform.position, currentTargetTransform.position);

            if (actionHandler.SqrDistanceFromCurrentTarget <= MaxSquareOfMeleeAttackRange)
            {
                LookAtCurrentTarget();

                if (skill && skill.TryActivate(currentTargetTransform, currentTargetStatChangeHandler,
                        actionHandler.SqrDistanceFromCurrentTarget))
                {
                    navMeshAgent.SetDestination(Transform.position);
                    navMeshAgent.isStopped = true;
                    state = EnemyState.Casting;
                    return;
                }

                if (actionHandler.SqrDistanceFromCurrentTarget > SquareOfStoppingDistance)
                    navMeshAgent.SetDestination(currentTargetTransform.position);
                else
                    navMeshAgent.SetDestination(Transform.position);

                if (Time.time - lastAttackTime > normalAttackPeriod)
                {
                    lastAttackTime = Time.time;
                    Animator.SetInteger(MovementMode, 0);
                    Animator.SetTrigger(Attack);
                    AudioManager.Instance.Play(Sfx.SwordSwing, Transform.position);
                    DealDamage(currentTargetStatChangeHandler, Stats[Stat.MeleeAttack], true, null);
                }
            }
            else
            {
                state = EnemyState.Locomoting;
            }
        }

        private void ChasePlayer()
        {
            if (HasNoTargetLeft())
            {
                StartReturning();
                return;
            }

            actionHandler.SqrDistanceFromCurrentTarget = GetSqrDistance(Transform.position, currentTargetTransform.position);

            if (actionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfChaseDistance)
            {
                StartReturning();
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
                    state = actionHandler.SqrDistanceFromCurrentTarget > MaxSquareOfMeleeAttackRange
                        ? EnemyState.Locomoting
                        : EnemyState.Attacking;

                    if (skill)
                        skill.ResetCoolDown();
                    GameManager.Instance.SetEngaged(this, true);
                }
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

            // Forget players that are no longer alive so the lookup below cannot fail.
            if (enmitiesAgainstPlayers.Count > 0)
            {
                var gone = new List<int>();
                foreach (var key in enmitiesAgainstPlayers.Keys)
                    if (!GameManager.Instance.PlayersAlive.ContainsKey(key))
                        gone.Add(key);
                foreach (var key in gone)
                    enmitiesAgainstPlayers.Remove(key);
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
