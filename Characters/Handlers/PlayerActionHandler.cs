using Characters.CharacterActionCommands;
using UnityEngine;
using UnityEngine.Events;
using UserInterface;

namespace Characters.Handlers
{
    public class PlayerActionHandler : CharacterActionHandler
    {
        private int playerIdentifier;

        private int recentActionInput = 0;

        private readonly Vector3 forwardRight = (Vector3.forward + Vector3.right).normalized;

        private UnityAction onVisibleGlobalCoolTimeUpdated;
        private UnityAction onSqrDistanceFromCurrentTargetUpdated;

        private Animator playerAnimator;
        private Transform playerTransform;
        private UnityAction<Quaternion> playerGoalRotationSetter;

        public void SetPlayerIdentifier(int identifier)
        {
            playerIdentifier = identifier;
        }

        public void SetPlayerReferences(Animator animator, Transform transform,
            UnityAction<Quaternion> goalRotationSetter)
        {
            playerAnimator = animator;
            playerTransform = transform;
            playerGoalRotationSetter = goalRotationSetter;
        }

        public void SetActionCommands(in IStatChangeDisplay statChangeDisplay, in ActionButtons actionButtons)
        {
            var globalCoolDownTime = GlobalCoolDownTime;

            var nullAction = new CharacterAction(
                new CharacterAction.CreationContext(-1, new NullActionCommand(gameObject),
                CharacterActionTargetType.Self, 0f, 0f, 0f));

            CharacterActions.Add(nullAction);

            CharacterActions.Add(new CharacterAction(
                new CharacterAction.CreationContext(1, new FireballSpell(gameObject),
                CharacterActionTargetType.NonSelf,
                globalCoolDownTime, globalCoolDownTime, 0.5f, 25f, 0f, 250,
                "불덩이", "25미터 이내 선택 대상에게 불덩이를 던진다.")));

            CharacterActions.Add(new CharacterAction(
                new CharacterAction.CreationContext(2, new CurseSpell(gameObject, 3),
                CharacterActionTargetType.NonSelf,
                0f, globalCoolDownTime, 0.5f, 25f, 0f, 500,
                "저주", "25미터 이내 선택 대상에게 저주를 내려 30초 동안 일정 시간마다 대상 HP를 소량 감소한다.")));

            var hitPointsHealingAbility = new HitPointsHealingAbility(gameObject, 1, statChangeDisplay);
            CharacterActions.Add(new CharacterAction(
                new CharacterAction.CreationContext(3, hitPointsHealingAbility,
                CharacterActionTargetType.Self,
                0f, globalCoolDownTime, 0.5f, 0f, 0f, 700,
                "HP 회복", "HP를 10초 동안 일정 시간마다 소량 회복한다.")));

            var manaPointsHealingAbility = new ManaPointsHealingAbility(gameObject, 2,
                actionButtons[4].GetComponent<OffGlobalCoolDownActionButton>(), statChangeDisplay);
            CharacterActions.Add(new CharacterAction(
                new CharacterAction.CreationContext(4, manaPointsHealingAbility,
                CharacterActionTargetType.Self,
                0f, 60f, 0.5f, 0f, 0f, 0,
                "MP 회복", "MP를 20초 동안 일정 시간마다 소량 회복한다.", true)));

            CharacterActions.Add(nullAction); // (not implemented) Ult.: non - self, off - global

            var sprint = new SprintAbility(gameObject, 0,
                actionButtons[6].GetComponent<OffGlobalCoolDownActionButton>(), statChangeDisplay);
            CharacterActions.Add(new CharacterAction(
                new CharacterAction.CreationContext(6, sprint,
                CharacterActionTargetType.Self,
                0f, 60f, 0.5f, 0f, 0f, 0,
                "잰 발놀림", "20초(전투 중 효과 지속 시간: 10초) 동안 더 빨리 걷거나 더 빨리 달릴 수 있다.")));

            foreach (var actionButton in actionButtons)
            {
                actionButton.SelfOrNull()?.Initialize(
                    () => SetRecentActionInput(actionButton.actionID),
                    CharacterActions[actionButton.actionID],
                    this);
            }

            onVisibleGlobalCoolTimeUpdated = actionButtons.UpdateVisibleCoolTime;
            onSqrDistanceFromCurrentTargetUpdated = actionButtons.UpdateActionUsableness;
        }

        public void SetRecentActionInput(int actionID)
        {
            recentActionInput = actionID;
        }

        public void Act(bool isMoving)
        {
            if (recentActionInput > 0)
            {
                if (VisibleGlobalCoolDownTime < 1f
                    || (!IsCasting && CharacterActions[recentActionInput].canIgnoreVisibleGlobalCoolDownTime))
                {
                    ActionToTake = recentActionInput; // 액션 예약
                    SetRecentTarget(CurrentTarget); // 현재 선택 대상 저장
                }
            }

            recentActionInput = 0; // 주의: 이 줄이 있어야 오동작하지 않는다.

            if (isMoving)
            {
                if (IsCasting && VisibleGlobalCoolDownTime > 0.5f)
                {
                    StopTakingAction(); // 캐스팅 액션 중단
                    RemoveActionToTake(); // 액션 예약 취소
                }
                else if (CharacterActions[ActionToTake].castTime > 0f)
                    RemoveActionToTake(); // 캐스팅 액션 예약 취소
            }

            if (ActionToTake != 0
                && ActionBeingTaken == 0
                && InvisibleGlobalCoolDownTime == 0f
                && (VisibleGlobalCoolDownTime == 0f
                || CharacterActions[ActionToTake].canIgnoreVisibleGlobalCoolDownTime))
            {
                if (!(RecentTarget == null) && CheckIfActionAffectsTarget() && CheckIfPlayerIsNotLookingAtTarget())
                {
                    MakePlayerLookAtTarget();
                }

                CharacterActions[ActionToTake].actionCommand
                    .Execute(playerIdentifier, RecentTarget, CharacterActions[ActionToTake]);
                ActionToTake = 0;
            }
        }

        private bool CheckIfActionAffectsTarget()
        {
            return (CharacterActions[ActionToTake].targetType == CharacterActionTargetType.NonSelf);
        }

        private bool CheckIfPlayerIsNotLookingAtTarget()
        {
            return Vector3.Dot(Vector3.Scale(RecentTarget.transform.position - playerTransform.position, forwardRight).normalized,
                Vector3.Scale(playerAnimator.GetBoneTransform(HumanBodyBones.Head).forward, forwardRight).normalized) < 0.8f; // 내적 계산 결과 반환
        }

        private void MakePlayerLookAtTarget()
        {
            Vector3 tempVelocity = (RecentTarget.transform.position - playerTransform.position).normalized;
            tempVelocity.y = 0f;
            if (tempVelocity != Vector3.zero)
            {
                playerGoalRotationSetter.Invoke(Quaternion.LookRotation(tempVelocity));
            }
        }

        public void StopTakingAction()
        {
            if (ActionBeingTaken > 0)
                CharacterActions[ActionBeingTaken].actionCommand.Stop();

            InvisibleGlobalCoolDownTime = 0.5f;
        }

        public void RemoveActionToTake()
        {
            ActionToTake = 0;
            SetRecentTarget(null);
        }

        public void UpdateGlobalCoolDownTime()
        {
            if (VisibleGlobalCoolDownTime > 0f)
                VisibleGlobalCoolDownTime =
                    Mathf.Max(VisibleGlobalCoolDownTime - Time.deltaTime, 0f);
            if (InvisibleGlobalCoolDownTime > 0f)
                InvisibleGlobalCoolDownTime =
                    Mathf.Max(InvisibleGlobalCoolDownTime - Time.deltaTime, 0f);

            onVisibleGlobalCoolTimeUpdated.Invoke();
        }

        public void UpdateSqrDistanceFromCurrentTarget(bool isCurrentTargetDead, UnityAction onTargetToDeselect)
        {
            var value
                = SqrDistanceFromCurrentTarget
                = (CurrentTarget == null) ?
                0f : Vector3.SqrMagnitude(playerTransform.position - CurrentTarget.transform.position);

            if (value > 1600f && !(CurrentTarget == null)) // 선택 중인 대상과 떨어진 거리가 40f를 초과하면
            {
                onTargetToDeselect.Invoke();
            }

            if (CurrentTarget != null && isCurrentTargetDead)
            {
                onTargetToDeselect.Invoke();
            }

            onSqrDistanceFromCurrentTargetUpdated?.Invoke();
        }

        public void SetLookAtValues()
        {
            if (CurrentTarget != null)
            {
                playerAnimator.SetLookAtPosition(CurrentTarget.transform.position
                    + Vector3.up * CurrentTarget.transform.lossyScale.y * 0.9f);

                playerAnimator.SetLookAtWeight(1f, 0.5f, 1f, 1f, 0.7f);
            }
            else
                playerAnimator.SetLookAtWeight(0f);
        }
    }
}