using Characters.StatisticsScripts;
using Enums;
using Managers;
using System.Linq;

public class GlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel manaPointsCostIndicator;

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

    public sealed override void React()
    {
        coolDownTimeIndicator.Set(playerActionHandler.VisibleGlobalCoolDownTime / playerActionHandler.GlobalCoolDownTime, false);
        if (gameManagerInstance.State != GameState.Running)
            disablenessIndicator.SetActive(true);
        else if (playerActionHandler.VisibleGlobalCoolDownTime <= 0f)
            disablenessIndicator.SetActive(false);
    }

    public sealed override void React2()
    {
        // Check Mana Points
        if (manaPointsCost > 0 && playerActionHandler.Stats[Stat.ManaPoints] < manaPointsCost)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        // Check Distance
        if (sqrRange > 0 && playerActionHandler.SqrDistanceFromCurrentTarget > sqrRange)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.effectColor = UsablenessColor;
    }
}