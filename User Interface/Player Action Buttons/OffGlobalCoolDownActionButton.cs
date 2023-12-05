using Characters.Handlers;
using Characters.StatisticsScripts;
using Enums;
using Managers;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OffGlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel manaPointsCostIndicator;

    private float actionCoolDownTime;
    private float currentCoolDownTime;


    private Coroutine coolDownCoroutine;

    [SerializeField] private bool isUsableDuringGlobalCoolDown;

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

        coolDownCoroutine = null;
    }

    public override void Initialize(UnityAction onPressed,
        CharacterAction characterAction, PlayerActionHandler playerActionHandler)
    {
        base.Initialize(onPressed, characterAction, playerActionHandler);

        currentCoolDownTime = 0f;
        actionCoolDownTime = characterAction.coolDownTime;
    }

    public sealed override void React()
    {
        if (currentCoolDownTime > 0f)
            coolDownTimeIndicator.Set(currentCoolDownTime / actionCoolDownTime, false);
        else
            coolDownTimeIndicator.Set(0f, false);

        if ((playerActionHandler.VisibleGlobalCoolDownTime > 0f && !isUsableDuringGlobalCoolDown)
            || gameManagerInstance.State != GameState.Running
            || playerActionHandler.IsCasting)
            disablenessIndicator.SetActive(true);
        else
            disablenessIndicator.SetActive(false);
    }

    public sealed override void React2()
    {
        if (manaPointsCostIndicator == null)
            return;

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

    public void StartCoolDown()
    {
        currentCoolDownTime = actionCoolDownTime;

        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);

        coolDownCoroutine = StartCoroutine(UpdateCoolDown());
    }

    private IEnumerator UpdateCoolDown()
    {
        while (currentCoolDownTime > 0f)
        {
            if (currentCoolDownTime <= 0f)
            {
                currentCoolDownTime = 0f;
                break;
            }

            yield return null;
            currentCoolDownTime -= Time.deltaTime;
        }
    }

    public void StopCoolDown()
    {
        currentCoolDownTime = 0f;
        if (coolDownCoroutine != null)
            StopCoroutine(coolDownCoroutine);
    }
}
