using UnityEngine;
using System.Collections;
using Characters.Handlers;
using GameData;

public class SprintAbility : SelfBuffingAction
{
    private readonly OffGlobalCoolDownActionButton button;

    private float InvisibleGlobalCoolDownTime { get; set; }

    public SprintAbility(GameObject actor, int buffIdentifier, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
    {
        BuffID = buffIdentifier;

        IsActionUnusable = false;

        ActorTransform = actor.transform;
        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorAnim = actor.GetComponent<Animator>();
        ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
        ActorStatChangeHandler = actor.GetComponent<StatChangeHandler>();
        ActorIStatChangeDisplay = actorIStatChangeDisplay;

        this.button = button;

        ParticleEffectName = ParticleEffectName.SprintBuff;
    }

    private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
    {
        ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

        IsActionUnusable = true;

        button.StartCoolDown();

        ActorIStatChangeDisplay.ShowBuffStart(BuffID, EffectTime);

        ActorStatChangeHandler.AddStatChangingEffect(BuffID,
            new StatChangeHandler.StatChangingEffectData
            {
                type = StatChangeHandler.StatChangingEffectType.Temporal,
                stat = 0,
                value = ActorActionHandler.Stats[Stat.LocomotionSpeed]
            });

        if (particleEffectName != ParticleEffectName.None)
            NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, ActorTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

        yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

        ActorActionHandler.ActionBeingTaken = 0;

        yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

        ActorIStatChangeDisplay.ShowBuffEnd(BuffID);
        ActorStatChangeHandler.RemoveStatChangingEffect(BuffID);

        yield return new WaitForSeconds(CoolDownTime - EffectTime - InvisibleGlobalCoolDownTime);

        IsActionUnusable = false;
    }

    public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
    {
        if (IsActionUnusable)
            return;

        CoolDownTime = actionInfo.coolDownTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;

        if (ActorAnim.GetBool("Battle Pose On"))
        {
            EffectTime = 10f; // 10 seconds sprint
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(
                TakeAction(actionInfo.id, ParticleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
        }
        else
        {
            EffectTime = 20f; // 20 seconds sprint
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(
                TakeAction(actionInfo.id, ParticleEffectName, Vector3.up * 0.2f, Vector3.zero, Vector3.one));
        }
    }

    public override void Stop()
    {
        if (!IsBuffOn) return;

        if (CurrentActionCoroutine != null)
        {
            ActorStatChangeHandler.RemoveStatChangingEffect(BuffID);
            IsActionUnusable = false;
            ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            button.StopCoolDown();
            CurrentActionCoroutine = null;
        }
    }
}
