using UnityEngine;
using CommandPattern;

public abstract class SelfBuffingAction: ICommand
{
    protected float CoolDownTime { get; set; } // 재사용 대기 시간
    public Coroutine CurrentActionCoroutine { get; set; } = null;
    protected ParticleEffectName ParticleEffectName = ParticleEffectName.None;
    protected MonoBehaviour ActorMonoBehaviour; // 코루틴 호출용
    protected Animator ActorAnim;
    protected Transform ActorTransform;
    protected IActable ActorIActable;
    protected IStatChangeDisplay ActorIStatChangeDisplay;

    #region 버프/디버프 액션용 변수, 프로퍼티
    private protected int BuffID;
    public bool IsBuffOn { get; protected set; } // 효과 적용 여부
    protected bool IsActionUnusable { get; set; } // 액션 취하기 가능 여부(효과 중복 적용 방지용)
    protected float EffectTime { get; set; } // 효과 적용 시간
    #endregion

    protected static readonly int ActionMode = Animator.StringToHash("ActionMode");

    public abstract void Execute(int actorID, GameObject target, ActionInfo actionInfo);

    public abstract void Stop();
}