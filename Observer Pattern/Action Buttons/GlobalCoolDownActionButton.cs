using System.Linq;
using UnityEngine;
using FluentBuilderPattern;

public class GlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel mPCostIndicator;
    private int mPCost = 0;
    private float sqrRange = 0f;

    private void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        coolDownTimeIndicator = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);

        foreach (var indicator in GetComponentsInChildren<UILabel>(true)
                     .Where(indicator => indicator.name.Contains("MP")))
        {
            mPCostIndicator = indicator;
            break;
        }
    }

    private void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        mPCost = PlayerActionCommands[actionID].mPCost;
        sqrRange = Mathf.Pow(PlayerActionCommands[actionID].range, 2f);
    }

    public sealed override void React()
    {
        coolDownTimeIndicator.Set(Player.VisibleGlobalCoolDownTime / Player.GlobalCoolDownTime, false);
        if (gameManagerInstance.State != GameState.Running) disablenessIndicator.Enable();
        else if (Player.VisibleGlobalCoolDownTime <= 0f) disablenessIndicator.Disable();
    }

    public sealed override void React2()
    {
        if (mPCost == 0 || sqrRange == 0f) return;

        // MP 검사
        if (Player.Stats[Stat.MP] < mPCost)
        {
            mPCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        mPCostIndicator.effectColor = UsablenessColor;

        // 거리 검사
        if (Player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            mPCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        mPCostIndicator.effectColor = UsablenessColor;
    }
}