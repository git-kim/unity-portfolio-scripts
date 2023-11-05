using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluentBuilderPattern;

public class OffGlobalCoolDownActionButton : ActionButton
{
    GameManager GAME;
    UIProgressBar coolDownTimeIndicator;
    DisablenessIndicator disablenessIndicator;
    UILabel mPCostIndicator;
    int mPCost = 0;
    float sqrRange = 0f;

    float currentCoolDownTime;

    //[Tooltip("액션 재사용 대기 시간")] [SerializeField]
    float actionCoolDownTime;

    Coroutine coolDownCoroutine;

    [Tooltip("글로벌 액션 재사용 대기 중에 사용이 가능한가?")] [SerializeField]
    bool isUsableDuringGlobalCoolDown;

    void Awake()
    {
        GAME = GameManager.Instance;
        coolDownTimeIndicator = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);

        foreach (UILabel mPCostIndicator in GetComponentsInChildren<UILabel>(true).Where(mPCostIndicator => mPCostIndicator.name.Contains("MP")))
        {
            this.mPCostIndicator = mPCostIndicator;
            break;
        }

        coolDownCoroutine = null;
    }

    void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        mPCost = playerActionCommands[actionID].mPCost;
        sqrRange = Mathf.Pow(playerActionCommands[actionID].range, 2f);

        currentCoolDownTime = 0f;
        actionCoolDownTime = player.ActionCommands[actionID].coolDownTime;
    }

    sealed override public void React()
    {
        if (currentCoolDownTime > 0f)
            coolDownTimeIndicator.Set(currentCoolDownTime / actionCoolDownTime, false);
        else
            coolDownTimeIndicator.Set(0f, false);

        if ((player.VisibleGlobalCoolDownTime > 0f && !isUsableDuringGlobalCoolDown)
            || GAME.State != GameState.Running
            || player.IsCasting)
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
    }

    sealed override public void React2()
    {
        if (mPCost == 0 || sqrRange == 0f) return;

        // MP 검사
        if (player.Stats[Stat.mP] < mPCost)
        {
            mPCostIndicator.effectColor = unusablenessColor;
            return;
        }
        else
        {
            mPCostIndicator.effectColor = usablenessColor;
        }

        // 거리 검사
        if (player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            mPCostIndicator.effectColor = unusablenessColor;
            return;
        }
        else
        {
            mPCostIndicator.effectColor = usablenessColor;
        }
    }

    public void StartCoolDown()
    {
        currentCoolDownTime = actionCoolDownTime;

        if (!(coolDownCoroutine is null))
            StopCoroutine(coolDownCoroutine);

        coolDownCoroutine = StartCoroutine(UpdateCoolDown());
    }

    IEnumerator UpdateCoolDown()
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
        if (!(coolDownCoroutine is null))
            StopCoroutine(coolDownCoroutine);
    }
}
