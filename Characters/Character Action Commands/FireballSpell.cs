using UnityEngine;
using System.Collections;
using Characters.Handlers;
using ObjectPool;
using Characters.StatisticsScripts;

namespace Characters.CharacterActionCommands
{
    public class FireballSpell : NonSelfTargetedAction
    {
        private readonly FireballSpawner fireballSpawner;
        private readonly StatChangeHandler actorStatChangeHandler;
        private StatChangeHandler targetStatChangeHandler;
        private int actionID;
        private int manaPointsCost;
        private float range;

        public FireballSpell(GameObject actor)
        {
            actorStatChangeHandler = actor.GetComponent<StatChangeHandler>();
            fireballSpawner = actor.GetComponentInChildren<FireballSpawner>();
            ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
            ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
            ActorAnimator = actor.GetComponent<Animator>();
            ActorStats = ActorActionHandler.Stats;
            ActorTransform = actor.transform;
        }

        private IEnumerator TakeAction(int manaPointsCost, float range, int actionID, int actorID)
        {
            // Check Distance
            if (Vector3.SqrMagnitude(ActorTransform.position - Target.transform.position) > range * range)
            {
                GameManagerInstance.ShowErrorMessage(1);
                yield break;
            }

            ActorAnimator.SetInteger(ActionMode, actionID);
            ActorActionHandler.ActionBeingTaken = actionID;

            ActionName = ActorActionHandler.CharacterActions[actionID].name;

            ActorActionHandler.VisibleGlobalCoolDownTime = CoolDownTime;
            ActorActionHandler.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;

            ActorActionHandler.CastingBarDisplay.SelfOrNull()?.ShowCastingBar(actionID, CastTime);
            ActorActionHandler.IsCasting = true;

            yield return new WaitForSeconds(CastTime);

            ActorActionHandler.IsCasting = false;

            if (!actorStatChangeHandler.HasZeroHitPoints && !targetStatChangeHandler.HasZeroHitPoints)
            {
                fireballSpawner.SpawnFireball(Target, ActorStats[Stat.MagicAttack], in ActionName);
                actorStatChangeHandler.DecreaseStat(Stat.ManaPoints, manaPointsCost);

                if (targetStatChangeHandler.TryGetComponent<Enemy>(out var enemy)
                    && !actorStatChangeHandler.HasZeroHitPoints)
                {
                    enemy.IncreaseEnmity(actorID, 2);
                }
            }

            if (ActorAnimator.GetInteger(ActionMode) == actionID)
                ActorAnimator.SetInteger(ActionMode, 0);

            ActorActionHandler.ActionBeingTaken = 0;
        }


        public override void Execute(int actorID, GameObject target, CharacterAction actionInfo)
        {
            manaPointsCost = actionInfo.manaPointsCost;

            // Check Mana Points
            if (manaPointsCost > ActorStats[Stat.ManaPoints])
            {
                GameManagerInstance.ShowErrorMessage(0);
                return;
            }

            // Check Target
            if (target == null)
            {
                GameManagerInstance.ShowErrorMessage(3);
                return;
            }

            Target = target;
            targetStatChangeHandler = target.GetComponent<StatChangeHandler>();

            // Check Others
            if (!targetStatChangeHandler || actorStatChangeHandler.Identifier.Equals(targetStatChangeHandler.Identifier))
            {
                GameManagerInstance.ShowErrorMessage(2);
                return;
            }

            if (actorStatChangeHandler.HasZeroHitPoints || targetStatChangeHandler.HasZeroHitPoints)
            {
                return;
            }

            CastTime = actionInfo.castTime;
            InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
            CoolDownTime = actionInfo.coolDownTime;
            actionID = actionInfo.id;
            range = actionInfo.range;

            CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(manaPointsCost, range, actionID, actorID));
        }

        public override void Stop()
        {
            if (CurrentActionCoroutine != null)
            {
                ActorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
                ActorAnimator.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다(애니메이션 중지).
                CurrentActionCoroutine = null;
            }

            // ActorIActable.ActionToTake = 0;
            ActorActionHandler.ActionBeingTaken = 0;
            ActorActionHandler.VisibleGlobalCoolDownTime = 0f;
            ActorActionHandler.InvisibleGlobalCoolDownTime = 0f;
            ActorActionHandler.IsCasting = false;

            ActorActionHandler.CastingBarDisplay.SelfOrNull()?.StopShowingCastingBar();
        }
    }
}