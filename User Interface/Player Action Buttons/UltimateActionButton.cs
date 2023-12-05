using Enums;
using Managers;
using UnityEngine;

public class UltimateActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar ultimateGauge;
    private DisablenessIndicator disablenessIndicator;
    private UISprite disablenessIndicatorSprite;

    void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        ultimateGauge = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        disablenessIndicatorSprite = disablenessIndicator.GetComponent<UISprite>();
    }

    public sealed override void React()
    {
        if (!gameObject.activeSelf) return;
        if (playerActionHandler.VisibleGlobalCoolDownTime > 0f ||
            gameManagerInstance.State != GameState.Running)
            disablenessIndicator.SetActive(true);
        else
            disablenessIndicator.SetActive(false);
    }

    public sealed override void React2()
    {
        if (!gameObject.activeSelf) return;
        if (sqrRange == 0f) return;

        // Check Distance
        if (playerActionHandler.SqrDistanceFromCurrentTarget > sqrRange)
        {
            disablenessIndicatorSprite.color = UnusablenessColor;
            return;
        }

        disablenessIndicatorSprite.color = Color.white;
    }
}