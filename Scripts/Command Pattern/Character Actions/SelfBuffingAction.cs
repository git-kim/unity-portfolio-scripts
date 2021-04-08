using System.Collections;
using UnityEngine;
using CommandPattern;

abstract public class SelfBuffingAction: ICommand
{
    #region 
    public float CoolDownTime { get; protected set; } // 재사용 대기 시간
    public Coroutine CurrentActionCoroutine { get; set; } = null;
    protected ParticleEffectName particleEffectName = ParticleEffectName.None;
    protected MonoBehaviour actorMonoBehaviour; // 코루틴 호출용
    protected Animator actorAnim;
    protected Transform actorTransform;
    protected IActable actorIActable;
    protected IStatChangeDisplay actorIStatChangeDisplay;
    #endregion

    #region 버프/디버프 액션용 변수, 프로퍼티
    public int buffID;
    public bool IsBuffOn { get; protected set; } // 효과 적용 여부
    public bool IsActionUnusable { get; protected set; } // 액션 취하기 가능 여부(효과 중복 적용 방지용)
    public float EffectTime { get; protected set; } // 효과 적용 시간
    #endregion

    abstract public void Execute(int actorID, GameObject target, ActionInfo actionInfo);

    abstract public void Stop();
}
