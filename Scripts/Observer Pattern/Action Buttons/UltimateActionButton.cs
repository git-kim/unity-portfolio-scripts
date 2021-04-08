using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ObserverPattern;

public class UltimateActionButton : ActionButton
{
    GameManager GAME;
    UIProgressBar ultimateGauge;
    DisablenessIndicator disablenessIndicator;
    UISprite disablenessIndicatorSprite;
    float sqrRange = 0f;

    void Awake()
    {
        GAME = GameManager.Instance;
        ultimateGauge = GetComponentInChildren<UIProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        disablenessIndicatorSprite = disablenessIndicator.GetComponent<UISprite>();
    }
    void Start()
    {
        if (!HasPlayerVariablesBeenSet)
            SetPlayerVariables();

        sqrRange = Mathf.Pow(playerActionCommands[actionID].range, 2f);
    }

    sealed override public void React()
    {
        if (!gameObject.activeSelf) return;
        if (player.VisibleGlobalCoolDownTime > 0f || GAME.State != GameState.Running)
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
    }

    sealed override public void React2()
    {
        if (!gameObject.activeSelf) return;
        if (sqrRange == 0f) return;

        // 거리 검사
        if (player.SqrDistanceFromCurrentTarget > sqrRange)
        {
            disablenessIndicatorSprite.color = unusablenessColor;
            return;
        }
        else
        {
            disablenessIndicatorSprite.color = Color.white;
        }
    }
}
