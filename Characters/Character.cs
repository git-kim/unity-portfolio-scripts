using Characters.Handlers;
using Characters.StatisticsScripts;
using UnityEngine;

namespace Characters
{
    public abstract class Character : MonoBehaviour
    {
        public int Identifier { get; set; }

        public Statistics Stats { get; private protected set; }

        [SerializeField] protected StatChangeHandler statChangeHandler;
        public bool IsDead => statChangeHandler.HasZeroHitPoints;

        [SerializeField] protected Transform Transform;
        [SerializeField] protected CharacterController CharacterController;
        [SerializeField] private protected Animator Animator;

        protected Vector3 Velocity;
        protected Quaternion GoalRotation;
        protected bool IsAbleToMove;
        protected bool IsMoving;
        private ControllerColliderHit hit;
        private Vector3 tempVelocity;
        private Vector3 localOffset;
        private Vector3 lastPosition;

        private protected float NegativeGravity;
        private protected Vector3 DragFactor;
        private protected Vector3 GroundCheckerPos;
        private protected float GroundCheckStartY;
        private protected bool IsOnGround;

        protected virtual void Awake()
        {
            hit = null;

            localOffset.y =
                CharacterController.center.y - CharacterController.height * 0.5f
                + CharacterController.radius;

            GoalRotation = Transform.rotation;
        }

        protected void MoveUsingVelocity()
        {
            if (hit != null)
            {
                // Handling edge collision
                if (!Physics.Linecast(Transform.position + localOffset,
                    Transform.position + Vector3.down * (CharacterController.stepOffset + 0.1f),
                    1 << 9,
                    QueryTriggerInteraction.Ignore))
                {
                    tempVelocity = gameObject.transform.position - hit.point; // Note: This line uses hit.point not hit.transform.position.
                    tempVelocity.y = 0f;
                    CharacterController.Move(tempVelocity.normalized * (CharacterController.radius - tempVelocity.magnitude + 0.15f));
                    CharacterController.Move(Vector3.down * CharacterController.radius);
                }

                hit = null;
            }

            if (!IsAbleToMove)
            {
                Velocity.x = 0f;
                Velocity.z = 0f;
            }

            lastPosition = Transform.position;

            if (Velocity.magnitude > 0f)
                CharacterController.Move(Velocity * (Stats[Stat.LocomotionSpeed] * Time.deltaTime));

            IsMoving = !(Vector3.SqrMagnitude(lastPosition - Transform.position) < 0.00005f);
        }

        protected void Rotate()
        {
            if (!IsAbleToMove)
                return;

            tempVelocity = Velocity;
            tempVelocity.y = 0f;

            if (tempVelocity.magnitude > 0.0001f)
            {
                GoalRotation = Quaternion.LookRotation(tempVelocity, Vector3.up);
            }

            if (GoalRotation != Transform.rotation)
            {
                Transform.rotation = Quaternion.RotateTowards(Transform.rotation, GoalRotation, 10f);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Note: Don't call Move() here. Move() may call this method.
            if (CharacterController.isGrounded)
                this.hit = hit;
        }

        private protected void UpdateStat()
        {
            statChangeHandler.ApplyActiveStatChangingEffects();
        }
    }
}