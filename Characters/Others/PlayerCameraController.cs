using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Runtime.InteropServices;
using Managers;
using Enums;

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

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN // user32 exists only on Windows; Web builds would throw on right-click
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out MousePosition mousePos);
#else
    private static bool SetCursorPos(int x, int y) => false;
    private static bool GetCursorPos(out MousePosition mousePos) { mousePos = default; return false; }
#endif

    void Awake()
    {
        virtualMainCamera = GetComponentInChildren<CinemachineCamera>();
        orbitalFollow = virtualMainCamera.GetComponent<CinemachineOrbitalFollow>();
    }

    void Update()
    {
        // Once the result panel is up the camera stays put, and the cursor must be free to click its buttons.
        if (GameManager.Instance.State != GameState.Running)
        {
            if (isRotating)
                StopRotating();
            return;
        }

        if (KeyManager.Instance.RMBDown)
        {
            isRotating = true;
            GetCursorPos(out mousePosition);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (KeyManager.Instance.RMBUp && isRotating)
            StopRotating();

        if (isRotating && Mouse.current != null)
        {
            var delta = Mouse.current.delta.ReadValue() * 0.1f; // the old Mouse X/Y axes used sensitivity 0.1
            var horizontal = orbitalFollow.HorizontalAxis.Value + delta.x * HorizontalSpeed;
            orbitalFollow.HorizontalAxis.Value = Mathf.Repeat(horizontal + 180f, 360f) - 180f;
            orbitalFollow.VerticalAxis.Value =
                orbitalFollow.VerticalAxis.ClampValue(orbitalFollow.VerticalAxis.Value - delta.y * VerticalSpeed);
        }

        mouseWheelValue = KeyManager.Instance.MouseWheel;

        if (Mathf.Abs(mouseWheelValue) > 0.0078125f)
        {
            virtualMainCamera.Lens.FieldOfView =
                Mathf.Clamp(virtualMainCamera.Lens.FieldOfView - mouseWheelValue * 20f, 20f, 80f); // wheel down zooms out
        }
    }

    private void StopRotating()
    {
        isRotating = false;
        SetCursorPos(mousePosition.X, mousePosition.Y);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
