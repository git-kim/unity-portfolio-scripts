using UnityEngine;
using Cinemachine;
using System.Runtime.InteropServices;
using Managers;

public class PlayerCameraController : MonoBehaviour
{
    private CinemachineFreeLook virtualMainCamera;
    private MousePosition mousePosition;
    private float mouseWheelValue;

    private struct MousePosition
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")] // Windows
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")] // Windows
    private static extern bool GetCursorPos(out MousePosition mousePos);

    void Awake()
    {
        virtualMainCamera = GetComponentInChildren<CinemachineFreeLook>();
    }

    void Update()
    {
        if (KeyManager.Instance.RMBDown)
        {
            virtualMainCamera.m_XAxis.m_MaxSpeed = 8f;
            virtualMainCamera.m_YAxis.m_MaxSpeed = 0.05f;

            GetCursorPos(out mousePosition);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (KeyManager.Instance.RMBUp)
        {
            virtualMainCamera.m_XAxis.m_MaxSpeed = 0f;
            virtualMainCamera.m_YAxis.m_MaxSpeed = 0f;

            SetCursorPos(mousePosition.X, mousePosition.Y);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        mouseWheelValue = KeyManager.Instance.MouseWheel;

        if (Mathf.Abs(mouseWheelValue) > 0.0078125f)
        {
            virtualMainCamera.m_Lens.FieldOfView =
                Mathf.Clamp(virtualMainCamera.m_Lens.FieldOfView +
                mouseWheelValue * 20f, 20f, 80f);
        }
    }
}