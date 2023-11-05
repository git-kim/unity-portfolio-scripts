using UnityEngine;
using System.Collections;
using FluentBuilderPattern;
using System.Linq;

public class MPHealAbility : SelfBuffingAction
{
    readonly GameManager GAME = GameManager.Instance;
    //readonly Statistics actorStats;
    //readonly IDamageable actorIDamageable;

    // string actionName;

    // 캐스팅 시간
    public float CastTime { get; protected set; }

    // 캐스팅 후 애니메이션 또는 파티클 효과 재생 시간
    public float InvisibleGlobalCoolDownTime { get; protected set; }

    readonly OffGlobalCoolDownActionButton button;

    public MPHealAbility(GameObject actor, int buffID, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
    {
        this.buffID = buffID;
        EffectTime = GAME.Buffs[buffID].effectTime;

        IsBuffOn = false;
        IsActionUnusable = false;

        actorTransform = actor.transform;
        actorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        actorAnim = actor.GetComponent<Animator>();
        actorIActable = actor.GetComponent<IActable>();
        //actorIDamageable = actor.GetComponent<IDamageable>();
        //actorStats = actorIActable.Stats;
        this.actorIStatChangeDisplay = actorIStatChangeDisplay;

        this.button = button;

        particleEffectName = ParticleEffectName.HealMP;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        //if (IsActionUnusable)
        //    yield break;

        actorAnim.SetInteger("ActionMode", actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        actorIActable.ActionBeingTaken = actionID;

        actorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        button.StartCoolDown();
        actorIStatChangeDisplay.ShowBuffStart(buffID, EffectTime);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (actorAnim.GetInteger("ActionMode") == actionID)
            actorAnim.SetInteger("ActionMode", 0); // ActionMode 값을 초기화한다.

        actorIActable.ActionBeingTaken = 0;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        actorIStatChangeDisplay.ShowBuffEnd(buffID);
        IsBuffOn = false;

        yield return new WaitForSeconds(CoolDownTime - EffectTime - InvisibleGlobalCoolDownTime);

        IsActionUnusable = false;
    }

    override public void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        if (IsActionUnusable)
            return;

        CoolDownTime = actionInfo.coolDownTime;
        CastTime = actionInfo.castTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        //actionName = actionInfo.name;

        if (!IsBuffOn)
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, particleEffectName, actorTransform, Vector3.up * 0.1f, Vector3.zero, Vector3.one));
        else
        {
            if (!(CurrentActionCoroutine is null))
                actorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, particleEffectName, actorTransform, Vector3.up * 0.1f, Vector3.zero, Vector3.one));
        }
    }

    override public void Stop()
    {
        if (!IsBuffOn) return;

        if (!(CurrentActionCoroutine is null))
        {
            IsBuffOn = false;
            IsActionUnusable = false;
            actorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            button.StopCoolDown();
            CurrentActionCoroutine = null;
        }
        actorIActable.IsCasting = false;
    }
}
