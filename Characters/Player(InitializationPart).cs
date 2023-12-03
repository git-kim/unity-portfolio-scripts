using UnityEngine;
using UnityEngine.Events;
using GameData;
using Characters.Handlers;

public sealed partial class Player : Character
{
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

    [HideInInspector] public UnityEvent onUpdateVisibleGlobalCoolTime;
    [HideInInspector] public UnityEvent onUpdateSqrDistanceFromCurrentTarget;

    private Coroutine landCoroutine;

    private PlayerCastingBarDisplay playerCastingBarDisplay;

    private HitAndManaPointsDisplay playerHPMPDisplay;

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

        playerHPMPDisplay = FindObjectOfType<HitAndManaPointsDisplay>();

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
            .SetBaseValue(Stat.HitPoints, 3000)
            .SetBaseValue(Stat.MaximumHitPoints, 3000)
            .SetBaseValue(Stat.ManaPoints, 3000)
            .SetBaseValue(Stat.MaximumManaPoints, 3000)
            .SetBaseValue(Stat.MeleeAttack, 10)
            .SetBaseValue(Stat.MeleeDefense, 10)
            .SetBaseValue(Stat.MagicAttack, 100)
            .SetBaseValue(Stat.MagicDefense, 100)
            .SetBaseValue(Stat.HitPointsRestorability, 200)
            .SetBaseValue(Stat.ManaPointsRestorability, 420)
            .SetBaseValue(Stat.LocomotionSpeed, 6);

        autoHitPointsRegeneration = (int)(Stats[Stat.MaximumHitPoints] * 0.008f);
        autoManaPointsRegeneration = (int)(Stats[Stat.MaximumHitPoints] * 0.025f);
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
                hitAndManaPointsDisplay = playerHPMPDisplay,
                statChangeDisplay = playerIStatChangeDisplay,
                onHitPointsBecomeZero = onDie.Invoke
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
}