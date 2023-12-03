using System.Collections;
using UnityEngine;
using Characters.Handlers;
using UnityEngine.Events;
using GameData;

public sealed partial class Player : Character
{
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int BattlePoseOn = Animator.StringToHash("Battle Pose On");
    private static readonly int InTheAir = Animator.StringToHash("In the Air");
    private static readonly int YSpeed = Animator.StringToHash("Y-Speed");
    private static readonly int MovementSpeedMult = Animator.StringToHash("MovementSpeedMult");
    private static readonly int MovementMode = Animator.StringToHash("MovementMode");
    private static readonly int IdleTimeout = Animator.StringToHash("Idle Timeout");
    private static readonly int Idle2On = Animator.StringToHash("Idle2 On");
    private static readonly int LandMode = Animator.StringToHash("LandMode");

    private GameManager gameManagerInstance;
    private KeyManager keyManagerInstance;
    private Transform playerTransform;
    private Transform audioListenerTransform;
    private Transform mainCameraTransform;

    private int animatorStateHash, idle1Hash, idle2Hash;
    private float idleTimeout;

    private int movementMode = 0b100;

    private const float InitialJumpUpSpeed = 2.4f; // 도약용 변수

    private HumanoidFeetIK playerFeetIK;

    private UnityEvent onLand, onFall, onJumpUp, onDie;

    private Coroutine landCoroutine;

    private PlayerCastingBarDisplay playerCastingBarDisplay;

    [SerializeField] private HitAndManaPointsDisplay hitAndManaPointsDisplay;

    private IStatChangeDisplay playerIStatChangeDisplay;

    private ISelectable currentTargetISelectable;
    private StatChangeHandler currentTargetStatChangeHandler;

    private int autoHitPointsRegeneration, autoManaPointsRegeneration;


    private float timeToTurnOffBattleMode = 3f;

    public IStatChangeDisplay PlayerIStatChangeDisplay => playerIStatChangeDisplay;

    [SerializeField] private PlayerActionHandler actionHandler;
    public float VisibleGlobalCoolDownTime => actionHandler.VisibleGlobalCoolDownTime;

    [SerializeField] private ActionButtons actionButtons;

    protected override void Awake()
    {
        playerTransform = gameObject.transform;
        gameManagerInstance = GameManager.Instance;
        keyManagerInstance = KeyManager.Instance;
        Controller = gameObject.GetComponent<CharacterController>();
        ControllerTransform = Controller.transform;

        playerCastingBarDisplay = FindObjectOfType<PlayerCastingBarDisplay>();

        playerIStatChangeDisplay = FindObjectOfType<PlayerStatChangeDisplay>();

        NegativeGravity = Physics.gravity.y;
        DragFactor = new Vector3(0.95f, 0.95f, 0.95f);

        Velocity.y = 0f;
        Animator = gameObject.GetComponent<Animator>();
        mainCameraTransform = GameObject.Find("Main Camera").transform;
        Animator.SetFloat(IdleTimeout, Random.Range(5f, 20f));
        idle1Hash = Animator.StringToHash("Idle 1");
        idle2Hash = Animator.StringToHash("Idle 2");
        GroundCheckerPos = ControllerTransform.position;
        GroundCheckerPos.y += Controller.center.y - Controller.height * 0.5f;

        GroundCheckStartY = Controller.stepOffset;

        onJumpUp = new UnityEvent();
        onFall = new UnityEvent();
        onLand = new UnityEvent();
        onDie = new UnityEvent();

        onJumpUp.AddListener(Jump);
        onFall.AddListener(Fall);
        onLand.AddListener(Land);
        onDie.AddListener(Die);

        IsAbleToMove = true;

        landCoroutine = null;

        currentTargetISelectable = null;
        currentTargetStatChangeHandler = null;

        audioListenerTransform = gameObject.GetComponentInChildren<AudioListener>().transform;

        base.Awake();
    }

    protected override void Start()
    {
        playerFeetIK = gameObject.GetComponent<HumanoidFeetIK>();
        onJumpUp.AddListener(playerFeetIK.DisableFeetIK);
        onFall.AddListener(playerFeetIK.DisableFeetIK);

        Identifier = gameManagerInstance.AddPlayerAlive(Identifier, playerTransform);
        SetStats();
        SetStatPointsRegeneratingEvent();
        InitializeStatChangeHandler();

        InitializeCharacterActionHandler();
        actionHandler.SetPlayerIdentifier(Identifier);
        actionHandler.SetActionCommands(PlayerIStatChangeDisplay, actionButtons);
        actionHandler.SetPlayerReferences(Animator, playerTransform, v => GoalRotation = v);

        gameManagerInstance.onGameTick.AddListener(UpdateStat);
    }

    private void SetStats()
    {
        Stats = new StatisticsBuilder()
            .SetBaseValue(Stat.HitPoints, 5000)
            .SetBaseValue(Stat.MaximumHitPoints, 5000)
            .SetBaseValue(Stat.ManaPoints, 5000)
            .SetBaseValue(Stat.MaximumManaPoints, 5000)
            .SetBaseValue(Stat.MeleeAttack, 10)
            .SetBaseValue(Stat.MeleeDefense, 10)
            .SetBaseValue(Stat.MagicAttack, 100)
            .SetBaseValue(Stat.MagicDefense, 100)
            .SetBaseValue(Stat.HitPointsRestorability, 300)
            .SetBaseValue(Stat.ManaPointsRestorability, 480)
            .SetBaseValue(Stat.LocomotionSpeed, 6);

        autoHitPointsRegeneration = (int)(Stats[Stat.MaximumHitPoints] * 0.008f);
        autoManaPointsRegeneration = (int)(Stats[Stat.MaximumManaPoints] * 0.025f);
    }

    private void SetStatPointsRegeneratingEvent()
    {
        gameManagerInstance.onGameTick.AddListener(RegenerateStatPoints);
    }

    private void InitializeStatChangeHandler()
    {
        statChangeHandler.Initialize(
            new StatChangeHandler.InitializationContext
            {
                identifier = Identifier,
                stats = Stats,
                hitAndManaPointsDisplay = hitAndManaPointsDisplay,
                statChangeDisplay = playerIStatChangeDisplay,
                onHitPointsBecomeZero = () => onDie.Invoke()
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
        if (statChangeHandler.HasZeroHitPoints) return;

        if (Animator.GetBool(BattlePoseOn))
        {
            statChangeHandler.IncreaseStat(Stat.HitPoints, (int)(autoHitPointsRegeneration * Random.Range(0.4f, 0.6f)));
            statChangeHandler.IncreaseStat(Stat.ManaPoints, (int)(autoManaPointsRegeneration * Random.Range(0.4f, 0.6f)));
        }
        else
        {
            statChangeHandler.IncreaseStat(Stat.HitPoints, (int)(autoHitPointsRegeneration * Random.Range(0.9f, 1.1f)));
            statChangeHandler.IncreaseStat(Stat.ManaPoints, (int)(autoManaPointsRegeneration * Random.Range(0.9f, 1.1f)));
        }
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
        playerFeetIK.DisableFeetIK();
        actionHandler.StopTakingAction();
        actionHandler.RemoveActionToTake();
        playerIStatChangeDisplay.RemoveAllDisplayingBuffs();
        DeselectTarget();
        yield return new WaitForSeconds(0.5f);

        gameManagerInstance.State = GameState.Over;
    }

    protected override void FixedUpdate()
    {
        actionHandler.UpdateGlobalCoolDownTime();

        UpdateAudioListenerRotation();

        if (gameManagerInstance.State == GameState.Over || statChangeHandler.HasZeroHitPoints)
            return;

        actionHandler.UpdateSqrDistanceFromCurrentTarget(currentTargetStatChangeHandler.SelfOrNull()?.HasZeroHitPoints ?? false, DeselectTarget);

        if (!gameManagerInstance.IsInBattle && Animator.GetBool(BattlePoseOn) && timeToTurnOffBattleMode > 0f)
        {
            timeToTurnOffBattleMode -= Time.deltaTime;
            if (timeToTurnOffBattleMode < 0f)
            {
                timeToTurnOffBattleMode = 3f;
                Animator.SetBool(BattlePoseOn, false);
            }
        }
    }

    private void UpdateAudioListenerRotation()
    {
        audioListenerTransform.rotation = Quaternion.LookRotation(audioListenerTransform.position - mainCameraTransform.position);
    }

    protected override void Update()
    {
        if (gameManagerInstance.State != GameState.Over && !statChangeHandler.HasZeroHitPoints)
            ChangePoseIfApplicable();

        Locomote();

        if (gameManagerInstance.State != GameState.Over && !statChangeHandler.HasZeroHitPoints)
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
        if (!IsDead)
            SetVelocityForDesiredDirection();

        bool hasBeenInTheAir = Animator.GetBool(InTheAir);

        CheckGround(hasBeenInTheAir, out IsNotInTheAir);

        if (hasBeenInTheAir)
        {
            if (IsNotInTheAir && !IsDead)
                onLand.Invoke(); // starts landing
        }
        else
        {
            if (IsNotInTheAir)
            {
                if (keyManagerInstance.Jump && IsAbleToMove && !IsDead)
                {
                    onJumpUp.Invoke(); // starts jumping up
                }
                else
                {
                    if (Animator.GetFloat(YSpeed) < -0.4f)
                        Animator.SetFloat(YSpeed, -0.4f); // limits speed

                    SetValuesAccordingToMovementMode();
                }
            }
            else if  (!IsDead)
            {
                if (Animator.GetFloat(YSpeed) <= -2f)
                    onFall.Invoke(); // starts falling
            }
        }

        Animator.SetFloat(YSpeed, Animator.GetFloat(YSpeed) + NegativeGravity * Time.deltaTime); // applies gravity
        Velocity.y = Animator.GetFloat(YSpeed);

        base.Rotate();

        Velocity = Vector3.Scale(Velocity, DragFactor); // applies drag

        base.Move();
    }

    void SetValuesAccordingToMovementMode()
    {
        if (Velocity.magnitude > 0.001f)
        {
            // KeyManagerInstance.MovementMode:
            // 보통 속도로 걷기(= 평보, 0b010)
            // 보통 속도로 달리기(0b100)
            // 빨리 걷기(= 속보, 0b011)
            // 빨리 달리기(= 질주, 0b101)
            movementMode = keyManagerInstance.MovementMode;

            if (statChangeHandler.HasStatChangingEffect(0))
                movementMode |= 0b001;

            switch (movementMode)
            {
                case 0b010:
                    Velocity *= 0.5f; // 또는 velocity = Vector3.Scale(velocity, new Vector3(0.5f, 0.5f, 0.5f));
                    Animator.SetFloat(MovementSpeedMult, 0.5f);
                    break;
                case 0b100:
                    Animator.SetFloat(MovementSpeedMult, 1.0f);
                    break;
                case 0b011:
                    Velocity *= 0.7f;
                    Animator.SetFloat(MovementSpeedMult, 0.7f);
                    break;
                case 0b101:
                    Velocity *= 1.4f;
                    Animator.SetFloat(MovementSpeedMult, 1.4f);
                    break;
                default:
                    Debug.LogError("이동 속도 계산 중 오류가 발생하였습니다.");
                    break;
            }

            Animator.SetInteger(MovementMode, movementMode & 0b110);
        }
        else
        {
            Animator.SetInteger(MovementMode, 0);
            Animator.SetFloat(MovementSpeedMult, 1f);
        }
    }

    /// <summary>
    /// 특정 조건을 충족하면 자세를 바꾼다.(즉 재생 대상 애니메이션을 변경한다.)
    /// </summary>
    private void ChangePoseIfApplicable()
    {
        #region 자세 변경 1(Idle 상태)
        animatorStateHash = Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (animatorStateHash == idle1Hash || animatorStateHash == idle2Hash)
        {
            idleTimeout = Animator.GetFloat(IdleTimeout) - Time.deltaTime;
            if (idleTimeout < 0f)
            {
                idleTimeout = Random.Range(10f, 20f);
                Animator.SetBool(Idle2On, !Animator.GetBool(Idle2On));
            }
            Animator.SetFloat(IdleTimeout, idleTimeout);
        }
        #endregion

        #region 자세 변경 2(전투 시 / 평상시)
        if (gameManagerInstance.IsInBattle)
        {
            if (!Animator.GetBool(BattlePoseOn))
                Animator.SetBool(BattlePoseOn, true);
        }
        else if (keyManagerInstance.BattlePose)
        {
            Animator.SetBool(BattlePoseOn, !Animator.GetBool(BattlePoseOn));
        }
        #endregion
    }

    /// <summary>
    /// 플레이어 캐릭터가 움직일 방향을 메인 카메라가 보는 방향을 기준으로 구한다.
    /// </summary>
    void SetVelocityForDesiredDirection()
    {
        Velocity.x = keyManagerInstance.H;
        Velocity.y = 0f;
        Velocity.z = keyManagerInstance.V;

        Velocity = mainCameraTransform.TransformDirection(Velocity); // velocity를 주 카메라 로컬 벡터로 간주하여 월드 공간 벡터로 변환한다.(벡터 크기는 변하지 않는다.)
        Velocity.y = 0f;
        Velocity.Normalize();
    }

    /// <summary>
    /// 원통형으로 플레이어 캐릭터가 공중에 있는지를 검사한다.(9번 레이어: Ground)
    /// </summary>
    void CheckGround(bool isInTheAir, out bool isNotInTheAir)
    {
        GroundCheckerPos = ControllerTransform.position;
        GroundCheckerPos.y += Controller.center.y - Controller.height * 0.5f;

        if (isInTheAir)
        {
            GroundCheckStartY = 0.05f;
            if ((Controller.collisionFlags & CollisionFlags.Below) != 0) GroundCheckStartY = Controller.stepOffset + 0.1f;
        }
        else GroundCheckStartY = Controller.stepOffset + 0.05f;

        isNotInTheAir =
            gameManagerInstance.CheckCylinder(GroundCheckerPos - new Vector3(0f, GroundCheckStartY, 0f), GroundCheckerPos + new Vector3(0f, 0.05f, 0f), Controller.radius * 0.5f, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 상자로만 검사할 때에는 Physics.CheckBox(groundChecker.position, new Vector3(0.03f, 0.05f, 0.03f), Quaternion.identity, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 우변에 (Controller.collisionFlags & CollisionFlags.Below) != 0;을 사용할 수도 있겠으나 Controller는 캡슐 형태 콜라이더이므로 땅 모서리에서 부정확하다.
    }

    void Jump()
    {
        keyManagerInstance.Jump = false;
        Animator.SetFloat(YSpeed, InitialJumpUpSpeed);
        Animator.SetBool(InTheAir, true);
        Animator.SetInteger(LandMode, 0);
    }

    void Land()
    {
        if (landCoroutine != null)
        {
            StopCoroutine(landCoroutine);
            landCoroutine = null;
        }

        if (Animator.GetFloat(YSpeed) < -2.5f)
        {
            Animator.SetInteger(LandMode, 1);
            landCoroutine = StartCoroutine(BlockMovementTemporarily(0.22f, () => { playerFeetIK.EnableFeetIK(); }));

            Animator.SetFloat(YSpeed, 0f);
            Animator.SetBool(InTheAir, false);
        }
        else
        {
            Animator.SetInteger(LandMode, 0);

            Animator.SetFloat(YSpeed, 0f);
            Animator.SetBool(InTheAir, false);
            playerFeetIK.EnableFeetIK();
        }
    }

    private void Fall()
    {
        Animator.SetBool(InTheAir, true);
        Animator.SetInteger(LandMode, 0);
    }

    private void Die()
    {
        gameManagerInstance.onGameTick.RemoveListener(UpdateStat);
        gameManagerInstance.PlayersAlive.Remove(Identifier);
        StopAllCoroutines();
        StartCoroutine(EndGame());
    }

    public void SelectTarget(GameObject gO)
    {
        currentTargetStatChangeHandler = gO.GetComponent<StatChangeHandler>();
        if (currentTargetStatChangeHandler.HasZeroHitPoints)
            return;

        actionHandler.SetCurrentTarget(gO);

        currentTargetISelectable = gO.GetComponent<ISelectable>();
        currentTargetISelectable.TargetIndicator.Enable();
    }

    public void DeselectTarget()
    {
        if (actionHandler.CurrentTarget == null)
            return;

        currentTargetISelectable.TargetIndicator.Disable();
        actionHandler.SetCurrentTarget(null);
        currentTargetISelectable = null;
        currentTargetStatChangeHandler = null;
    }
}