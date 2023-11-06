using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FluentBuilderPattern;

public class CurseSpell : NonSelfTargetedAction
{
    #region 버프/디버프 액션용 변수, 프로퍼티
    private readonly int buffID;
    private bool IsBuffOn { get; set; } // 효과 적용 여부
    private bool IsActionUnusable { get; set; } // 액션 취하기 가능 여부(효과 중복 적용 방지용)
    private float EffectTime { get; set; } // 효과 적용 시간
    #endregion

    private readonly IDamageable actorIDamageable;
    private IDamageable targetIDamageable;
    private IStatChangeDisplay targetIStatChangeDisplay;
    private Transform targetTransform;

    private MonoBehaviour targetMonoBehaviour;

    private readonly ParticleEffectName particleEffectName;

    private int mPCost;
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
        actorIDamageable = actor.GetComponent<IDamageable>();
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

        actorIDamageable.DecreaseStat(Stat.MP, mPCost, false, false);

        targetIDamageable.DecreaseStat(Stat.HP, Mathf.RoundToInt(ActorStats[Stat.MaxHP] * 0.004f), false);
        targetIStatChangeDisplay.ShowHPChange(Mathf.RoundToInt(ActorStats[Stat.MaxHP] * 0.004f), true, in ActionName);
        targetIDamageable.UpdateStatBars();

        if (targetIDamageable is Enemy enemy && !actorIDamageable.IsDead)
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

        if (!targetIDamageable.IsDead && targetIDamageable.ActiveBuffEffects.ContainsKey(buffID))
            targetIStatChangeDisplay.ShowBuffEnd(buffID);

        RemoveDebuff();
        IsBuffOn = false;
    }

    public override void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        if (IsActionUnusable)
            return;

        // MP 검사
        if (mPCost > ActorStats[Stat.MP])
        {
            GameManagerInstance.ShowErrorMessage(0); // MP 부족 메시지 출력
            return;
        }

        // 대상 검사
        if (target == null)
        {
            GameManagerInstance.ShowErrorMessage(3);
            return;
        }

        this.Target = target;
        targetTransform = target.transform;
        targetIDamageable = target.GetComponent<IDamageable>();

        // 추가 검사(대상, 사용자)
        if (targetIDamageable == null || actorIDamageable.Identifier.Equals(targetIDamageable.Identifier))
        {
            GameManagerInstance.ShowErrorMessage(2);
            return;
        }

        if (actorIDamageable.IsDead || targetIDamageable.IsDead)
        {
            return;
        }

        range = actionInfo.range;

        // 거리 검사
        if (Vector3.SqrMagnitude(ActorTransform.position - targetTransform.position) > range * range)
        {
            GameManagerInstance.ShowErrorMessage(1); // 거리 초과 메시지 출력
            return;
        }

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        mPCost = actionInfo.mPCost;
        ActionName = actionInfo.name;
        targetMonoBehaviour = target.GetComponent<MonoBehaviour>();

        if (targetMonoBehaviour is Enemy enemy)
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
        if (!targetIDamageable.ActiveBuffEffects.ContainsKey(buffID))
        {
            targetIDamageable.ActiveBuffEffects.Add(buffID, new KeyValuePair<Stat, int>(Stat.HP, 50));
        }
    }

    private void RemoveDebuff()
    {
        targetIDamageable.ActiveBuffEffects.Remove(buffID);
    }
}
