using Managers;
using UnityEngine;

public class UltimateActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar ultimateGauge;
    private DisablenessIndicator disablenessIndicator;
    private UISprite disablenessIndicatorSprite;
    private float sqrRange = 0f;

    void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        ultimateGauge = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        disablenessIndicatorSprite = disablenessIndicator.GetComponent<UISprite>();
    }
    void Start()
    {
        sqrRange = Mathf.Pow(playerActionHandler.CharacterActions[actionID].range, 2f);
    }

    public sealed override void React()
    {
        if (!gameObject.activeSelf) return;
        if (playerActionHandler.VisibleGlobalCoolDownTime > 0f || gameManagerInstance.State != GameState.Running)
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
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