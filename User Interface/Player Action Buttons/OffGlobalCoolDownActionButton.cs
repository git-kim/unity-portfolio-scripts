using System.Collections;
using System.Linq;
using UnityEngine;
using Managers;
using Characters.StatisticsScripts;
using Enums;

public class OffGlobalCoolDownActionButton : ActionButton
{
    private GameManager gameManagerInstance;
    private UIProgressBar coolDownTimeIndicator;
    private DisablenessIndicator disablenessIndicator;
    private UILabel manaPointsCostIndicator;
    private int manaPointsCost = 0;
    private float sqrRange = 0f;

    private float currentCoolDownTime;

    private float actionCoolDownTime;

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

    private void Start()
    {
        manaPointsCost = playerActionHandler.CharacterActions[actionID].manaPointsCost;
        sqrRange = Mathf.Pow(playerActionHandler.CharacterActions[actionID].range, 2f);

        currentCoolDownTime = 0f;
        actionCoolDownTime = playerActionHandler.CharacterActions[actionID].coolDownTime;
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
            disablenessIndicator.Enable();
        else disablenessIndicator.Disable();
    }

    public sealed override void React2()
    {
        if (manaPointsCost == 0 || sqrRange == 0f) return;

        // Check Mana Points
        if (playerActionHandler.Stats[Stat.ManaPoints] < manaPointsCost)
        {
            manaPointsCostIndicator.effectColor = UnusablenessColor;
            return;
        }
        else
        {
            manaPointsCostIndicator.effectColor = UsablenessColor;
        }

        // Check Distance
        if (playerActionHandler.SqrDistanceFromCurrentTarget > sqrRange)
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
