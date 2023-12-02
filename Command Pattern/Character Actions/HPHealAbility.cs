using UnityEngine;
using System.Collections;
using GameData;
using Characters.Components;

public class HPHealAbility : SelfBuffingAction
{
    private readonly GameManager gameManagerInstance = GameManager.Instance;
    private int manaPointsCost;
    private readonly Statistics actorStats;
    private readonly StatChangeable actorStatChangeable;

    private string actionName;

    private float InvisibleGlobalCoolDownTime { get; set; }

    public HPHealAbility(GameObject actor, int buffID, IStatChangeDisplay actorIStatChangeDisplay)
    {
        BuffID = buffID;
        EffectTime = gameManagerInstance.Buffs[buffID].effectTime;

        IsBuffOn = false;
        // IsActionUnusable = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnim = actor.GetComponent<Animator>();
        ActorIActable = actor.GetComponent<IActable>();
        actorStatChangeable = actor.GetComponent<StatChangeable>();
        actorStats = ActorIActable.Stats;
        this.ActorIStatChangeDisplay = actorIStatChangeDisplay;

        ParticleEffectName = ParticleEffectName.HealHP;
    }

    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName,
        Transform targetTransform, Vector3 localPosition,
        Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorAnim.SetInteger(ActionMode, actionID);

        ActorIActable.ActionBeingTaken = actionID;

        ActorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        ActorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        actorStatChangeable.DecreaseStat(Stat.ManaPoints, manaPointsCost);

        var hitPointsIncrement = Mathf.RoundToInt(actorStats[Stat.MaximumHitPoints] * 0.04f);
        actorStatChangeable.IncreaseStat(Stat.HitPoints, hitPointsIncrement);
        ActorIStatChangeDisplay.ShowHitPointsChange(hitPointsIncrement, false, in actionName);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (ActorAnim.GetInteger(ActionMode) == actionID)
            ActorAnim.SetInteger(ActionMode, 0);

        ActorIActable.ActionBeingTaken = 0;
        IsActionUnusable = false;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        ActorIStatChangeDisplay.ShowBuffEnd(BuffID);
        IsBuffOn = false;
    }

    public override void Execute(int actorID, GameObject target, ActionInfo actionInfo)
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
        manaPointsCost = actionInfo.mPCost;
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
        ActorIActable.IsCasting = false;
    }
}