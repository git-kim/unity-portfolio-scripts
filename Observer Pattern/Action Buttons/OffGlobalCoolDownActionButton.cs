using System.Collections;
using System.Linq;
using UnityEngine;
using GameData;

public class OffGlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel manaPointsCostIndicator;
    private int manaPointsCost = 0;
    private float sqrRange = 0f;

    private float currentCoolDownTime;

    //[Tooltip("액션 재사용 대기 시간")] [SerializeField]
    private float actionCoolDownTime;

    private Coroutine coolDownCoroutine;

    [Tooltip("글로벌 액션 재사용 대기 중에 사용이 가능한가?")] [SerializeField]
    private bool isUsableDuringGlobalCoolDown;

    private void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        coolDownTimeIndicator = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);

        foreach (var indicator in GetComponentsInChildren<UILabel>(true)
                     .Where(indicator => indicator.name.Contains("MP")))
        {
            manaPointsCostIndicator = indicator;
            break;
        }

        coolDownCoroutine = null;
    }

    private void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        manaPointsCost = PlayerActionCommands[actionID].manaPointsCost;
        sqrRange = Mathf.Pow(PlayerActionCommands[actionID].range, 2f);

        currentCoolDownTime = 0f;
        actionCoolDownTime = Player.CharacterActions[actionID].coolDownTime;
    }

    public sealed override void React()
    {
        if (currentCoolDownTime > 0f)
            coolDownTimeIndicator.Set(currentCoolDownTime / actionCoolDownTime, false);
        else
            coolDownTimeIndicator.Set(0f, false);

        if ((Player.VisibleGlobalCoolDownTime > 0f && !isUsableDuringGlobalCoolDown)
            || gameManagerInstance.State != GameState.Running
            || Player.IsCasting)
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
    }

    public sealed override void React2()
    {
        if (manaPointsCost == 0 || sqrRange == 0f) return;

        // MP 검사
        if (Player.Stats[Stat.ManaPoints] < manaPointsCost)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }
        else
        {
            manaPointsCostIndicator.effectColor = UsablenessColor;
        }

        // 거리 검사
        if (Player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;
    }

    public void StartCoolDown()
    {
        currentCoolDownTime = actionCoolDownTime;

        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);

        coolDownCoroutine = StartCoroutine(UpdateCoolDown());
    }

    private IEnumerator UpdateCoolDown()
    {
        while (currentCoolDownTime > 0f)
        {
            if (currentCoolDownTime <= 0f)
            {
                currentCoolDownTime = 0f;
                break;
            }

            yield return null;
            currentCoolDownTime -= Time.deltaTime;
        }
    }

    public void StopCoolDown()
    {
        currentCoolDownTime = 0f;
        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);
    }
}
