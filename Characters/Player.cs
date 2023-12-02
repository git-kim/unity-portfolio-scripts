using System.Collections;
using UnityEngine;
using Characters.Handlers;
using UnityEngine.Events;

public sealed partial class Player : Character
{
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int BattlePoseOn = Animator.StringToHash("Battle Pose On");
    private static readonly int InTheAir = Animator.StringToHash("In the Air");
    private static readonly int YSpeed = Animator.StringToHash("Y-Speed");
    private static readonly int MovementSpeedMult = Animator.StringToHash("MovementSpeedMult");
    private static readonly int MovementMode = Animator.StringToHash("MovementMode");
    private static readonly int IdleTimeout = Animator.StringToHash("Idle Timeout");
    private static readonly int Idle2On = Animator.StringToHash("Idle2 On");
    private static readonly int LandMode = Animator.StringToHash("LandMode");

    public bool IsDead => statChangeHandler.HasZeroHitPoints;

    IEnumerator BlockMovementTemporarily(float timeInSeconds, UnityAction onEnd)
    {
        IsAbleToMove = false;
        yield return new WaitForSeconds(timeInSeconds);
        IsAbleToMove = true;
        onEnd?.Invoke();
    }

    IEnumerator EndGame()
    {
        Animator.SetTrigger(Dead);
        playerFeetIK.DisableFeetIK();
        StopTakingAction();
        RemoveActionToTake();
        playerIStatChangeDisplay.RemoveAllDisplayingBuffs();
        DeselectTarget();
        yield return new WaitForSeconds(0.5f); // 리마인더: 시간은 변경하여야 할 수도 있다.

        gameManagerInstance.State = GameState.Over;
    }

    protected override void FixedUpdate()
    {
        if (gameManagerInstance.State == GameState.Over || statChangeHandler.HasZeroHitPoints)
            return;

        UpdateGlobalCoolDownTime();

        // 오디오 리스너 회전 갱신(카메라가 보는 방향을 보도록)
        audioListenerTransform.rotation = Quaternion.LookRotation(audioListenerTransform.position - mainCameraTransform.position);

        UpdateSqrDistanceFromCurrentTarget();

        if (!gameManagerInstance.IsInBattle && Animator.GetBool(BattlePoseOn) && timeToTurnOffBattleMode > 0f)
        {
            timeToTurnOffBattleMode -= Time.deltaTime;
            if (timeToTurnOffBattleMode < 0f)
            {
                timeToTurnOffBattleMode = 3f;
                Animator.SetBool(BattlePoseOn, false);
            }

        }
    }

    private void UpdateSqrDistanceFromCurrentTarget()
    {
        var value
            = characterActionHandler.SqrDistanceFromCurrentTarget
            = (CurrentTarget == null) ?
            0f : Vector3.SqrMagnitude(playerTransform.position - CurrentTarget.transform.position);

        if (value > 1600f && !(CurrentTarget == null)) // 선택 중인 대상과 떨어진 거리가 40f를 초과하면
        {
            DeselectTarget();
        }

        if (CurrentTarget != null && currentTargetStatChangeHandler.HasZeroHitPoints)
        {
            DeselectTarget();
        }

        onUpdateSqrDistanceFromCurrentTarget.Invoke();
    }

    protected override void Update()
    {
        if (gameManagerInstance.State != GameState.Over && !statChangeHandler.HasZeroHitPoints)
            ChangePoseIfApplicable();

        Locomote();

        if (gameManagerInstance.State != GameState.Over && !statChangeHandler.HasZeroHitPoints)
            Act();
    }

    /// <summary>
    /// 플레이어의 키 입력에 맞추어 플레이어 캐릭터가 액션을 취하게 한다.
    /// </summary>
    private void Act()
    {
        if (recentActionInput > 0)
        {
            if (VisibleGlobalCoolDownTime < 1f
                || (!IsCasting && CharacterActions[recentActionInput].canIgnoreVisibleGlobalCoolDownTime))
            {
                characterActionHandler.ActionToTake = recentActionInput; // 액션 예약
                RecentTarget = CurrentTarget; // 현재 선택 대상 저장
            }
        }

        recentActionInput = 0; // 주의: 이 줄이 있어야 오동작하지 않는다.

        if (IsMoving)
        {
            if (IsCasting && VisibleGlobalCoolDownTime > 0.5f)
            {
                StopTakingAction(); // 캐스팅 액션 중단
                RemoveActionToTake(); // 액션 예약 취소
            }
            else if (CharacterActions[characterActionHandler.ActionToTake].castTime > 0f)
                RemoveActionToTake(); // 캐스팅 액션 예약 취소
        }

        if (characterActionHandler.ActionToTake != 0
            && characterActionHandler.ActionBeingTaken == 0
            && characterActionHandler.InvisibleGlobalCoolDownTime == 0f
            && (VisibleGlobalCoolDownTime == 0f
            || CharacterActions[characterActionHandler.ActionToTake].canIgnoreVisibleGlobalCoolDownTime))
        {
            if (!(RecentTarget == null) && CheckIfActionAffectsTarget() && CheckIfPlayerIsNotLookingAtTarget())
            {
                MakePlayerLookAtTarget();
            }

            CharacterActions[characterActionHandler.ActionToTake].actionCommand
                .Execute(Identifier, RecentTarget, CharacterActions[characterActionHandler.ActionToTake]);
            characterActionHandler.ActionToTake = 0;
        }
    }

    /// <summary>
    /// 플레이어 캐릭터가 해당 액션을 선택 대상에게 취할지를 검사한다.
    /// </summary>
    /// <returns></returns>
    private bool CheckIfActionAffectsTarget()
    {
        return (CharacterActions[characterActionHandler.ActionToTake].targetType == CharacterActionTargetType.NonSelf);
    }

    /// <summary>
    /// 플레이어 캐릭터(머리 기준)가 현재 선택 대상을 보고 있지 않은지 검사한다.
    /// </summary>
    /// <returns></returns>
    private bool CheckIfPlayerIsNotLookingAtTarget()
    {
        return Vector3.Dot(Vector3.Scale(RecentTarget.transform.position - playerTransform.position, forwardRight).normalized,
            Vector3.Scale(Animator.GetBoneTransform(HumanBodyBones.Head).forward, forwardRight).normalized) < 0.8f; // 내적 계산 결과 반환
    }

    /// <summary>
    /// 플레이어 캐릭터(하단 중앙 기준)가 현재 선택 대상을 보도록 몸 전체를 회전한다.
    /// </summary>
    private void MakePlayerLookAtTarget()
    {
        Vector3 tempVelocity = (RecentTarget.transform.position - playerTransform.position).normalized;
        tempVelocity.y = 0f;
        if (tempVelocity != Vector3.zero) // 예외 처리
        {
            TargetRotation = Quaternion.LookRotation(tempVelocity); // 참고: 실제 회전 처리는 Rotate 함수에서 한다.
        }
    }

    /// <summary>
    /// 플레이어가 선택한 대상이 있으면 플레이어 캐릭터가 그 대상을 보게 한다.(몸 전체를 회전하지는 않는다.)
    /// </summary>
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0)
            return;

        if (CurrentTarget != null)
        {
            Animator.SetLookAtPosition(CurrentTarget.transform.position
                + Vector3.up * CurrentTarget.transform.lossyScale.y * 0.9f);

            Animator.SetLookAtWeight(1f, 0.5f, 1f, 1f, 0.7f);
        }
        else
            Animator.SetLookAtWeight(0f);
    }

    /// <summary>
    /// 플레이어 캐릭터 위치를 변경한다. 착지, 도약, 낙하가 포함된다.
    /// </summary>
    private void Locomote()
    {
        if (!IsDead)
            SetVelocityForDesiredDirection();

        bool hasBeenInTheAir = Animator.GetBool(InTheAir);

        CheckGround(hasBeenInTheAir, out IsNotInTheAir);

        if (hasBeenInTheAir)
        {
            if (IsNotInTheAir && !IsDead)
                onLand.Invoke(); // 착지 시작
        }
        else
        {
            if (IsNotInTheAir)
            {
                if (keyManagerInstance.Jump && IsAbleToMove && !IsDead)
                {
                    onJumpUp.Invoke(); // 도약 시작
                }
                else
                {
                    // y 축 방향 속도 제한
                    if (Animator.GetFloat(YSpeed) < -0.4f)
                        Animator.SetFloat(YSpeed, -0.4f);

                    CalculateLocomotionSpeed();
                }
            }
            else if  (!IsDead)
            {
                if (Animator.GetFloat(YSpeed) <= -2f)
                    onFall.Invoke(); // 낙하 시작
            }
        }

        Animator.SetFloat(YSpeed, Animator.GetFloat(YSpeed) + NegativeGravity * Time.deltaTime); // 중력 적용
        Velocity.y = Animator.GetFloat(YSpeed);

        base.Rotate(); // 상위 클래스 함수 호출(캐릭터 회전)

        Velocity = Vector3.Scale(Velocity, DragFactor); // 항력 적용

        base.Move(); // 상위 클래스 함수 호출(캐릭터 이동, isMoving 값 변경)
    }

    /// <summary>
    /// 이동 속도를 계산한다. 이동 속도 버프('잰 발놀림' 효과)가 적용된다.
    /// </summary>
    void CalculateLocomotionSpeed()
    {
        if (Velocity.magnitude > 0.001f)
        {
            // KeyManagerInstance.MovementMode:
            // 보통 속도로 걷기(= 평보, 0b010)
            // 보통 속도로 달리기(0b100)
            // 빨리 걷기(= 속보, 0b011)
            // 빨리 달리기(= 질주, 0b101)
            movementMode = keyManagerInstance.MovementMode;

            if (sprint.IsBuffOn) movementMode |= 0b001; // '잰 발놀림' 효과 적용

            switch (movementMode)
            {
                case 0b010:
                    Velocity *= 0.5f; // 또는 velocity = Vector3.Scale(velocity, new Vector3(0.5f, 0.5f, 0.5f));
                    Animator.SetFloat(MovementSpeedMult, 0.5f);
                    break;
                case 0b100:
                    Animator.SetFloat(MovementSpeedMult, 1.0f);
                    break;
                case 0b011:
                    Velocity *= 0.7f;
                    Animator.SetFloat(MovementSpeedMult, 0.7f);
                    break;
                case 0b101:
                    Velocity *= 1.4f;
                    Animator.SetFloat(MovementSpeedMult, 1.4f);
                    break;
                default:
                    Debug.LogError("이동 속도 계산 중 오류가 발생하였습니다.");
                    break;
            }

            Animator.SetInteger(MovementMode, movementMode & 0b110);
        }
        else
        {
            Animator.SetInteger(MovementMode, 0);
            Animator.SetFloat(MovementSpeedMult, 1f);
        }
    }

    /// <summary>
    /// 액션 재사용 대기 시간 관련 처리를 한다.
    /// </summary>
    private void UpdateGlobalCoolDownTime()
    {
        if (VisibleGlobalCoolDownTime > 0f)
            characterActionHandler.VisibleGlobalCoolDownTime =
                Mathf.Max(VisibleGlobalCoolDownTime - Time.deltaTime, 0f);
        if (characterActionHandler.InvisibleGlobalCoolDownTime > 0f)
            characterActionHandler.InvisibleGlobalCoolDownTime =
                Mathf.Max(characterActionHandler.InvisibleGlobalCoolDownTime - Time.deltaTime, 0f);

        onUpdateVisibleGlobalCoolTime.Invoke();
    }

    /// <summary>
    /// 특정 조건을 충족하면 자세를 바꾼다.(즉 재생 대상 애니메이션을 변경한다.)
    /// </summary>
    private void ChangePoseIfApplicable()
    {
        #region 자세 변경 1(Idle 상태)
        animatorStateHash = Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (animatorStateHash == idle1Hash || animatorStateHash == idle2Hash)
        {
            idleTimeout = Animator.GetFloat(IdleTimeout) - Time.deltaTime;
            if (idleTimeout < 0f)
            {
                idleTimeout = Random.Range(10f, 20f);
                Animator.SetBool(Idle2On, !Animator.GetBool(Idle2On));
            }
            Animator.SetFloat(IdleTimeout, idleTimeout);
        }
        #endregion

        #region 자세 변경 2(전투 시 / 평상시)
        if (gameManagerInstance.IsInBattle)
        {
            if (!Animator.GetBool(BattlePoseOn))
                Animator.SetBool(BattlePoseOn, true);
        }
        else if (keyManagerInstance.BattlePose)
        {
            Animator.SetBool(BattlePoseOn, !Animator.GetBool(BattlePoseOn));
        }
        #endregion
    }

    /// <summary>
    /// 플레이어 캐릭터가 움직일 방향을 메인 카메라가 보는 방향을 기준으로 구한다.
    /// </summary>
    void SetVelocityForDesiredDirection()
    {
        Velocity.x = keyManagerInstance.H;
        Velocity.y = 0f;
        Velocity.z = keyManagerInstance.V;

        Velocity = mainCameraTransform.TransformDirection(Velocity); // velocity를 주 카메라 로컬 벡터로 간주하여 월드 공간 벡터로 변환한다.(벡터 크기는 변하지 않는다.)
        Velocity.y = 0f;
        Velocity.Normalize();
    }

    /// <summary>
    /// 원통형으로 플레이어 캐릭터가 공중에 있는지를 검사한다.(9번 레이어: Ground)
    /// </summary>
    void CheckGround(bool isInTheAir, out bool isNotInTheAir)
    {
        GroundCheckerPos = ControllerTransform.position;
        GroundCheckerPos.y += Controller.center.y - Controller.height * 0.5f;

        if (isInTheAir)
        {
            GroundCheckStartY = 0.05f;
            if ((Controller.collisionFlags & CollisionFlags.Below) != 0) GroundCheckStartY = Controller.stepOffset + 0.1f;
        }
        else GroundCheckStartY = Controller.stepOffset + 0.05f;

        isNotInTheAir =
            gameManagerInstance.CheckCylinder(GroundCheckerPos - new Vector3(0f, GroundCheckStartY, 0f), GroundCheckerPos + new Vector3(0f, 0.05f, 0f), Controller.radius * 0.5f, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 상자로만 검사할 때에는 Physics.CheckBox(groundChecker.position, new Vector3(0.03f, 0.05f, 0.03f), Quaternion.identity, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 우변에 (Controller.collisionFlags & CollisionFlags.Below) != 0;을 사용할 수도 있겠으나 Controller는 캡슐 형태 콜라이더이므로 땅 모서리에서 부정확하다.
    }

    /// <summary>
    /// 기술(액션) 사용을 중지한다.
    /// </summary>
    void StopTakingAction()
    {
        if (characterActionHandler.ActionBeingTaken > 0)
            CharacterActions[characterActionHandler.ActionBeingTaken].actionCommand.Stop();

        characterActionHandler.InvisibleGlobalCoolDownTime = 0.5f;
    }

    void RemoveActionToTake()
    {
        characterActionHandler.ActionToTake = 0;
        RecentTarget = null;
    }

    void Jump()
    {
        keyManagerInstance.Jump = false;
        Animator.SetFloat(YSpeed, InitialJumpUpSpeed);
        Animator.SetBool(InTheAir, true);
        Animator.SetInteger(LandMode, 0);
    }

    void Land()
    {
        if (landCoroutine != null)
        {
            StopCoroutine(landCoroutine);
            landCoroutine = null;
        }

        if (Animator.GetFloat(YSpeed) < -2.5f)
        {
            Animator.SetInteger(LandMode, 1);
            landCoroutine = StartCoroutine(BlockMovementTemporarily(0.22f, () => { playerFeetIK.EnableFeetIK(); }));

            Animator.SetFloat(YSpeed, 0f);
            Animator.SetBool(InTheAir, false);
        }
        else
        {
            Animator.SetInteger(LandMode, 0);

            Animator.SetFloat(YSpeed, 0f);
            Animator.SetBool(InTheAir, false);
            playerFeetIK.EnableFeetIK();
        }
    }

    void Fall()
    {
        Animator.SetBool(InTheAir, true);
        Animator.SetInteger(LandMode, 0);
    }

    void Die()
    {
        gameManagerInstance.PlayersAlive.Remove(Identifier);
        StopAllCoroutines();
        StartCoroutine(EndGame());
    }

    public void SelectTarget(GameObject gO)
    {
        currentTargetStatChangeHandler = gO.GetComponent<StatChangeHandler>();
        if (currentTargetStatChangeHandler.HasZeroHitPoints)
            return;

        CurrentTarget = gO;
        currentTargetISelectable = gO.GetComponent<ISelectable>();
        currentTargetISelectable.TargetIndicator.Enable();
    }

    public void DeselectTarget()
    {
        if (CurrentTarget == null)
            return;

        currentTargetISelectable.TargetIndicator.Disable();
        CurrentTarget = null;
        currentTargetISelectable = null;
        currentTargetStatChangeHandler = null;
    }
}