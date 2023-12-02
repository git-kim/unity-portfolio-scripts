using UnityEngine;
using GameData;
using CommandPattern;

public abstract class NonSelfTargetedAction: ICommand
{
    protected readonly GameManager GameManagerInstance = GameManager.Instance;

    public Coroutine CurrentActionCoroutine { get; set; } = null;

    protected string ActionName;

    protected static readonly int ActionMode = Animator.StringToHash("ActionMode");

    // 사용자
    protected MonoBehaviour ActorMonoBehaviour;
    protected IActable ActorIActable;
    protected Animator ActorAnimator;
    protected GameData.Statistics ActorStats;
    protected Transform ActorTransform;
    // protected Actions ActorActionCommands;

    // 대상
    protected GameObject Target;

    // 캐스팅 시간
    protected float CastTime { get; set; }

    // 캐스팅 후 애니메이션 또는 파티클 효과 재생 시간
    protected float InvisibleGlobalCoolDownTime { get; set; }

    // 재사용 대기 시간
    protected float CoolDownTime { get; set; }

    public abstract void Execute(int actorID, GameObject target, ActionInfo actionInfo);

    public abstract void Stop();
}
