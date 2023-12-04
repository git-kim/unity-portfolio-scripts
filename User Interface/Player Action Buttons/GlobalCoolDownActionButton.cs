using System.Linq;
using UnityEngine;
using Managers;
using Characters.StatisticsScripts;
using Enums;

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
        manaPointsCost = playerActionHandler.CharacterActions[actionID].manaPointsCost;
        sqrRange = Mathf.Pow(playerActionHandler.CharacterActions[actionID].range, 2f);
    }

    public sealed override void React()
    {
        coolDownTimeIndicator.Set(playerActionHandler.VisibleGlobalCoolDownTime / playerActionHandler.GlobalCoolDownTime, false);
        if (gameManagerInstance.State != GameState.Running) disablenessIndicator.Enable();
        else if (playerActionHandler.VisibleGlobalCoolDownTime <= 0f) disablenessIndicator.Disable();
    }

    public sealed override void React2()
    {
        if (manaPointsCost == 0 || sqrRange == 0f) return;

        // Check Mana Points
        if (playerActionHandler.Stats[Stat.ManaPoints] < manaPointsCost)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;

        // Check Distance
        if (playerActionHandler.SqrDistanceFromCurrentTarget > sqrRange)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;
    }
}