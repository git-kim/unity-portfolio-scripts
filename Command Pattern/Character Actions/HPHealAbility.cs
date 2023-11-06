using UnityEngine;
using System.Collections;
using FluentBuilderPattern;

public class HPHealAbility : SelfBuffingAction
{
    private readonly GameManager gameManagerInstance = GameManager.Instance;
    private int mPCost;
    private readonly Statistics actorStats;
    private readonly IDamageable actorIDamageable;

    private string actionName;

    //// 캐스팅 시간
    //public float CastTime { get; protected set; }

    // 캐스팅 후 애니메이션 또는 파티클 효과 재생 시간
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
        actorIDamageable = actor.GetComponent<IDamageable>();
        actorStats = ActorIActable.Stats;
        this.ActorIStatChangeDisplay = actorIStatChangeDisplay;

        ParticleEffectName = ParticleEffectName.HealHP;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorAnim.SetInteger(ActionMode, actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        ActorIActable.ActionBeingTaken = actionID;

        ActorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        ActorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        actorStats[Stat.MP] -= mPCost;
        actorIDamageable.IncreaseStat(Stat.HP, Mathf.RoundToInt(actorStats[Stat.MaxHP] * 0.04f), false);
        ActorIStatChangeDisplay.ShowHPChange(Mathf.RoundToInt(actorStats[Stat.MaxHP] * 0.04f), false, in actionName);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (ActorAnim.GetInteger(ActionMode) == actionID)
            ActorAnim.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다.

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

        // MP 검사
        if (mPCost > actorStats[Stat.MP])
        {
            gameManagerInstance.ShowErrorMessage(0); // MP 부족 메시지 출력
            return;
        }

        CoolDownTime = actionInfo.coolDownTime;
        //CastTime = actionInfo.castTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        mPCost = actionInfo.mPCost;
        actionName = actionInfo.name;

        if (!IsBuffOn)
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, ParticleEffectName, ActorTransform, Vector3.up * 0.1f, Vector3.zero, Vector3.one));
        else
        {
            if (CurrentActionCoroutine != null)
                ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, ParticleEffectName, ActorTransform, Vector3.up * 0.1f, Vector3.zero, Vector3.one));
        }
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