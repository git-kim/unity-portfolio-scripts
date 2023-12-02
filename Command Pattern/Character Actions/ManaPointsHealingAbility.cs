using UnityEngine;
using System.Collections;
using Characters.Handlers;

public class ManaPointsHealingAbility : SelfBuffingAction
{
    private readonly GameManager gameManagerInstance = GameManager.Instance;

    // 캐스팅 시간
    public float CastTime { get; protected set; }

    // 캐스팅 후 애니메이션 또는 파티클 효과 재생 시간
    private float InvisibleGlobalCoolDownTime { get; set; }

    private readonly OffGlobalCoolDownActionButton button;

    public ManaPointsHealingAbility(GameObject actor, int buffID, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
    {
        BuffID = buffID;
        EffectTime = gameManagerInstance.Buffs[buffID].effectTime;

        IsBuffOn = false;
        IsActionUnusable = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnim = actor.GetComponent<Animator>();
        ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
        //actorIDamageable = actor.GetComponent<IDamageable>();
        //actorStats = actorIActable.Stats;
        ActorIStatChangeDisplay = actorIStatChangeDisplay;

        this.button = button;

        ParticleEffectName = ParticleEffectName.HealMP;
    }

    /// <summary>
    /// Execute 함수에서 호출되는 함수이다. toDirection이 Vector3.zero로 지정되면 회전 값이 변경되지 않는다.
    /// </summary>
    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        //if (IsActionUnusable)
        //    yield break;

        ActorAnim.SetInteger(ActionMode, actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        ActorActionHandler.ActionBeingTaken = actionID;

        ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = IsBuffOn = true;
        button.StartCoolDown();
        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        if (ActorAnim.GetInteger(ActionMode) == actionID)
            ActorAnim.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다.

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
        CastTime = actionInfo.castTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        //actionName = actionInfo.name;

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
            IsActionUnusable = false;
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            button.StopCoolDown();
            CurrentActionCoroutine = null;
        }

        ActorActionHandler.IsCasting = false;
    }
}
