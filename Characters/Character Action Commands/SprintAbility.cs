using UnityEngine;
using System.Collections;
using Characters.Handlers;
using UserInterface;
using Managers;
using Characters.StatisticsScripts;
using Enums;

namespace Characters.CharacterActionCommands
{
    public class SprintAbility : SelfBuffingAction
    {
        private readonly OffGlobalCoolDownActionButton button;

        private float InvisibleGlobalCoolDownTime { get; set; }

        public SprintAbility(GameObject actor, int buffIndex,
            OffGlobalCoolDownActionButton button, IStatChangeDisplay actorIStatChangeDisplay)
        {
            BuffIndex = buffIndex;

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

        private IEnumerator TakeAction(int actionID, ParticleEffectName particleEffectName,
            Vector3 localPosition, Vector3 toDirection, Vector3 localScale,
            bool shouldEffectFollowTarget = true)
        {
            ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

            IsActionUnusable = true;

            button.StartCoolDown();

            ActorIStatChangeDisplay.ShowBuffStart(BuffIndex, EffectTime);

            ActorStatChangeHandler.AddStatChangingEffect(BuffIndex,
                new StatChangeHandler.StatChangingEffectData
                {
                    type = StatChangeHandler.StatChangingEffectType.Temporal,
                    stat = 0,
                    value = ActorActionHandler.Stats[Stat.LocomotionSpeed]
                });

            if (particleEffectName != ParticleEffectName.None)
                NonPooledParticleEffectManager.Instance.PlayParticleEffect(
                    particleEffectName, ActorTransform, localPosition, toDirection,
                    localScale, 1f, shouldEffectFollowTarget);

            yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);

            ActorActionHandler.ActionBeingTaken = 0;

            yield return new WaitForSeconds(EffectTime - InvisibleGlobalCoolDownTime);

            ActorIStatChangeDisplay.ShowBuffEnd(BuffIndex);
            ActorStatChangeHandler.RemoveStatChangingEffect(BuffIndex);

            yield return new WaitForSeconds(CoolDownTime - EffectTime - InvisibleGlobalCoolDownTime);

            IsActionUnusable = false;
        }

        public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
        {
            if (IsActionUnusable)
                return;

            CoolDownTime = actionInfo.coolDownTime;
            InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;

            EffectTime = GameManager.Instance.IsInBattle ? 10f : 20f;
            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(
                TakeAction(actionInfo.id, ParticleEffectName, Vector3.up * 0.2f,
                Vector3.zero, Vector3.one));
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
        }
    }
}