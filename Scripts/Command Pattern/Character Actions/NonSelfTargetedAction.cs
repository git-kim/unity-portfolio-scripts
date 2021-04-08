using System.Collections;
using UnityEngine;
using FluentBuilderPattern;
using CommandPattern;

abstract public class NonSelfTargetedAction: ICommand
{
    protected GameManager GAME = GameManager.Instance;

    public Coroutine CurrentActionCoroutine { get; set; } = null;

    protected string actionName;

    // 사용자
    protected MonoBehaviour actorMonoBehaviour;
    protected IActable actorIActable;
    protected Animator actorAnim;
    protected Statistics actorStats;
    protected Transform actorTransform;
    // protected Actions actorActionCommands;

    // 대상
    protected GameObject target;

    // 캐스팅 시간
    public float CastTime { get; protected set; }

    // 캐스팅 후 애니메이션 또는 파티클 효과 재생 시간
    public float InvisibleGlobalCoolDownTime { get; protected set; }

    // 재사용 대기 시간
    public float CoolDownTime { get; protected set; }

    abstract public void Execute(int actorID, GameObject target, ActionInfo actionInfo);

    abstract public void Stop();
}
