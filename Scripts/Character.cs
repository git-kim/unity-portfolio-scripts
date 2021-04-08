using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Character : MonoBehaviour // 추상 클래스(참고: 부분 설계도이므로 인스턴스화가 불가하다.)
{
    #region 이 클래스와 자식 클래스에서 사용되는 변수
    protected CharacterController cC;
    protected Transform cCTransform;

    protected float locomotionSpeed; // 이동 속도
    protected Vector3 velocity;
    protected bool isAbleToMove, isMoving;
    protected Quaternion targetRotation;
    #endregion

    #region 각 자식 클래스에서 사용되는 변수
    protected int id;
    protected Animator anim;
    protected float negGravity;
    protected Vector3 dragFactor; // 항력 처리용 인자
    protected bool isNotInTheAir;
    protected Vector3 groundCheckerPos;
    protected float groundCheckStartY;

    [HideInInspector]
    public GameObject currentTarget;

    protected GameObject recentTarget;
    #endregion

    #region 이 클래스 함수 호출 시 사용되는 변수
    // 자식 클래스 함수에서는 바로 접근이 안 되도록 private으로 둔다.
    // (참고: private 변수도 자식에게 상속은 된다.)

    ControllerColliderHit cCHitInfo;
    Vector3 tempVelocity;
    Vector3 cCLocalOffset;
    Vector3 lastPos;
    #endregion

    virtual protected void Awake() // 가상 함수(참고: 하위 클래스에서 재정의한다. 재정의하지 않으면 해당 클래스 객체에서도 이 함수가 호출된다.)
    {
        cCHitInfo = null;
        cCLocalOffset = new Vector3(0f, cC.center.y - cC.height * 0.5f + cC.radius, 0f);
        targetRotation = gameObject.transform.rotation;
    }

    abstract protected void Start();  // 추상 함수(참고: 하위 클래스에서 반드시 구현하여야 한다.)

    abstract protected void FixedUpdate(); // 추상 함수

    abstract protected void Update(); // 추상 함수

    /// <summary>
    /// 캐릭터를 움직인다.
    /// </summary>
    protected void Move()
    {
        if (!(cCHitInfo is null))
        {
            //// 모서리 충돌 처리(캐릭터 하단 중앙을 기준으로 실제로는 공중에 있으나 캐릭터 컨트롤러가 지면에 충돌하였다고 판단되었을 때)
            if (!Physics.Linecast(cCTransform.position + cCLocalOffset, cCTransform.position + Vector3.down * (cC.stepOffset + 0.1f), 1 << 9, QueryTriggerInteraction.Ignore))
            {
                tempVelocity = gameObject.transform.position - cCHitInfo.point; // 주의: hit.transform.position이 아니라 hit.point이다.
                tempVelocity.y = 0f;
                cC.Move(tempVelocity.normalized * ((cC.radius - tempVelocity.magnitude) + 0.15f));
                cC.Move(Vector3.down * cC.radius);
                // gameObject.transform.Translate(tempVelocity.normalized * ((cC.radius - tempVelocity.magnitude) + 0.15f), Space.World);
            }
            cCHitInfo = null; // 저장한 정보를 사용하였으므로 제거한다.
        }

        if (!isAbleToMove)
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        lastPos = cCTransform.position;

        if (velocity.magnitude > 0f)
            cC.Move(velocity * locomotionSpeed * Time.deltaTime);

        if (Vector3.SqrMagnitude(lastPos - cCTransform.position) < 0.00005f)
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }
    }

    /// <summary>
    /// 움직이려는 방향으로 몸을 향하게 한다.
    /// </summary>
    protected void Rotate()
    {
        if (!isAbleToMove) return;

        tempVelocity = velocity;
        tempVelocity.y = 0f;
        if (tempVelocity.magnitude > 0.0001f)
        {
            targetRotation = Quaternion.LookRotation(tempVelocity, Vector3.up);
        }

        if (targetRotation != gameObject.transform.rotation)
        {
            gameObject.transform.rotation = Quaternion.RotateTowards(gameObject.transform.rotation, targetRotation, 10f);
        }
    }

    /// <summary>
    /// 캐릭터 컨트롤러가 지면에 충돌하였다고 판단되면 충돌 정보를 저장한다.
    /// </summary>
    void OnControllerColliderHit(ControllerColliderHit hit) // cC.Move 호출 시 조건이 충족되면 호출되는 함수(주의: 이 함수 내에서 캐릭터 이동을 하면 스택 오버플로가 발생할 수 있다.)
    {
        if (cC.isGrounded) cCHitInfo = hit;
    }
}