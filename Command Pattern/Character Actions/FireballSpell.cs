using UnityEngine;
using GameData;
using System.Collections;
using Characters.Handlers;

public class FireballSpell : NonSelfTargetedAction
{
    private readonly FireballSpawner fireballSpawner;
    private readonly StatChangeHandler actorStatChangeHandler;
    private StatChangeHandler targetStatChangeHandler;
    private int actionID;
    private int mPCost;
    private float range;

    public FireballSpell(GameObject actor)
    {
        actorStatChangeHandler = actor.GetComponent<StatChangeHandler>();
        if (actorStatChangeHandler.SelfOrNull() == null)
            Debug.LogError(GetType().Name + " 사용 객체에 IDamageable이 존재하지 않습니다.");

        fireballSpawner = actor.GetComponentInChildren<FireballSpawner>();
        if (fireballSpawner == null)
            Debug.LogError(GetType().Name + " 사용 객체에 FireballSpawner가 존재하지 않습니다.");

        ActorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        ActorActionHandler = actor.GetComponent<CharacterActionHandler>();
        ActorAnimator = actor.GetComponent<Animator>();
        ActorStats = ActorActionHandler.Stats;
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
            actorStatChangeHandler.DecreaseStat(Stat.ManaPoints, mPCost);

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
        mPCost = actionInfo.manaPointsCost;

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
        ActorActionHandler.ActionBeingTaken = 0;
        ActorActionHandler.VisibleGlobalCoolDownTime = 0f;
        ActorActionHandler.InvisibleGlobalCoolDownTime = 0f;
        ActorActionHandler.IsCasting = false;

        ActorActionHandler.CastingBarDisplay.SelfOrNull()?.StopShowingCastingBar();
    }
}