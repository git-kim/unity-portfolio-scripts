using System;
using UnityEngine;
using System.Linq;

public class PlayerCastingBarDisplay : CastingBarDisplay
{
    private IActable playerIActable;
    private ActionButtons actionButtons;
    private UIPanel castingBarPanel;
    private UISprite actionIconSprite, castingBarActionIconSprite;
    private UIProgressBar castingBar;
    private UILabel timeLabel;
    private TweenAlpha timeLabelTween;
    private bool shouldShowCastingBar = false;
    public bool IsShown => Math.Abs(castingBarPanel.alpha - 1f) < float.Epsilon;
    private float castTime, remainingCastTime;

    private void Awake()
    {
        foreach (UISprite sprite in gameObject.GetComponentsInChildren<UISprite>().Where(sprite => sprite.name.Contains("Icon")))
        {
            castingBarActionIconSprite = sprite;
            break;
        }

        castingBarPanel = gameObject.GetComponentInParent<UIPanel>();
        castingBar = gameObject.GetComponent<UIProgressBar>();
        timeLabel = gameObject.GetComponentInChildren<UILabel>();
        timeLabelTween = timeLabel.GetComponent<TweenAlpha>();
    }

    private void Start()
    {
        actionButtons = FindObjectOfType<ActionButtons>();
        playerIActable = FindObjectOfType<Player>().GetComponent<IActable>();
        castingBarPanel.alpha = 0f;
    }

    public override void ShowCastingBar(int actionID, float castTime)
    {
        actionIconSprite = actionButtons.GetActionIcon(actionID);
        castingBarActionIconSprite.spriteName = actionIconSprite.spriteName;
        castingBarActionIconSprite.color = actionIconSprite.color;

        this.castTime = remainingCastTime = castTime;
        castingBar.value = 1f;
        timeLabel.text = playerIActable.VisibleGlobalCoolDownTime.ToString("0.0");

        timeLabelTween.ResetToBeginning();
        timeLabelTween.enabled = false;

        shouldShowCastingBar = true;
        castingBarPanel.alpha = 1f;
    }

    public override void StopShowingCastingBar()
    {
        timeLabel.text = "중단";
        timeLabelTween.enabled = true;
        timeLabelTween.PlayForward();
    }

    private void HideCastingBar()
    {
        castTime = 0f;
        shouldShowCastingBar = false;
        castingBarPanel.alpha = 0f;
    }

    private void Update()
    {
        if (!shouldShowCastingBar) return;
        if (!timeLabelTween.enabled)
        {
            if (!playerIActable.IsCasting) // 주의: VisibleGlobalCoolDownTime이 0f인지로 판단하면 연속 액션 사용 시 실제 액션을 취하지는 않지만 캐스팅 바 값이 계속 변하는 버그가 발생할 수 있다.
            {
                HideCastingBar();
            }
            else
            {
                remainingCastTime = Mathf.Max(0f, remainingCastTime - Time.deltaTime);
                castingBar.value = (castTime - remainingCastTime) / castTime;
            }

            timeLabel.text = playerIActable.VisibleGlobalCoolDownTime.ToString("0.0");
        }
        else castingBar.value = 0f;
    }
}