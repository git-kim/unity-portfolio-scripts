using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Runtime.InteropServices; // DllImport

public class PlayerCameraController : MonoBehaviour
{
    private GameManager gameManagerInstance;
    private KeyManager keyManagerInstance;
    readonly List<CinemachineFreeLook> cameras = new List<CinemachineFreeLook>();
    private MousePosition mousePosition; // 참고: "out"으로 사용할 변수는 초기화할 필요가 없다.
    private float mouseWheelValue;

    private struct MousePosition
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")] // Windows 전용
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")] // Windows 전용
    private static extern bool GetCursorPos(out MousePosition mousePos);

    void Awake()
    {
        gameManagerInstance = GameManager.Instance;
        keyManagerInstance = KeyManager.Instance;

        foreach (var cam in gameObject.GetComponentsInChildren<CinemachineFreeLook>())
        {
            cameras.Add(cam);
        }
    }

    void Update()
    {
        if (gameManagerInstance.State == GameState.CutscenePlaying)
            return;

        // 마우스 오른쪽 버튼 누른 채로 마우스 커서 이동 시 회전
        if (keyManagerInstance.RMBDown)
        {
            foreach (var cam in cameras)
            {
                cam.m_XAxis.m_MaxSpeed = 8f;
                cam.m_YAxis.m_MaxSpeed = 0.05f;
            }

            GetCursorPos(out mousePosition); // 현재 커서 위치 저장
            Cursor.lockState = CursorLockMode.Locked; // 커서 이동 잠금(중앙)
            Cursor.visible = false;
        }
        else if (keyManagerInstance.RMBUp)
        {
            foreach (var cam in cameras)
            {
                cam.m_XAxis.m_MaxSpeed = 0f;
                cam.m_YAxis.m_MaxSpeed = 0f;
            }

            Cursor.lockState = CursorLockMode.None; // 커서 이동 잠금 해제
            SetCursorPos(mousePosition.X, mousePosition.Y); // 본래 커서 위치로 커서 이동하기
            Cursor.visible = true;
        }

        // 마우스 휠 스크롤링 시 축소 / 확대
        mouseWheelValue = keyManagerInstance.MouseWheel;

        if (Mathf.Abs(mouseWheelValue) > 0.0078125f)
        {
            foreach (var cam in cameras)
            {
                cam.m_Lens.FieldOfView = Mathf.Clamp(cam.m_Lens.FieldOfView + mouseWheelValue * 20f, 20, 80);
            }
        }
    }
}