using UnityEngine;
using Unity.Cinemachine;
using System.Runtime.InteropServices;
using Managers;

public class PlayerCameraController : MonoBehaviour
{
    // Orbit speed while the right mouse button is held (same values the old FreeLook axes used).
    private const float HorizontalSpeed = 8f;
    private const float VerticalSpeed = 0.05f;

    private CinemachineCamera virtualMainCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private MousePosition mousePosition;
    private float mouseWheelValue;
    private bool isRotating;

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
        virtualMainCamera = GetComponentInChildren<CinemachineCamera>();
        orbitalFollow = virtualMainCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    void Update()
    {
        if (KeyManager.Instance.RMBDown)
        {
            isRotating = true;
            GetCursorPos(out mousePosition);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (KeyManager.Instance.RMBUp)
        {
            isRotating = false;
            SetCursorPos(mousePosition.X, mousePosition.Y);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isRotating)
        {
            var horizontal = orbitalFollow.HorizontalAxis.Value + Input.GetAxis("Mouse X") * HorizontalSpeed;
            orbitalFollow.HorizontalAxis.Value = Mathf.Repeat(horizontal + 180f, 360f) - 180f;
            orbitalFollow.VerticalAxis.Value =
                Mathf.Clamp01(orbitalFollow.VerticalAxis.Value - Input.GetAxis("Mouse Y") * VerticalSpeed);
        }

        mouseWheelValue = KeyManager.Instance.MouseWheel;

        if (Mathf.Abs(mouseWheelValue) > 0.0078125f)
        {
            virtualMainCamera.Lens.FieldOfView =
                Mathf.Clamp(virtualMainCamera.Lens.FieldOfView + mouseWheelValue * 20f, 20f, 80f);
        }
    }
}
