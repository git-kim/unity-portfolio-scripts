using UnityEngine;
using System.Collections;
using Characters.Handlers;

public class SprintAbility : SelfBuffingAction
{
    private readonly OffGlobalCoolDownActionButton button;

    private float InvisibleGlobalCoolDownTime { get; set; }

    public SprintAbility(GameObject actor, int buffID, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
    {
        BuffID = buffID;

        IsBuffOn = false;
        IsActionUnusable = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnim = actor.GetComponent<Animator>();
        ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
        ActorIStatChangeDisplay = actorIStatChangeDisplay;

        this.button = button;

        ParticleEffectName = ParticleEffectName.SprintBuff;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;

        button.StartCoolDown();

        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, ActorTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        ActorActionHandler.ActionBeingTaken = 0;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        ActorIStatChangeDisplay.ShowBuffEnd(BuffID);
        IsBuffOn = false;

        yield return new WaitForSeconds(CoolDownTime - EffectTime - InvisibleGlobalCoolDownTime);

        IsActionUnusable = false;
    }

    public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
    {
        if (IsActionUnusable)
            return;

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;

        if (ActorAnim.GetBool("Battle Pose On")) // 전투 자세이면
        {
            EffectTime = 10f; // 10초 질주 가능
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, ParticleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
        }
        else
        {
            EffectTime = 20f; // 20초 질주 가능
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(actionInfo.id, ParticleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
        }
    }

    public override void Stop()
    {
        if (!IsBuffOn) return;

        if (CurrentActionCoroutine != null)
        {
            IsBuffOn = false;
            IsActionUnusable = false;
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            button.StopCoolDown();
            CurrentActionCoroutine = null;
        }
    }
}
