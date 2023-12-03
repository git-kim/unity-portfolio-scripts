using UnityEngine;
using CommandPattern;
using Characters.Handlers;

public abstract class SelfBuffingAction: ICommand
{
    protected float CoolDownTime { get; set; }
    public Coroutine CurrentActionCoroutine { get; set; } = null;
    protected ParticleEffectName ParticleEffectName = ParticleEffectName.None;
    protected MonoBehaviour ActorMonoBehaviour;
    protected Animator ActorAnim;
    protected Transform ActorTransform;
    protected CharacterActionHandler ActorActionHandler;
    protected StatChangeHandler ActorStatChangeHandler;
    protected IStatChangeDisplay ActorIStatChangeDisplay;

    private protected int BuffID;
    public bool IsBuffOn => ActorStatChangeHandler.HasStatChangingEffect(BuffID);
    protected bool IsActionUnusable { get; set; }
    protected float EffectTime { get; set; }

    protected static readonly int ActionMode = Animator.StringToHash("ActionMode");

    public abstract void Execute(int actorID, GameObject target, CharacterAction actionInfo);

    public abstract void Stop();
}