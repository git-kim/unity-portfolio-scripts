using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FluentBuilderPattern;

public class CurseSpell : NonSelfTargetedAction
{
    #region 버프/디버프 액션용 변수, 프로퍼티
    public int buffID;
    public bool IsBuffOn { get; protected set; } // 효과 적용 여부
    public bool IsActionUnusable { get; protected set; } // 액션 취하기 가능 여부(효과 중복 적용 방지용)
    public float EffectTime { get; protected set; } // 효과 적용 시간
    #endregion

    readonly IDamageable actorIDamageable;
    IDamageable targetIDamageable;
    IStatChangeDisplay targetIStatChangeDisplay;
    Transform targetTransform;

    MonoBehaviour targetMonoBehaviour;

    readonly ParticleEffectName particleEffectName;

    int mPCost;
    float range;

    public CurseSpell(GameObject actor, int buffID)
    {
        this.buffID = buffID;
        EffectTime = GAME.Buffs[buffID].effectTime;

        IsBuffOn = false;
        // IsActionUnusable = false;

        actorTransform = actor.transform;
        actorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        actorAnim = actor.GetComponent<Animator>();
        actorIActable = actor.GetComponent<IActable>();
        actorIDamageable = actor.GetComponent<IDamageable>();
        actorStats = actorIActable.Stats;

        particleEffectName = ParticleEffectName.CurseDebuff;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    IEnumerator TakeAction(int actionID, int actorID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        actorAnim.SetInteger("ActionMode", actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        actorIActable.ActionBeingTaken = actionID;

        actorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        actorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        AfflictWithDebuff();
        targetIStatChangeDisplay.ShowBuffStart(buffID, EffectTime);

        actorIDamageable.DecreaseStat(Stat.mP, mPCost, false, false);

        targetIDamageable.DecreaseStat(Stat.hP, Mathf.RoundToInt(actorStats[Stat.maxHP] * 0.004f), false);
        targetIStatChangeDisplay.ShowHPChange(Mathf.RoundToInt(actorStats[Stat.maxHP] * 0.004f), true, in actionName);
        targetIDamageable.UpdateStatBars();

        if (targetIDamageable is Enemy enemy && !actorIDamageable.IsDead)
        {
            enemy.IncreaseEnmity(actorID, 1);
        }

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (actorAnim.GetInteger("ActionMode") == actionID)
            actorAnim.SetInteger("ActionMode", 0); // ActionMode 값을 초기화한다.

        actorIActable.ActionBeingTaken = 0;
        IsActionUnusable = false;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        if (!targetIDamageable.IsDead && targetIDamageable.ActiveBuffEffects.ContainsKey(buffID))
            targetIStatChangeDisplay.ShowBuffEnd(buffID);

        RemoveDebuff();
        IsBuffOn = false;
    }

    override public void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        if (IsActionUnusable)
            return;

        // MP 검사
        if (mPCost > actorStats[Stat.mP])
        {
            GAME.ShowErrorMessage(0); // MP 부족 메시지 출력
            return;
        }

        // 대상 검사
        if (target is null)
        {
            GAME.ShowErrorMessage(3);
            return;
        }

        this.target = target;
        targetTransform = target.transform;
        targetIDamageable = target.GetComponent<IDamageable>();

        // 추가 검사(대상, 사용자)
        if (targetIDamageable is null || actorIDamageable.ID.Equals(targetIDamageable.ID))
        {
            GAME.ShowErrorMessage(2);
            return;
        }
        else if (actorIDamageable.IsDead || targetIDamageable.IsDead)
        {
            return;
        }

        range = actionInfo.range;

        // 거리 검사
        if (Vector3.SqrMagnitude(actorTransform.position - targetTransform.position) > range * range)
        {
            GAME.ShowErrorMessage(1); // 거리 초과 메시지 출력
            return;
        }

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        mPCost = actionInfo.mPCost;
        actionName = actionInfo.name;
        targetMonoBehaviour = target.GetComponent<MonoBehaviour>();

        if (targetMonoBehaviour is Enemy enemy)
            targetIStatChangeDisplay = enemy.EnemyIStatChangeDisplay;

        if (!IsBuffOn)
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, actorID, particleEffectName, targetTransform, Vector3.up * (targetTransform.lossyScale.y - 1f), Vector3.zero, targetTransform.localScale));
        else
        {
            if (!(CurrentActionCoroutine is null))
                actorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, actorID, particleEffectName, targetTransform, Vector3.up * (targetTransform.lossyScale.y - 1f), Vector3.zero, targetTransform.localScale));
        }
    }

    override public void Stop()
    {
        if (!IsBuffOn) return;

        if (!(CurrentActionCoroutine is null))
        {
            IsBuffOn = false;
            actorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = null;
        }
        actorIActable.IsCasting = false;
    }

    void AfflictWithDebuff()
    {
        if (!targetIDamageable.ActiveBuffEffects.ContainsKey(buffID))
        {
            targetIDamageable.ActiveBuffEffects.Add(buffID, new KeyValuePair<Stat, int>(Stat.hP, 50));
        }
    }

    void RemoveDebuff()
    {
        targetIDamageable.ActiveBuffEffects.Remove(buffID);
    }
}
