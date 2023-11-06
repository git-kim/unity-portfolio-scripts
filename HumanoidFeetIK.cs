using UnityEngine;


sealed public class HumanoidFeetIK : MonoBehaviour // sealed: 이 클래스가 상속되지 못하게 한다.
{
    #region 변수 1(인스펙터 표시)
    [Tooltip("발이 닿는 Layer")] [SerializeField]
    private LayerMask layerMask = (1 << 0);

    [Tooltip("플레이어 캐릭터 기준점 위로 최대 허용 발 높이(양수)")] [Min(0f)] [SerializeField]
    private float maxFeetHeightY = 0.5f;

    [Tooltip("플레이어 캐릭터 기준점 아래로 최대 허용 발 깊이(양수)")] [Min(0f)] [SerializeField]
    private float maxFeetDepthY = 0.5f;

    [Tooltip("발 위치 오프셋(y 축 방향)")] [SerializeField]
    private float feetOffsetY = -0.02f;

    [Tooltip("LateUpdate 호출당 발 위치/회전 변경률")] [Range(0, 1f)] [SerializeField]
    private float feetAdjRate = 0.5f;

    [Tooltip("몸 중심 이동에 사용하는 smoothTime")] [Range(0, 1f)] [SerializeField]
    private float bodyAdjTime = 0.05f;
    #endregion

    #region 변수 2
    private Animator anim;

    private float footIKWeight;
    private float leftFootOffsetY, rightFootOffsetY;
    private Vector3 leftFootIKGoalPos, rightFootIKGoalPos; // 발 목표 위치 계산용
    private Vector3 raycastOriginForRightFoot, raycastOriginForLeftFoot; // 발 목표 위치 계산용
    private Vector3 layerNormalForLeftFoot = Vector3.up, layerNormalForRightFoot = Vector3.up; // 발 목표 회전(y 축 기준) 계산용
    private Vector3 newFootIKPos;
    private Quaternion newFootIKRot;

    private float bodyIKWeight;
    private Vector3 bodyOffset;
    private Vector3 bodyDampVelocity;

    private bool isFeetIKEnabled;
    #endregion

    private const float InvalidValue = 262144f; // 특정 조건 처리용 임의 이진수 저장 변수(리마인더: 맵 x 좌표가 이 수치까지 된다면 변경하여야 한다.)
    private static readonly int LeftFootIKRWeightFactor = Animator.StringToHash("Left Foot IK R Weight Factor");
    private static readonly int RightFootIKRWeightFactor = Animator.StringToHash("Right Foot IK R Weight Factor");

    public void EnableFeetIK() { isFeetIKEnabled = true; }

    public void DisableFeetIK() { isFeetIKEnabled = false; }

    private void Awake()
    {
        anim = gameObject.GetComponent<Animator>();
        isFeetIKEnabled = true;
        bodyIKWeight = footIKWeight = 1f;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        // 왼발
        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, anim.GetFloat(LeftFootIKRWeightFactor) * footIKWeight);
        SetFootIK(AvatarIKGoal.LeftFoot, in leftFootIKGoalPos, in layerNormalForLeftFoot, leftFootOffsetY);

        // 오른발
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, anim.GetFloat(RightFootIKRWeightFactor) * footIKWeight);
        SetFootIK(AvatarIKGoal.RightFoot, in rightFootIKGoalPos, in layerNormalForRightFoot, rightFootOffsetY);

        // 몸(발 IK 목표 위치만 설정하면 무릎 쪽이 부자연스러우니 몸 중심 위치도 설정한다.)
        anim.bodyPosition += Vector3.Lerp(Vector3.zero, bodyOffset, bodyIKWeight);
    }


    void SetFootIK(AvatarIKGoal foot, in Vector3 footIKGoalPos, in Vector3 footLayerNormal, float footOffsetY)
    {
        newFootIKPos = anim.GetIKPosition(foot);

        if (footIKGoalPos.x != InvalidValue) // ray가 hit이 되었으면
        {
            newFootIKPos.y += footOffsetY;
            if (newFootIKPos.y < footIKGoalPos.y) newFootIKPos.y = footIKGoalPos.y;

            newFootIKRot = Quaternion.FromToRotation(Vector3.up, footLayerNormal);
            newFootIKRot *= Quaternion.Euler(0f, anim.GetIKRotation(foot).eulerAngles.y, 0f);
            // 참고: 사원수 곱 A * B = B에 A를 글로벌 회전으로 적용하기 또는 A에 B를 로컬 회전으로 적용하기.

            anim.SetIKRotation(foot, newFootIKRot);
        }
        anim.SetIKPosition(foot, newFootIKPos);
    }

    private void LateUpdate() // 참고: OnAnimatorIK보다 나중에 호출된다.
    {
        if (isFeetIKEnabled)
        {
            bodyIKWeight = footIKWeight = 1f;

            FindRaycastOrigin(HumanBodyBones.LeftFoot, out raycastOriginForLeftFoot);
            FindFootIKGoalPos(in raycastOriginForLeftFoot, ref layerNormalForLeftFoot, out leftFootIKGoalPos);

            FindRaycastOrigin(HumanBodyBones.RightFoot, out raycastOriginForRightFoot); 
            FindFootIKGoalPos(in raycastOriginForRightFoot, ref layerNormalForRightFoot, out rightFootIKGoalPos);

            var position = transform.position;
            leftFootOffsetY = Mathf.Lerp(leftFootOffsetY, (leftFootIKGoalPos.y - position.y), feetAdjRate);
            rightFootOffsetY = Mathf.Lerp(rightFootOffsetY, (rightFootIKGoalPos.y - position.y), feetAdjRate);
            UpdateBodyOffset((leftFootOffsetY < rightFootOffsetY) ? leftFootOffsetY : rightFootOffsetY);
        }
        else
            bodyIKWeight = footIKWeight = 0f;
    }

    private void FindRaycastOrigin(HumanBodyBones foot, out Vector3 raycastOriginForFoot)
    {
        raycastOriginForFoot = anim.GetBoneTransform(foot).position;
        raycastOriginForFoot.y = transform.position.y + maxFeetHeightY;
    }

    private void FindFootIKGoalPos(in Vector3 raycastOrigin, ref Vector3 layerNormal, out Vector3 footIKGoalPos)
    {
        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hitInfo, maxFeetDepthY + maxFeetHeightY, layerMask))
        {
            footIKGoalPos = raycastOrigin;
            footIKGoalPos.y = feetOffsetY + hitInfo.point.y;
            layerNormal = Vector3.Lerp(layerNormal, hitInfo.normal, feetAdjRate);

            // Debug.DrawLine(raycastOrigin, raycastOrigin + Vector3.down * (maxFeetDepthY + maxFeetHeightY), Color.cyan); // 디버깅용
        }
        else // ray가 hit이 되지 않았으면
        {
            footIKGoalPos.x = InvalidValue;
            footIKGoalPos.y = footIKGoalPos.z = 0f;
        }
    }

    private void UpdateBodyOffset(float minFootOffsetY)
    {
        if (rightFootIKGoalPos.x != InvalidValue && leftFootIKGoalPos.x != InvalidValue) // ray가 둘 다 hit이 되었으면
            Vector3.SmoothDamp(bodyOffset, Vector3.up * minFootOffsetY, ref bodyDampVelocity, bodyAdjTime);
        else
            Vector3.SmoothDamp(bodyOffset, Vector3.zero, ref bodyDampVelocity, bodyAdjTime);

        bodyOffset += bodyDampVelocity * Time.deltaTime;
    }
}
