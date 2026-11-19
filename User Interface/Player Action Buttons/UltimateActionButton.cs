using Enums;
using Managers;
using UnityEngine;
using UnityEngine.UI;

public class UltimateActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private DisablenessIndicator disablenessIndicator;
    private Image disablenessIndicatorImage;

    void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        disablenessIndicatorImage = disablenessIndicator.GetComponent<Image>();
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
            disablenessIndicatorImage.color = UnusablenessColor;
            return;
        }

        disablenessIndicatorImage.color = Color.white;
    }
}
