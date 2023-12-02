using UnityEngine;

public abstract class Character : MonoBehaviour // 추상 클래스(참고: 부분 설계도이므로 인스턴스화가 불가하다.)
{
    #region 이 클래스와 자식 클래스에서 사용되는 변수
    protected CharacterController Controller;
    protected Transform ControllerTransform;

    protected float LocomotionSpeed; // 이동 속도
    protected Vector3 Velocity;
    protected bool IsAbleToMove, IsMoving;
    protected Quaternion TargetRotation;
    #endregion

    #region 각 자식 클래스에서 사용되는 변수
    private protected Animator Animator;
    private protected float NegativeGravity;
    private protected Vector3 DragFactor; // 항력 처리용 인자
    private protected bool IsNotInTheAir;
    private protected Vector3 GroundCheckerPos;
    private protected float GroundCheckStartY;

    private protected GameObject CurrentTarget;
    private protected GameObject RecentTarget;
    #endregion

    #region 이 클래스 함수 호출 시 사용되는 변수
    // 자식 클래스 함수에서는 바로 접근이 안 되도록 private으로 둔다.
    // (참고: private 변수도 자식에게 상속은 된다.)

    private ControllerColliderHit hit;
    private Vector3 tempVelocity;
    private Vector3 localOffset;
    private Vector3 lastPosition;
    #endregion

    private int identifier;
    public int Identifier
    {
        get => identifier;
        set => identifier = value;
    }

    protected virtual void Awake() // 가상 함수(참고: 하위 클래스에서 재정의한다. 재정의하지 않으면 해당 클래스 객체에서도 이 함수가 호출된다.)
    {
        hit = null;
        localOffset = new Vector3(0f, Controller.center.y - Controller.height * 0.5f + Controller.radius, 0f);
        TargetRotation = gameObject.transform.rotation;
    }

    protected abstract void Start();  // 추상 함수(참고: 하위 클래스에서 반드시 구현하여야 한다.)

    protected abstract void FixedUpdate(); // 추상 함수

    protected abstract void Update(); // 추상 함수

    /// <summary>
    /// 캐릭터를 움직인다.
    /// </summary>
    protected void Move()
    {
        if (hit != null)
        {
            //// 모서리 충돌 처리(캐릭터 하단 중앙을 기준으로 실제로는 공중에 있으나 캐릭터 컨트롤러가 지면에 충돌하였다고 판단되었을 때)
            if (!Physics.Linecast(ControllerTransform.position + localOffset, ControllerTransform.position + Vector3.down * (Controller.stepOffset + 0.1f), 1 << 9, QueryTriggerInteraction.Ignore))
            {
                tempVelocity = gameObject.transform.position - hit.point; // 주의: hit.transform.position이 아니라 hit.point이다.
                tempVelocity.y = 0f;
                Controller.Move(tempVelocity.normalized * ((Controller.radius - tempVelocity.magnitude) + 0.15f));
                Controller.Move(Vector3.down * Controller.radius);
                // gameObject.transform.Translate(tempVelocity.normalized * ((cC.radius - tempVelocity.magnitude) + 0.15f), Space.World);
            }
            hit = null; // 저장한 정보를 사용하였으므로 제거한다.
        }

        if (!IsAbleToMove)
        {
            Velocity.x = 0f;
            Velocity.z = 0f;
        }

        lastPosition = ControllerTransform.position;

        if (Velocity.magnitude > 0f)
            Controller.Move(Velocity * (LocomotionSpeed * Time.deltaTime));

        IsMoving = !(Vector3.SqrMagnitude(lastPosition - ControllerTransform.position) < 0.00005f);
    }

    /// <summary>
    /// 움직이려는 방향으로 몸을 향하게 한다.
    /// </summary>
    protected void Rotate()
    {
        if (!IsAbleToMove) return;

        tempVelocity = Velocity;
        tempVelocity.y = 0f;
        if (tempVelocity.magnitude > 0.0001f)
        {
            TargetRotation = Quaternion.LookRotation(tempVelocity, Vector3.up);
        }

        if (TargetRotation != gameObject.transform.rotation)
        {
            gameObject.transform.rotation = Quaternion.RotateTowards(gameObject.transform.rotation, TargetRotation, 10f);
        }
    }

    /// <summary>
    /// 캐릭터 컨트롤러가 지면에 충돌하였다고 판단되면 충돌 정보를 저장한다.
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit) // Move 호출 시 조건이 충족되면 호출되는 함수(주의: 이 함수 내에서 캐릭터 이동을 하면 스택 오버플로가 발생할 수 있다.)
    {
        if (Controller.isGrounded)
            this.hit = hit;
    }
}