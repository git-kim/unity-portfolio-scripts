using UnityEngine;
using FluentBuilderPattern;
using System.Collections;

public class FireballSpell : NonSelfTargetedAction
{
    private readonly FireballSpawner fireballSpawner;
    private readonly IDamageable actorIDamageable;
    private IDamageable targetIDamageable;
    private int actionID;
    private int mPCost;
    private float range;

    public FireballSpell(GameObject actor)
    {
        actorIDamageable = actor.GetComponent<IDamageable>();
        if (actorIDamageable.SelfOrNull() == null)
            Debug.LogError(GetType().Name + " 사용 객체에 IDamageable이 존재하지 않습니다.");

        fireballSpawner = actor.GetComponentInChildren<FireballSpawner>();
        if (fireballSpawner == null)
            Debug.LogError(GetType().Name + " 사용 객체에 FireballSpawner가 존재하지 않습니다.");

        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorIActable = actor.GetComponent<IActable>();
        ActorAnimator = actor.GetComponent<Animator>();
        ActorStats = ActorIActable.Stats;
        ActorTransform = actor.transform;
    }

    // 액션 취하기용 코루틴
    private IEnumerator TakeAction(int mPCost, float range, int actionID, int actorID)
    {
        // 거리 검사
        if (Vector3.SqrMagnitude(ActorTransform.position - Target.transform.position) > range * range)
        {
            GameManagerInstance.ShowErrorMessage(1); // 거리 초과 메시지 출력
            yield break;
        }

        ActorAnimator.SetInteger(ActionMode, actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        ActorIActable.ActionBeingTaken = actionID;

        ActionName = ActorIActable.ActionCommands[actionID].name;

        ActorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        ActorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;
        //if (CastTime > 0f)
        //{
            if (!(ActorIActable.CastingBarDisplay == null))
                ActorIActable.CastingBarDisplay.ShowCastingBar(actionID, CastTime);
            ActorIActable.IsCasting = true;
            yield return new WaitForSeconds(CastTime);
            //actorAnim.SetTrigger("NextClip");
            ActorIActable.IsCasting = false;
            if (!actorIDamageable.IsDead && !targetIDamageable.IsDead)
            {
                fireballSpawner.SpawnFireball(Target, ActorStats[Stat.MagicAttackPower], in ActionName);
                actorIDamageable.DecreaseStat(Stat.MP, mPCost, false, false);

                if (targetIDamageable is Enemy enemy && !actorIDamageable.IsDead)
                {
                    enemy.IncreaseEnmity(actorID, 2);
                }
            }
        //}
        //else
        //{
        //    yield return new WaitForSeconds(InvisibleGlobalCoolDownTime);
        //    if (!actorIDamageable.IsDead && !targetIDamageable.IsDead)
        //    {
        //        fireballSpawner.SpawnFireball(target, actorStats[Stat.magicAttackPower], in actionName);
        //        actorStats[Stat.mP] -= mPCost;
        //        actorIDamageable.UpdateStatBars();
        //    }
        //}

        if (ActorAnimator.GetInteger(ActionMode) == actionID)
            ActorAnimator.SetInteger(ActionMode, 0); // ActionMode 값을 초기화한다(애니메이션 중지).

        ActorIActable.ActionBeingTaken = 0;
    }


    public override void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        mPCost = actionInfo.mPCost;

        // MP 검사
        if (mPCost > ActorStats[Stat.MP])
        {
            GameManagerInstance.ShowErrorMessage(0); // MP 부족 메시지 출력
            return;
        }

        // 대상 검사
        if (target == null)
        {
            GameManagerInstance.ShowErrorMessage(3);
            return;
        }

        Target = target;
        targetIDamageable = target.GetComponent<IDamageable>();

        // 추가 검사(대상, 사용자)
        if (targetIDamageable.SelfOrNull() == null || actorIDamageable.Identifier.Equals(targetIDamageable.Identifier))
        {
            GameManagerInstance.ShowErrorMessage(2);
            return;
        }

        if (actorIDamageable.IsDead || targetIDamageable.IsDead)
        {
            return;
        }

        CastTime = actionInfo.castTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        CoolDownTime = actionInfo.coolDownTime;
        actionID = actionInfo.id;
        range = actionInfo.range;

        CurrentActionCoroutine = ActorMonoBehaviour.StartCoroutine(TakeAction(mPCost, range, actionID, actorID));
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
        ActorIActable.ActionBeingTaken = 0;
        ActorIActable.VisibleGlobalCoolDownTime = 0f;
        ActorIActable.InvisibleGlobalCoolDownTime = 0f;
        ActorIActable.IsCasting = false;

        if (ActorIActable.CastingBarDisplay != null)
            ActorIActable.CastingBarDisplay.StopShowingCastingBar();
    }
}