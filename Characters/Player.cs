using System.Collections;
using UnityEngine;
using Characters.Handlers;
using UnityEngine.Events;
using UserInterface;
using Managers;
using Characters.StatisticsScripts;
using Enums;

namespace Characters
{
    public sealed partial class Player : Character
    {
        private static readonly int Dead = Animator.StringToHash("Dead");
        private static readonly int BattlePoseOn = Animator.StringToHash("Battle Pose On");
        private static readonly int InTheAir = Animator.StringToHash("In the Air");
        private static readonly int YSpeed = Animator.StringToHash("Y-Speed");
        private static readonly int MovementSpeedMult = Animator.StringToHash("MovementSpeedMult");
        private static readonly int MovementMode = Animator.StringToHash("MovementMode");
        private static readonly int LandMode = Animator.StringToHash("LandMode");

        private const float InitialJumpUpSpeed = 2.4f;

        private const float OutOfBattleBattlePoseUndoingTime = 3f;
        private float battlePoseStartTime;

        [SerializeField] private Transform audioListenerTransform;

        [SerializeField] private HumanoidFeetIKSetter feetIKSetter;

        [SerializeField] private PlayerCastingBarDisplay playerCastingBarDisplay;
        [SerializeField] private HitAndManaPointsDisplay hitAndManaPointsDisplay;
        [SerializeField] private PlayerStatChangeDisplay playerStatChangeDisplay;
        public IStatChangeDisplay StatChangeDisplay => playerStatChangeDisplay;

        [SerializeField] private PlayerActionHandler actionHandler;
        [SerializeField] private ActionButtons actionButtons;

        private bool hasJumped;

        // Seconds per footstep at normal running speed; scaled by the locomotion speed multiplier.
        private const float RunStepInterval = 0.34f;
        private float stepTimer;

        private Coroutine landingRoutine;

        private PlayerTarget currentTarget;
        private StatChangeHandler currentTargetStatChangeHandler;

        protected override void Awake()
        {
            base.Awake();

            NegativeGravity = Physics.gravity.y;
            DragFactor = new Vector3(0.95f, 0.95f, 0.95f);

            Velocity.y = 0f;

            GroundCheckerPos = Transform.position;
            GroundCheckerPos.y += CharacterController.center.y - CharacterController.height * 0.5f;

            GroundCheckStartY = CharacterController.stepOffset;

            IsAbleToMove = true;

            landingRoutine = null;

            currentTarget = null;
            currentTargetStatChangeHandler = null;
        }

        private void Start()
        {
            Identifier = GameManager.Instance.AddPlayerAlive(Identifier, Transform);
            SetStats();
            InitializeStatChangeHandler();

            InitializeCharacterActionHandler();
            actionHandler.SetPlayerIdentifier(Identifier);
            actionHandler.SetActionCommands(StatChangeDisplay, actionButtons);
            actionHandler.SetPlayerReferences(Animator, Transform, v => GoalRotation = v);

            GameManager.Instance.onGameTick.AddListener(RegenerateStatPoints);
            GameManager.Instance.onGameTick.AddListener(UpdateStat);
        }

        private void SetStats()
        {
            Stats = new StatisticsBuilder()
                .SetBaseValue(Stat.HitPoints, 3000)
                .SetBaseValue(Stat.MaximumHitPoints, 3000)
                .SetBaseValue(Stat.ManaPoints, 2500)
                .SetBaseValue(Stat.MaximumManaPoints, 2500)
                .SetBaseValue(Stat.MeleeAttack, 10)
                .SetBaseValue(Stat.MeleeDefense, 10)
                .SetBaseValue(Stat.MagicAttack, 150)
                .SetBaseValue(Stat.MagicDefense, 100)
                .SetBaseValue(Stat.HitPointsRestorability, 280)
                .SetBaseValue(Stat.ManaPointsRestorability, 480)
                .SetBaseValue(Stat.LocomotionSpeed, 6)
                .SetBaseValue(Stat.HitPointsAutoRegeneration, 500)
                .SetBaseValue(Stat.ManaPointsAutoRegeneration, 500);
        }

        private void InitializeStatChangeHandler()
        {
            statChangeHandler.Initialize(
                new StatChangeHandler.InitializationContext
                {
                    identifier = Identifier,
                    stats = Stats,
                    hitAndManaPointsDisplay = hitAndManaPointsDisplay,
                    statChangeDisplay = StatChangeDisplay,
                    onHitPointsBecomeZero = Die
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
                    castingBarDisplay = playerCastingBarDisplay,
                    characterActions = new CharacterActions(),
                    stats = Stats
                });
        }

        private void RegenerateStatPoints()
        {
            if (statChangeHandler.HasZeroHitPoints)
                return;

            if (GameManager.Instance.IsInBattle)
            {
                statChangeHandler.IncreaseStat(Stat.HitPoints,
                    (int)(Stats[Stat.HitPointsAutoRegeneration] * Random.Range(0.2f, 0.3f)));
                statChangeHandler.IncreaseStat(Stat.ManaPoints,
                    (int)(Stats[Stat.ManaPointsAutoRegeneration] * Random.Range(0.2f, 0.3f)));
                return;
            }

            statChangeHandler.IncreaseStat(Stat.HitPoints,
                (int)(Stats[Stat.HitPointsAutoRegeneration] * Random.Range(0.9f, 1.1f)));
            statChangeHandler.IncreaseStat(Stat.ManaPoints,
                (int)(Stats[Stat.ManaPointsAutoRegeneration] * Random.Range(0.9f, 1.1f)));
        }

        private IEnumerator BlockMovementTemporarily(float timeInSeconds, UnityAction onEnd)
        {
            IsAbleToMove = false;
            yield return new WaitForSeconds(timeInSeconds);
            IsAbleToMove = true;
            onEnd?.Invoke();
        }

        private IEnumerator EndGame()
        {
            Animator.SetTrigger(Dead);
            feetIKSetter.DisableFeetIK();
            actionHandler.StopTakingAction();
            actionHandler.RemoveActionToTake();
            StatChangeDisplay.RemoveAllDisplayingBuffs();
            DeselectTarget();
            yield return new WaitForSeconds(0.5f);

            GameManager.Instance.EndGame(GameState.Over);
        }

        // Timers and target checks run once per rendered frame (Update), not on the physics step.
        private void UpdateTimersAndTarget()
        {
            actionHandler.UpdateGlobalCoolDownTime();

            UpdateAudioListenerRotation();

            if (GameManager.Instance.State != GameState.Running || statChangeHandler.HasZeroHitPoints)
                return;

            actionHandler.UpdateSqrDistanceFromCurrentTarget(
                currentTargetStatChangeHandler.SelfOrNull()?.HasZeroHitPoints ?? false, DeselectTarget);
        }

        private void UpdateAudioListenerRotation()
        {
            audioListenerTransform.rotation =
                Quaternion.LookRotation(
                    audioListenerTransform.position - GameManager.Instance.MainCameraTransform.position);
        }

        private void Update()
        {
            UpdateTimersAndTarget();

            if (GameManager.Instance.State == GameState.Running && !statChangeHandler.HasZeroHitPoints)
                ToggleBattlePoseIfNeeded();

            Locomote();

            if (GameManager.Instance.State == GameState.Running && !statChangeHandler.HasZeroHitPoints)
                actionHandler.Act(IsMoving);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != 0)
                return;

            actionHandler.SetLookAtValues();
        }

        private void Locomote()
        {
            var hasControl = !IsDead && GameManager.Instance.State == GameState.Running;
            if (hasControl)
                SetVelocityForDesiredDirection();
            else if (!IsDead)
                Velocity = Vector3.zero; // all of it: a leftover fall speed alone keeps the run animation going

            bool hasBeenInTheAir = Animator.GetBool(InTheAir);

            CheckGroundCylindrically(hasBeenInTheAir, out IsOnGround);

            if (hasBeenInTheAir)
            {
                if (IsOnGround && !IsDead && !hasJumped)
                    Land();

                hasJumped = false;
            }
            else
            {
                hasJumped = false;

                if (IsOnGround)
                {
                    if (KeyManager.Instance.Jump && IsAbleToMove && hasControl)
                    {
                        feetIKSetter.DisableFeetIK();
                        Jump();
                        hasJumped = true;
                    }
                    else if (Animator.GetFloat(YSpeed) < -0.4f)
                    {
                        Animator.SetFloat(YSpeed, -0.4f); // limits the downward speed
                    }
                }
                else if (!IsDead && Animator.GetFloat(YSpeed) <= -2.0f)
                {
                    feetIKSetter.DisableFeetIK();
                    Fall();
                }
            }

            ApplyLocomotionModeFactorsToVelocity();
            ApplyGravityToYSpeed();
            ApplyYSpeedToVelocity();
            Rotate();
            ApplyDragToVelocity();
            MoveUsingVelocity();
            PlayFootsteps();
        }

        private void PlayFootsteps()
        {
            if (!IsOnGround || !IsMoving || IsDead || Animator.GetBool(InTheAir))
            {
                stepTimer = 0.1f; // the first step lands shortly after starting to move
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f)
                return;

            stepTimer = RunStepInterval / Mathf.Max(0.3f, Animator.GetFloat(MovementSpeedMult));
            AudioManager.Instance.Play(Sfx.Footstep, Transform.position);
        }

        private void ApplyLocomotionModeFactorsToVelocity()
        {
            if (Velocity.magnitude < 0.001f)
            {
                Animator.SetInteger(MovementMode, 0);
                Animator.SetFloat(MovementSpeedMult, 1f);
                return;
            }

            var mode = KeyManager.Instance.LocomotionMode;

            if (statChangeHandler.HasStatChangingEffect(0))
                mode |= LocomotionMode.Sprint;

            switch (mode)
            {
                case LocomotionMode.Walk:
                    Velocity *= 0.5f;
                    Animator.SetFloat(MovementSpeedMult, 0.5f);
                    break;
                case LocomotionMode.Run:
                    Animator.SetFloat(MovementSpeedMult, 1.0f);
                    break;
                case LocomotionMode.FastWalk:
                    Velocity *= 0.7f;
                    Animator.SetFloat(MovementSpeedMult, 0.7f);
                    break;
                case LocomotionMode.FastRun:
                    Velocity *= 1.4f;
                    Animator.SetFloat(MovementSpeedMult, 1.4f);
                    break;
            }

            Animator.SetInteger(MovementMode, (int)KeyManager.Instance.LocomotionMode);
        }

        private void ApplyGravityToYSpeed()
        {
            var ySpeed = Animator.GetFloat(YSpeed) + NegativeGravity * Time.deltaTime;
            Animator.SetFloat(YSpeed, ySpeed);
        }

        private void ApplyYSpeedToVelocity()
        {
            Velocity.y = Animator.GetFloat(YSpeed);
        }

        private void ApplyDragToVelocity()
        {
            Velocity = Vector3.Scale(Velocity, DragFactor);
        }

        private void ToggleBattlePoseIfNeeded()
        {
            if (GameManager.Instance.IsInBattle)
            {
                if (!Animator.GetBool(BattlePoseOn))
                {
                    Animator.SetBool(BattlePoseOn, true);
                    battlePoseStartTime = Time.time;
                }
                return;
            }

            if (KeyManager.Instance.BattlePose)
            {
                var value = !Animator.GetBool(BattlePoseOn);
                Animator.SetBool(BattlePoseOn, value);
                if (value)
                {
                    battlePoseStartTime = Time.time;
                }
                return;
            }

            if (Animator.GetBool(BattlePoseOn)
                && Time.time - battlePoseStartTime > OutOfBattleBattlePoseUndoingTime)
            {
                 Animator.SetBool(BattlePoseOn, false);
            }
        }

        void SetVelocityForDesiredDirection()
        {
            Velocity.x = KeyManager.Instance.H;
            Velocity.y = 0f;
            Velocity.z = KeyManager.Instance.V;

            Velocity = GameManager.Instance.MainCameraTransform.TransformDirection(Velocity);
            Velocity.y = 0f;
            Velocity.Normalize();
        }

        private void CheckGroundCylindrically(bool hasBeenInTheAir, out bool isOnGround)
        {
            GroundCheckerPos = Transform.position;
            GroundCheckerPos.y += CharacterController.center.y - CharacterController.height * 0.5f;

            if (hasBeenInTheAir)
            {
                GroundCheckStartY = 0.05f;
                if ((CharacterController.collisionFlags & CollisionFlags.Below) != 0)
                    GroundCheckStartY = CharacterController.stepOffset + 0.1f;
            }
            else
            {
                GroundCheckStartY = CharacterController.stepOffset + 0.05f;
            }

            isOnGround = Utilities.CheckCylinder(
                GroundCheckerPos - new Vector3(0f, GroundCheckStartY, 0f),
                GroundCheckerPos + new Vector3(0f, 0.05f, 0f),
                CharacterController.radius * 0.5f,
                1 << 9, QueryTriggerInteraction.Ignore);
        }

        private void Jump()
        {
            KeyManager.Instance.Jump = false;
            AudioManager.Instance.Play(Sfx.Jump, Transform.position);
            Animator.SetFloat(YSpeed, InitialJumpUpSpeed);
            Animator.SetBool(InTheAir, true);
            Animator.SetInteger(LandMode, 0);
        }

        private void Land()
        {
            if (landingRoutine != null)
            {
                StopCoroutine(landingRoutine);
                landingRoutine = null;
            }

            var hardLanding = Animator.GetFloat(YSpeed) < -2.5f;
            AudioManager.Instance.Play(Sfx.Landing, Transform.position, hardLanding ? 1f : 0.6f);

            if (hardLanding)
            {
                Animator.SetInteger(LandMode, 1);
                landingRoutine = StartCoroutine(
                    BlockMovementTemporarily(0.22f,feetIKSetter.EnableFeetIK));
            }
            else
            {
                Animator.SetInteger(LandMode, 0);
                feetIKSetter.EnableFeetIK();
            }

            Animator.SetFloat(YSpeed, 0f);
            Animator.SetBool(InTheAir, false);
        }

        private void Fall()
        {
            Animator.SetBool(InTheAir, true);
            Animator.SetInteger(LandMode, 0);
        }

        private void Die()
        {
            GameManager.Instance.onGameTick.RemoveListener(UpdateStat);
            GameManager.Instance.onGameTick.RemoveListener(RegenerateStatPoints);
            GameManager.Instance.PlayersAlive.Remove(Identifier);
            StopAllCoroutines();
            AudioManager.Instance.Play(Sfx.CharacterDeath, Transform.position);
            StartCoroutine(EndGame());
        }

        public void SelectTarget(GameObject gO)
        {
            var targetStatChangeHandler = gO.GetComponent<StatChangeHandler>();
            if (targetStatChangeHandler.HasZeroHitPoints)
                return;

            if (currentTarget)
                currentTarget.TargetIndicator.SetActive(false); // the previous target loses its marker

            currentTargetStatChangeHandler = targetStatChangeHandler;
            actionHandler.SetCurrentTarget(gO);

            currentTarget = gO.GetComponent<PlayerTarget>();
            currentTarget.TargetIndicator.SetActive(true);
        }

        public void DeselectTarget()
        {
            if (actionHandler.CurrentTarget == null)
                return;

            currentTarget.TargetIndicator.SetActive(false);
            actionHandler.SetCurrentTarget(null);
            currentTarget = null;
            currentTargetStatChangeHandler = null;
        }
    }
}