using UnityEngine;
using Characters.Handlers;
using Managers;
using Characters.StatisticsScripts;

namespace Characters.CharacterActionCommands
{
    public abstract class NonSelfTargetedAction : CharacterActionCommand
    {
        protected readonly GameManager GameManagerInstance = GameManager.Instance;

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
    }
}