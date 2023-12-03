using UnityEngine;
using System.Collections;
using Characters.Handlers;
using UserInterface;
using Managers;
using Characters.StatisticsScripts;

namespace Characters.CharacterActionCommands
{
    public class ManaPointsHealingAbility : SelfBuffingAction
    {
        private readonly GameManager gameManagerInstance = GameManager.Instance;

        public float CastTime { get; protected set; }
        private float InvisibleGlobalCoolDownTime { get; set; }

        private readonly OffGlobalCoolDownActionButton button;

        public ManaPointsHealingAbility(GameObject actor, int buffIndex, OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
        {
            BuffIndex = buffIndex;
            EffectTime = gameManagerInstance.Buffs[buffIndex].effectTime;

            IsActionUnusable = false;

            ActorTransform = actor.transform;
            ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
            ActorAnim = actor.GetComponent<Animator>();
            ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
            ActorIStatChangeDisplay = actorIStatChangeDisplay;
            ActorStatChangeHandler = actor.GetComponent<StatChangeHandler>();

            this.button = button;

            ParticleEffectName = ParticleEffectName.HealMP;
        }

        private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, bool shouldEffectFollowTarget = true)
        {
            //if (IsActionUnusable)
            //    yield break;

            ActorAnim.SetInteger(ActionMode, actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
            ActorActionHandler.ActionBeingTaken = actionID;

            ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

            IsActionUnusable = true;
            button.StartCoolDown();
            ActorIStatChangeDisplay.ShowBuffStart(BuffIndex, EffectTime);
            ActorStatChangeHandler.AddStatChangingEffect(BuffIndex,
                new StatChangeHandler.StatChangingEffectData
                {
                    type = StatChangeHandler.StatChangingEffectType.AppliedPerTick,
                    stat = Stat.ManaPoints,
                    value = ActorActionHandler.Stats[Stat.ManaPointsRestorability]
                });

            if (particleEffectName != ParticleEffectName.None)
                NonPooledParticleEffectManager.Instance.PlayParticleEffect(particleEffectName, targetTransform, localPosition, toDirection, localScale, 1f, shouldEffectFollowTarget);

            yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

            if (ActorAnim.GetInteger(ActionMode) == actionID)
                ActorAnim.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다.

            ActorActionHandler.ActionBeingTaken = 0;

            yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

            ActorStatChangeHandler.RemoveStatChangingEffect(BuffIndex);
            ActorIStatChangeDisplay.ShowBuffEnd(BuffIndex);

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
                ActorStatChangeHandler.RemoveStatChangingEffect(BuffIndex);
                IsActionUnusable = false;
                ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
                button.StopCoolDown();
                CurrentActionCoroutine = null;
            }

            ActorActionHandler.IsCasting = false;
        }
    }
}