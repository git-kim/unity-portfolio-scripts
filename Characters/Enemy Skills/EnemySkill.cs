using System.Collections;
using Characters.Handlers;
using UnityEngine;

namespace Characters.EnemySkills
{
    /// <summary>
    /// A special attack an enemy uses on its own cooldown, in place of a normal attack.
    /// While it runs the enemy stays in its Casting state and does not move.
    /// </summary>
    public abstract class EnemySkill : MonoBehaviour
    {
        protected static readonly int SkillTrigger = Animator.StringToHash("Skill");
        protected static readonly int CastBool = Animator.StringToHash("Cast");

        [SerializeField] protected string skillName;
        [SerializeField] private float coolDown = 12f;
        [SerializeField] private float firstUseDelay = 6f;
        [Tooltip("Horizontal distance to the target within which the skill may start.")]
        [SerializeField] private float startRange = 5f;

        protected Enemy Owner { get; private set; }
        protected Animator Animator { get; private set; }

        private float readyTime;
        private Coroutine running;

        public string SkillName => skillName;
        public bool IsRunning => running != null;

        public void Initialize(Enemy owner, Animator animator)
        {
            Owner = owner;
            Animator = animator;
        }

        /// <summary>Called when the enemy enters a fight, so the skill is not used on the first swing.</summary>
        public void ResetCoolDown()
        {
            readyTime = Time.time + firstUseDelay;
        }

        public bool TryActivate(Transform target, StatChangeHandler targetStats, float sqrDistanceToTarget)
        {
            if (IsRunning || Time.time < readyTime || sqrDistanceToTarget > startRange * startRange)
                return false;
            if (!targetStats || targetStats.HasZeroHitPoints)
                return false;

            readyTime = Time.time + coolDown;
            running = StartCoroutine(Run(target, targetStats));
            return true;
        }

        public void Cancel()
        {
            if (running == null)
                return;

            StopCoroutine(running);
            running = null;
            CleanUp();
        }

        private IEnumerator Run(Transform target, StatChangeHandler targetStats)
        {
            yield return Execute(target, targetStats);
            running = null;
            CleanUp();
        }

        protected abstract IEnumerator Execute(Transform target, StatChangeHandler targetStats);

        /// <summary>Removes anything the skill spawned. Runs after the skill ends or is cancelled.</summary>
        protected virtual void CleanUp() { }

        protected static float SqrHorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return (a - b).sqrMagnitude;
        }

        private void OnDisable()
        {
            Cancel();
        }
    }
}
