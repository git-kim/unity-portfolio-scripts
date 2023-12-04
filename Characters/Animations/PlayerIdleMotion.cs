using UnityEngine;

namespace Characters.Animations
{
    public class PlayerIdleMotion : StateMachineBehaviour
    {
        private static readonly int IdleTimeout = Animator.StringToHash("Idle Timeout");
        private static readonly int Idle2On = Animator.StringToHash("Idle2 On");

        private static bool isFirstTime = true;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!isFirstTime)
                return;
            animator.SetFloat(IdleTimeout, Random.Range(5f, 20f));
            isFirstTime = false;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var idleTimeout = animator.GetFloat(IdleTimeout) - Time.deltaTime;
            if (idleTimeout < float.Epsilon)
            {
                idleTimeout = Random.Range(10f, 20f);
                animator.SetBool(Idle2On, !animator.GetBool(Idle2On));
            }

            animator.SetFloat(IdleTimeout, idleTimeout);
        }
    }
}