using UnityEngine;
using GameData;
using CommandPattern;
using Characters.Handlers;

public abstract class NonSelfTargetedAction: ICommand
{
    protected readonly GameManager GameManagerInstance = GameManager.Instance;

    public Coroutine CurrentActionCoroutine { get; set; } = null;

    protected string ActionName;

    protected static readonly int ActionMode = Animator.StringToHash("ActionMode");

    protected MonoBehaviour ActorMonoBehaviour;
    protected CharacterActionHandler ActorActionHandler;
    protected Animator ActorAnimator;
    protected Statistics ActorStats;
    protected Transform ActorTransform;

    protected GameObject Target;

    protected float CastTime { get; set; }

    protected float InvisibleGlobalCoolDownTime { get; set; }

    protected float CoolDownTime { get; set; }

    public abstract void Execute(int actorID, GameObject target, CharacterAction actionInfo);

    public abstract void Stop();
}
