using UnityEngine;
using System.Collections;
using GameData;
using Characters.Handlers;

public class HitPointsHealingAbility : SelfBuffingAction
{
    private readonly GameManager gameManagerInstance = GameManager.Instance;
    private int manaPointsCost;
    private readonly Statistics actorStats;
    private readonly StatChangeHandler actorStatChangeHandler;

    private string actionName;

    private float InvisibleGlobalCoolDownTime { get; set; }

    public HitPointsHealingAbility(GameObject actor, int buffID, IStatChangeDisplay actorIStatChangeDisplay)
    {
        BuffID = buffID;
        EffectTime = gameManagerInstance.Buffs[buffID].effectTime;

        IsBuffOn = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnim = actor.GetComponent<Animator>();
        ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
        actorStatChangeHandler = actor.GetComponent<StatChangeHandler>();
        actorStats = ActorActionHandler.Stats;
        this.ActorIStatChangeDisplay = actorIStatChangeDisplay;

        ParticleEffectName = ParticleEffectName.HealHP;
    }

    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName,
        Transform targetTransform, Vector3 localPosition,
        Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorAnim.SetInteger(ActionMode, actionID);

        ActorActionHandler.ActionBeingTaken = actionID;

        ActorActionHandler.VisibleGlobalCoolDownTime = CoolDownTime;
        ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        actorStatChangeHandler.DecreaseStat(Stat.ManaPoints, manaPointsCost);

        var hitPointsIncrement = Mathf.RoundToInt(actorStats[Stat.MaximumHitPoints] * 0.04f);
        actorStatChangeHandler.IncreaseStat(Stat.HitPoints, hitPointsIncrement);
        ActorIStatChangeDisplay.ShowHitPointsChange(hitPointsIncrement, false, in actionName);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (ActorAnim.GetInteger(ActionMode) == actionID)
            ActorAnim.SetInteger(ActionMode, 0);

        ActorActionHandler.ActionBeingTaken = 0;
        IsActionUnusable = false;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        ActorIStatChangeDisplay.ShowBuffEnd(BuffID);
        IsBuffOn = false;
    }

    public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
    {
        if (IsActionUnusable)
            return;

        // Check Mana Points
        if (manaPointsCost > actorStats[Stat.ManaPoints])
        {
            gameManagerInstance.ShowErrorMessage(0);
            return;
        }

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        manaPointsCost = actionInfo.manaPointsCost;
        actionName = actionInfo.name;

        if (IsBuffOn && CurrentActionCoroutine != null)
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);

        CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(
            TakeAction(actionInfo.id, ParticleEffectName, ActorTransform,
            Vector3.up * 0.1f, Vector3.zero, Vector3.one));
    }

    public override void Stop()
    {
        if (!IsBuffOn) return;

        if (CurrentActionCoroutine != null)
        {
            IsBuffOn = false;
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = null;
        }
        ActorActionHandler.IsCasting = false;
    }
}