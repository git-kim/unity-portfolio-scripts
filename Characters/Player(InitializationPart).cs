using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
// using Cinemachine;
using GameData;
using Characters.Components;

public sealed partial class Player : Character, IActable
{
    #region 인스펙터 비사용 변수
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

    private readonly Actions actionCommands = new Actions(); // 주의: = new Actions();를 잊으면 안 된다.

    private PlayerCastingBarDisplay playerCastingBarDisplay;

    private float sqrDistanceFromCurrentTarget = 0f;

    private SprintAbility sprint;
    private HPHealAbility hPHealAbility;
    private MPHealAbility mPHealAbility;

    private HitAndManaPointsDisplay playerHPMPDisplay;

    private IStatChangeDisplay playerIStatChangeDisplay;

    private ISelectable currentTargetISelectable;
    private StatChangeable currentTargetStatChangeable;

    private int autoHitPointsRegeneration, autoManaPointsRegeneration;

    private readonly Vector3 forwardRight = (Vector3.forward + Vector3.right).normalized;

    private float timeToTurnOffBattleMode = 3f;
    #endregion

    #region 프로퍼티
    public IStatChangeDisplay PlayerIStatChangeDisplay => playerIStatChangeDisplay;
    #endregion

    #region 인터페이스 프로퍼티 구현
    // 참고: 프로퍼티는 인스펙터에 나타나지 않는다.
    public Actions ActionCommands => actionCommands;
    public CastingBarDisplay CastingBarDisplay => playerCastingBarDisplay;
    public bool IsCasting { get; set; } = false;
    public int ActionToTake { get; set; } = 0;
    public int ActionBeingTaken { get; set; } = 0;
    public float VisibleGlobalCoolDownTime { get; set; } = 0f;
    public float InvisibleGlobalCoolDownTime { get; set; } = 0f;
    public float GlobalCoolDownTime { get; private set; }
    public float SqrDistanceFromCurrentTarget => sqrDistanceFromCurrentTarget;

    public Statistics Stats { get; private set; }
    #endregion

    [SerializeField] private StatChangeable statChangeable;

    // Awake: 이 스크립트가 달린 gameObject가 생성되어 활성화하면 (최초 1회) 호출되는 초기화 함수(스크립트 컴포넌트 활성 여부와는 무관하게 호출된다.)
    protected override void Awake()
    {
        Identifier = 0;
        GlobalCoolDownTime = 2f;
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

        // 참고: 아래 네 줄을 입력하고 싶지 않다면 위 UnityEvent 앞에 [SerializeField]를 붙이면 된다(이러면 인스펙터에서도 각 이벤트에 리스너 지정이 가능하다).
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
        currentTargetStatChangeable = null;

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

        SetInitialStats();
        SetActionCommands(); // should be called after SetInitialStats()
    }

    private void SetInitialStats()
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

        statChangeable.Initialize(new StatChangeable.InitializationContext
        {
            Identifier = Identifier,
            stats = Stats,
            hitAndManaPointsDisplay = playerHPMPDisplay,
            statChangeDisplay = playerIStatChangeDisplay,
            onHitPointsBecomeZero = onDie.Invoke
        });

        gameManagerInstance.onGameTick.AddListener(RegenerateStatPoints);
    }

    /// <summary>
    /// 사용 가능 기술(액션)을 지정한다.
    /// </summary>
    void SetActionCommands()
    {
        ActionInfo nullAction = new ActionInfo(-1, new NullAction(gameObject), ActionTargetType.Self, 0f, 0f, 0f); // Placeholder로 사용할 액션 정보 변수

        // 0(널 액션)
        actionCommands.Add(nullAction);

        // 1(불덩이) - Non-self, global
        actionCommands.Add(new ActionInfo(1, new FireballSpell(gameObject), ActionTargetType.NonSelf, GlobalCoolDownTime, GlobalCoolDownTime, 0.5f, 25f, 0f, 250,
            "불덩이", "25미터 이내 선택 대상에게 불덩이를 던진다."));

        // 2(저주) - Non-self, global
        actionCommands.Add(new ActionInfo(2, new CurseSpell(gameObject, 3), ActionTargetType.NonSelf, 0f, GlobalCoolDownTime, 0.5f, 25f, 0f, 500,
            "저주", "25미터 이내 선택 대상에게 저주를 내려 30초 동안 일정 시간 간격마다 대상 HP를 소량 감소한다."));

        // 3(HP 회복 능력) - Self, global
        hPHealAbility = new HPHealAbility(gameObject, 1, PlayerIStatChangeDisplay);
        actionCommands.Add(new ActionInfo(3, hPHealAbility, ActionTargetType.Self, 0f, GlobalCoolDownTime, 0.5f, 0f, 0f, 800,
            "HP 회복", "HP를 10초 동안 일정 시간 간격마다 소량 회복한다."));

        // 4(MP 회복 능력) - Self, off-global, 캐스팅 중이 아니면 글로벌 재사용 대기 시간을 무시한다.
        mPHealAbility = new MPHealAbility(gameObject, 2, actionButtons[4].GetComponent<OffGlobalCoolDownActionButton>(), PlayerIStatChangeDisplay);
        actionCommands.Add(new ActionInfo(4, mPHealAbility, ActionTargetType.Self, 0f, 60f, 0.5f, 0f, 0f, 0,
            "MP 회복", "MP를 20초 동안 일정 시간 간격마다 소량 회복한다.", true));

        // 5(빔 - 궁극 주문) - Non-self, off-global
        actionCommands.Add(nullAction);

        // 6(잰 발놀림) - Self, off-global, 글로벌 재사용 대기 시간을 무시하지 않는다.
        sprint = new SprintAbility(gameObject, 0, actionButtons[6].GetComponent<OffGlobalCoolDownActionButton>(), PlayerIStatChangeDisplay);
        actionCommands.Add(new ActionInfo(6, sprint, ActionTargetType.Self, 0f, 60f, 0.5f, 0f, 0f, 0,
            "잰 발놀림", "20초(전투 중 효과 지속 시간: 10초) 동안 더 빨리 걷거나 더 빨리 달릴 수 있다."));
    }

    /// <summary>
    /// HP, MP를 자연히 회복한다.
    /// </summary>
    void RegenerateStatPoints()
    {
        if (statChangeable.HasZeroHitPoints) return;

        if (Animator.GetBool(BattlePoseOn))
        {
            statChangeable.IncreaseStat(Stat.HitPoints, (int)(autoHitPointsRegeneration * Random.Range(0.4f, 0.6f)));
            statChangeable.IncreaseStat(Stat.ManaPoints, (int)(autoManaPointsRegeneration * Random.Range(0.4f, 0.6f)));
        }
        else
        {
            statChangeable.IncreaseStat(Stat.HitPoints, (int)(autoHitPointsRegeneration * Random.Range(0.9f, 1.1f)));
            statChangeable.IncreaseStat(Stat.ManaPoints, (int)(autoManaPointsRegeneration * Random.Range(0.9f, 1.1f)));
        }

        if (hPHealAbility.IsBuffOn)
        {
            statChangeable.IncreaseStat(Stat.HitPoints, Stats[Stat.HitPointsRestorability]);
            statChangeable.ShowHitPointsChange(Stats[Stat.HitPointsRestorability], false, null);
        }

        if (mPHealAbility.IsBuffOn)
        {
            statChangeable.IncreaseStat(Stat.ManaPoints, Stats[Stat.ManaPointsRestorability]);
        }
    }
}