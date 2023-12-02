using System.Linq;
using UnityEngine;
using GameData;

public class GlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel manaPointsCostIndicator;
    private int manaPointsCost = 0;
    private float sqrRange = 0f;

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
    }

    private void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        manaPointsCost = PlayerActionCommands[actionID].manaPointsCost;
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
        if (manaPointsCost == 0 || sqrRange == 0f) return;

        // MP 검사
        if (Player.Stats[Stat.ManaPoints] < manaPointsCost)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;

        // 거리 검사
        if (Player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;
    }
}