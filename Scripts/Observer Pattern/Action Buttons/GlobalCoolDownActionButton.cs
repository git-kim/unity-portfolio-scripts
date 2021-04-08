using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluentBuilderPattern;

public class GlobalCoolDownActionButton : ActionButton
{
    GameManager GAME;
    UIProgressBar coolDownTimeIndicator;
    DisablenessIndicator disablenessIndicator;
    UILabel mPCostIndicator;
    int mPCost = 0;
    float sqrRange = 0f;

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
    }

    void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        mPCost = playerActionCommands[actionID].mPCost;
        sqrRange = Mathf.Pow(playerActionCommands[actionID].range, 2f);
    }

    sealed override public void React()
    {
        coolDownTimeIndicator.Set(player.VisibleGlobalCoolDownTime / player.GlobalCoolDownTime, false);
        if (GAME.State != GameState.Running) disablenessIndicator.Enable();
        else if (player.VisibleGlobalCoolDownTime <= 0f) disablenessIndicator.Disable();
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
}
