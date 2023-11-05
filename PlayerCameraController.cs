using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Runtime.InteropServices; // DllImport

public class PlayerCameraController : MonoBehaviour
{
    GameManager GAME;
    KeyManager KEY;
    readonly List<CinemachineFreeLook> cameras = new List<CinemachineFreeLook>();
    MousePosition mP; // 참고: "out"으로 사용할 변수는 초기화할 필요가 없다.
    float mouseWheelValue;

    struct MousePosition
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll")] // Windows 전용
    public static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")] // Windows 전용
    private static extern bool GetCursorPos(out MousePosition mousePos);

    void Awake()
    {
        GAME = GameManager.Instance;
        KEY = KeyManager.Instance;

        foreach (CinemachineFreeLook cam in gameObject.GetComponentsInChildren<CinemachineFreeLook>())
        {
            cameras.Add(cam);
        }
    }

    void Update()
    {
        if (GAME.State == GameState.CutscenePlaying)
            return;

        // 마우스 오른쪽 버튼 누른 채로 마우스 커서 이동 시 회전
        if (KEY.RMBDown)
        {
            foreach (CinemachineFreeLook cam in cameras)
            {
                cam.m_XAxis.m_MaxSpeed = 8f;
                cam.m_YAxis.m_MaxSpeed = 0.05f;
            }

            GetCursorPos(out mP); // 현재 커서 위치 저장
            Cursor.lockState = CursorLockMode.Locked; // 커서 이동 잠금(중앙)
            Cursor.visible = false;
        }
        else if (KEY.RMBUp)
        {
            foreach (CinemachineFreeLook cam in cameras)
            {
                cam.m_XAxis.m_MaxSpeed = 0f;
                cam.m_YAxis.m_MaxSpeed = 0f;
            }

            Cursor.lockState = CursorLockMode.None; // 커서 이동 잠금 해제
            SetCursorPos(mP.x, mP.y); // 본래 커서 위치로 커서 이동하기
            Cursor.visible = true;
        }

        // 마우스 휠 스크롤링 시 축소 / 확대
        mouseWheelValue = KEY.MouseWheel;

        if (Mathf.Abs(mouseWheelValue) > 0.0078125f)
        {
            foreach (CinemachineFreeLook cam in cameras)
            {
                cam.m_Lens.FieldOfView = Mathf.Clamp(cam.m_Lens.FieldOfView + mouseWheelValue * 20f, 20, 80);
            }
        }
    }
}

// 참고: 여러 카메라를 이용할 상황을 대비하여 foreach를 사용하였다.