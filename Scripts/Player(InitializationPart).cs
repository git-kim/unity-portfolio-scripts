using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
// using Cinemachine;
using FluentBuilderPattern;

sealed public partial class Player : Character, IDamageable, IActable
{
    #region 인스펙터 비사용 변수
    GameManager GAME;
    KeyManager KEY;
    Transform playerTransform;
    Transform audioListenerTransform;
    Transform mainCameraTransform;

    List<ActionButton> actionButtons = new List<ActionButton>();

    int animatorStateHash, idle1Hash, idle2Hash;
    float idleTimeout;

    int movementMode = 0b100;

    const float initialJumpUpSpeed = 2.4f; // 도약용 변수

    [HideInInspector] public int recentActionInput = 0; // 액션 버튼 입력 처리용

    static public float globalCoolDownTime = 2f;

    HumanoidFeetIK playerFeetIK;

    UnityEvent OnLand, OnFall, OnJumpUp, OnDie;

    [HideInInspector] public UnityEvent OnUpdateVisibleGlobalCoolTime;
    [HideInInspector] public UnityEvent OnUpdateSqrDistanceFromCurrentTarget;

    Coroutine landCoroutine;

    Statistics stats;

    readonly Actions actionCommands = new Actions(); // 주의: = new Actions();를 잊으면 안 된다.

    PlayerCastingBarDisplay playerCastingBarDisplay;

    float sqrDistanceFromCurrentTarget = 0f;

    SprintAbility sprint;
    HPHealAbility hPHealAbility;
    MPHealAbility mPHealAbility;

    PlayerHPMPDisplay playerHPMPDisplay;

    IStatChangeDisplay playerIStatChangeDisplay;

    ISelectable currentTargetISelectable;
    IDamageable currentTargetIDamageable;

    int playerHPRegen, playerMPRegen;

    Vector3 forwardRight = (Vector3.forward + Vector3.right).normalized;

    float timeToTurnOffBattleMode = 3f;
    #endregion

    #region 프로퍼티
    public int ID => id;
    public IStatChangeDisplay PlayerIStatChangeDisplay { get { return playerIStatChangeDisplay; } }
    #endregion

    #region 인터페이스 프로퍼티 구현
    // 참고: 프로퍼티는 인스펙터에 나타나지 않는다.
    public Actions ActionCommands => actionCommands;
    public CastingBarDisplay CastingBarDisplay { get { return playerCastingBarDisplay; } }
    public Statistics Stats { get => stats; set { Stats = stats; } }
    public Dictionary<int, KeyValuePair<Stat, int>> ActiveBuffEffects { get; set; } = new Dictionary<int, KeyValuePair<Stat, int>>();
    public bool IsDead { get; set; } = false;
    public bool IsCasting { get; set; } = false;
    public int ActionToTake { get; set; } = 0;
    public int ActionBeingTaken { get; set; } = 0;
    public float VisibleGlobalCoolDownTime { get; set; } = 0f;
    public float InvisibleGlobalCoolDownTime { get; set; } = 0f;
    public float GlobalCoolDownTime => globalCoolDownTime;
    public float SqrDistanceFromCurrentTarget => sqrDistanceFromCurrentTarget;
    #endregion


    // Awake: 이 스크립트가 달린 gameObject가 생성되면 (최초 1회) 호출되는 초기화 함수(스크립트 컴포넌트 활성 여부와는 무관하게 호출된다.)
    override protected void Awake()
    {
        id = 0;
        playerTransform = gameObject.transform;
        GAME = GameManager.Instance;
        KEY = KeyManager.Instance;
        cC = gameObject.GetComponent<CharacterController>();
        cCTransform = cC.transform;

        playerCastingBarDisplay = FindObjectOfType<PlayerCastingBarDisplay>();

        playerIStatChangeDisplay = FindObjectOfType<PlayerStatChangeDisplay>();

        negGravity = -GAME.gravity;
        dragFactor = new Vector3(0.95f, 0.95f, 0.95f);

        locomotionSpeed = 6f;
        velocity.y = 0f;
        anim = gameObject.GetComponent<Animator>();
        mainCameraTransform = GameObject.Find("Main Camera").transform;
        anim.SetFloat("Idle Timeout", Random.Range(5f, 20f));
        idle1Hash = Animator.StringToHash("Idle 1");
        idle2Hash = Animator.StringToHash("Idle 2");
        groundCheckerPos = cCTransform.position;
        groundCheckerPos.y += cC.center.y - cC.height * 0.5f;

        groundCheckStartY = cC.stepOffset;

        // 참고: 아래 네 줄을 입력하고 싶지 않다면 위 UnityEvent 앞에 [SerializeField]를 붙이면 된다(이러면 인스펙터에서도 각 이벤트에 리스너 지정이 가능하다).
        OnJumpUp = new UnityEvent();
        OnFall = new UnityEvent();
        OnLand = new UnityEvent();
        OnDie = new UnityEvent();

        OnJumpUp.AddListener(Jump);
        OnFall.AddListener(Fall);
        OnLand.AddListener(Land);
        OnDie.AddListener(Die);

        isAbleToMove = true;

        landCoroutine = null;

        currentTarget = recentTarget = null; // 주의: 이 줄이 있어야 오류가 발생하지 않는다.

        currentTargetISelectable = null;
        currentTargetIDamageable = null;

        audioListenerTransform = gameObject.GetComponentInChildren<AudioListener>().transform;

        SetInitialStats(); // 초기 스탯 설정

        actionButtons.Add(null);
        actionButtons.AddRange(Resources.FindObjectsOfTypeAll<ActionButton>());
        actionButtons = actionButtons.Take(1).Concat(actionButtons.Skip(1).OrderBy(btn => btn.actionID)).ToList();

        SetActionCommands(); // 액션 설정

        base.Awake(); // 상위 클래스 함수 호출
    }

    // Start: 스크립트 컴포넌트가 '활성 상태'이면 Update 최초 호출 전에 호출되는 초기화 함수(최초 1회)
    // (참고: 스크립트 컴포넌트가 활성 상태일(상태가 될) 때마다 호출되는 함수는 OnEnable이며 Start 전에 OnEnable이 호출된다.)
    override protected void Start()
    {
        playerFeetIK = gameObject.GetComponent<HumanoidFeetIK>();
        OnJumpUp.AddListener(playerFeetIK.DisableFeetIK);
        OnFall.AddListener(playerFeetIK.DisableFeetIK);

        playerHPMPDisplay = FindObjectOfType<PlayerHPMPDisplay>();
        id = GAME.AddPlayerAlive(id, playerTransform);
        GAME.OnGameTick.AddListener(RegenerateHPMP);
    }


    /// <summary>
    /// 초기 스탯을 지정한다.
    /// </summary>
    private void SetInitialStats()
    {
        stats = new StatisticsBuilder()
            .SetHP(10000).SetMaxHP(10000)
            .SetMP(10000).SetMaxMP(10000)
            .SetMeleeAttackPower(10)
            .SetMeleeDefensePower(10)
            .SetMagicAttackPower(100)
            .SetMagicDefensePower(100)
            .SetHPRestoringPower(200)
            .SetMPRestoringPower(420);

        playerHPRegen = (int)(stats[Stat.maxHP] * 0.008f);
        playerMPRegen = (int)(stats[Stat.maxHP] * 0.025f);
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
        actionCommands.Add(new ActionInfo(1, new FireballSpell(gameObject), ActionTargetType.NonSelf, globalCoolDownTime, globalCoolDownTime, 0.5f, 25f, 0f, 250,
            "불덩이", "25미터 이내 선택 대상에게 불덩이를 던진다."));

        // 2(저주) - Non-self, global
        actionCommands.Add(new ActionInfo(2, new CurseSpell(gameObject, 3), ActionTargetType.NonSelf, 0f, globalCoolDownTime, 0.5f, 25f, 0f, 500,
            "저주", "25미터 이내 선택 대상에게 저주를 내려 30초 동안 일정 시간 간격마다 대상 HP를 소량 감소한다."));

        // 3(HP 회복 능력) - Self, global
        hPHealAbility = new HPHealAbility(gameObject, 1, PlayerIStatChangeDisplay);
        actionCommands.Add(new ActionInfo(3, hPHealAbility, ActionTargetType.Self, 0f, globalCoolDownTime, 0.5f, 0f, 0f, 800,
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
    void RegenerateHPMP()
    {
        if (IsDead) return;

        if (anim.GetBool("Battle Pose On"))
        {
            IncreaseStat(Stat.hP, (int)(playerHPRegen * Random.Range(0.4f, 0.6f)), false);
            IncreaseStat(Stat.mP, (int)(playerMPRegen * Random.Range(0.4f, 0.6f)), false);
        }
        else
        {
            IncreaseStat(Stat.hP, (int)(playerHPRegen * Random.Range(0.9f, 1.1f)), false);
            IncreaseStat(Stat.mP, (int)(playerMPRegen * Random.Range(0.9f, 1.1f)), false);
        }

        if (hPHealAbility.IsBuffOn)
        {
            IncreaseStat(Stat.hP, stats[Stat.hPRestoringPower], true);
        }

        if (mPHealAbility.IsBuffOn)
        {
            IncreaseStat(Stat.mP, stats[Stat.mPRestoringPower], true);
        }
    }
}
