using System.Collections;
using UnityEngine;
using FluentBuilderPattern;

sealed public partial class Player : Character, IDamageable, IActable
{
    #region 코루틴
    IEnumerator CannotMoveDuring(float time, System.Action voidCallback)
    {
        isAbleToMove = false;
        yield return new WaitForSeconds(time);
        isAbleToMove = true;
        voidCallback?.Invoke(); // voidFunction이 null이 아니면 voidFunction을 호출한다.
    }

    IEnumerator EndGame()
    {
        anim.SetTrigger("Dead");
        playerFeetIK.DisableFeetIK();
        StopTakingAction();
        RemoveActionToTake();
        playerIStatChangeDisplay.RemoveAllDisplayingBuffs();
        DeselectTarget();
        yield return new WaitForSeconds(0.5f); // 리마인더: 시간은 변경하여야 할 수도 있다.

        GAME.State = GameState.Over;
    }
    #endregion

    override protected void FixedUpdate()
    {
        if (GAME.State == GameState.Over || IsDead)
            return;

        UpdateGlobalCoolDownTime();

        // 오디오 리스너 회전 갱신(카메라가 보는 방향을 보도록)
        audioListenerTransform.rotation = Quaternion.LookRotation(audioListenerTransform.position - mainCameraTransform.position);

        UpdateSqrDistanceFromCurrentTarget(out sqrDistanceFromCurrentTarget);

        if (!GAME.IsInBattle && anim.GetBool("Battle Pose On") && timeToTurnOffBattleMode > 0f)
        {
            timeToTurnOffBattleMode -= Time.deltaTime;
            if (timeToTurnOffBattleMode < 0f)
            {
                timeToTurnOffBattleMode = 3f;
                anim.SetBool("Battle Pose On", false);
            }

        }
    }

    void UpdateSqrDistanceFromCurrentTarget(out float sqrDistanceFromCurrentTarget)
    {
        sqrDistanceFromCurrentTarget =
            (currentTarget == null) ? 0f : Vector3.SqrMagnitude(playerTransform.position - currentTarget.transform.position);

        if (sqrDistanceFromCurrentTarget > 1600f && !(currentTarget == null)) // 선택 중인 대상과 떨어진 거리가 40f를 초과하면
        {
            DeselectTarget();
        }

        if (currentTarget != null && currentTargetIDamageable.IsDead)
        {
            DeselectTarget();
        }

        OnUpdateSqrDistanceFromCurrentTarget.Invoke();
    }

    override protected void Update()
    {

        if (GAME.State != GameState.Over && !IsDead)
            ChangePoseIfApplicable();

        Locomote();

        if (GAME.State != GameState.Over && !IsDead)
            Act();
    }

    /// <summary>
    /// 플레이어의 키 입력에 맞추어 플레이어 캐릭터가 액션을 취하게 한다.
    /// </summary>
    void Act()
    {
        if (recentActionInput > 0)
        {
            if (VisibleGlobalCoolDownTime < 1f
                || (!IsCasting && actionCommands[recentActionInput].canIgnoreVisibleGlobalCoolDownTime))
            {
                ActionToTake = recentActionInput; // 액션 예약
                recentTarget = currentTarget; // 현재 선택 대상 저장
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
            else if (actionCommands[ActionToTake].castTime > 0f)
                RemoveActionToTake(); // 캐스팅 액션 예약 취소
        }

        if (ActionToTake != 0 && ActionBeingTaken == 0 && InvisibleGlobalCoolDownTime == 0f
            && (VisibleGlobalCoolDownTime == 0f || actionCommands[ActionToTake].canIgnoreVisibleGlobalCoolDownTime))
        {
            if (!(recentTarget is null) && CheckIfActionAffectsTarget() && CheckIfPlayerIsNotLookingAtTarget())
            {
                MakePlayerLookAtTarget();
            }

            actionCommands[ActionToTake].actionCommand.Execute(id, recentTarget, actionCommands[ActionToTake]); // 액션 취하기
            ActionToTake = 0;
        }
    }

    /// <summary>
    /// 플레이어 캐릭터가 해당 액션을 선택 대상에게 취할지를 검사한다.
    /// </summary>
    /// <returns></returns>
    bool CheckIfActionAffectsTarget()
    {
        return (actionCommands[ActionToTake].targetType == ActionTargetType.NonSelf);
    }

    /// <summary>
    /// 플레이어 캐릭터(머리 기준)가 현재 선택 대상을 보고 있지 않은지 검사한다.
    /// </summary>
    /// <returns></returns>
    bool CheckIfPlayerIsNotLookingAtTarget()
    {
        return Vector3.Dot(Vector3.Scale(recentTarget.transform.position - playerTransform.position, forwardRight).normalized,
            Vector3.Scale(anim.GetBoneTransform(HumanBodyBones.Head).forward, forwardRight).normalized) < 0.8f; // 내적 계산 결과 반환
    }

    /// <summary>
    /// 플레이어 캐릭터(하단 중앙 기준)가 현재 선택 대상을 보도록 몸 전체를 회전한다.
    /// </summary>
    void MakePlayerLookAtTarget()
    {
        Vector3 tempVelocity = (recentTarget.transform.position - playerTransform.position).normalized;
        tempVelocity.y = 0f;
        if (tempVelocity != Vector3.zero) // 예외 처리
        {
            targetRotation = Quaternion.LookRotation(tempVelocity); // 참고: 실제 회전 처리는 Rotate 함수에서 한다.
        }
    }

    /// <summary>
    /// 플레이어가 선택한 대상이 있으면 플레이어 캐릭터가 그 대상을 보게 한다.(몸 전체를 회전하지는 않는다.)
    /// </summary>
    void OnAnimatorIK()
    {
        if (currentTarget != null)
        {
            anim.SetLookAtPosition(currentTarget.transform.position
                + Vector3.up * currentTarget.transform.lossyScale.y * 0.9f);

            anim.SetLookAtWeight(1f, 0.5f, 1f, 1f, 0.7f);
        }
        else
            anim.SetLookAtWeight(0f);
    }

    /// <summary>
    /// 플레이어 캐릭터 위치를 변경한다. 착지, 도약, 낙하가 포함된다.
    /// </summary>
    void Locomote()
    {
        if (!IsDead)
            GetMovementDirection();

        bool hasBeenInTheAir = anim.GetBool("In the Air");

        CheckGround(hasBeenInTheAir, out isNotInTheAir);

        if (hasBeenInTheAir)
        {
            if (isNotInTheAir && !IsDead)
                OnLand.Invoke(); // 착지 시작
        }
        else
        {
            if (isNotInTheAir)
            {
                if (KEY.Jump && isAbleToMove && !IsDead)
                {
                    OnJumpUp.Invoke(); // 도약 시작
                }
                else
                {
                    // y 축 방향 속도 제한
                    if (anim.GetFloat("Y-Speed") < -0.4f)
                        anim.SetFloat("Y-Speed", -0.4f);

                    CalculateLocomotionSpeed();
                }
            }
            else if  (!IsDead)
            {
                if (anim.GetFloat("Y-Speed") <= -2f)
                    OnFall.Invoke(); // 낙하 시작
            }
        }

        anim.SetFloat("Y-Speed", anim.GetFloat("Y-Speed") + negGravity * Time.deltaTime); // 중력 적용
        velocity.y = anim.GetFloat("Y-Speed");

        base.Rotate(); // 상위 클래스 함수 호출(캐릭터 회전)

        velocity = Vector3.Scale(velocity, dragFactor); // 항력 적용

        base.Move(); // 상위 클래스 함수 호출(캐릭터 이동, isMoving 값 변경)
    }

    /// <summary>
    /// 이동 속도를 계산한다. 이동 속도 버프('잰 발놀림' 효과)가 적용된다.
    /// </summary>
    void CalculateLocomotionSpeed()
    {
        if (velocity.magnitude > 0.001f)
        {
            // KEY.MovementMode:
            // 보통 속도로 걷기(= 평보, 0b010)
            // 보통 속도로 달리기(0b100)
            // 빨리 걷기(= 속보, 0b011)
            // 빨리 달리기(= 질주, 0b101)
            movementMode = KEY.MovementMode;

            if (sprint.IsBuffOn) movementMode |= 0b001; // '잰 발놀림' 효과 적용

            switch (movementMode)
            {
                case 0b010:
                    velocity *= 0.5f; // 또는 velocity = Vector3.Scale(velocity, new Vector3(0.5f, 0.5f, 0.5f));
                    anim.SetFloat("MovementSpeedMult", 0.5f);
                    break;
                case 0b100:
                    anim.SetFloat("MovementSpeedMult", 1.0f);
                    break;
                case 0b011:
                    velocity *= 0.7f;
                    anim.SetFloat("MovementSpeedMult", 0.7f);
                    break;
                case 0b101:
                    velocity *= 1.4f;
                    anim.SetFloat("MovementSpeedMult", 1.4f);
                    break;
                default:
                    Debug.LogError("이동 속도 계산 중 오류가 발생하였습니다.");
                    break;
            }

            anim.SetInteger("MovementMode", movementMode & 0b110);
        }
        else
        {
            anim.SetInteger("MovementMode", 0);
            anim.SetFloat("MovementSpeedMult", 1f);
        }
    }

    /// <summary>
    /// 액션 재사용 대기 시간 관련 처리를 한다.
    /// </summary>
    private void UpdateGlobalCoolDownTime()
    {
        if (VisibleGlobalCoolDownTime > 0f) VisibleGlobalCoolDownTime = Mathf.Max(VisibleGlobalCoolDownTime - Time.deltaTime, 0f);
        if (InvisibleGlobalCoolDownTime > 0f) InvisibleGlobalCoolDownTime = Mathf.Max(InvisibleGlobalCoolDownTime - Time.deltaTime, 0f);

        OnUpdateVisibleGlobalCoolTime.Invoke();
    }

    /// <summary>
    /// 특정 조건을 충족하면 자세를 바꾼다.(즉 재생 대상 애니메이션을 변경한다.)
    /// </summary>
    private void ChangePoseIfApplicable()
    {
        #region 자세 변경 1(Idle 상태)
        animatorStateHash = anim.GetCurrentAnimatorStateInfo(0).shortNameHash;
        if (animatorStateHash == idle1Hash || animatorStateHash == idle2Hash)
        {
            idleTimeout = anim.GetFloat("Idle Timeout") - Time.deltaTime;
            if (idleTimeout < 0f)
            {
                idleTimeout = Random.Range(10f, 20f);
                anim.SetBool("Idle2 On", !anim.GetBool("Idle2 On"));
            }
            anim.SetFloat("Idle Timeout", idleTimeout);
        }
        #endregion

        #region 자세 변경 2(전투 시 / 평상시)
        if (GAME.IsInBattle)
        {
            if (!anim.GetBool("Battle Pose On"))
                anim.SetBool("Battle Pose On", true);
        }
        else if (KEY.BattlePose)
        {
            anim.SetBool("Battle Pose On", !anim.GetBool("Battle Pose On"));
        }
        #endregion
    }

    /// <summary>
    /// 플레이어 캐릭터가 움직일 방향을 메인 카메라가 보는 방향을 기준으로 구한다.
    /// </summary>
    void GetMovementDirection()
    {
        velocity.x = KEY.H;
        velocity.y = 0f;
        velocity.z = KEY.V;

        velocity = mainCameraTransform.TransformDirection(velocity); // velocity를 주 카메라 로컬 벡터로 간주하여 월드 공간 벡터로 변환한다.(벡터 크기는 변하지 않는다.)
        velocity.y = 0f;
        velocity.Normalize();
    }

    /// <summary>
    /// 원통형으로 플레이어 캐릭터가 공중에 있는지를 검사한다.(9번 레이어: Ground)
    /// </summary>
    void CheckGround(bool isInTheAir, out bool isNotInTheAir)
    {
        groundCheckerPos = cCTransform.position;
        groundCheckerPos.y += cC.center.y - cC.height * 0.5f;

        if (isInTheAir)
        {
            groundCheckStartY = 0.05f;
            if ((cC.collisionFlags & CollisionFlags.Below) != 0) groundCheckStartY = cC.stepOffset + 0.1f;
        }
        else groundCheckStartY = cC.stepOffset + 0.05f;

        isNotInTheAir =
            GAME.CheckCylinder(groundCheckerPos - new Vector3(0f, groundCheckStartY, 0f), groundCheckerPos + new Vector3(0f, 0.05f, 0f), cC.radius * 0.5f, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 상자로만 검사할 때에는 Physics.CheckBox(groundChecker.position, new Vector3(0.03f, 0.05f, 0.03f), Quaternion.identity, 1 << 9, QueryTriggerInteraction.Ignore);
        // 참고: 우변에 (cC.collisionFlags & CollisionFlags.Below) != 0;을 사용할 수도 있겠으나 cC는 캡슐 형태 콜라이더이므로 땅 모서리에서 부정확하다.
    }

    /// <summary>
    /// 기술(액션) 사용을 중지한다.
    /// </summary>
    void StopTakingAction()
    {
        if (ActionBeingTaken > 0)
            actionCommands[ActionBeingTaken].actionCommand.Stop();

        InvisibleGlobalCoolDownTime = 0.5f;
    }

    void RemoveActionToTake()
    {
        ActionToTake = 0;
        recentTarget = null;
    }

    void Jump()
    {
        KEY.Jump = false;
        anim.SetFloat("Y-Speed", initialJumpUpSpeed);
        anim.SetBool("In the Air", true);
        anim.SetInteger("LandMode", 0);
    }

    void Land()
    {
        if (!(landCoroutine is null))
        {
            StopCoroutine(landCoroutine);
            landCoroutine = null;
        }

        if (anim.GetFloat("Y-Speed") < -2.5f)
        {
            anim.SetInteger("LandMode", 1);
            landCoroutine = StartCoroutine(CannotMoveDuring(0.22f, () => { playerFeetIK.EnableFeetIK(); }));

            anim.SetFloat("Y-Speed", 0f);
            anim.SetBool("In the Air", false);
        }
        else
        {
            anim.SetInteger("LandMode", 0);

            anim.SetFloat("Y-Speed", 0f);
            anim.SetBool("In the Air", false);
            playerFeetIK.EnableFeetIK();
        }
    }

    void Fall()
    {
        anim.SetBool("In the Air", true);
        anim.SetInteger("LandMode", 0);
    }

    void Die()
    {
        GAME.PlayersAlive.Remove(id);
        StopAllCoroutines();
        StartCoroutine(EndGame());
    }

    public void UpdateStatBars()
    {
        playerHPMPDisplay.UpdateHPBar(stats[Stat.hP], stats[Stat.maxHP]);
        playerHPMPDisplay.UpdateMPBar(stats[Stat.mP], stats[Stat.maxMP]);
    }

    public void IncreaseStat(Stat stat, int increment, bool shouldShowHPChangeDigits, bool additionalOption = false)
    {
        stats[stat] += increment;

        switch (stat)
        {
            case Stat.hP:
                {
                    if (stats[stat] > stats[Stat.maxHP])
                        stats[stat] = stats[Stat.maxHP];

                    if (shouldShowHPChangeDigits)
                        playerIStatChangeDisplay.ShowHPChange(increment, false, null);

                    playerHPMPDisplay.UpdateHPBar(stats[Stat.hP], stats[Stat.maxHP]);
                }
                break;
            case Stat.mP:
                {
                    if (stats[stat] > stats[Stat.maxMP])
                        stats[stat] = stats[Stat.maxMP];

                    playerHPMPDisplay.UpdateMPBar(stats[Stat.mP], stats[Stat.maxMP]);
                }
                break;
            default:
                break;
        }
    }

    public void DecreaseStat(Stat stat, int decrement, bool shouldShowHPChangeDigits, bool additionalOption = false)
    {
        stats[stat] -= decrement;

        switch (stat)
        {
            case Stat.hP:
                {
                    if (stats[stat] < 0)
                    {
                        stats[stat] = 0;
                        if (!IsDead)
                        {
                            IsDead = true;
                            stats[Stat.mP] = 0;
                            playerHPMPDisplay.UpdateMPBar(stats[Stat.mP], stats[Stat.maxMP]);
                            OnDie.Invoke();
                        }
                    }

                    if (shouldShowHPChangeDigits)
                        playerIStatChangeDisplay.ShowHPChange(decrement, true, null);

                    playerHPMPDisplay.UpdateHPBar(stats[Stat.hP], stats[Stat.maxHP]);
                }
                break;
            case Stat.mP:
                {
                    if (stats[stat] < 0)
                        stats[stat] = 0;

                    playerHPMPDisplay.UpdateMPBar(stats[Stat.mP], stats[Stat.maxMP]);
                }
                break;
            default:
                break;
        }
    }

    public void SelectTarget(GameObject gO)
    {
        currentTargetIDamageable = gO.GetComponent<IDamageable>();
        if (currentTargetIDamageable.IsDead) return;

        currentTarget = gO;
        currentTargetISelectable = gO.GetComponent<ISelectable>();
        currentTargetISelectable.TargetIndicator.Enable();
    }

    public void DeselectTarget()
    {
        if (currentTarget != null)
        {
            currentTargetISelectable.TargetIndicator.Disable();
            currentTarget = null;
            currentTargetISelectable = null;
            currentTargetIDamageable = null;
        }
    }
}