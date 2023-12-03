﻿using UnityEngine;
using CommandPattern;
using Characters.Handlers;

public abstract class SelfBuffingAction: CharacterActionCommand
{
    protected float CoolDownTime { get; set; }

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
}