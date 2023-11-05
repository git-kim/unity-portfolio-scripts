using UnityEngine;
using CommandPattern;
using FluentBuilderPattern;
using System.Collections;

public class FireballSpell : NonSelfTargetedAction
{
    readonly FireballSpawner fireballSpawner;
    readonly IDamageable actorIDamageable;
    IDamageable targetIDamageable;
    int actionID;
    int mPCost;
    float range;

    public FireballSpell(GameObject actor)
    {
        GAME = GameManager.Instance;

        actorIDamageable = actor.GetComponent<IDamageable>();
        if (actorIDamageable is null)
            Debug.LogError(GetType().Name + " 사용 객체에 IDamageable이 존재하지 않습니다.");

        fireballSpawner = actor.GetComponentInChildren<FireballSpawner>();
        if (fireballSpawner is null)
            Debug.LogError(GetType().Name + " 사용 객체에 FireballSpawner가 존재하지 않습니다.");

        actorMonoBehaviour = actor.GetComponent<MonoBehaviour>();
        actorIActable = actor.GetComponent<IActable>();
        actorAnim = actor.GetComponent<Animator>();
        actorStats = actorIActable.Stats;
        actorTransform = actor.transform;
    }


    // 액션 취하기용 코루틴
    IEnumerator TakeAction(int mPCost, float range, int actionID, int actorID)
    {
        // 거리 검사
        if (Vector3.SqrMagnitude(actorTransform.position - target.transform.position) > range * range)
        {
            GAME.ShowErrorMessage(1); // 거리 초과 메시지 출력
            yield break;
        }

        actorAnim.SetInteger("ActionMode", actionID); // ActionMode에 actionID 값을 저장한다(애니메이션 시작).
        actorIActable.ActionBeingTaken = actionID;

        actionName = actorIActable.ActionCommands[actionID].name;

        actorIActable.VisibleGlobalCoolDownTime = CoolDownTime;
        actorIActable.InvisibleGlobalCoolDownTime = InvisibleGlobalCoolDownTime;
        //if (CastTime > 0f)
        //{
            if (!(actorIActable.CastingBarDisplay is null))
                actorIActable.CastingBarDisplay.ShowCastingBar(actionID, CastTime);
            actorIActable.IsCasting = true;
            yield return new WaitForSeconds(CastTime);
            //actorAnim.SetTrigger("NextClip");
            actorIActable.IsCasting = false;
            if (!actorIDamageable.IsDead && !targetIDamageable.IsDead)
            {
                fireballSpawner.SpawnFireball(target, actorStats[Stat.magicAttackPower], in actionName);
                actorIDamageable.DecreaseStat(Stat.mP, mPCost, false, false);

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

        if (actorAnim.GetInteger("ActionMode") == actionID)
            actorAnim.SetInteger("ActionMode", 0); // ActionMode 값을 초기화한다(애니메이션 중지).

        actorIActable.ActionBeingTaken = 0;
    }


    override public void Execute(int actorID, GameObject target, ActionInfo actionInfo)
    {
        mPCost = actionInfo.mPCost;

        // MP 검사
        if (mPCost > actorStats[Stat.mP])
        {
            GAME.ShowErrorMessage(0); // MP 부족 메시지 출력
            return;
        }

        // 대상 검사
        if (target is null)
        {
            GAME.ShowErrorMessage(3);
            return;
        }

        this.target = target;
        targetIDamageable = target.GetComponent<IDamageable>();

        // 추가 검사(대상, 사용자)
        if (targetIDamageable is null || actorIDamageable.ID.Equals(targetIDamageable.ID))
        {
            GAME.ShowErrorMessage(2);
            return;
        }
        else if (actorIDamageable.IsDead || targetIDamageable.IsDead)
        {
            return;
        }

        CastTime = actionInfo.castTime;
        InvisibleGlobalCoolDownTime = actionInfo.invisibleGlobalCoolDownTime;
        CoolDownTime = actionInfo.coolDownTime;
        actionID = actionInfo.id;
        range = actionInfo.range;

        CurrentActionCoroutine = actorMonoBehaviour.StartCoroutine(TakeAction(mPCost, range, actionID, actorID));
    }

    override public void Stop()
    {
        if (!(CurrentActionCoroutine is null))
        {
            actorMonoBehaviour.StopCoroutine(CurrentActionCoroutine);
            actorAnim.SetInteger("ActionMode", 0); // ActionMode 값을 초기화한다(애니메이션 중지).
            CurrentActionCoroutine = null;
        }

        // actorIActable.ActionToTake = 0;
        actorIActable.ActionBeingTaken = 0;
        actorIActable.VisibleGlobalCoolDownTime = 0f;
        actorIActable.InvisibleGlobalCoolDownTime = 0f;
        actorIActable.IsCasting = false;

        if (!(actorIActable.CastingBarDisplay is null))
            actorIActable.CastingBarDisplay.StopShowingCastingBar();
    }
}
