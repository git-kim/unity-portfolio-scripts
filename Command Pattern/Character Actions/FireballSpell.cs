using UnityEngine;
using GameData;
using System.Collections;
using Characters.Components;

public class FireballSpell : NonSelfTargetedAction
{
    private readonly FireballSpawner fireballSpawner;
    private readonly StatChangeable actorStatChangeable;
    private StatChangeable targetStatChangeable;
    private int actionID;
    private int mPCost;
    private float range;

    public FireballSpell(GameObject actor)
    {
        actorStatChangeable = actor.GetComponent<StatChangeable>();
        if (actorStatChangeable.SelfOrNull() == null)
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

        ActorIActable.CastingBarDisplay.SelfOrNull()?.ShowCastingBar(actionID, CastTime);
        ActorIActable.IsCasting = true;

        yield return new WaitForSeconds(CastTime);

        ActorIActable.IsCasting = false;
        if (!actorStatChangeable.HasZeroHitPoints && !targetStatChangeable.HasZeroHitPoints)
        {
            fireballSpawner.SpawnFireball(Target, ActorStats[Stat.MagicAttack], in ActionName);
            actorStatChangeable.DecreaseStat(Stat.ManaPoints, mPCost);

            if (targetStatChangeable.TryGetComponent<Enemy>(out var enemy)
                && !actorStatChangeable.HasZeroHitPoints)
            {
                enemy.IncreaseEnmity(actorID, 2);
            }
        }


        if (ActorAnimator.GetInteger(ActionMode) == actionID)
            ActorAnimator.SetInteger(ActionMode, 0);

        ActorIActable.ActionBeingTaken = 0;
    }


    public override void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        mPCost = actionInfo.mPCost;

        // Check Mana Points
        if (mPCost > ActorStats[Stat.ManaPoints])
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
        targetStatChangeable = target.GetComponent<StatChangeable>();

        // Check Others
        if (!targetStatChangeable || actorStatChangeable.Identifier.Equals(targetStatChangeable.Identifier))
        {
            GameManagerInstance.ShowErrorMessage(2);
            return;
        }

        if (actorStatChangeable.HasZeroHitPoints || targetStatChangeable.HasZeroHitPoints)
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

        ActorIActable.CastingBarDisplay.SelfOrNull()?.StopShowingCastingBar();
    }
}