using Characters.StatisticsScripts;
using Enums;
using Managers;
using System.Linq;
using TMPro;
using UserInterface;

public class GlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private ProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private TMP_Text manaPointsCostIndicator;

    private void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        coolDownTimeIndicator = GetComponentInChildren<ProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        manaPointsCostIndicator = GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(indicator => indicator.name.Contains("MP"));
    }

    public sealed override void React()
    {
        coolDownTimeIndicator.Value = playerActionHandler.VisibleGlobalCoolDownTime / playerActionHandler.GlobalCoolDownTime;
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
            manaPointsCostIndicator.outlineColor = UnusablenessColor;
            return;
        }

        // Check Distance
        if (sqrRange > 0 && playerActionHandler.SqrDistanceFromCurrentTarget > sqrRange)
        {
            manaPointsCostIndicator.outlineColor = UnusablenessColor;
            return;
        }

        manaPointsCostIndicator.outlineColor = UsablenessColor;
    }
}
