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
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        sqrRange = Mathf.Pow(PlayerActionCommands[actionID].range, 2f);
    }

    public sealed override void React()
    {
        if (!gameObject.activeSelf) return;
        if (Player.VisibleGlobalCoolDownTime > 0f || gameManagerInstance.State != GameState.Running)
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
    }

    public sealed override void React2()
    {
        if (!gameObject.activeSelf) return;
        if (sqrRange == 0f) return;

        // 거리 검사
        if (Player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            disablenessIndicatorSprite.color = UnusablenessColor;
            return;
        }

        disablenessIndicatorSprite.color = Color.white;
    }
}