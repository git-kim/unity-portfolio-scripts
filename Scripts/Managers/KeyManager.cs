using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyManager : Singleton<KeyManager>
{
    GameManager GAME;

    private KeyManager() { } // '비싱글턴(non-singleton) 생성자 사용' 방지

    const string vAxisName = "Vertical";
    const string hAxisName = "Horizontal";
    const string mouseWheelName = "Mouse ScrollWheel";
    const string jumpName = "Jump";
    // readonly string[] actionNames = { "Action1", "Action2", "Action3", "Action4" , "Ultimate Action", "Sprint" }; // 주의: 배열은 런타임에 생성되므로 const를 사용할 수 없다.
    // => 수정 사항: NGUI 버튼 스크립트에서 값을 넘기도록 변경하였다.
    const string battlePoseName = "Battle Pose";
    const string walkOrRunName = "WalkOrRun";

    // V 축 방향 키와 H 축 방향 키를 거의 동시에 눌렀다가 떼는 상황에서 플레이어 캐릭터 회전이 어색하게 보일 수 있다.
    // 그래서 별도 변수를 사용하여 두 방향 키 Up을 동시에 처리할 필요가 있다.
    float tempV, tempH;
    float delayToResetVH;

    float delayToResetJump; // 도약 예약을 단시간 동안 가능하게 하려고 선언한 변수

    public float V { get; private set; }
    public float H { get; private set; }
    public float MouseWheel { get; private set; }
    public bool LMBDown { get; private set; }
    public bool RMBDown { get; private set; }
    public bool RMBUp { get; private set; }
    public bool Jump { get; set; }
    public int Action { get; private set; } // 0: 스킬 키를 누르지 않음; 1: 1번 스킬 키를 누름; 2: 2번 스킬...
    public bool Ult { get; private set; } // 궁극 기술
    public bool BattlePose { get; private set; }
    public int MovementMode { get; set; } // 비트 플래그: 0 보통 속도로; 1 빨리(질주 버프); 2 걷기; 4 달리기
    public bool SprintBuff { get; private set; }

    void ResetValues()
    {
        V = H = MouseWheel = 0f;
        tempV = tempH = 0f;
        delayToResetVH = 0.06f;
        delayToResetJump = 0.2f;
        LMBDown = RMBDown = RMBUp = false;
        Jump = BattlePose = false;
        Action = 0;
        MovementMode = 0b100;
        SprintBuff = false;
    }

    void Awake()
    {
        ResetValues();
        GAME = GameManager.Instance;
    }

    void Update()
    {
        //if (GAME.State == GameState.Over)
        //{
        //    ResetValues();
        //    return;
        //}

        V = Input.GetAxis(vAxisName);
        H = Input.GetAxis(hAxisName);
        if (Mathf.Abs(V) > 0.001f && Mathf.Abs(H) > 0.001f)
        {
            tempV = V;
            tempH = H;
            delayToResetVH = 0.06f;
        }
        else if (Mathf.Abs(V) > 0.001f && Mathf.Abs(tempH) > 0.001f)
        {
            H = tempH;
            delayToResetVH -= Time.deltaTime;
            if (delayToResetVH < 0f)
            {
                tempH = tempV = 0f;
                delayToResetVH = 0.06f;
            }
        }
        else if (Mathf.Abs(H) > 0.001f && Mathf.Abs(tempV) > 0.001f)
        {
            V = tempV;
            delayToResetVH -= Time.deltaTime;
            if (delayToResetVH < 0f)
            {
                tempH = tempV = 0f;
                delayToResetVH = 0.06f;
            }
        }
        else
        {
            tempH = tempV = 0f;
            delayToResetVH = 0.06f;
        }

        MouseWheel = Input.GetAxis(mouseWheelName); // 화면 확대/축소 정도
        RMBDown = Input.GetMouseButtonDown(1); // 화면 회전 시작 여부
        RMBUp = Input.GetMouseButtonUp(1); // 화면 회전 중지 여부

        LMBDown = Input.GetMouseButtonDown(0);

        if (Jump)
        {
            if (delayToResetJump > 0f)
            {
                delayToResetJump -= Time.deltaTime;
            }
            else
            {
                Jump = Input.GetButtonDown(jumpName);
                delayToResetJump = 0.2f;
            }
        }
        else
        {
            Jump = Input.GetButtonDown(jumpName);
            delayToResetJump = 0.2f;
        }

        ////Action = 0;
        ////for (int i = 0; i < actionNames.Length; ++i)
        ////{
        ////    Action = Input.GetButtonDown(actionNames[i]) ? (i + 1) : Action;
        ////}

        BattlePose = Input.GetButtonDown(battlePoseName); // 플레이어 캐릭터 전투 시 / 평상시 자세 토글링용

        if (Input.GetButtonDown(walkOrRunName)) MovementMode ^= 0b110; // 걷기 / 달리기 토글링용
    }
}
