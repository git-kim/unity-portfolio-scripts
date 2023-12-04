using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class HumanoidFeetIKSetter : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform characterTransform;

    private static readonly int LeftFootIKRWeightFactor = Animator.StringToHash("Left Foot IK R Weight Factor");
    private static readonly int RightFootIKRWeightFactor = Animator.StringToHash("Right Foot IK R Weight Factor");

    [SerializeField] private LayerMask groundLayerMask = (1 << 9);
    [Min(0f)] [SerializeField] private float maximumFeetYAboveBasePoint = 0.5f;
    [Min(0f)] [SerializeField] private float maximumFeetYBelowBasePoint = 0.5f;
    [SerializeField] private float feetOffsetY = -0.01f;
    [Range(0, 1f)] [SerializeField] private float feetAdjustmentRate = 0.5f;
    [Range(0, 1f)] [SerializeField] private float bodyAdjustmentTime = 0.05f;

    private float footIKWeight;
    private Transform leftFootBoneTransform;
    private Transform rightFootBoneTransform;
    private float leftFootOffsetY;
    private float rightFootOffsetY;
    private Vector3 leftFootIKGoalPosition;
    private Vector3 rightFootIKGoalPosition;
    private Vector3 rightFootRaycastOrigin;
    private Vector3 leftFootRaycastOrigin;
    private Vector3 leftFootNormal = Vector3.up;
    private Vector3 rightFootNormal = Vector3.up;

    private float bodyIKWeight;
    private Vector3 bodyOffset;
    private Vector3 bodyDampVelocity;

    private bool isFeetIKEnabled;

    private const float InvalidValue = 262144f; // arbitrary value

    public void EnableFeetIK() { isFeetIKEnabled = true; }

    public void DisableFeetIK() { isFeetIKEnabled = false; }

    private void Awake()
    {
        characterAnimator = gameObject.GetComponent<Animator>();
        isFeetIKEnabled = true;
        bodyIKWeight = footIKWeight = 1f;
    }

    private void Start()
    {
        leftFootBoneTransform = characterAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFootBoneTransform = characterAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        characterAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
        characterAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, characterAnimator.GetFloat(LeftFootIKRWeightFactor) * footIKWeight);
        SetFootIK(AvatarIKGoal.LeftFoot, in leftFootIKGoalPosition, in leftFootNormal, leftFootOffsetY);

        characterAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
        characterAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, characterAnimator.GetFloat(RightFootIKRWeightFactor) * footIKWeight);
        SetFootIK(AvatarIKGoal.RightFoot, in rightFootIKGoalPosition, in rightFootNormal, rightFootOffsetY);

        characterAnimator.bodyPosition += Vector3.Lerp(Vector3.zero, bodyOffset, bodyIKWeight);
    }

    private void SetFootIK(AvatarIKGoal foot, in Vector3 footIKGoalPos, in Vector3 footLayerNormal, float footOffsetY)
    {
        var newFootIKPos = characterAnimator.GetIKPosition(foot);

        if (footIKGoalPos.x != InvalidValue)
        {
            newFootIKPos.y = Mathf.Max(newFootIKPos.y + footOffsetY, footIKGoalPos.y);

            var newFootIKRot = Quaternion.FromToRotation(Vector3.up, footLayerNormal);
            newFootIKRot *= Quaternion.Euler(0f, characterAnimator.GetIKRotation(foot).eulerAngles.y, 0f);

            characterAnimator.SetIKRotation(foot, newFootIKRot);
        }

        characterAnimator.SetIKPosition(foot, newFootIKPos);
    }

    private void LateUpdate() // called later than OnAnimatorIK()
    {
        if (!isFeetIKEnabled)
        {
            bodyIKWeight = footIKWeight = 0f;
            return;
        }

        bodyIKWeight = footIKWeight = 1f;

        FindRaycastOrigin(leftFootBoneTransform, out leftFootRaycastOrigin);
        FindFootIKGoalPosition(in leftFootRaycastOrigin, ref leftFootNormal, out leftFootIKGoalPosition);

        FindRaycastOrigin(rightFootBoneTransform, out rightFootRaycastOrigin);
        FindFootIKGoalPosition(in rightFootRaycastOrigin, ref rightFootNormal, out rightFootIKGoalPosition);

        var position = characterTransform.position;

        var leftFootTargetY = leftFootIKGoalPosition.y - position.y;
        leftFootOffsetY = Mathf.MoveTowards(leftFootOffsetY, leftFootTargetY,
            Mathf.Abs(leftFootTargetY - leftFootOffsetY) * feetAdjustmentRate);

        var rightFootTargetY = rightFootIKGoalPosition.y - position.y;
        rightFootOffsetY = Mathf.MoveTowards(rightFootOffsetY, rightFootTargetY,
            Mathf.Abs(rightFootTargetY - rightFootOffsetY) * feetAdjustmentRate);

        UpdateBodyOffset(Mathf.Min(leftFootOffsetY, rightFootOffsetY));
    }

    private void FindRaycastOrigin(Transform footBoneTransform, out Vector3 raycastOriginForFoot)
    {
        raycastOriginForFoot = footBoneTransform.position;
        raycastOriginForFoot.y = characterTransform.position.y + maximumFeetYAboveBasePoint;
    }

    private void FindFootIKGoalPosition(in Vector3 raycastOrigin, ref Vector3 layerNormal, out Vector3 footIKGoalPos)
    {
        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hitInfo,
            maximumFeetYBelowBasePoint + maximumFeetYAboveBasePoint, groundLayerMask))
        {
            footIKGoalPos = raycastOrigin;
            footIKGoalPos.y = feetOffsetY + hitInfo.point.y;

            var targetNormal = hitInfo.normal;
            layerNormal = Vector3.MoveTowards(layerNormal, targetNormal,
                (targetNormal - layerNormal).magnitude * feetAdjustmentRate);
            // Debug.DrawLine(raycastOrigin, raycastOrigin + Vector3.down * (maxFeetDepthY + maxFeetHeightY), Color.cyan);
            return;
        }

        footIKGoalPos.x = InvalidValue;
        footIKGoalPos.y = footIKGoalPos.z = 0f;
    }

    private void UpdateBodyOffset(float minFootOffsetY)
    {
        if (rightFootIKGoalPosition.x != InvalidValue && leftFootIKGoalPosition.x != InvalidValue)
            Vector3.SmoothDamp(bodyOffset, Vector3.up * minFootOffsetY, ref bodyDampVelocity, bodyAdjustmentTime);
        else
            Vector3.SmoothDamp(bodyOffset, Vector3.zero, ref bodyDampVelocity, bodyAdjustmentTime);

        bodyOffset += bodyDampVelocity * Time.deltaTime;
    }
}