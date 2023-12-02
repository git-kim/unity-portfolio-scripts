using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using GameData;
using Characters.Handlers;

public sealed partial class Player : Character
{
    private GameManager gameManagerInstance;
    private KeyManager keyManagerInstance;
    private Transform playerTransform;
    private Transform audioListenerTransform;
    private Transform mainCameraTransform;

    private List<ActionButton> actionButtons = new List<ActionButton>();

    private int animatorStateHash, idle1Hash, idle2Hash;
    private float idleTimeout;

    private int movementMode = 0b100;

    private const float InitialJumpUpSpeed = 2.4f; // 도약용 변수

    [HideInInspector] public int recentActionInput = 0; // 액션 버튼 입력 처리용

    private HumanoidFeetIK playerFeetIK;

    private UnityEvent onLand, onFall, onJumpUp, onDie;

    [HideInInspector] public UnityEvent onUpdateVisibleGlobalCoolTime;
    [HideInInspector] public UnityEvent onUpdateSqrDistanceFromCurrentTarget;

    private Coroutine landCoroutine;


    private PlayerCastingBarDisplay playerCastingBarDisplay;

    private SprintAbility sprint;
    private HitPointsHealingAbility hitPointsHealingAbility;
    private ManaPointsHealingAbility manaPointsHealingAbility;

    private HitAndManaPointsDisplay playerHPMPDisplay;

    private IStatChangeDisplay playerIStatChangeDisplay;

    private ISelectable currentTargetISelectable;
    private StatChangeHandler currentTargetStatChangeHandler;

    private int autoHitPointsRegeneration, autoManaPointsRegeneration;

    private readonly Vector3 forwardRight = (Vector3.forward + Vector3.right).normalized;

    private float timeToTurnOffBattleMode = 3f;

    public IStatChangeDisplay PlayerIStatChangeDisplay => playerIStatChangeDisplay;

    public Statistics Stats { get; private set; }
    [SerializeField] private StatChangeHandler statChangeHandler;

    [SerializeField] private CharacterActionHandler characterActionHandler;
    public CharacterActions CharacterActions => characterActionHandler.CharacterActions;
    public float VisibleGlobalCoolDownTime => characterActionHandler.VisibleGlobalCoolDownTime;
    public float GlobalCoolDownTime => characterActionHandler.GlobalCoolDownTime;
    public float SqrDistanceFromCurrentTarget => characterActionHandler.SqrDistanceFromCurrentTarget;
    public bool IsCasting => characterActionHandler.IsCasting;

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

        LocomotionSpeed = 6f;
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

        CurrentTarget = RecentTarget = null; // 주의: 이 줄이 있어야 오류가 발생하지 않는다.

        currentTargetISelectable = null;
        currentTargetStatChangeHandler = null;

        audioListenerTransform = gameObject.GetComponentInChildren<AudioListener>().transform;

        actionButtons.Add(null);
        actionButtons.AddRange(Resources.FindObjectsOfTypeAll<ActionButton>());
        actionButtons = actionButtons.Take(1).Concat(actionButtons.Skip(1).OrderBy(btn => btn.actionID)).ToList();

        base.Awake(); // 상위 클래스 함수 호출
    }

    // Start: 스크립트 컴포넌트가 '활성 상태'이면 Update 최초 호출 전에 호출되는 초기화 함수(최초 1회)
    // (참고: 스크립트 컴포넌트가 활성 상태일(상태가 될) 때마다 호출되는 함수는 OnEnable이며 Start 전에 OnEnable이 호출된다.)
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
        SetActionCommands();
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
            .SetBaseValue(Stat.ManaPointsRestorability, 420);

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
                castingBarDisplay = playerCastingBarDisplay,
                characterActions = new CharacterActions(),
                stats = Stats
            });
    }

    void SetActionCommands()
    {
        var actionCommands = characterActionHandler.CharacterActions;
        var globalCoolDownTime = characterActionHandler.GlobalCoolDownTime;

        var nullAction = new CharacterAction(
            new CharacterAction.CreationContext(-1, new NullActionCommand(gameObject),
            CharacterActionTargetType.Self, 0f, 0f, 0f));

        actionCommands.Add(nullAction);

        actionCommands.Add(new CharacterAction(
            new CharacterAction.CreationContext(1, new FireballSpell(gameObject),
            CharacterActionTargetType.NonSelf,
            globalCoolDownTime, globalCoolDownTime, 0.5f, 25f, 0f, 250,
            "불덩이", "25미터 이내 선택 대상에게 불덩이를 던진다.")));

        actionCommands.Add(new CharacterAction(
            new CharacterAction.CreationContext(2, new CurseSpell(gameObject, 3),
            CharacterActionTargetType.NonSelf,
            0f, globalCoolDownTime, 0.5f, 25f, 0f, 500,
            "저주", "25미터 이내 선택 대상에게 저주를 내려 30초 동안 일정 시간마다 대상 HP를 소량 감소한다.")));

        hitPointsHealingAbility = new HitPointsHealingAbility(gameObject, 1, PlayerIStatChangeDisplay);
        actionCommands.Add(new CharacterAction(
            new CharacterAction.CreationContext(3, hitPointsHealingAbility,
            CharacterActionTargetType.Self,
            0f, globalCoolDownTime, 0.5f, 0f, 0f, 800,
            "HP 회복", "HP를 10초 동안 일정 시간마다 소량 회복한다.")));

        manaPointsHealingAbility = new ManaPointsHealingAbility(gameObject, 2, actionButtons[4].GetComponent<OffGlobalCoolDownActionButton>(), PlayerIStatChangeDisplay);
        actionCommands.Add(new CharacterAction(
            new CharacterAction.CreationContext(4, manaPointsHealingAbility,
            CharacterActionTargetType.Self,
            0f, 60f, 0.5f, 0f, 0f, 0,
            "MP 회복", "MP를 20초 동안 일정 시간마다 소량 회복한다.", true)));

        actionCommands.Add(nullAction); // (not implemented) Ult.: non - self, off - global

        sprint = new SprintAbility(gameObject, 0, actionButtons[6].GetComponent<OffGlobalCoolDownActionButton>(), PlayerIStatChangeDisplay);
        actionCommands.Add(new CharacterAction(
            new CharacterAction.CreationContext(6, sprint,
            CharacterActionTargetType.Self,
            0f, 60f, 0.5f, 0f, 0f, 0,
            "잰 발놀림", "20초(전투 중 효과 지속 시간: 10초) 동안 더 빨리 걷거나 더 빨리 달릴 수 있다.")));
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

        if (hitPointsHealingAbility.IsBuffOn)
        {
            statChangeHandler.IncreaseStat(Stat.HitPoints, Stats[Stat.HitPointsRestorability]);
            statChangeHandler.ShowHitPointsChange(Stats[Stat.HitPointsRestorability], false, null);
        }

        if (manaPointsHealingAbility.IsBuffOn)
        {
            statChangeHandler.IncreaseStat(Stat.ManaPoints, Stats[Stat.ManaPointsRestorability]);
        }
    }
}