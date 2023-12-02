using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameData;
using Characters.Components;

public class CurseSpell : NonSelfTargetedAction
{
    private readonly int buffID;
    private bool IsBuffOn { get; set; }
    private bool IsActionUnusable { get; set; }
    private float EffectTime { get; set; }

    private readonly StatChangeable actorStatChangeable;
    private StatChangeable targetStatChangeable;
    private IStatChangeDisplay targetIStatChangeDisplay;
    private Transform targetTransform;

    private readonly ParticleEffectName particleEffectName;

    private int manaPointsCost;
    private float range;

    public CurseSpell(GameObject actor, int buffID)
    {
        this.buffID = buffID;
        EffectTime = GameManagerInstance.Buffs[buffID].effectTime;

        IsBuffOn = false;
        // IsActionUnusable = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnimator = actor.GetComponent<Animator>();
        ActorIActable = actor.GetComponent<IActable>();
        actorStatChangeable = actor.GetComponent<StatChangeable>();
        ActorStats = ActorIActable.Stats;

        particleEffectName = ParticleEffectName.CurseDebuff;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    private IEnumerator TakeAction(int actionID, int actorID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorAnimator.SetInteger(ActionMode, actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        ActorIActable.ActionBeingTaken = actionID;

        ActorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        ActorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        AfflictWithDebuff();
        targetIStatChangeDisplay.ShowBuffStart(buffID, EffectTime);

        actorStatChangeable.DecreaseStat(Stat.ManaPoints, manaPointsCost);

        var effectiveDamage = Mathf.RoundToInt(ActorStats[Stat.MagicAttack] * 0.25f);
        targetStatChangeable.DecreaseStat(Stat.HitPoints, effectiveDamage);
        targetIStatChangeDisplay.ShowHitPointsChange(effectiveDamage, true, in ActionName);

        if (targetStatChangeable.TryGetComponent<Enemy>(out var enemy)
            && !actorStatChangeable.HasZeroHitPoints)
        {
            enemy.IncreaseEnmity(actorID, 1);
        }

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (ActorAnimator.GetInteger(ActionMode) == actionID)
            ActorAnimator.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다.

        ActorIActable.ActionBeingTaken = 0;
        IsActionUnusable = false;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        if (!targetStatChangeable.HasZeroHitPoints
            && targetStatChangeable.ActiveStatChangingEffects.ContainsKey(buffID))
            targetIStatChangeDisplay.ShowBuffEnd(buffID);

        RemoveDebuff();
        IsBuffOn = false;
    }

    public override void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        if (IsActionUnusable)
            return;

        // Check Mana Points
        if (manaPointsCost > ActorStats[Stat.ManaPoints])
        {
            GameManagerInstance.ShowErrorMessage(0);
            return;
        }

        // Check Target
        if (target == null)
        {
            GameManagerInstance.ShowErrorMessage(3);
            return;
        }

        this.Target = target;
        targetTransform = target.transform;
        targetStatChangeable = target.GetComponent<StatChangeable>();

        // Check Others
        if (targetStatChangeable == null
            || actorStatChangeable.Identifier.Equals(targetStatChangeable.Identifier))
        {
            GameManagerInstance.ShowErrorMessage(2);
            return;
        }

        if (actorStatChangeable.HasZeroHitPoints || targetStatChangeable.HasZeroHitPoints)
        {
            return;
        }

        range = actionInfo.range;

        // Check Distance
        if (Vector3.SqrMagnitude(ActorTransform.position - targetTransform.position) > range * range)
        {
            GameManagerInstance.ShowErrorMessage(1);
            return;
        }

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        manaPointsCost = actionInfo.mPCost;
        ActionName = actionInfo.name;

        if (target.TryGetComponent<Enemy>(out var enemy))
            targetIStatChangeDisplay = enemy.EnemyIStatChangeDisplay;

        if (!IsBuffOn)
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, actorID, particleEffectName, targetTransform, Vector3.up * (targetTransform.lossyScale.y - 1f), Vector3.zero, targetTransform.localScale));
        else
        {
            if (!(CurrentActionCoroutine == null))
                ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, actorID, particleEffectName, targetTransform, Vector3.up * (targetTransform.lossyScale.y - 1f), Vector3.zero, targetTransform.localScale));
        }
    }

    public override void Stop()
    {
        if (!IsBuffOn) return;

        if (!(CurrentActionCoroutine == null))
        {
            IsBuffOn = false;
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = null;
        }
        ActorIActable.IsCasting = false;
    }

    private void AfflictWithDebuff()
    {
        if (!targetStatChangeable.ActiveStatChangingEffects.ContainsKey(buffID))
        {
            targetStatChangeable.ActiveStatChangingEffects.Add(buffID,
                new KeyValuePair<Stat, int>(Stat.HitPoints, 40));
        }
    }

    private void RemoveDebuff()
    {
        targetStatChangeable.ActiveStatChangingEffects.Remove(buffID);
    }
}