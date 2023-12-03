using Characters.Handlers;
using Characters.StatisticsScripts;
using UnityEngine;

namespace Characters
{
    public abstract class Character : MonoBehaviour
    {
        [SerializeField] protected StatChangeHandler statChangeHandler;
        public bool IsDead => statChangeHandler.HasZeroHitPoints;
        public Statistics Stats { get; private protected set; }

        protected CharacterController Controller;
        protected Transform ControllerTransform;

        protected Vector3 Velocity;
        protected bool IsAbleToMove;
        protected bool IsMoving;
        protected Quaternion GoalRotation;

        private protected Animator Animator;
        private protected float NegativeGravity;
        private protected Vector3 DragFactor;
        private protected bool IsNotInTheAir;
        private protected Vector3 GroundCheckerPos;
        private protected float GroundCheckStartY;

        private ControllerColliderHit hit;
        private Vector3 tempVelocity;
        private Vector3 localOffset;
        private Vector3 lastPosition;

        private int identifier;
        public int Identifier
        {
            get => identifier;
            set => identifier = value;
        }

        protected virtual void Awake()
        {
            hit = null;
            localOffset = new Vector3(0f, Controller.center.y - Controller.height * 0.5f + Controller.radius, 0f);
            GoalRotation = gameObject.transform.rotation;
        }

        protected void Move()
        {
            if (hit != null)
            {
                // Handling edge collision
                if (!Physics.Linecast(ControllerTransform.position + localOffset,
                    ControllerTransform.position + Vector3.down * (Controller.stepOffset + 0.1f),
                    1 << 9,
                    QueryTriggerInteraction.Ignore))
                {
                    tempVelocity = gameObject.transform.position - hit.point; // Note: This line uses hit.point not hit.transform.position.
                    tempVelocity.y = 0f;
                    Controller.Move(tempVelocity.normalized * (Controller.radius - tempVelocity.magnitude + 0.15f));
                    Controller.Move(Vector3.down * Controller.radius);
                }

                hit = null;
            }

            if (!IsAbleToMove)
            {
                Velocity.x = 0f;
                Velocity.z = 0f;
            }

            lastPosition = ControllerTransform.position;

            if (Velocity.magnitude > 0f)
                Controller.Move(Velocity * (Stats[Stat.LocomotionSpeed] * Time.deltaTime));

            IsMoving = !(Vector3.SqrMagnitude(lastPosition - ControllerTransform.position) < 0.00005f);
        }

        protected void Rotate()
        {
            if (!IsAbleToMove) return;

            tempVelocity = Velocity;
            tempVelocity.y = 0f;
            if (tempVelocity.magnitude > 0.0001f)
            {
                GoalRotation = Quaternion.LookRotation(tempVelocity, Vector3.up);
            }

            if (GoalRotation != gameObject.transform.rotation)
            {
                gameObject.transform.rotation = Quaternion.RotateTowards(gameObject.transform.rotation, GoalRotation, 10f);
            }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Note: Don't call Move() here. Move() may call this method.
            if (Controller.isGrounded)
                this.hit = hit;
        }

        private protected void UpdateStat()
        {
            statChangeHandler.ApplyActiveStatChangingEffects();
        }
    }
}