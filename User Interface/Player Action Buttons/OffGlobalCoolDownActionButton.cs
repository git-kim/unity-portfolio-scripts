using Characters.Handlers;
using Characters.StatisticsScripts;
using Enums;
using Managers;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UserInterface;

public class OffGlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private ProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private TMP_Text manaPointsCostIndicator;

    private float actionCoolDownTime;
    private float currentCoolDownTime;


    private Coroutine coolDownCoroutine;

    [SerializeField] private bool isUsableDuringGlobalCoolDown;

    private void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        coolDownTimeIndicator = GetComponentInChildren<ProgressBar>(true);
        disablenessIndicator = GetComponentInChildren<DisablenessIndicator>(true);
        manaPointsCostIndicator = GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(indicator => indicator.name.Contains("MP"));

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
            coolDownTimeIndicator.Value = currentCoolDownTime / actionCoolDownTime;
        else
            coolDownTimeIndicator.Value = 0f;

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
