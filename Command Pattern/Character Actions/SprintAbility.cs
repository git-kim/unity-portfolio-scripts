using UnityEngine;
using CommandPattern;
using System.Linq;
using System.Collections;

public class SprintAbility : SelfBuffingAction
{
    readonly OffGlobalCoolDownActionButton button;

    public float InvisibleGlobalCoolDownTime { get; protected set; }

    public SprintAbility(GameObject actor, int buffID, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
    {
        this.buffID = buffID;

        IsBuffOn = false;
        IsActionUnusable = false;

        actorTransform = actor.transform;
        actorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        actorAnim = actor.GetComponent<Animator>();
        actorIActable = actor.GetComponent<IActable>();
        this.actorIStatChangeDisplay = actorIStatChangeDisplay;

        this.button = button;

        particleEffectName = ParticleEffectName.SprintBuff;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        actorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;

        button.StartCoolDown();

        actorIStatChangeDisplay.ShowBuffStart(buffID, EffectTime);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, actorTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

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
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;

        if (actorAnim.GetBool("Battle Pose On")) // 전투 자세이면
        {
            EffectTime = 10f; // 10초 질주 가능
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, particleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
        }
        else
        {
            EffectTime = 20f; // 20초 질주 가능
            CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, particleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
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
    }
}
